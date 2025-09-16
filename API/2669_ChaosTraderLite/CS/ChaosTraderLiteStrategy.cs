using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Chaos Trader Lite strategy implementing Bill Williams' three wise men entry concepts.
/// Places stop orders when divergent bars, Awesome Oscillator accelerations or confirmed fractals appear.
/// </summary>
public class ChaosTraderLiteStrategy : Strategy
{
	private const int LipsShift = 3;
	private const int TeethShift = 5;

	private readonly StrategyParam<int> _magnitudePips;
	private readonly StrategyParam<bool> _useFirstWiseMan;
	private readonly StrategyParam<bool> _useSecondWiseMan;
	private readonly StrategyParam<bool> _useThirdWiseMan;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<DataType> _candleType;

	private SmoothedMovingAverage _lipsSmma;
	private SmoothedMovingAverage _teethSmma;
	private AwesomeOscillator _awesomeOscillator;

	private readonly Queue<decimal> _lipsShiftQueue = new();
	private readonly Queue<decimal> _teethShiftQueue = new();

	private CandleInfo? _bar0;
	private CandleInfo? _bar1;
	private CandleInfo? _bar2;
	private CandleInfo? _bar3;
	private CandleInfo? _bar4;

	private decimal? _lips0;
	private decimal? _teeth0;
	private decimal? _teeth1;

	private decimal? _ao0;
	private decimal? _ao1;
	private decimal? _ao2;
	private decimal? _ao3;
	private decimal? _ao4;
	private decimal? _ao5;

	private decimal? _longStopLoss;
	private decimal? _shortStopLoss;

	private Order _buyStopOrder;
	private Order _sellStopOrder;

	/// <summary>
	/// Magnitude threshold in pips between price and Alligator lips.
	/// </summary>
	public int MagnitudePips
	{
		get => _magnitudePips.Value;
		set => _magnitudePips.Value = value;
	}

	/// <summary>
	/// Enable the first wise man divergent bar setup.
	/// </summary>
	public bool UseFirstWiseMan
	{
		get => _useFirstWiseMan.Value;
		set => _useFirstWiseMan.Value = value;
	}

	/// <summary>
	/// Enable the second wise man Awesome Oscillator acceleration setup.
	/// </summary>
	public bool UseSecondWiseMan
	{
		get => _useSecondWiseMan.Value;
		set => _useSecondWiseMan.Value = value;
	}

	/// <summary>
	/// Enable the third wise man fractal breakout setup.
	/// </summary>
	public bool UseThirdWiseMan
	{
		get => _useThirdWiseMan.Value;
		set => _useThirdWiseMan.Value = value;
	}

	/// <summary>
	/// Order volume used for stop entries.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Candle type processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="ChaosTraderLiteStrategy"/>.
	/// </summary>
	public ChaosTraderLiteStrategy()
	{
		_magnitudePips = Param(nameof(MagnitudePips), 10)
			.SetGreaterThanZero()
			.SetDisplay("Magnitude", "Distance from lips in pips", "General");

		_useFirstWiseMan = Param(nameof(UseFirstWiseMan), true)
			.SetDisplay("First Wise Man", "Enable divergent bar setup", "General");

		_useSecondWiseMan = Param(nameof(UseSecondWiseMan), true)
			.SetDisplay("Second Wise Man", "Enable Awesome Oscillator setup", "General");

		_useThirdWiseMan = Param(nameof(UseThirdWiseMan), true)
			.SetDisplay("Third Wise Man", "Enable fractal breakout setup", "General");

		_volume = Param(nameof(Volume), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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

		_bar0 = _bar1 = _bar2 = _bar3 = _bar4 = null;

		_lipsShiftQueue.Clear();
		_teethShiftQueue.Clear();

		_lips0 = null;
		_teeth0 = null;
		_teeth1 = null;

		_ao0 = null;
		_ao1 = null;
		_ao2 = null;
		_ao3 = null;
		_ao4 = null;
		_ao5 = null;

		_longStopLoss = null;
		_shortStopLoss = null;

		_buyStopOrder = null;
		_sellStopOrder = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		StartProtection();

		_lipsSmma = new SmoothedMovingAverage { Length = 5 };
		_teethSmma = new SmoothedMovingAverage { Length = 8 };
		_awesomeOscillator = new AwesomeOscillator { ShortPeriod = 5, LongPeriod = 34 };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _lipsSmma);
			DrawIndicator(area, _teethSmma);
			DrawIndicator(area, _awesomeOscillator);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateBarHistory(candle);

		var median = (candle.HighPrice + candle.LowPrice) / 2m;
		var lipsValue = _lipsSmma.Process(median);
		var teethValue = _teethSmma.Process(median);
		var awesomeValue = _awesomeOscillator.Process(candle.HighPrice, candle.LowPrice);

		if (lipsValue.IsFinal)
		{
			var lips = lipsValue.GetValue<decimal>();
			_lipsShiftQueue.Enqueue(lips);
			if (_lipsShiftQueue.Count > LipsShift)
			{
				_lips0 = _lipsShiftQueue.Dequeue();
			}
		}

		if (teethValue.IsFinal)
		{
			var teeth = teethValue.GetValue<decimal>();
			_teethShiftQueue.Enqueue(teeth);
			if (_teethShiftQueue.Count > TeethShift)
			{
				_teeth1 = _teeth0;
				_teeth0 = _teethShiftQueue.Dequeue();
			}
		}

		if (awesomeValue.IsFinal)
		{
			var ao = awesomeValue.GetValue<decimal>();
			_ao5 = _ao4;
			_ao4 = _ao3;
			_ao3 = _ao2;
			_ao2 = _ao1;
			_ao1 = _ao0;
			_ao0 = ao;
		}

		var upFractal = GetUpFractal();
		var downFractal = GetDownFractal();

		if (IsFormedAndOnlineAndAllowTrading())
			EvaluateSignals(candle, upFractal, downFractal);

		UpdateProtection(candle);
	}

	private void EvaluateSignals(ICandleMessage candle, decimal? upFractal, decimal? downFractal)
	{
		if (_bar0 is not CandleInfo current || _bar1 is not CandleInfo previous)
			return;

		var point = Security?.PriceStep ?? 1m;
		var magnitudeThreshold = MagnitudePips * point;

		if (UseFirstWiseMan && _lips0 is decimal lips)
		{
			if (IsBullishDivergent(current, previous))
			{
				var distance = lips - current.High;
				if (distance > magnitudeThreshold)
					PlaceBuySetup(current, point);
			}

			if (IsBearishDivergent(current, previous))
			{
				var distance = current.Low - lips;
				if (distance > magnitudeThreshold)
					PlaceSellSetup(current, point);
			}
		}

		if (UseSecondWiseMan && _ao1.HasValue && _ao2.HasValue && _ao3.HasValue && _ao4.HasValue && _ao5.HasValue)
		{
			var currentAo = _ao1.Value;
			var bar2Ao = _ao2.Value;
			var bar3Ao = _ao3.Value;
			var bar4Ao = _ao4.Value;
			var bar5Ao = _ao5.Value;

			var bullishAcceleration = currentAo > bar2Ao && bar2Ao > bar3Ao && bar3Ao > bar4Ao && bar4Ao < bar5Ao;
			if (bullishAcceleration)
				PlaceBuySetup(current, point);

			var bearishAcceleration = currentAo < bar2Ao && bar2Ao < bar3Ao && bar3Ao < bar4Ao && bar4Ao > bar5Ao;
			if (bearishAcceleration)
				PlaceSellSetup(current, point);
		}

		if (UseThirdWiseMan && _teeth0.HasValue)
		{
			var teeth = _teeth0.Value;
			var offset = MagnitudePips * point;

			if (upFractal.HasValue && candle.ClosePrice > teeth + offset)
				PlaceBuySetup(current, point);

			if (downFractal.HasValue && candle.ClosePrice < teeth - offset)
				PlaceSellSetup(current, point);
		}
	}

	private void UpdateProtection(ICandleMessage candle)
	{
		if (Position > 0 && _longStopLoss is decimal longStop)
		{
			if (candle.LowPrice <= longStop)
			{
				SellMarket(Position);
				_longStopLoss = null;
			}
		}
		else if (Position < 0 && _shortStopLoss is decimal shortStop)
		{
			if (candle.HighPrice >= shortStop)
			{
				BuyMarket(-Position);
				_shortStopLoss = null;
			}
		}
	}

	private void PlaceBuySetup(CandleInfo bar, decimal point)
	{
		if (Volume <= 0)
			return;

		var entryPrice = bar.High + point;
		if (entryPrice <= 0m)
			return;

		var stopPrice = bar.Low - point;

		CancelOrderIfActive(ref _sellStopOrder);

		if (Position < 0)
		{
			BuyMarket(-Position);
			_shortStopLoss = null;
		}

		CancelOrderIfActive(ref _buyStopOrder);
		_buyStopOrder = BuyStop(Volume, entryPrice);

		if (_longStopLoss is decimal existing)
		{
			if (stopPrice > existing)
				_longStopLoss = stopPrice;
		}
		else
		{
			_longStopLoss = stopPrice;
		}
	}

	private void PlaceSellSetup(CandleInfo bar, decimal point)
	{
		if (Volume <= 0)
			return;

		var entryPrice = bar.Low - point;
		if (entryPrice <= 0m)
			return;

		var stopPrice = bar.High + point;

		CancelOrderIfActive(ref _buyStopOrder);

		if (Position > 0)
		{
			SellMarket(Position);
			_longStopLoss = null;
		}

		CancelOrderIfActive(ref _sellStopOrder);
		_sellStopOrder = SellStop(Volume, entryPrice);

		if (_shortStopLoss is decimal existing)
		{
			if (stopPrice < existing)
				_shortStopLoss = stopPrice;
		}
		else
		{
			_shortStopLoss = stopPrice;
		}
	}

	private static bool IsBullishDivergent(CandleInfo current, CandleInfo previous)
	{
		var median = (current.High + current.Low) / 2m;
		return current.Low < previous.Low && current.Close > median;
	}

	private static bool IsBearishDivergent(CandleInfo current, CandleInfo previous)
	{
		var median = (current.High + current.Low) / 2m;
		return current.High > previous.High && current.Close < median;
	}

	private decimal? GetUpFractal()
	{
		if (_bar0 is not CandleInfo bar0 || _bar1 is not CandleInfo bar1 || _bar2 is not CandleInfo bar2 ||
			_bar3 is not CandleInfo bar3 || _bar4 is not CandleInfo bar4)
			return null;

		return bar2.High > bar3.High && bar2.High > bar4.High && bar2.High > bar1.High && bar2.High > bar0.High
			? bar2.High
			: null;
	}

	private decimal? GetDownFractal()
	{
		if (_bar0 is not CandleInfo bar0 || _bar1 is not CandleInfo bar1 || _bar2 is not CandleInfo bar2 ||
			_bar3 is not CandleInfo bar3 || _bar4 is not CandleInfo bar4)
			return null;

		return bar2.Low < bar3.Low && bar2.Low < bar4.Low && bar2.Low < bar1.Low && bar2.Low < bar0.Low
			? bar2.Low
			: null;
	}

	private void UpdateBarHistory(ICandleMessage candle)
	{
		_bar4 = _bar3;
		_bar3 = _bar2;
		_bar2 = _bar1;
		_bar1 = _bar0;

		_bar0 = new CandleInfo
		{
			Open = candle.OpenPrice,
			High = candle.HighPrice,
			Low = candle.LowPrice,
			Close = candle.ClosePrice
		};
	}

	private void CancelOrderIfActive(ref Order order)
	{
		if (order != null && order.State == OrderStates.Active)
			CancelOrder(order);

		order = null;
	}


	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position <= 0)
			_longStopLoss = null;
		if (Position >= 0)
			_shortStopLoss = null;
	}

	private struct CandleInfo
	{
		public decimal Open { get; init; }
		public decimal High { get; init; }
		public decimal Low { get; init; }
		public decimal Close { get; init; }
	}
}
