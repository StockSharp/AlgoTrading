namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Strategy based on the Ozymandias indicator.
/// Opens a position when the indicator changes its direction and closes the opposite one.
/// </summary>
public class OzymandiasStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<DataType> _candleType;

	private int _trend;
	private int _nextTrend;
	private decimal _maxl;
	private decimal _minh;
	private decimal _baseLine;
	private decimal _prevHigh;
	private decimal _prevLow;
	private int? _prevDirection;
	private static readonly object _sync = new();

	// Rolling window for high prices (for highest calculation)
	private readonly Queue<decimal> _highWindow = new();
	// Rolling window for low prices (for lowest calculation)
	private readonly Queue<decimal> _lowWindow = new();
	// Rolling window for hh values (for SMA of highest)
	private readonly Queue<decimal> _hhQueue = new();
	private decimal _hhSum;
	// Rolling window for ll values (for SMA of lowest)
	private readonly Queue<decimal> _llQueue = new();
	private decimal _llSum;
	// ATR manual calculation
	private readonly Queue<decimal> _trQueue = new();
	private decimal _trSum;
	private decimal _prevClose;
	private bool _hasPrevClose;
	private int _candleCount;

	/// <summary>
	/// Lookback length for calculations.
	/// </summary>
	public int Length { get => _length.Value; set => _length.Value = value; }

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initialize the strategy parameters.
	/// </summary>
	public OzymandiasStrategy()
	{
		_length = Param(nameof(Length), 8)
			.SetDisplay("Length", "Lookback period", "Indicator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for candles", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		lock (_sync)
		{
			_trend = 0;
			_nextTrend = 0;
			_maxl = 0m;
			_minh = decimal.MaxValue;
			_baseLine = 0m;
			_prevHigh = 0m;
			_prevLow = 0m;
			_prevDirection = null;
			_candleCount = 0;
			_highWindow.Clear();
			_lowWindow.Clear();
			_hhQueue.Clear();
			_hhSum = 0m;
			_llQueue.Clear();
			_llSum = 0m;
			_trQueue.Clear();
			_trSum = 0m;
			_prevClose = 0m;
			_hasPrevClose = false;
		}
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

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
		if (candle.State != CandleStates.Finished)
			return;

		lock (_sync)
		{
			var high = candle.HighPrice;
			var low = candle.LowPrice;
			var close = candle.ClosePrice;
			var len = Length;
			var atrLen = 14;

			_candleCount++;

			_highWindow.Enqueue(high);
			if (_highWindow.Count > len)
				_highWindow.Dequeue();

			_lowWindow.Enqueue(low);
			if (_lowWindow.Count > len)
				_lowWindow.Dequeue();

			decimal tr;
			if (_hasPrevClose)
				tr = Math.Max(high - low, Math.Max(Math.Abs(high - _prevClose), Math.Abs(low - _prevClose)));
			else
				tr = high - low;

			_trQueue.Enqueue(tr);
			_trSum += tr;
			if (_trQueue.Count > atrLen)
				_trSum -= _trQueue.Dequeue();

			_prevClose = close;
			_hasPrevClose = true;

			if (_highWindow.Count < len)
				return;

			var hh = GetMax(_highWindow);
			var ll = GetMin(_lowWindow);

			_hhQueue.Enqueue(hh);
			_hhSum += hh;
			if (_hhQueue.Count > len)
				_hhSum -= _hhQueue.Dequeue();

			_llQueue.Enqueue(ll);
			_llSum += ll;
			if (_llQueue.Count > len)
				_llSum -= _llQueue.Dequeue();

			if (_hhQueue.Count < len || _trQueue.Count < atrLen)
				return;

			var hma = _hhSum / _hhQueue.Count;
			var lma = _llSum / _llQueue.Count;

			if (_prevHigh == 0m && _prevLow == 0m)
			{
				_prevHigh = high;
				_prevLow = low;
				_baseLine = close;
				return;
			}

			var trend0 = _trend;

			if (_nextTrend == 1)
			{
				_maxl = Math.Max(ll, _maxl);
				if (hma < _maxl && close < _prevLow)
				{
					trend0 = 1;
					_nextTrend = 0;
					_minh = hh;
				}
			}

			if (_nextTrend == 0)
			{
				_minh = Math.Min(hh, _minh);
				if (lma > _minh && close > _prevHigh)
				{
					trend0 = 0;
					_nextTrend = 1;
					_maxl = ll;
				}
			}

			int direction;
			if (trend0 == 0)
			{
				_baseLine = _trend != 0 ? _baseLine : Math.Max(_maxl, _baseLine);
				direction = 1;
			}
			else
			{
				_baseLine = _trend != 1 ? _baseLine : Math.Min(_minh, _baseLine);
				direction = 0;
			}

			if (_prevDirection is int prevDir && direction != prevDir)
			{
				if (direction == 1 && Position <= 0)
					BuyMarket(Volume + Math.Abs(Position));
				else if (direction == 0 && Position >= 0)
					SellMarket(Volume + Math.Abs(Position));
			}

			_prevDirection = direction;
			_trend = trend0;
			_prevHigh = high;
			_prevLow = low;
		}
	}

	private static decimal GetMax(IEnumerable<decimal> values)
	{
		var hasValue = false;
		var result = 0m;

		foreach (var value in values)
		{
			if (!hasValue || value > result)
			{
				result = value;
				hasValue = true;
			}
		}

		return result;
	}

	private static decimal GetMin(IEnumerable<decimal> values)
	{
		var hasValue = false;
		var result = 0m;

		foreach (var value in values)
		{
			if (!hasValue || value < result)
			{
				result = value;
				hasValue = true;
			}
		}

		return result;
	}
}
