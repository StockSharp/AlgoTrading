using System;
using Ecng.ComponentModel;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on breakout and retest of the first 15-minute candle with ATR-based risk management.
/// </summary>
public class ThinkTechAISignalsStrategy : Strategy
{
	private readonly StrategyParam<decimal> _riskRewardRatio;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<bool> _useTrendFilter;
	private readonly StrategyParam<bool> _useRsiFilter;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _firstHigh;
	private decimal _firstLow;
	private bool _firstCaptured;
	private DateTime _currentDate;
	private decimal _takeProfit;
	private decimal _stopLoss;

	public decimal RiskRewardRatio { get => _riskRewardRatio.Value; set => _riskRewardRatio.Value = value; }
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }
	public bool UseTrendFilter { get => _useTrendFilter.Value; set => _useTrendFilter.Value = value; }
	public bool UseRsiFilter { get => _useRsiFilter.Value; set => _useRsiFilter.Value = value; }
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public decimal RsiOversold { get => _rsiOversold.Value; set => _rsiOversold.Value = value; }
	public decimal RsiOverbought { get => _rsiOverbought.Value; set => _rsiOverbought.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ThinkTechAISignalsStrategy()
	{
		_riskRewardRatio = Param(nameof(RiskRewardRatio), 2m)
		.SetDisplay("Risk/Reward", "Profit to risk ratio", "General")
		.SetCanOptimize(true)
		.SetOptimize(1m, 5m, 0.5m);

		_atrLength = Param(nameof(AtrLength), 14)
		.SetDisplay("ATR Length", "ATR calculation length", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(5, 40, 5);

		_atrMultiplier = Param(nameof(AtrMultiplier), 1.5m)
		.SetDisplay("ATR Mult", "ATR multiplier for stop", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(0.5m, 3m, 0.5m);

		_useTrendFilter = Param(nameof(UseTrendFilter), true)
		.SetDisplay("Use Trend Filter", "Enable 50 EMA filter", "Filters");

		_useRsiFilter = Param(nameof(UseRsiFilter), false)
		.SetDisplay("Use RSI Filter", "Enable RSI filter", "Filters");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
		.SetDisplay("RSI Period", "RSI calculation period", "Indicators");

		_rsiOversold = Param(nameof(RsiOversold), 30m)
		.SetDisplay("RSI Oversold", "RSI oversold level", "Filters");

		_rsiOverbought = Param(nameof(RsiOverbought), 70m)
		.SetDisplay("RSI Overbought", "RSI overbought level", "Filters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Working candle timeframe", "Data");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_firstCaptured = false;
		_firstHigh = 0;
		_firstLow = 0;
		_takeProfit = 0;
		_stopLoss = 0;
		_currentDate = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var atr = new AverageTrueRange { Length = AtrLength };
		var ema = new ExponentialMovingAverage { Length = 50 };
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(atr, ema, rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue, decimal emaValue, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (_currentDate != candle.OpenTime.Date)
		{
			_currentDate = candle.OpenTime.Date;
			_firstHigh = candle.HighPrice;
			_firstLow = candle.LowPrice;
			_firstCaptured = true;
			return;
		}

		var range = _firstHigh - _firstLow;

		var longFilter = !UseTrendFilter || candle.ClosePrice > emaValue;
		var shortFilter = !UseTrendFilter || candle.ClosePrice < emaValue;
		var rsiLong = !UseRsiFilter || rsiValue < RsiOversold;
		var rsiShort = !UseRsiFilter || rsiValue > RsiOverbought;

		if (_firstCaptured && Position == 0)
		{
			var buySignal = candle.LowPrice <= _firstHigh && candle.ClosePrice > _firstHigh && longFilter && rsiLong;
			var sellSignal = candle.HighPrice >= _firstLow && candle.ClosePrice < _firstLow && shortFilter && rsiShort;

			if (buySignal)
			{
				_takeProfit = _firstHigh + range * RiskRewardRatio;
				_stopLoss = _firstLow - atrValue * AtrMultiplier;
				BuyMarket();
			}
			else if (sellSignal)
			{
				_takeProfit = _firstLow - range * RiskRewardRatio;
				_stopLoss = _firstHigh + atrValue * AtrMultiplier;
				SellMarket();
			}
		}

		if (Position > 0)
		{
			if (candle.ClosePrice >= _takeProfit || candle.ClosePrice <= _stopLoss)
				SellMarket();
		}
		else if (Position < 0)
		{
			if (candle.ClosePrice <= _takeProfit || candle.ClosePrice >= _stopLoss)
				BuyMarket();
		}
	}
}
