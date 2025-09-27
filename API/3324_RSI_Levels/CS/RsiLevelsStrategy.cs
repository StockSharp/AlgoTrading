namespace StockSharp.Samples.Strategies;

using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

using StockSharp.Algo;

/// <summary>
/// Port of the MetaTrader 5 "RSI Levels" expert advisor.
/// Opens long positions when RSI enters the oversold zone and shorts when it enters the overbought zone.
/// Applies fixed stop-loss and take-profit levels together with risk-based position sizing.
/// </summary>
public class RsiLevelsStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _overboughtLevel;
	private readonly StrategyParam<decimal> _oversoldLevel;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;

	private RelativeStrengthIndex _rsi = null!;
	private decimal? _previousRsi;

	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal? _longStopPrice;
	private decimal? _shortStopPrice;
	private decimal? _longTakePrice;
	private decimal? _shortTakePrice;

	private decimal _priceStep;
	private decimal _stepPrice;

	/// <summary>
	/// Initializes a new instance of <see cref="RsiLevelsStrategy"/>.
	/// </summary>
	public RsiLevelsStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe used for RSI calculations", "General");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("RSI Period", "Number of periods used for RSI", "Indicators");

		_overboughtLevel = Param(nameof(OverboughtLevel), 70m)
		.SetRange(0m, 100m)
		.SetDisplay("Overbought", "Upper RSI threshold", "Signals");

		_oversoldLevel = Param(nameof(OversoldLevel), 30m)
		.SetRange(0m, 100m)
		.SetDisplay("Oversold", "Lower RSI threshold", "Signals");

		_riskPercent = Param(nameof(RiskPercent), 2m)
		.SetRange(0.1m, 50m)
		.SetDisplay("Risk %", "Risk per trade expressed as equity percentage", "Risk");

		_stopLossPoints = Param(nameof(StopLossPoints), 500)
		.SetNotNegative()
		.SetDisplay("Stop Loss (points)", "Protective stop size in symbol points", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 1000)
		.SetNotNegative()
		.SetDisplay("Take Profit (points)", "Target size in symbol points", "Risk");
	}

	/// <summary>
	/// Working candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// RSI calculation period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// RSI overbought threshold.
	/// </summary>
	public decimal OverboughtLevel
	{
		get => _overboughtLevel.Value;
		set => _overboughtLevel.Value = value;
	}

	/// <summary>
	/// RSI oversold threshold.
	/// </summary>
	public decimal OversoldLevel
	{
		get => _oversoldLevel.Value;
		set => _oversoldLevel.Value = value;
	}

	/// <summary>
	/// Risk per trade in percent of portfolio value.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in symbol points.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance in symbol points.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security == null)
		yield break;

		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_rsi = null!;
		_previousRsi = null;
		_longEntryPrice = null;
		_shortEntryPrice = null;
		_longStopPrice = null;
		_shortStopPrice = null;
		_longTakePrice = null;
		_shortTakePrice = null;
		_priceStep = 0m;
		_stepPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_priceStep = Security?.PriceStep ?? 0m;
		_stepPrice = Security?.StepPrice ?? 0m;

		if (_priceStep <= 0m)
		LogWarning("PriceStep is not configured. Stop-loss and take-profit distances will use raw point values.");

		if (_stepPrice <= 0m)
		LogWarning("StepPrice is not configured. Risk-based position sizing may be inaccurate.");

		_rsi = new RelativeStrengthIndex
		{
			Length = RsiPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_rsi, ProcessCandle)
		.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (_previousRsi is null)
		{
			_previousRsi = rsiValue;
			return;
		}

		var previous = _previousRsi.Value;
		var oversoldNow = rsiValue < OversoldLevel;
		var oversoldPrev = previous < OversoldLevel;
		var overboughtNow = rsiValue >= OverboughtLevel;
		var overboughtPrev = previous >= OverboughtLevel;

		if (oversoldNow && !oversoldPrev)
		HandleBuySignal(candle);

		if (overboughtNow && !overboughtPrev)
		HandleSellSignal(candle);

		UpdateRiskManagement(candle);

		_previousRsi = rsiValue;
	}

	private void HandleBuySignal(ICandleMessage candle)
	{
		if (Position < 0m)
		{
			CloseShortPositions("RSI entered oversold zone");
			return;
		}

		if (Position != 0m)
		return;

		var volume = CalculateOrderVolume();
		if (volume <= 0m)
		{
			LogWarning("Calculated volume is not positive. Buy signal skipped.");
			return;
		}

		BuyMarket(volume);

		_longEntryPrice = candle.ClosePrice;
		_longStopPrice = CalculateLongStop(candle.ClosePrice);
		_longTakePrice = CalculateLongTake(candle.ClosePrice);
		_shortEntryPrice = null;
		_shortStopPrice = null;
		_shortTakePrice = null;

		LogInfo($"Opened long at {candle.ClosePrice} with volume {volume}.");
	}

	private void HandleSellSignal(ICandleMessage candle)
	{
		if (Position > 0m)
		{
			CloseLongPositions("RSI entered overbought zone");
			return;
		}

		if (Position != 0m)
		return;

		var volume = CalculateOrderVolume();
		if (volume <= 0m)
		{
			LogWarning("Calculated volume is not positive. Sell signal skipped.");
			return;
		}

		SellMarket(volume);

		_shortEntryPrice = candle.ClosePrice;
		_shortStopPrice = CalculateShortStop(candle.ClosePrice);
		_shortTakePrice = CalculateShortTake(candle.ClosePrice);
		_longEntryPrice = null;
		_longStopPrice = null;
		_longTakePrice = null;

		LogInfo($"Opened short at {candle.ClosePrice} with volume {volume}.");
	}

	private decimal CalculateOrderVolume()
	{
		var stopPoints = StopLossPoints;
		var riskPercent = RiskPercent;
		var security = Security;

		if (riskPercent <= 0m)
		{
			LogWarning("Risk percent is not configured.");
			return GetMinimumVolume();
		}

		if (stopPoints <= 0)
		{
			LogWarning("Stop-loss points are zero. Using minimum volume.");
			return GetMinimumVolume();
		}

		var capital = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;
		if (capital <= 0m)
		{
			LogWarning("Portfolio value is not available. Using minimum volume.");
			return GetMinimumVolume();
		}

		var stepPrice = _stepPrice;
		if (stepPrice <= 0m)
		{
			LogWarning("StepPrice is zero. Unable to compute risk sizing.");
			return GetMinimumVolume();
		}

		var riskAmount = capital * riskPercent / 100m;
		var rawVolume = riskAmount / (stopPoints * stepPrice);

		if (security?.VolumeStep is decimal step && step > 0m)
		rawVolume = Math.Floor(rawVolume / step) * step;

		if (security?.MinVolume is decimal min && rawVolume < min)
		rawVolume = min;

		if (security?.MaxVolume is decimal max && max > 0m && rawVolume > max)
		rawVolume = max;

		if (rawVolume <= 0m)
		rawVolume = GetMinimumVolume();

		return rawVolume;
	}

	private decimal GetMinimumVolume()
	{
		var security = Security;
		if (security?.MinVolume is decimal min && min > 0m)
		return min;

		if (security?.VolumeStep is decimal step && step > 0m)
		return step;

		return 1m;
	}

	private decimal? CalculateLongStop(decimal entryPrice)
	{
		if (StopLossPoints <= 0)
		return null;

		var step = _priceStep > 0m ? _priceStep : 1m;
		return entryPrice - StopLossPoints * step;
	}

	private decimal? CalculateLongTake(decimal entryPrice)
	{
		if (TakeProfitPoints <= 0)
		return null;

		var step = _priceStep > 0m ? _priceStep : 1m;
		return entryPrice + TakeProfitPoints * step;
	}

	private decimal? CalculateShortStop(decimal entryPrice)
	{
		if (StopLossPoints <= 0)
		return null;

		var step = _priceStep > 0m ? _priceStep : 1m;
		return entryPrice + StopLossPoints * step;
	}

	private decimal? CalculateShortTake(decimal entryPrice)
	{
		if (TakeProfitPoints <= 0)
		return null;

		var step = _priceStep > 0m ? _priceStep : 1m;
		return entryPrice - TakeProfitPoints * step;
	}

	private void UpdateRiskManagement(ICandleMessage candle)
	{
		if (Position > 0m)
		{
			if (_longStopPrice is decimal stop && candle.LowPrice <= stop)
			{
				LogInfo($"Long stop hit at {stop}.");
				CloseLongPositions("Stop-loss reached");
				return;
			}

			if (_longTakePrice is decimal take && candle.HighPrice >= take)
			{
				LogInfo($"Long take-profit hit at {take}.");
				CloseLongPositions("Take-profit reached");
			}
		}
		else if (Position < 0m)
		{
			if (_shortStopPrice is decimal stop && candle.HighPrice >= stop)
			{
				LogInfo($"Short stop hit at {stop}.");
				CloseShortPositions("Stop-loss reached");
				return;
			}

			if (_shortTakePrice is decimal take && candle.LowPrice <= take)
			{
				LogInfo($"Short take-profit hit at {take}.");
				CloseShortPositions("Take-profit reached");
			}
		}
	}

	private void CloseLongPositions(string reason)
	{
		var volume = Position;
		if (volume <= 0m)
		return;

		LogInfo($"Closing long position because {reason}.");
		SellMarket(volume);
		ResetLongState();
	}

	private void CloseShortPositions(string reason)
	{
		var volume = -Position;
		if (volume <= 0m)
		return;

		LogInfo($"Closing short position because {reason}.");
		BuyMarket(volume);
		ResetShortState();
	}

	private void ResetLongState()
	{
		_longEntryPrice = null;
		_longStopPrice = null;
		_longTakePrice = null;
	}

	private void ResetShortState()
	{
		_shortEntryPrice = null;
		_shortStopPrice = null;
		_shortTakePrice = null;
	}
}

