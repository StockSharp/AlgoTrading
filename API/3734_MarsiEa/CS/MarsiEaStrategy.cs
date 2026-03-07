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

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Moving average plus RSI strategy ported from the MARSIEA MetaTrader expert.
/// Executes a single position at a time with fixed stop-loss and take-profit levels measured in pips.
/// </summary>
public class MarsiEaStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;

	private SimpleMovingAverage _sma;
	private RelativeStrengthIndex _rsi;
	private decimal? _virtualStopPrice;
	private decimal? _virtualTakePrice;

	/// <summary>
	/// Candle type used to feed indicators.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Moving average period.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Overbought threshold for RSI.
	/// </summary>
	public decimal RsiOverbought
	{
		get => _rsiOverbought.Value;
		set => _rsiOverbought.Value = value;
	}

	/// <summary>
	/// Oversold threshold for RSI.
	/// </summary>
	public decimal RsiOversold
	{
		get => _rsiOversold.Value;
		set => _rsiOversold.Value = value;
	}

	/// <summary>
	/// Risk percentage used to size the entry volume.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MarsiEaStrategy"/> class.
	/// </summary>
	public MarsiEaStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Series used for indicator calculations", "General");

		_maPeriod = Param(nameof(MaPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Simple moving average length", "Indicators")
			
			.SetOptimize(5, 50, 1);

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI lookback length", "Indicators")
			
			.SetOptimize(5, 50, 1);

		_rsiOverbought = Param(nameof(RsiOverbought), 55m)
			.SetDisplay("RSI Overbought", "Upper RSI threshold", "Signals");

		_rsiOversold = Param(nameof(RsiOversold), 45m)
			.SetDisplay("RSI Oversold", "Lower RSI threshold", "Signals");

		_riskPercent = Param(nameof(RiskPercent), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Risk Percent", "Equity percentage risked per trade", "Money Management");

		_stopLossPips = Param(nameof(StopLossPips), 100m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss (pips)", "Protective stop distance in pips", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 300m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit (pips)", "Profit target distance in pips", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_sma = null;
		_rsi = null;
		_virtualStopPrice = null;
		_virtualTakePrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_sma = new SimpleMovingAverage { Length = MaPeriod };
		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_sma, _rsi, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _sma);
			DrawIndicator(area, _rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_sma.IsFormed || !_rsi.IsFormed)
			return;

		// Check virtual SL/TP
		if (Position > 0m)
		{
			if (_virtualStopPrice.HasValue && candle.LowPrice <= _virtualStopPrice.Value)
			{
				SellMarket(Position);
				_virtualStopPrice = null;
				_virtualTakePrice = null;
				return;
			}
			if (_virtualTakePrice.HasValue && candle.HighPrice >= _virtualTakePrice.Value)
			{
				SellMarket(Position);
				_virtualStopPrice = null;
				_virtualTakePrice = null;
				return;
			}
		}
		else if (Position < 0m)
		{
			if (_virtualStopPrice.HasValue && candle.HighPrice >= _virtualStopPrice.Value)
			{
				BuyMarket(Math.Abs(Position));
				_virtualStopPrice = null;
				_virtualTakePrice = null;
				return;
			}
			if (_virtualTakePrice.HasValue && candle.LowPrice <= _virtualTakePrice.Value)
			{
				BuyMarket(Math.Abs(Position));
				_virtualStopPrice = null;
				_virtualTakePrice = null;
				return;
			}
		}

		// Only one position can be active at the same time
		if (Position != 0m)
			return;

		var closePrice = candle.ClosePrice;

		var volume = CalculateTradeVolume();
		if (volume <= 0m)
			return;

		var pipSize = GetPipSize();
		if (pipSize <= 0m)
			pipSize = 1m;

		if (closePrice > maValue && rsiValue < RsiOversold)
		{
			BuyMarket(volume);
			_virtualStopPrice = closePrice - StopLossPips * pipSize;
			_virtualTakePrice = closePrice + TakeProfitPips * pipSize;
		}
		else if (closePrice < maValue && rsiValue > RsiOverbought)
		{
			SellMarket(volume);
			_virtualStopPrice = closePrice + StopLossPips * pipSize;
			_virtualTakePrice = closePrice - TakeProfitPips * pipSize;
		}
	}

	private decimal CalculateTradeVolume()
	{
		var portfolioValue = Portfolio?.CurrentValue ?? 0m;
		var priceStep = Security?.PriceStep ?? 0m;
		var stepPrice = 1m;
		var pipSize = GetPipSize();

		if (RiskPercent <= 0m || portfolioValue <= 0m || priceStep <= 0m || stepPrice <= 0m || pipSize <= 0m)
			return NormalizeVolume(Volume > 0m ? Volume : 1m);

		var riskAmount = portfolioValue * RiskPercent / 100m;
		var perUnitRisk = StopLossPips * pipSize / priceStep * stepPrice;

		if (StopLossPips <= 0m || perUnitRisk <= 0m)
			return NormalizeVolume(Volume > 0m ? Volume : 1m);

		var volume = riskAmount / perUnitRisk;
		return NormalizeVolume(volume);
	}

	private decimal NormalizeVolume(decimal volume)
	{
		if (volume <= 0m)
			volume = 1m;

		var volumeStep = Security?.VolumeStep ?? 0m;
		if (volumeStep > 0m)
		{
			var steps = Math.Max(1m, Math.Round(volume / volumeStep, MidpointRounding.AwayFromZero));
			volume = steps * volumeStep;
		}

		var minVolume = Security?.MinVolume ?? 0m;
		if (minVolume > 0m && volume < minVolume)
			volume = minVolume;

		return volume;
	}

	private decimal CalculatePriceSteps(decimal pips)
	{
		if (pips <= 0m)
			return 0m;

		var priceStep = Security?.PriceStep ?? 0m;
		var pipSize = GetPipSize();

		if (priceStep <= 0m || pipSize <= 0m)
			return 0m;

		var steps = pips * pipSize / priceStep;
		return steps > 0m ? steps : 0m;
	}

	private decimal GetPipSize()
	{
		var priceStep = Security?.PriceStep ?? 0m;
		if (priceStep <= 0m)
			return 0m;

		var decimals = Security?.Decimals ?? 0;
		return decimals == 3 || decimals == 5 ? priceStep * 10m : priceStep;
	}
}

