using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that switches moving average pairs based on trend state.
/// Opens long on fast MA crossing above slow MA and closes on reverse cross.
/// </summary>
public class StateAwareMaCrossStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _baseMaLength;
	private readonly StrategyParam<int> _s00ShortLength;
	private readonly StrategyParam<int> _s00LongLength;
	private readonly StrategyParam<int> _s01ShortLength;
	private readonly StrategyParam<int> _s01LongLength;
	private readonly StrategyParam<int> _s10ShortLength;
	private readonly StrategyParam<int> _s10LongLength;
	private readonly StrategyParam<int> _s11ShortLength;
	private readonly StrategyParam<int> _s11LongLength;

	private ExponentialMovingAverage _baseMa;
	private ExponentialMovingAverage _s00Short;
	private HullMovingAverage _s00Long;
	private SimpleMovingAverage _s01Short;
	private SmoothedMovingAverage _s01Long;
	private SmoothedMovingAverage _s10Short;
	private HullMovingAverage _s10Long;
	private SmoothedMovingAverage _s11Short;
	private SmoothedMovingAverage _s11Long;

	private decimal _prevBaseMa;
	private readonly decimal[] _prevShort = new decimal[4];
	private readonly decimal[] _prevLong = new decimal[4];
	private readonly bool[] _wasShortBelowLong = new bool[4];
	private readonly bool[] _isStateInitialized = new bool[4];

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Base EMA period.
	/// </summary>
	public int BaseMaLength
	{
		get => _baseMaLength.Value;
		set => _baseMaLength.Value = value;
	}

	/// <summary>
	/// State 00 fast EMA length.
	/// </summary>
	public int S00ShortLength
	{
		get => _s00ShortLength.Value;
		set => _s00ShortLength.Value = value;
	}

	/// <summary>
	/// State 00 slow HMA length.
	/// </summary>
	public int S00LongLength
	{
		get => _s00LongLength.Value;
		set => _s00LongLength.Value = value;
	}

	/// <summary>
	/// State 01 fast SMA length.
	/// </summary>
	public int S01ShortLength
	{
		get => _s01ShortLength.Value;
		set => _s01ShortLength.Value = value;
	}

	/// <summary>
	/// State 01 slow RMA length.
	/// </summary>
	public int S01LongLength
	{
		get => _s01LongLength.Value;
		set => _s01LongLength.Value = value;
	}

	/// <summary>
	/// State 10 fast RMA length.
	/// </summary>
	public int S10ShortLength
	{
		get => _s10ShortLength.Value;
		set => _s10ShortLength.Value = value;
	}

	/// <summary>
	/// State 10 slow HMA length.
	/// </summary>
	public int S10LongLength
	{
		get => _s10LongLength.Value;
		set => _s10LongLength.Value = value;
	}

	/// <summary>
	/// State 11 fast RMA length.
	/// </summary>
	public int S11ShortLength
	{
		get => _s11ShortLength.Value;
		set => _s11ShortLength.Value = value;
	}

	/// <summary>
	/// State 11 slow RMA length.
	/// </summary>
	public int S11LongLength
	{
		get => _s11LongLength.Value;
		set => _s11LongLength.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public StateAwareMaCrossStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");

		_baseMaLength = Param(nameof(BaseMaLength), 20)
		.SetGreaterThanZero()
		.SetDisplay("Base EMA Length", "Period for base EMA", "Base MA");

		_s00ShortLength = Param(nameof(S00ShortLength), 15)
		.SetGreaterThanZero()
		.SetDisplay("State 00 Fast", "EMA period for state 00", "State 00");

		_s00LongLength = Param(nameof(S00LongLength), 24)
		.SetGreaterThanZero()
		.SetDisplay("State 00 Slow", "HMA period for state 00", "State 00");

		_s01ShortLength = Param(nameof(S01ShortLength), 19)
		.SetGreaterThanZero()
		.SetDisplay("State 01 Fast", "SMA period for state 01", "State 01");

		_s01LongLength = Param(nameof(S01LongLength), 45)
		.SetGreaterThanZero()
		.SetDisplay("State 01 Slow", "RMA period for state 01", "State 01");

		_s10ShortLength = Param(nameof(S10ShortLength), 16)
		.SetGreaterThanZero()
		.SetDisplay("State 10 Fast", "RMA period for state 10", "State 10");

		_s10LongLength = Param(nameof(S10LongLength), 59)
		.SetGreaterThanZero()
		.SetDisplay("State 10 Slow", "HMA period for state 10", "State 10");

		_s11ShortLength = Param(nameof(S11ShortLength), 12)
		.SetGreaterThanZero()
		.SetDisplay("State 11 Fast", "RMA period for state 11", "State 11");

		_s11LongLength = Param(nameof(S11LongLength), 36)
		.SetGreaterThanZero()
		.SetDisplay("State 11 Slow", "RMA period for state 11", "State 11");
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

		_prevBaseMa = default;
		Array.Clear(_prevShort, 0, _prevShort.Length);
		Array.Clear(_prevLong, 0, _prevLong.Length);
		Array.Clear(_wasShortBelowLong, 0, _wasShortBelowLong.Length);
		Array.Clear(_isStateInitialized, 0, _isStateInitialized.Length);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_baseMa = new ExponentialMovingAverage { Length = BaseMaLength };
		_s00Short = new ExponentialMovingAverage { Length = S00ShortLength };
		_s00Long = new HullMovingAverage { Length = S00LongLength };
		_s01Short = new SimpleMovingAverage { Length = S01ShortLength };
		_s01Long = new SmoothedMovingAverage { Length = S01LongLength };
		_s10Short = new SmoothedMovingAverage { Length = S10ShortLength };
		_s10Long = new HullMovingAverage { Length = S10LongLength };
		_s11Short = new SmoothedMovingAverage { Length = S11ShortLength };
		_s11Long = new SmoothedMovingAverage { Length = S11LongLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx([
		_baseMa,
		_s00Short, _s00Long,
		_s01Short, _s01Long,
		_s10Short, _s10Long,
		_s11Short, _s11Long
		], ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _baseMa);
			DrawIndicator(area, _s00Short);
			DrawIndicator(area, _s00Long);
			DrawIndicator(area, _s01Short);
			DrawIndicator(area, _s01Long);
			DrawIndicator(area, _s10Short);
			DrawIndicator(area, _s10Long);
			DrawIndicator(area, _s11Short);
			DrawIndicator(area, _s11Long);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue[] values)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (values[0].ToNullableDecimal() is not decimal baseValue)
		return;

		var slopeUp = baseValue > _prevBaseMa;
		var aboveBase = candle.ClosePrice > baseValue;
		var state = (slopeUp ? 2 : 0) + (aboveBase ? 1 : 0);

		var shortIndex = 1 + state * 2;
		var longIndex = shortIndex + 1;

		if (values[shortIndex].ToNullableDecimal() is not decimal shortVal ||
		values[longIndex].ToNullableDecimal() is not decimal longVal)
		{
			_prevBaseMa = baseValue;
			return;
		}

		if (!_isStateInitialized[state])
		{
			_prevShort[state] = shortVal;
			_prevLong[state] = longVal;
			_wasShortBelowLong[state] = shortVal < longVal;
			_isStateInitialized[state] = true;
			_prevBaseMa = baseValue;
			return;
		}

		var isShortBelowLong = shortVal < longVal;

		if (_wasShortBelowLong[state] && !isShortBelowLong && Position <= 0)
		{
			RegisterBuy();
		}
		else if (!_wasShortBelowLong[state] && isShortBelowLong && Position > 0)
		{
			ClosePosition();
		}

		_prevShort[state] = shortVal;
		_prevLong[state] = longVal;
		_wasShortBelowLong[state] = isShortBelowLong;
		_prevBaseMa = baseValue;
	}
}
