namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

public class ExtrapolatedPivotConnectorStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _pivotLength;
	private readonly StrategyParam<int> _highStart;
	private readonly StrategyParam<int> _highEnd;
	private readonly StrategyParam<int> _lowStart;
	private readonly StrategyParam<int> _lowEnd;

	private readonly List<ICandleMessage> _candles = [];
	private readonly List<(int index, decimal price)> _highPivots = [];
	private readonly List<(int index, decimal price)> _lowPivots = [];

	private int _barIndex;
	private decimal? _prevClose;
	private decimal? _prevResY;
	private decimal? _prevSupY;

	public ExtrapolatedPivotConnectorStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles to use", "General");

		_pivotLength = Param(nameof(PivotLength), 100)
		.SetGreaterThanZero()
		.SetDisplay("Pivot Length", "Bars left/right for pivot", "General");

		_highStart = Param(nameof(HighStart), 1)
		.SetDisplay("A-High Position", "First pivot high index", "General");

		_highEnd = Param(nameof(HighEnd), 0)
		.SetDisplay("B-High Position", "Second pivot high index", "General");

		_lowStart = Param(nameof(LowStart), 1)
		.SetDisplay("A-Low Position", "First pivot low index", "General");

		_lowEnd = Param(nameof(LowEnd), 0)
		.SetDisplay("B-Low Position", "Second pivot low index", "General");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int PivotLength
	{
		get => _pivotLength.Value;
		set => _pivotLength.Value = value;
	}

	public int HighStart
	{
		get => _highStart.Value;
		set => _highStart.Value = value;
	}

	public int HighEnd
	{
		get => _highEnd.Value;
		set => _highEnd.Value = value;
	}

	public int LowStart
	{
		get => _lowStart.Value;
		set => _lowStart.Value = value;
	}

	public int LowEnd
	{
		get => _lowEnd.Value;
		set => _lowEnd.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_candles.Clear();
		_highPivots.Clear();
		_lowPivots.Clear();

		_barIndex = default;
		_prevClose = null;
		_prevResY = null;
		_prevSupY = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		SubscribeCandles(CandleType)
		.Bind(ProcessCandle)
		.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_barIndex++;

		_candles.Add(candle);
		var maxCount = PivotLength * 2 + 1;

		if (_candles.Count > maxCount)
		_candles.RemoveAt(0);

		if (_candles.Count == maxCount)
		{
		var pivotIndex = PivotLength;
		var pivotCandle = _candles[pivotIndex];

		var isHigh = true;
		var isLow = true;

		for (var i = 0; i < maxCount; i++)
		{
		if (i == pivotIndex)
		continue;

		var c = _candles[i];

		if (c.High >= pivotCandle.High)
		isHigh = false;

		if (c.Low <= pivotCandle.Low)
		isLow = false;
		}

		var pivotBarIndex = _barIndex - PivotLength - 1;

		if (isHigh)
		{
		_highPivots.Add((pivotBarIndex, pivotCandle.High));

		if (_highPivots.Count > 100)
		_highPivots.RemoveAt(0);
		}

		if (isLow)
		{
		_lowPivots.Add((pivotBarIndex, pivotCandle.Low));

		if (_lowPivots.Count > 100)
		_lowPivots.RemoveAt(0);
		}
		}

		decimal? resY = null;

		if (_highPivots.Count > HighStart && _highPivots.Count > HighEnd)
		{
		var idx1 = _highPivots.Count - 1 - HighStart;
		var idx2 = _highPivots.Count - 1 - HighEnd;
		var p1 = _highPivots[idx1];
		var p2 = _highPivots[idx2];

		if (p1.index != p2.index)
		{
		var m = (p2.price - p1.price) / (p2.index - p1.index);
		var b = p1.price - m * p1.index;
		resY = m * _barIndex + b;
		}
		}

		decimal? supY = null;

		if (_lowPivots.Count > LowStart && _lowPivots.Count > LowEnd)
		{
		var idx1 = _lowPivots.Count - 1 - LowStart;
		var idx2 = _lowPivots.Count - 1 - LowEnd;
		var p1 = _lowPivots[idx1];
		var p2 = _lowPivots[idx2];

		if (p1.index != p2.index)
		{
		var m = (p2.price - p1.price) / (p2.index - p1.index);
		var b = p1.price - m * p1.index;
		supY = m * _barIndex + b;
		}
		}

		if (IsFormedAndOnlineAndAllowTrading())
		{
		if (_prevClose is decimal pc)
		{
		if (resY is decimal r && _prevResY is decimal pr && pc <= pr && candle.ClosePrice > r && Position <= 0)
		{
		BuyMarket();
		}
		else if (supY is decimal s && _prevSupY is decimal ps && pc >= ps && candle.ClosePrice < s && Position >= 0)
		{
		SellMarket();
		}
		}
		}

		_prevClose = candle.ClosePrice;
		_prevResY = resY;
		_prevSupY = supY;
	}
}

