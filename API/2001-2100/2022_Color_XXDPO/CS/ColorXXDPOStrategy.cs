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
/// Strategy based on a double smoothed Detrended Price Oscillator.
/// </summary>
public class ColorXXDPOStrategy : Strategy
{
	private readonly StrategyParam<int> _firstLength;
	private readonly StrategyParam<int> _secondLength;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _signalCooldownBars;

	private readonly SimpleMovingAverage _ma1 = new();
	private readonly SimpleMovingAverage _ma2 = new();
	private static readonly object _sync = new();

	private decimal _prev1;
	private decimal _prev2;
	private bool _isInitialized;
	private int _cooldownRemaining;

	public ColorXXDPOStrategy()
	{
		_firstLength = Param(nameof(FirstLength), 21)
			.SetDisplay("First MA Length", "Length for the first smoothing stage.", "Indicators");

		_secondLength = Param(nameof(SecondLength), 5)
			.SetDisplay("Second MA Length", "Length for the second smoothing stage.", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for strategy calculation.", "General");

		_signalCooldownBars = Param(nameof(SignalCooldownBars), 3)
			.SetNotNegative()
			.SetDisplay("Signal Cooldown Bars", "Closed candles to wait before a new direction change.", "General");

		_ma1.Length = FirstLength;
		_ma2.Length = SecondLength;
	}

	public int FirstLength
	{
		get => _firstLength.Value;
		set => _firstLength.Value = value;
	}

	public int SecondLength
	{
		get => _secondLength.Value;
		set => _secondLength.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int SignalCooldownBars
	{
		get => _signalCooldownBars.Value;
		set => _signalCooldownBars.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_ma1.Reset();
		_ma2.Reset();
		_ma1.Length = FirstLength;
		_ma2.Length = SecondLength;
		_prev1 = 0m;
		_prev2 = 0m;
		_isInitialized = false;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_ma1.Length = FirstLength;
		_ma2.Length = SecondLength;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		lock (_sync)
		{
			if (candle.State != CandleStates.Finished)
				return;

			if (_cooldownRemaining > 0)
				_cooldownRemaining--;

			var ma1Result = _ma1.Process(candle.ClosePrice, candle.OpenTime, true);
			if (!_ma1.IsFormed || ma1Result.IsEmpty)
				return;

			var ma1 = ma1Result.ToDecimal();
			var dpo = candle.ClosePrice - ma1;
			var xxdpoResult = _ma2.Process(dpo, candle.OpenTime, true);
			if (!_ma2.IsFormed || xxdpoResult.IsEmpty)
				return;

			var xxdpo = xxdpoResult.ToDecimal();

			if (!_isInitialized)
			{
				_prev2 = xxdpo;
				_prev1 = xxdpo;
				_isInitialized = true;
				return;
			}

			var turnedUp = _prev2 >= _prev1 && xxdpo > _prev1;
			var turnedDown = _prev2 <= _prev1 && xxdpo < _prev1;

			if (_cooldownRemaining == 0 && turnedUp && Position <= 0)
			{
				BuyMarket(Volume + (Position < 0 ? -Position : 0m));
				_cooldownRemaining = SignalCooldownBars;
			}
			else if (_cooldownRemaining == 0 && turnedDown && Position >= 0)
			{
				SellMarket(Volume + (Position > 0 ? Position : 0m));
				_cooldownRemaining = SignalCooldownBars;
			}

			_prev2 = _prev1;
			_prev1 = xxdpo;
		}
	}
}
