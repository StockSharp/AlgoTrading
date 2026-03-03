using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Anomalous Holonomy Field Theory strategy.
/// Combines EMA trend with RSI extremes and VWAP distance for composite signal.
/// </summary>
public class AnomalousHolonomyFieldTheoryStrategy : Strategy
{
	private readonly StrategyParam<decimal> _signalThreshold;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private int _barIndex;
	private int _lastTradeBar;

	/// <summary>
	/// Absolute signal level required for trades.
	/// </summary>
	public decimal SignalThreshold
	{
		get => _signalThreshold.Value;
		set => _signalThreshold.Value = value;
	}

	/// <summary>
	/// Cooldown bars between trades.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
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
			.SetRange(0.5m, 10m);

		_cooldownBars = Param(nameof(CooldownBars), 100)
			.SetDisplay("Cooldown Bars", "Bars between trades", "Trading");

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
		_barIndex = 0;
		_lastTradeBar = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema20 = new ExponentialMovingAverage { Length = 20 };
		var ema50 = new ExponentialMovingAverage { Length = 50 };
		var rsi = new RelativeStrengthIndex { Length = 14 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema20, ema50, rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema20);
			DrawIndicator(area, ema50);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema20, decimal ema50, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barIndex++;
		var close = candle.ClosePrice;
		var cooldownOk = _barIndex - _lastTradeBar > CooldownBars;

		var signal = 0m;

		// EMA trend component
		if (close > ema20)
			signal += close > ema50 ? 1.5m : 1m;
		else if (close < ema20)
			signal -= close < ema50 ? 1.5m : 1m;

		// VWAP distance component
		var vwap = candle.TotalVolume != 0 ? candle.TotalPrice / candle.TotalVolume : close;
		var vwapDist = close != 0 ? (close - vwap) / close * 100m : 0m;
		vwapDist = Math.Max(-2m, Math.Min(2m, vwapDist));
		signal += vwapDist;

		// RSI extremes component
		if (rsiValue < 30m && signal > 0m)
			signal += 1.5m;
		else if (rsiValue < 40m && signal > 0m)
			signal += 0.5m;
		else if (rsiValue > 70m && signal < 0m)
			signal -= 1.5m;
		else if (rsiValue > 60m && signal < 0m)
			signal -= 0.5m;

		if (signal >= SignalThreshold && Position <= 0 && cooldownOk)
		{
			BuyMarket();
			_lastTradeBar = _barIndex;
		}
		else if (signal <= -SignalThreshold && Position >= 0 && cooldownOk)
		{
			SellMarket();
			_lastTradeBar = _barIndex;
		}
	}
}
