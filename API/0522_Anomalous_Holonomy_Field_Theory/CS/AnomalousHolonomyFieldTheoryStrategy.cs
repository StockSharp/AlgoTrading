using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Anomalous Holonomy Field Theory strategy.
/// Combines EMA, ROC, VWAP, RSI, MACD and ATR to build a composite signal.
/// </summary>
public class AnomalousHolonomyFieldTheoryStrategy : Strategy
{
	private readonly StrategyParam<decimal> _signalThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;

	/// <summary>
	/// Absolute signal level required for trades.
	/// </summary>
	public decimal SignalThreshold
	{
		get => _signalThreshold.Value;
		set => _signalThreshold.Value = value;
	}

	/// <summary>
	/// Type of candles to use.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="AnomalousHolonomyFieldTheoryStrategy"/>.
	/// </summary>
	public AnomalousHolonomyFieldTheoryStrategy()
	{
		_signalThreshold = Param(nameof(SignalThreshold), 2m)
			.SetDisplay("Signal Threshold", "Absolute signal level required for trades", "Parameters")
			.SetCanOptimize(true)
			.SetRange(0.5m, 5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_entryPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var ema20 = new ExponentialMovingAverage { Length = 20 };
		var ema50 = new ExponentialMovingAverage { Length = 50 };
		var rsi = new RelativeStrengthIndex { Length = 14 };
		var atr = new AverageTrueRange { Length = 14 };
		var roc = new RateOfChange { Length = 10 };
		var macd = new MovingAverageConvergenceDivergence();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema20, ema50, rsi, atr, roc, macd, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema20);
			DrawIndicator(area, ema50);
			DrawIndicator(area, rsi);
			DrawIndicator(area, atr);
			DrawIndicator(area, roc);
			DrawIndicator(area, macd);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle,
		decimal ema20, decimal ema50, decimal rsi, decimal atr, decimal roc,
		decimal macdValue, decimal macdSignal, decimal macdHist)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;

		var signal = 0m;

		if (close > ema20)
		{
			signal += close > ema50 ? 1.5m : 1m;

			if (roc > 0)
				signal += roc / 10m;
		}
		else if (close < ema20)
		{
			signal -= close < ema50 ? 1.5m : 1m;

			if (roc < 0)
				signal += roc / 10m;
		}

		var vwap = candle.TotalVolume != 0 ? candle.TotalPrice / candle.TotalVolume : close;
		var vwapDist = (close - vwap) / close * 100m;
		vwapDist = Math.Max(-2m, Math.Min(2m, vwapDist));
		signal += vwapDist;

		if (rsi < 30m && signal > 0m)
			signal += 1.5m;
		else if (rsi < 40m && signal > 0m)
			signal += 0.5m;
		else if (rsi > 70m && signal < 0m)
			signal -= 1.5m;
		else if (rsi > 60m && signal < 0m)
			signal -= 0.5m;

		if (macdValue > macdSignal && signal > 0m)
			signal += 0.5m;
		else if (macdValue < macdSignal && signal < 0m)
			signal -= 0.5m;

		var threshold = SignalThreshold;

		if (signal >= threshold && Position <= 0)
		{
			_entryPrice = close;
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (signal <= -threshold && Position >= 0)
		{
			_entryPrice = close;
			SellMarket(Volume + Math.Abs(Position));
		}

		if (Position > 0 && close <= _entryPrice - atr * 1.5m)
			SellMarket(Position);
		else if (Position < 0 && close >= _entryPrice + atr * 1.5m)
			BuyMarket(-Position);
	}
}
