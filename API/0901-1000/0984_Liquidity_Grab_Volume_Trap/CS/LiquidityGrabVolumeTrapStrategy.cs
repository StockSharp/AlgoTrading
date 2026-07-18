using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Liquidity Grab Strategy (Volume Trap).
/// Detects liquidity grabs where price sweeps beyond recent range
/// then reverses back, indicating a trap.
/// </summary>
public class LiquidityGrabVolumeTrapStrategy : Strategy
{
	private readonly StrategyParam<int> _lookback;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private Highest _highest;
	private Lowest _lowest;
	private int _barsSinceSignal;
	private decimal _prevHigh;
	private decimal _prevLow;

	public int Lookback
	{
		get => _lookback.Value;
		set => _lookback.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	public LiquidityGrabVolumeTrapStrategy()
	{
		_lookback = Param(nameof(Lookback), 10)
			.SetDisplay("Lookback", "Bars for range", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candles for calculations", "General");

		_cooldownBars = Param(nameof(CooldownBars), 50)
			.SetDisplay("Cooldown Bars", "Min bars between signals", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_highest = new Highest { Length = Lookback };
		_lowest = new Lowest { Length = Lookback };
		_barsSinceSignal = 0;
		_prevHigh = 0;
		_prevLow = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_highest, _lowest, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_highest = null;
		_lowest = null;
		_barsSinceSignal = 0;
		_prevHigh = 0;
		_prevLow = 0;
	}

	private void ProcessCandle(ICandleMessage candle, decimal highVal, decimal lowVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barsSinceSignal++;

		if (!_highest.IsFormed || !_lowest.IsFormed)
		{
			_prevHigh = highVal;
			_prevLow = lowVal;
			return;
		}

		// Use previous bar's range values
		var rangeHigh = _prevHigh;
		var rangeLow = _prevLow;

		// Update previous values for next bar
		_prevHigh = highVal;
		_prevLow = lowVal;

		// Bullish grab: wick swept below prior range low but closed back inside
		var bullGrab = candle.LowPrice < rangeLow && candle.ClosePrice > rangeLow;

		// Bearish grab: wick swept above prior range high but closed back inside
		var bearGrab = candle.HighPrice > rangeHigh && candle.ClosePrice < rangeHigh;

		// Cooldown check
		if (_barsSinceSignal < CooldownBars)
			return;

		if (bullGrab && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_barsSinceSignal = 0;
		}
		else if (bearGrab && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_barsSinceSignal = 0;
		}
	}
}
