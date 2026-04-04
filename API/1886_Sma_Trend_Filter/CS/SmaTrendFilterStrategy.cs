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
/// Multi-timeframe SimpleMovingAverage trend filter strategy.
/// Uses three groups of SMAs on three timeframes.
/// When majority of SMAs across timeframes agree on direction, opens position.
/// </summary>
public class SmaTrendFilterStrategy : Strategy
{
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType1;
	private readonly StrategyParam<DataType> _candleType2;
	private readonly StrategyParam<DataType> _candleType3;

	// Timeframe 1 SMAs
	private readonly SimpleMovingAverage _sma1_5 = new() { Length = 5 };
	private readonly SimpleMovingAverage _sma1_8 = new() { Length = 8 };
	private readonly SimpleMovingAverage _sma1_13 = new() { Length = 13 };
	private readonly SimpleMovingAverage _sma1_21 = new() { Length = 21 };
	private readonly SimpleMovingAverage _sma1_34 = new() { Length = 34 };

	// Timeframe 2 SMAs
	private readonly SimpleMovingAverage _sma2_5 = new() { Length = 5 };
	private readonly SimpleMovingAverage _sma2_8 = new() { Length = 8 };
	private readonly SimpleMovingAverage _sma2_13 = new() { Length = 13 };
	private readonly SimpleMovingAverage _sma2_21 = new() { Length = 21 };
	private readonly SimpleMovingAverage _sma2_34 = new() { Length = 34 };

	// Timeframe 3 SMAs
	private readonly SimpleMovingAverage _sma3_5 = new() { Length = 5 };
	private readonly SimpleMovingAverage _sma3_8 = new() { Length = 8 };
	private readonly SimpleMovingAverage _sma3_13 = new() { Length = 13 };
	private readonly SimpleMovingAverage _sma3_21 = new() { Length = 21 };
	private readonly SimpleMovingAverage _sma3_34 = new() { Length = 34 };

	// Previous values for each SMA per timeframe (5 per tf)
	private decimal _prev1_0, _prev1_1, _prev1_2, _prev1_3, _prev1_4;
	private decimal _prev2_0, _prev2_1, _prev2_2, _prev2_3, _prev2_4;
	private decimal _prev3_0, _prev3_1, _prev3_2, _prev3_3, _prev3_4;

	// Trend scores per timeframe
	private decimal _uitog0, _uitog1, _uitog2;
	private decimal _ditog0, _ditog1, _ditog2;
	private bool _isReady0, _isReady1, _isReady2;

	private int _signal;
	private int _barsSinceTrade;

	/// <summary>
	/// Minimum number of primary timeframe bars between orders.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Primary timeframe for calculations.
	/// </summary>
	public DataType CandleType1
	{
		get => _candleType1.Value;
		set => _candleType1.Value = value;
	}

	/// <summary>
	/// Secondary timeframe for calculations.
	/// </summary>
	public DataType CandleType2
	{
		get => _candleType2.Value;
		set => _candleType2.Value = value;
	}

	/// <summary>
	/// Tertiary timeframe for calculations.
	/// </summary>
	public DataType CandleType3
	{
		get => _candleType3.Value;
		set => _candleType3.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public SmaTrendFilterStrategy()
	{
		_cooldownBars = Param(nameof(CooldownBars), 200)
			.SetDisplay("Cooldown Bars", "Minimum number of primary timeframe bars between orders", "Trading");
		_candleType1 = Param(nameof(CandleType1), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type 1", "Primary timeframe", "General");
		_candleType2 = Param(nameof(CandleType2), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type 2", "Secondary timeframe", "General");
		_candleType3 = Param(nameof(CandleType3), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type 3", "Tertiary timeframe", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_signal = 0;
		_barsSinceTrade = 0;

		_prev1_0 = _prev1_1 = _prev1_2 = _prev1_3 = _prev1_4 = 0m;
		_prev2_0 = _prev2_1 = _prev2_2 = _prev2_3 = _prev2_4 = 0m;
		_prev3_0 = _prev3_1 = _prev3_2 = _prev3_3 = _prev3_4 = 0m;

		_uitog0 = _uitog1 = _uitog2 = 0m;
		_ditog0 = _ditog1 = _ditog2 = 0m;
		_isReady0 = _isReady1 = _isReady2 = false;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType1), (Security, CandleType2), (Security, CandleType3)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sub1 = SubscribeCandles(CandleType1);
		sub1.Bind(_sma1_5, _sma1_8, _sma1_13, _sma1_21, _sma1_34, ProcessTf1).Start();

		var sub2 = SubscribeCandles(CandleType2);
		sub2.Bind(_sma2_5, _sma2_8, _sma2_13, _sma2_21, _sma2_34, ProcessTf2).Start();

		var sub3 = SubscribeCandles(CandleType3);
		sub3.Bind(_sma3_5, _sma3_8, _sma3_13, _sma3_21, _sma3_34, ProcessTf3).Start();
	}

	private void ProcessTf1(ICandleMessage candle, decimal v0, decimal v1, decimal v2, decimal v3, decimal v4)
	{
		if (candle.State != CandleStates.Finished)
			return;

		ProcessTfValues(0, v0, v1, v2, v3, v4);

		if (_isReady0)
		{
			_barsSinceTrade++;
			EvaluateSignal();
		}
	}

	private void ProcessTf2(ICandleMessage candle, decimal v0, decimal v1, decimal v2, decimal v3, decimal v4)
	{
		if (candle.State != CandleStates.Finished)
			return;

		ProcessTfValues(1, v0, v1, v2, v3, v4);
	}

	private void ProcessTf3(ICandleMessage candle, decimal v0, decimal v1, decimal v2, decimal v3, decimal v4)
	{
		if (candle.State != CandleStates.Finished)
			return;

		ProcessTfValues(2, v0, v1, v2, v3, v4);
	}

	private void ProcessTfValues(int tfIndex, decimal v0, decimal v1, decimal v2, decimal v3, decimal v4)
	{
		var vals = new[] { v0, v1, v2, v3, v4 };

		for (var i = 0; i < 5; i++)
		{
			if (vals[i] == 0m)
				return;
		}

		// Get previous values
		decimal p0, p1, p2, p3, p4;
		switch (tfIndex)
		{
			case 0: p0 = _prev1_0; p1 = _prev1_1; p2 = _prev1_2; p3 = _prev1_3; p4 = _prev1_4; break;
			case 1: p0 = _prev2_0; p1 = _prev2_1; p2 = _prev2_2; p3 = _prev2_3; p4 = _prev2_4; break;
			default: p0 = _prev3_0; p1 = _prev3_1; p2 = _prev3_2; p3 = _prev3_3; p4 = _prev3_4; break;
		}

		var prevs = new[] { p0, p1, p2, p3, p4 };
		var isReady = true;

		for (var i = 0; i < 5; i++)
		{
			if (prevs[i] == 0m)
				isReady = false;
		}

		// Store current values
		switch (tfIndex)
		{
			case 0: _prev1_0 = v0; _prev1_1 = v1; _prev1_2 = v2; _prev1_3 = v3; _prev1_4 = v4; break;
			case 1: _prev2_0 = v0; _prev2_1 = v1; _prev2_2 = v2; _prev2_3 = v3; _prev2_4 = v4; break;
			default: _prev3_0 = v0; _prev3_1 = v1; _prev3_2 = v2; _prev3_3 = v3; _prev3_4 = v4; break;
		}

		if (!isReady)
			return;

		var up = 0;
		var down = 0;

		for (var i = 0; i < 5; i++)
		{
			if (vals[i] > prevs[i])
				up++;
			else if (vals[i] < prevs[i])
				down++;
		}

		var upPct = up / 5m * 100m;
		var downPct = down / 5m * 100m;

		switch (tfIndex)
		{
			case 0: _uitog0 = upPct; _ditog0 = downPct; _isReady0 = true; break;
			case 1: _uitog1 = upPct; _ditog1 = downPct; _isReady1 = true; break;
			default: _uitog2 = upPct; _ditog2 = downPct; _isReady2 = true; break;
		}
	}

	private void EvaluateSignal()
	{
		if (!_isReady0 || !_isReady1 || !_isReady2)
			return;

		// Compute average trend strength across all 3 timeframes
		var avgUp = (_uitog0 + _uitog1 + _uitog2) / 3m;
		var avgDown = (_ditog0 + _ditog1 + _ditog2) / 3m;

		// Determine signal based on average trend strength
		_signal = 0;

		if (avgUp >= 80m)
			_signal = 2;
		else if (avgDown >= 80m)
			_signal = -2;
		else if (avgUp >= 60m)
			_signal = 1;
		else if (avgDown >= 60m)
			_signal = -1;

		if (_barsSinceTrade < CooldownBars)
			return;

		// Close logic: close when signal reverses strongly
		if (Position > 0 && _signal <= -2)
		{
			SellMarket();
			_barsSinceTrade = 0;
			return;
		}

		if (Position < 0 && _signal >= 2)
		{
			BuyMarket();
			_barsSinceTrade = 0;
			return;
		}

		// Open logic: open when signal is strong (level 2)
		if (_signal >= 2 && Position <= 0)
		{
			BuyMarket();
			_barsSinceTrade = 0;
		}
		else if (_signal <= -2 && Position >= 0)
		{
			SellMarket();
			_barsSinceTrade = 0;
		}
	}
}
