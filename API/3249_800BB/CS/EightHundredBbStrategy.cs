using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Reimplementation of the MetaTrader "800BB" expert advisor that trades Bollinger Band re-entries.
/// Positions are sized via ATR-based stop distance and a configurable portfolio risk percentage.
/// </summary>
public class EightHundredBbStrategy : Strategy
{
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<decimal> _takeProfitAtrMultiplier;
	private readonly StrategyParam<decimal> _stopLossAtrMultiplier;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<DataType> _candleType;

	private BollingerBands? _bollinger;
	private AverageTrueRange? _atr;

	private decimal? _previousUpperBand;
	private decimal? _previousLowerBand;
	private decimal? _previousOpen;
	private PriceStatus _previousStatus = PriceStatus.Nothing;

	private decimal? _longStopPrice;
	private decimal? _longTakePrice;
	private decimal? _shortStopPrice;
	private decimal? _shortTakePrice;

	private decimal _pipSize;
	private decimal _pipValueMoney;

	/// <summary>
	/// Percentage of account equity risked per trade.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed as an ATR multiple.
	/// </summary>
	public decimal TakeProfitAtrMultiplier
	{
		get => _takeProfitAtrMultiplier.Value;
		set => _takeProfitAtrMultiplier.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed as an ATR multiple.
	/// </summary>
	public decimal StopLossAtrMultiplier
	{
		get => _stopLossAtrMultiplier.Value;
		set => _stopLossAtrMultiplier.Value = value;
	}

	/// <summary>
	/// ATR lookback period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Bollinger Band moving average period.
	/// </summary>
	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	/// <summary>
	/// Bollinger Band standard deviation multiplier.
	/// </summary>
	public decimal BollingerDeviation
	{
		get => _bollingerDeviation.Value;
		set => _bollingerDeviation.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="EightHundredBbStrategy"/> class.
	/// </summary>
	public EightHundredBbStrategy()
	{
		_riskPercent = Param(nameof(RiskPercent), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Risk %", "Percentage of equity risked on each trade", "Risk");

		_takeProfitAtrMultiplier = Param(nameof(TakeProfitAtrMultiplier), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("TP ATR Multiplier", "Take-profit distance in ATR multiples", "Risk");

		_stopLossAtrMultiplier = Param(nameof(StopLossAtrMultiplier), 1m)
			.SetGreaterThanZero()
			.SetDisplay("SL ATR Multiplier", "Stop-loss distance in ATR multiples", "Risk");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR lookback period", "Indicators");

		_bollingerPeriod = Param(nameof(BollingerPeriod), 800)
			.SetGreaterThanZero()
			.SetDisplay("BB Period", "Bollinger Band moving average period", "Indicators");

		_bollingerDeviation = Param(nameof(BollingerDeviation), 2m)
			.SetGreaterThanZero()
			.SetDisplay("BB Deviation", "Bollinger Band standard deviation multiplier", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe for signals", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_previousUpperBand = null;
		_previousLowerBand = null;
		_previousOpen = null;
		_previousStatus = PriceStatus.Nothing;

		_longStopPrice = null;
		_longTakePrice = null;
		_shortStopPrice = null;
		_shortTakePrice = null;

		_pipSize = 0m;
		_pipValueMoney = 0m;
		_bollinger = null;
		_atr = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		InitializePipMetrics();

		_bollinger = new BollingerBands
		{
			Length = BollingerPeriod,
			Width = BollingerDeviation
		};

		_atr = new AverageTrueRange
		{
			Length = AtrPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_bollinger, _atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _bollinger);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal middleBand, decimal upperBand, decimal lowerBand, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		HandleProtectiveExits(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			UpdatePreviousValues(candle, upperBand, lowerBand);
			return;
		}

		var prevUpper = _previousUpperBand;
		var prevLower = _previousLowerBand;
		var prevOpen = _previousOpen;
		var prevStatus = _previousStatus;

		if (prevUpper.HasValue && prevLower.HasValue && prevOpen.HasValue && atrValue > 0m)
		{
			TryEnterLong(candle, prevLower.Value, prevOpen.Value, prevStatus, atrValue);
			TryEnterShort(candle, prevUpper.Value, prevOpen.Value, prevStatus, atrValue);
		}

		UpdatePreviousValues(candle, upperBand, lowerBand);
	}

	private void TryEnterLong(ICandleMessage candle, decimal prevLowerBand, decimal prevOpen, PriceStatus prevStatus, decimal atrValue)
	{
		if (candle.OpenPrice < prevLowerBand)
			return;

		var crossedBelow = prevOpen <= prevLowerBand || prevStatus == PriceStatus.CrossedBelowLower;
		if (!crossedBelow)
			return;

		if (Position > 0m)
			return;

		var volume = CalculateOrderVolume(atrValue);
		if (volume <= 0m)
			return;

		if (Position < 0m)
			ClosePosition();

		var entryPrice = candle.OpenPrice;
		var stopDistance = atrValue * StopLossAtrMultiplier;
		var takeDistance = atrValue * TakeProfitAtrMultiplier;

		BuyMarket(volume);

		_longStopPrice = stopDistance > 0m ? entryPrice - stopDistance : null;
		_longTakePrice = takeDistance > 0m ? entryPrice + takeDistance : null;
		_shortStopPrice = null;
		_shortTakePrice = null;
	}

	private void TryEnterShort(ICandleMessage candle, decimal prevUpperBand, decimal prevOpen, PriceStatus prevStatus, decimal atrValue)
	{
		if (candle.OpenPrice > prevUpperBand)
			return;

		var crossedAbove = prevOpen >= prevUpperBand || prevStatus == PriceStatus.CrossedAboveUpper;
		if (!crossedAbove)
			return;

		if (Position < 0m)
			return;

		var volume = CalculateOrderVolume(atrValue);
		if (volume <= 0m)
			return;

		if (Position > 0m)
			ClosePosition();

		var entryPrice = candle.OpenPrice;
		var stopDistance = atrValue * StopLossAtrMultiplier;
		var takeDistance = atrValue * TakeProfitAtrMultiplier;

		SellMarket(volume);

		_shortStopPrice = stopDistance > 0m ? entryPrice + stopDistance : null;
		_shortTakePrice = takeDistance > 0m ? entryPrice - takeDistance : null;
		_longStopPrice = null;
		_longTakePrice = null;
	}

	private void HandleProtectiveExits(ICandleMessage candle)
	{
		if (Position > 0m)
		{
			var stopHit = _longStopPrice.HasValue && candle.LowPrice <= _longStopPrice.Value;
			var takeHit = _longTakePrice.HasValue && candle.HighPrice >= _longTakePrice.Value;

			if (stopHit || takeHit)
			{
				SellMarket(Position);
				_longStopPrice = null;
				_longTakePrice = null;
			}
		}
		else if (Position < 0m)
		{
			var stopHit = _shortStopPrice.HasValue && candle.HighPrice >= _shortStopPrice.Value;
			var takeHit = _shortTakePrice.HasValue && candle.LowPrice <= _shortTakePrice.Value;

			if (stopHit || takeHit)
			{
				BuyMarket(-Position);
				_shortStopPrice = null;
				_shortTakePrice = null;
			}
		}
	}

	private void UpdatePreviousValues(ICandleMessage candle, decimal upperBand, decimal lowerBand)
	{
		_previousUpperBand = upperBand;
		_previousLowerBand = lowerBand;
		_previousOpen = candle.OpenPrice;

		if (candle.OpenPrice > upperBand || candle.ClosePrice > upperBand)
		{
			_previousStatus = PriceStatus.CrossedAboveUpper;
		}
		else if (candle.OpenPrice < lowerBand || candle.ClosePrice < lowerBand)
		{
			_previousStatus = PriceStatus.CrossedBelowLower;
		}
		else
		{
			_previousStatus = PriceStatus.Nothing;
		}
	}

	private decimal CalculateOrderVolume(decimal atrValue)
	{
		if (atrValue <= 0m || StopLossAtrMultiplier <= 0m)
			return 0m;

		var security = Security;
		if (security == null)
			return 0m;

		var portfolio = Portfolio;
		var equity = portfolio?.CurrentValue ?? portfolio?.BeginValue ?? 0m;
		if (equity <= 0m)
			return 0m;

		var stopDistance = atrValue * StopLossAtrMultiplier;
		if (stopDistance <= 0m || _pipSize <= 0m || _pipValueMoney <= 0m)
			return 0m;

		var pipsToStop = stopDistance / _pipSize;
		if (pipsToStop <= 0m)
			return 0m;

		var riskAmount = equity * RiskPercent / 100m;
		if (riskAmount <= 0m)
			return 0m;

		var riskPerLot = pipsToStop * _pipValueMoney;
		if (riskPerLot <= 0m)
			return 0m;

		var rawVolume = riskAmount / riskPerLot;
		if (rawVolume <= 0m)
			return 0m;

		return AdjustVolume(rawVolume);
	}

	private decimal AdjustVolume(decimal volume)
	{
		var security = Security;
		if (security == null)
			return volume;

		var step = security.VolumeStep;
		if (step.HasValue && step.Value > 0m)
		{
			var steps = Math.Max(1m, Math.Round(volume / step.Value, MidpointRounding.AwayFromZero));
			volume = steps * step.Value;
		}

		var minVolume = security.MinVolume;
		if (minVolume.HasValue && minVolume.Value > 0m && volume < minVolume.Value)
			volume = minVolume.Value;

		var maxVolume = security.MaxVolume;
		if (maxVolume.HasValue && maxVolume.Value > 0m && volume > maxVolume.Value)
			volume = maxVolume.Value;

		return volume;
	}

	private void InitializePipMetrics()
	{
		var priceStep = Security?.PriceStep ?? 0m;
		if (priceStep <= 0m)
			priceStep = 0.0001m;

		_pipSize = priceStep;

		var decimals = Security?.Decimals;
		if (decimals is 3 or 5)
			_pipSize *= 10m;

		var stepPrice = Security?.StepPrice ?? 0m;
		if (stepPrice <= 0m)
			stepPrice = 1m;

		var multiplier = priceStep > 0m ? _pipSize / priceStep : 1m;
		if (multiplier <= 0m)
			multiplier = 1m;

		_pipValueMoney = stepPrice * multiplier;

		if (_pipValueMoney <= 0m)
			_pipValueMoney = stepPrice;
	}

	private enum PriceStatus
	{
		Nothing,
		CrossedBelowLower,
		CrossedAboveUpper
	}
}
