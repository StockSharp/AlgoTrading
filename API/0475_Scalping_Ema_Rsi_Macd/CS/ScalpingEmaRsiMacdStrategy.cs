namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// 30-minute scalping strategy based on EMA crossover with RSI, MACD and volume filter.
/// </summary>
public class ScalpingEmaRsiMacdStrategy : Strategy
{
	private readonly StrategyParam<int> _fastEmaLength;
	private readonly StrategyParam<int> _slowEmaLength;
	private readonly StrategyParam<int> _trendEmaLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _rsiOverbought;
	private readonly StrategyParam<int> _rsiOversold;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<decimal> _riskReward;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFastEma;
	private decimal _prevSlowEma;
	private decimal _prevMacd;
	private decimal _stopPrice;
	private decimal _takeProfitPrice;
	private decimal _entryPrice;

	public int FastEmaLength { get => _fastEmaLength.Value; set => _fastEmaLength.Value = value; }
	public int SlowEmaLength { get => _slowEmaLength.Value; set => _slowEmaLength.Value = value; }
	public int TrendEmaLength { get => _trendEmaLength.Value; set => _trendEmaLength.Value = value; }
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public int RsiOverbought { get => _rsiOverbought.Value; set => _rsiOverbought.Value = value; }
	public int RsiOversold { get => _rsiOversold.Value; set => _rsiOversold.Value = value; }
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }
	public decimal RiskReward { get => _riskReward.Value; set => _riskReward.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ScalpingEmaRsiMacdStrategy()
	{
		_fastEmaLength = Param(nameof(FastEmaLength), 12)
			.SetDisplay("Fast EMA Length", "Length for fast EMA", "Indicators")
			.SetOptimize(8, 20, 1);

		_slowEmaLength = Param(nameof(SlowEmaLength), 26)
			.SetDisplay("Slow EMA Length", "Length for slow EMA", "Indicators")
			.SetOptimize(20, 40, 1);

		_trendEmaLength = Param(nameof(TrendEmaLength), 55)
			.SetDisplay("Trend EMA Length", "Length for trend EMA", "Indicators")
			.SetOptimize(40, 80, 5);

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetDisplay("RSI Length", "Length for RSI", "Indicators")
			.SetOptimize(10, 20, 1);

		_rsiOverbought = Param(nameof(RsiOverbought), 65)
			.SetDisplay("RSI Overbought", "Upper RSI bound", "Indicators")
			.SetOptimize(60, 80, 5);

		_rsiOversold = Param(nameof(RsiOversold), 35)
			.SetDisplay("RSI Oversold", "Lower RSI bound", "Indicators")
			.SetOptimize(20, 40, 5);

		_atrLength = Param(nameof(AtrLength), 14)
			.SetDisplay("ATR Length", "Length for ATR", "Indicators")
			.SetOptimize(10, 20, 1);

		_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
			.SetDisplay("ATR Multiplier", "Multiplier for stop-loss", "Risk")
			.SetOptimize(1m, 3m, 0.5m);

		_riskReward = Param(nameof(RiskReward), 2m)
			.SetDisplay("Risk Reward", "Take profit multiplier", "Risk")
			.SetOptimize(1m, 3m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevFastEma = 0;
		_prevSlowEma = 0;
		_prevMacd = 0;
		_stopPrice = 0;
		_takeProfitPrice = 0;
		_entryPrice = 0;

		var fastEma = new ExponentialMovingAverage { Length = FastEmaLength };
		var slowEma = new ExponentialMovingAverage { Length = SlowEmaLength };
		var trendEma = new ExponentialMovingAverage { Length = TrendEmaLength };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var macd = new MovingAverageConvergenceDivergence();
		macd.ShortMa.Length = FastEmaLength;
		macd.LongMa.Length = SlowEmaLength;
		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastEma, slowEma, trendEma, rsi, macd, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastEma);
			DrawIndicator(area, slowEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastEma, decimal slowEma, decimal trendEma, decimal rsi, decimal macd, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;

		// Trend detection
		var upTrend = close > trendEma && fastEma > slowEma;
		var downTrend = close < trendEma && fastEma < slowEma;

		// EMA crossover detection
		var bullCross = _prevFastEma > 0 && _prevFastEma <= _prevSlowEma && fastEma > slowEma;
		var bearCross = _prevFastEma > 0 && _prevFastEma >= _prevSlowEma && fastEma < slowEma;

		// MACD momentum
		var macdRising = macd > _prevMacd;
		var macdFalling = macd < _prevMacd;

		// Entry conditions
		var longCondition = bullCross && upTrend && rsi > 40m && rsi < RsiOverbought && macdRising;
		var shortCondition = bearCross && downTrend && rsi < 60m && rsi > RsiOversold && macdFalling;

		if (longCondition && Position <= 0)
		{
			BuyMarket();
			_entryPrice = close;
			_stopPrice = close - atr * AtrMultiplier;
			_takeProfitPrice = close + (close - _stopPrice) * RiskReward;
		}
		else if (shortCondition && Position >= 0)
		{
			SellMarket();
			_entryPrice = close;
			_stopPrice = close + atr * AtrMultiplier;
			_takeProfitPrice = close - (_stopPrice - close) * RiskReward;
		}
		// Stop-loss and take-profit exits
		else if (Position > 0 && _stopPrice > 0)
		{
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takeProfitPrice)
			{
				SellMarket();
				_stopPrice = 0;
				_takeProfitPrice = 0;
			}
		}
		else if (Position < 0 && _stopPrice > 0)
		{
			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takeProfitPrice)
			{
				BuyMarket();
				_stopPrice = 0;
				_takeProfitPrice = 0;
			}
		}

		_prevFastEma = fastEma;
		_prevSlowEma = slowEma;
		_prevMacd = macd;
	}
}
