using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA crossover strategy with trailing exit.
/// </summary>
public class LongEmaAdvancedExitStrategy : Strategy
{
	private readonly StrategyParam<int> _shortPeriod;
	private readonly StrategyParam<int> _midPeriod;
	private readonly StrategyParam<int> _longPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _prevShort;
	private decimal _prevMid;
	private int _barsSinceSignal;

	public int ShortPeriod { get => _shortPeriod.Value; set => _shortPeriod.Value = value; }
	public int MidPeriod { get => _midPeriod.Value; set => _midPeriod.Value = value; }
	public int LongPeriod { get => _longPeriod.Value; set => _longPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }

	public LongEmaAdvancedExitStrategy()
	{
		_shortPeriod = Param(nameof(ShortPeriod), 10)
			.SetDisplay("Short EMA", "Short EMA period", "Indicators");
		_midPeriod = Param(nameof(MidPeriod), 20)
			.SetDisplay("Mid EMA", "Mid EMA period", "Indicators");
		_longPeriod = Param(nameof(LongPeriod), 40)
			.SetDisplay("Long EMA", "Long EMA period", "Indicators");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candles", "General");
		_cooldownBars = Param(nameof(CooldownBars), 38)
			.SetDisplay("Cooldown Bars", "Min bars between signals", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevShort = 0;
		_prevMid = 0;
		_barsSinceSignal = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevShort = 0;
		_prevMid = 0;
		_barsSinceSignal = 0;

		var emaShort = new ExponentialMovingAverage { Length = ShortPeriod };
		var emaMid = new ExponentialMovingAverage { Length = MidPeriod };
		var emaLong = new ExponentialMovingAverage { Length = LongPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(emaShort, emaMid, emaLong, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, emaShort);
			DrawIndicator(area, emaMid);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal shortVal, decimal midVal, decimal longVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barsSinceSignal++;

		if (_prevShort == 0 || _prevMid == 0)
		{
			_prevShort = shortVal;
			_prevMid = midVal;
			return;
		}

		if (_barsSinceSignal < CooldownBars)
		{
			_prevShort = shortVal;
			_prevMid = midVal;
			return;
		}

		// Entry: short crosses above mid + price above long EMA -> buy
		var crossUp = _prevShort <= _prevMid && shortVal > midVal;
		if (crossUp && candle.ClosePrice > longVal && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_barsSinceSignal = 0;
		}

		// Entry: short crosses below mid + price below long EMA -> sell
		var crossDown = _prevShort >= _prevMid && shortVal < midVal;
		if (crossDown && candle.ClosePrice < longVal && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_barsSinceSignal = 0;
		}

		_prevShort = shortVal;
		_prevMid = midVal;
	}
}
