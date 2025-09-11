
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simple trendlines drawing strategy.
/// Automatically calculates slope and builds a trendline between two points.
/// </summary>
public class SimpleTrendlinesStrategy : Strategy
{
	private readonly StrategyParam<int> _xAxis;
	private readonly StrategyParam<int> _offset;
	private readonly StrategyParam<bool> _strictMode;
	private readonly StrategyParam<int> _strictType;
	private readonly StrategyParam<DataType> _candleType;

	private Trendline _trendline;
	private readonly Queue<decimal> _closes = new();

	/// <summary>
	/// X axis distance between points.
	/// </summary>
	public int XAxis
	{
		get => _xAxis.Value;
		set => _xAxis.Value = value;
	}

	/// <summary>
	/// Offset from the current bar index.
	/// </summary>
	public int Offset
	{
		get => _offset.Value;
		set => _offset.Value = value;
	}

	/// <summary>
	/// Enable strict mode validation.
	/// </summary>
	public bool StrictMode
	{
		get => _strictMode.Value;
		set => _strictMode.Value = value;
	}

	/// <summary>
	/// Strict type. 0 - price above line, 1 - price below line.
	/// </summary>
	public int StrictType
	{
		get => _strictType.Value;
		set => _strictType.Value = value;
	}

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="SimpleTrendlinesStrategy"/>.
	/// </summary>
	public SimpleTrendlinesStrategy()
	{
		_xAxis = Param(nameof(XAxis), 20)
			.SetGreaterThanZero()
			.SetDisplay("X Axis", "Distance between points", "General")
			.SetCanOptimize(true);

		_offset = Param(nameof(Offset), 0)
			.SetDisplay("Offset", "Bars offset from current index", "General")
			.SetCanOptimize(true);

		_strictMode = Param(nameof(StrictMode), false)
			.SetDisplay("Strict Mode", "Enable strict validation", "General");

		_strictType = Param(nameof(StrictType), 0)
			.SetDisplay("Strict Type", "0 - price above, 1 - price below", "General")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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

		_trendline = default;
		_closes.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_trendline = new Trendline(XAxis, Offset, StrictMode, StrictType);

		var subscription = SubscribeCandles(CandleType);
		subscription.WhenNew(ProcessCandle).Start();

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

		_closes.Enqueue(candle.ClosePrice);
		if (_closes.Count > Offset + 1)
		_closes.Dequeue();

		_trendline.DrawLine(true, candle.LowPrice, candle.HighPrice, _closes);
		_trendline.DrawTrendline(true);
	}

	private sealed class Trendline
	{
		private readonly int _xAxis;
		private readonly int _offset;
		private readonly bool _strictMode;
		private readonly int _strictType;

		private decimal? _slope;
		private decimal? _y1;
		private decimal? _y2;
		private int _changeInX;

		public Trendline(int xAxis, int offset, bool strictMode, int strictType)
		{
			_xAxis = xAxis;
			_offset = offset;
			_strictMode = strictMode;
			_strictType = strictType;
		}

		public void DrawLine(bool condition, decimal y1, decimal y2, IEnumerable<decimal> src)
		{
			decimal? savedSlope = null;
			decimal? savedY1 = null;
			decimal? savedY2 = null;

			if (condition && (!_strictMode))
			{
				savedSlope = (y2 - y1) / _xAxis;
				savedY1 = y1;
				savedY2 = y2;
			}
			else if (condition && _strictMode)
			{
				var slope = (y2 - y1) / _xAxis;
				var list = new List<decimal>(src);
				var valid = true;

				if (list.Count >= _offset + 1)
				{
					for (var i = 0; i <= _offset; i++)
					{
					var j = _offset - i;
					var value = list[list.Count - 1 - j];
					var check = y2 + slope * i;

					if (_strictType == 0 ? value >= check : value <= check)
					continue;

					valid = false;
					break;
					}

					if (valid)
					{
					savedSlope = slope;
					savedY1 = y1;
					savedY2 = y2;
					}
				}
			}

			if (savedSlope != null)
			{
				_slope = savedSlope;
				_y1 = savedY1;
				_y2 = savedY2;
				_changeInX = _offset;
			}
		}

		public void DrawTrendline(bool condition)
		{
			if (condition && _slope != null)
			_changeInX++;
		}
	}
}
