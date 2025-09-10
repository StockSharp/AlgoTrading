using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bullish Reversal Bar strategy.
/// Enters long when a bullish reversal bar forms below the Alligator and price breaks above its high.
/// Optional filters: Awesome Oscillator and Market Facilitation Index squat bars.
/// </summary>
public class BullishReversalBarStrategy : Strategy
{
	private readonly StrategyParam<bool> _enableAo;
	private readonly StrategyParam<bool> _enableMfi;
	private readonly StrategyParam<DataType> _candleType;

	private readonly SmoothedMovingAverage _jaw = new() { Length = 13 };
	private readonly SmoothedMovingAverage _teeth = new() { Length = 8 };
	private readonly SmoothedMovingAverage _lips = new() { Length = 5 };
	private readonly SimpleMovingAverage _aoFast = new() { Length = 5 };
	private readonly SimpleMovingAverage _aoSlow = new() { Length = 34 };

	private readonly decimal?[] _jawBuffer = new decimal?[9];
	private readonly decimal?[] _teethBuffer = new decimal?[6];
	private readonly decimal?[] _lipsBuffer = new decimal?[4];
	private int _jawCount;
	private int _teethCount;
	private int _lipsCount;

	private readonly decimal?[] _highs = new decimal?[5];
	private readonly decimal?[] _lows = new decimal?[5];

	private readonly decimal[] _lowBuffer = new decimal[7];
	private int _lowCount;

	private readonly bool[] _squat = new bool[3];
	private decimal _prevMfi;
	private decimal _prevVolume;
	private decimal _prevAo;

	private int _trend;
	private int _prevTrend;
	private decimal? _upFractalLevel;
	private decimal? _downFractalLevel;
	private decimal? _upFractalActivation;
	private decimal? _downFractalActivation;

	private decimal? _confirmation;
	private decimal? _invalidation;
	private decimal? _stopLoss;

	/// <summary>
	/// Use Awesome Oscillator filter.
	/// </summary>
	public bool EnableAo
	{
		get => _enableAo.Value;
		set => _enableAo.Value = value;
	}

	/// <summary>
	/// Use Market Facilitation Index filter.
	/// </summary>
	public bool EnableMfi
	{
		get => _enableMfi.Value;
		set => _enableMfi.Value = value;
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="BullishReversalBarStrategy"/>.
	/// </summary>
	public BullishReversalBarStrategy()
	{
		_enableAo = Param(nameof(EnableAo), false)
			.SetDisplay("Enable AO", "Use Awesome Oscillator filter", "Filters");

		_enableMfi = Param(nameof(EnableMfi), false)
			.SetDisplay("Enable MFI", "Use Market Facilitation Index filter", "Filters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for strategy", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		Array.Clear(_jawBuffer);
		Array.Clear(_teethBuffer);
		Array.Clear(_lipsBuffer);
		Array.Clear(_highs);
		Array.Clear(_lows);
		Array.Clear(_lowBuffer);
		Array.Clear(_squat);

		_jawCount = _teethCount = _lipsCount = 0;
		_lowCount = 0;
		_prevMfi = 0;
		_prevVolume = 0;
		_prevAo = 0;
		_trend = 0;
		_prevTrend = 0;
		_upFractalLevel = null;
		_downFractalLevel = null;
		_upFractalActivation = null;
		_downFractalActivation = null;
		_confirmation = null;
		_invalidation = null;
		_stopLoss = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_jaw, _teeth, _lips, _aoFast, _aoSlow, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _jaw);
			DrawIndicator(area, _teeth);
			DrawIndicator(area, _lips);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal jawRaw, decimal teethRaw, decimal lipsRaw, decimal aoFast, decimal aoSlow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		for (var i = 0; i < 8; i++)
			_jawBuffer[i] = _jawBuffer[i + 1];
		_jawBuffer[8] = jawRaw;
		decimal? jaw = null;
		if (_jawCount >= 8)
			jaw = _jawBuffer[0];
		else
			_jawCount++;

		for (var i = 0; i < 5; i++)
			_teethBuffer[i] = _teethBuffer[i + 1];
		_teethBuffer[5] = teethRaw;
		decimal? teeth = null;
		if (_teethCount >= 5)
			teeth = _teethBuffer[0];
		else
			_teethCount++;

		for (var i = 0; i < 3; i++)
			_lipsBuffer[i] = _lipsBuffer[i + 1];
		_lipsBuffer[3] = lipsRaw;
		decimal? lips = null;
		if (_lipsCount >= 3)
			lips = _lipsBuffer[0];
		else
			_lipsCount++;

		if (jaw is null || teeth is null || lips is null)
			return;

		var ao = aoFast - aoSlow;
		var diff = ao - _prevAo;
		_prevAo = ao;

		for (var i = 0; i < 4; i++)
		{
			_highs[i] = _highs[i + 1];
			_lows[i] = _lows[i + 1];
		}
		_highs[4] = candle.HighPrice;
		_lows[4] = candle.LowPrice;

		decimal? upFractal = null;
		decimal? downFractal = null;

		if (_highs[2] is decimal h2 &&
			_highs[0] is decimal h0 && _highs[1] is decimal h1 &&
			_highs[3] is decimal h3 && _highs[4] is decimal h4 &&
			h2 > h0 && h2 > h1 && h2 > h3 && h2 > h4)
			upFractal = h2;

		if (_lows[2] is decimal l2 &&
			_lows[0] is decimal l0 && _lows[1] is decimal l1 &&
			_lows[3] is decimal l3 && _lows[4] is decimal l4 &&
			l2 < l0 && l2 < l1 && l2 < l3 && l2 < l4)
			downFractal = l2;

		if (upFractal is decimal up)
			_upFractalLevel = up;
		if (downFractal is decimal down)
			_downFractalLevel = down;

		if (_upFractalLevel is decimal upLevel && upLevel > teeth)
			_upFractalActivation = upLevel;
		if (_downFractalLevel is decimal downLevel && downLevel < teeth)
			_downFractalActivation = downLevel;

		if (_upFractalActivation is decimal actUp && candle.HighPrice > actUp)
		{
			_trend = 1;
			_upFractalActivation = null;
			_downFractalActivation = _downFractalLevel;
		}

		if (_downFractalActivation is decimal actDown && candle.LowPrice < actDown)
		{
			_trend = -1;
			_downFractalActivation = null;
			_upFractalActivation = _upFractalLevel;
		}

		if (_trend == 1)
			_upFractalActivation = null;
		else if (_trend == -1)
			_downFractalActivation = null;

		for (var i = 0; i < 6; i++)
			_lowBuffer[i] = _lowBuffer[i + 1];
		_lowBuffer[6] = candle.LowPrice;
		if (_lowCount < 6)
			_lowCount++;

		var lowest = _lowBuffer[0];
		for (var i = 1; i < 7; i++)
			if (_lowBuffer[i] < lowest)
				lowest = _lowBuffer[i];
		var bullishBar = _lowCount >= 6 && candle.ClosePrice > (candle.HighPrice + candle.LowPrice) / 2m && lowest == candle.LowPrice;

		var volume = candle.TotalVolume;
		var mfi = volume > 0 ? (candle.HighPrice - candle.LowPrice) / volume : 0m;
		var squat = mfi < _prevMfi && volume > _prevVolume;
		_prevMfi = mfi;
		_prevVolume = volume;
		_squat[0] = _squat[1];
		_squat[1] = _squat[2];
		_squat[2] = squat;
		var squatRecent = _squat[0] || _squat[1] || _squat[2];

		var isTrue = bullishBar && candle.HighPrice < jaw && candle.HighPrice < teeth && candle.HighPrice < lips;
		if (EnableAo && diff >= 0)
			isTrue = false;
		if (EnableMfi && !squatRecent)
			isTrue = false;

		if (isTrue)
		{
			_confirmation = candle.HighPrice;
			_invalidation = candle.LowPrice;
		}

		if (_confirmation is decimal confirm)
		{
			if (candle.LowPrice < _invalidation)
			{
				_confirmation = null;
			}
			else if (candle.HighPrice > confirm && Position <= 0)
			{
				var volumeToBuy = Volume + Math.Abs(Position);
				BuyMarket(volumeToBuy);
				_stopLoss = _invalidation;
				_confirmation = null;
			}
		}

		if (_stopLoss is decimal sl && Position > 0 && candle.LowPrice < sl)
		{
			SellMarket(Position);
			_stopLoss = null;
		}

		if (Position > 0 && _prevTrend == 1 && _trend == -1)
		{
			SellMarket(Position);
			_stopLoss = null;
		}

		_prevTrend = _trend;
	}
}
