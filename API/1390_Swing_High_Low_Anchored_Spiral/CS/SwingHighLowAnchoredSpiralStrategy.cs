using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Draws a spiral anchored at the most recent swing high or low.
/// </summary>
public class SwingHighLowAnchoredSpiralStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<int> _turns;
	private readonly StrategyParam<bool> _anchorHigh;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _flipH;
	private readonly StrategyParam<bool> _flipV;

	private decimal[] _highBuffer = Array.Empty<decimal>();
	private decimal[] _lowBuffer = Array.Empty<decimal>();
	private int _bufferCount;
	private DateTimeOffset? _anchorTime;
	private decimal _anchorValue;
	private DateTimeOffset _prevTime;
	private decimal _prevValue;

	/// <summary>
	/// Pivot length.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Number of turns.
	/// </summary>
	public int Turns
	{
		get => _turns.Value;
		set => _turns.Value = value;
	}

	/// <summary>
	/// Anchor on swing high.
	/// </summary>
	public bool AnchorHigh
	{
		get => _anchorHigh.Value;
		set => _anchorHigh.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Flip horizontally.
	/// </summary>
	public bool FlipH
	{
		get => _flipH.Value;
		set => _flipH.Value = value;
	}

	/// <summary>
	/// Flip vertically.
	/// </summary>
	public bool FlipV
	{
		get => _flipV.Value;
		set => _flipV.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="SwingHighLowAnchoredSpiralStrategy"/>.
	/// </summary>
	public SwingHighLowAnchoredSpiralStrategy()
	{
		_length = Param(nameof(Length), 14)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Pivot length", "Swing");

		_turns = Param(nameof(Turns), 2)
			.SetGreaterThanZero()
			.SetDisplay("Turns", "Spiral turns", "Spiral");

		_anchorHigh = Param(nameof(AnchorHigh), true)
			.SetDisplay("Anchor High", "Use swing high", "Swing");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");

		_flipH = Param(nameof(FlipH), false)
			.SetDisplay("Flip Horizontal", "Mirror horizontally", "Spiral");

		_flipV = Param(nameof(FlipV), false)
			.SetDisplay("Flip Vertical", "Mirror vertically", "Spiral");
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

		_highBuffer = Array.Empty<decimal>();
		_lowBuffer = Array.Empty<decimal>();
		_bufferCount = 0;
		_anchorTime = null;
		_anchorValue = 0m;
		_prevTime = default;
		_prevValue = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnline())
			return;

		UpdateBuffers(candle);

		var tf = (TimeSpan)CandleType.Arg;

		if (_bufferCount == _highBuffer.Length)
		{
			var pivotIndex = Length;
			var ph = _highBuffer[pivotIndex];
			var pl = _lowBuffer[pivotIndex];
			var isHigh = true;
			var isLow = true;

			for (var i = 0; i < _highBuffer.Length; i++)
			{
				if (i == pivotIndex)
					continue;

				if (ph <= _highBuffer[i])
					isHigh = false;

				if (pl >= _lowBuffer[i])
					isLow = false;
			}

			if (AnchorHigh && isHigh)
			{
				_anchorTime = candle.OpenTime - tf * (Length + 1);
				_anchorValue = ph;
				_prevTime = _anchorTime.Value;
				_prevValue = _anchorValue;
			}
			else if (!AnchorHigh && isLow)
			{
				_anchorTime = candle.OpenTime - tf * (Length + 1);
				_anchorValue = pl;
				_prevTime = _anchorTime.Value;
				_prevValue = _anchorValue;
			}
		}

		if (_anchorTime is null)
			return;

		var bars = (int)((candle.OpenTime - _anchorTime.Value).Ticks / tf.Ticks);
		if (bars <= 0)
			return;

		var angle = 2m * (decimal)Math.PI * bars / Math.Max(1, Turns * Length);
		var flipH = FlipH ? -1m : 1m;
		var flipV = FlipV ? -1m : 1m;
		var radius = (candle.ClosePrice - _anchorValue) * bars / Math.Max(1, Length);
		var y = _anchorValue + flipV * radius * (decimal)Math.Sin((double)angle) * flipH;
		var time = candle.OpenTime;

		DrawLine(_prevTime, _prevValue, time, y);
		_prevTime = time;
		_prevValue = y;
	}

	private void UpdateBuffers(ICandleMessage candle)
	{
		var size = Length * 2 + 1;

		if (_highBuffer.Length != size)
		{
			_highBuffer = new decimal[size];
			_lowBuffer = new decimal[size];
			_bufferCount = 0;
		}

		if (_bufferCount < size)
		{
			_highBuffer[_bufferCount] = candle.HighPrice;
			_lowBuffer[_bufferCount] = candle.LowPrice;
			_bufferCount++;
		}
		else
		{
			for (var i = 0; i < size - 1; i++)
			{
				_highBuffer[i] = _highBuffer[i + 1];
				_lowBuffer[i] = _lowBuffer[i + 1];
			}
			_highBuffer[^1] = candle.HighPrice;
			_lowBuffer[^1] = candle.LowPrice;
		}
	}
}
