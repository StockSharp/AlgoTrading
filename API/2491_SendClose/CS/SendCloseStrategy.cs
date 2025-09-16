using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// SendClose strategy replicates fractal breakout lines with close-based exits.
/// This class recreates the MT5 SendClose expert using StockSharp high level API.
/// </summary>
public class SendCloseStrategy : Strategy
{
	private enum FractalType
	{
		Up,
		Down
	}

	private readonly struct FractalPoint
	{
		public FractalPoint(FractalType type, DateTimeOffset time, decimal price)
		{
			Type = type;
			Time = time;
			Price = price;
		}

		public FractalType Type { get; }
		public DateTimeOffset Time { get; }
		public decimal Price { get; }
	}

	private readonly struct FractalLine
	{
		public FractalLine(FractalPoint recent, FractalPoint older)
		{
			if (recent.Time < older.Time)
			{
				Recent = older;
				Older = recent;
			}
			else
			{
				Recent = recent;
				Older = older;
			}
		}

		public FractalPoint Recent { get; }
		public FractalPoint Older { get; }

		public decimal GetPrice(DateTimeOffset time)
		{
			var totalSeconds = (decimal)(Recent.Time - Older.Time).TotalSeconds;
			if (totalSeconds == 0m)
				return Recent.Price;

			var offsetSeconds = (decimal)(time - Older.Time).TotalSeconds;
			return Older.Price + (Recent.Price - Older.Price) * (offsetSeconds / totalSeconds);
		}
	}

	private readonly StrategyParam<bool> _enableSellLine;
	private readonly StrategyParam<bool> _enableBuyLine;
	private readonly StrategyParam<bool> _enableCloseSellLine;
	private readonly StrategyParam<bool> _enableCloseBuyLine;
	private readonly StrategyParam<int> _maxPositions;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _lineOffsetSteps;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _h0;
	private decimal _h1;
	private decimal _h2;
	private decimal _h3;
	private decimal _h4;

	private decimal _l0;
	private decimal _l1;
	private decimal _l2;
	private decimal _l3;
	private decimal _l4;

	private DateTimeOffset _t0;
	private DateTimeOffset _t1;
	private DateTimeOffset _t2;
	private DateTimeOffset _t3;
	private DateTimeOffset _t4;

	private int _bufferCount;

	private FractalPoint? _fractal0;
	private FractalPoint? _fractal1;
	private FractalPoint? _fractal2;
	private FractalPoint? _fractal3;
	private FractalPoint? _fractal4;
	private FractalPoint? _fractal5;

	private FractalLine? _sellLine;
	private FractalLine? _buyLine;

	/// <summary>
	/// Enable sell breakout line.
	/// </summary>
	public bool EnableSellLine
	{
		get => _enableSellLine.Value;
		set => _enableSellLine.Value = value;
	}

	/// <summary>
	/// Enable buy breakout line.
	/// </summary>
	public bool EnableBuyLine
	{
		get => _enableBuyLine.Value;
		set => _enableBuyLine.Value = value;
	}

	/// <summary>
	/// Enable upper close line (based on sell trend line).
	/// </summary>
	public bool EnableCloseSellLine
	{
		get => _enableCloseSellLine.Value;
		set => _enableCloseSellLine.Value = value;
	}

	/// <summary>
	/// Enable lower close line (based on buy trend line).
	/// </summary>
	public bool EnableCloseBuyLine
	{
		get => _enableCloseBuyLine.Value;
		set => _enableCloseBuyLine.Value = value;
	}

	/// <summary>
	/// Maximum number of lots that can remain open.
	/// </summary>
	public int MaxPositions
	{
		get => _maxPositions.Value;
		set => _maxPositions.Value = value;
	}

	/// <summary>
	/// Order volume per entry.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Offset in price steps for close lines.
	/// </summary>
	public int LineOffsetSteps
	{
		get => _lineOffsetSteps.Value;
		set => _lineOffsetSteps.Value = value;
	}

	/// <summary>
	/// Candle type used for analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="SendCloseStrategy"/>.
	/// </summary>
	public SendCloseStrategy()
	{
		_enableSellLine = Param(nameof(EnableSellLine), true)
			.SetDisplay("Sell Line", "Enable sell fractal breakout line", "General");

		_enableBuyLine = Param(nameof(EnableBuyLine), true)
			.SetDisplay("Buy Line", "Enable buy fractal breakout line", "General");

		_enableCloseSellLine = Param(nameof(EnableCloseSellLine), true)
			.SetDisplay("Close Line 1", "Enable closing line above sell trend", "General");

		_enableCloseBuyLine = Param(nameof(EnableCloseBuyLine), true)
			.SetDisplay("Close Line 2", "Enable closing line below buy trend", "General");

		_maxPositions = Param(nameof(MaxPositions), 1)
			.SetGreaterThanZero()
			.SetDisplay("Max Positions", "Maximum number of simultaneous lots", "Risk");

		_orderVolume = Param(nameof(OrderVolume), 0.10m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume per signal", "Risk");

		_lineOffsetSteps = Param(nameof(LineOffsetSteps), 15)
			.SetGreaterThanZero()
			.SetDisplay("Offset Steps", "Offset in price steps for close levels", "Execution");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles used for calculations", "General");
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

		// Clear buffers that hold recent highs, lows, and times.
		_h0 = _h1 = _h2 = _h3 = _h4 = 0m;
		_l0 = _l1 = _l2 = _l3 = _l4 = 0m;
		_t0 = _t1 = _t2 = _t3 = _t4 = default;
		_bufferCount = 0;

		// Reset stored fractal points and active lines.
		_fractal0 = _fractal1 = _fractal2 = _fractal3 = _fractal4 = _fractal5 = null;
		_sellLine = null;
		_buyLine = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Subscribe to candle data and process each completed candle.
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Work only with completed candles to match the MT5 expert behaviour.
		if (candle.State != CandleStates.Finished)
			return;

		// Update internal buffers and detect new fractal points.
		UpdateBuffers(candle);
		UpdateFractalLines();

		// Ensure trading is allowed before evaluating signals.
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var offset = GetOffset();
		var shouldClose = false;

		// Check closing logic derived from the upper fractal trend line.
		if (EnableCloseSellLine && _sellLine is { } sellLine)
		{
			var closePrice = GetLinePrice(sellLine, candle.CloseTime) + offset;
			if (IsTouched(closePrice, candle))
				shouldClose = true;
		}

		// Check closing logic derived from the lower fractal trend line.
		if (EnableCloseBuyLine && _buyLine is { } buyLine)
		{
			var closePrice = GetLinePrice(buyLine, candle.CloseTime) - offset;
			if (IsTouched(closePrice, candle))
				shouldClose = true;
		}

		// Close any open position if price reached one of the close lines.
		if (shouldClose && Position != 0m)
		{
			ClosePosition();
			return;
		}

		// Entry logic for sell breakout.
		if (EnableSellLine && _sellLine is { } sellEntryLine)
		{
			var sellPrice = GetLinePrice(sellEntryLine, candle.CloseTime);
			if (IsTouched(sellPrice, candle))
			{
				if (Position > 0m)
				{
					// Flatten long positions before attempting to go short.
					ClosePosition();
				}
				else if (CanIncreaseShort())
				{
					SellMarket(OrderVolume);
				}
			}
		}

		// Entry logic for buy breakout.
		if (EnableBuyLine && _buyLine is { } buyEntryLine)
		{
			var buyPrice = GetLinePrice(buyEntryLine, candle.CloseTime);
			if (IsTouched(buyPrice, candle))
			{
				if (Position < 0m)
				{
					// Flatten short positions before attempting to go long.
					ClosePosition();
				}
				else if (CanIncreaseLong())
				{
					BuyMarket(OrderVolume);
				}
			}
		}
	}

	private void UpdateBuffers(ICandleMessage candle)
	{
		// Shift buffers to keep the latest five candles for fractal detection.
		_h4 = _h3;
		_h3 = _h2;
		_h2 = _h1;
		_h1 = _h0;
		_h0 = candle.HighPrice;

		_l4 = _l3;
		_l3 = _l2;
		_l2 = _l1;
		_l1 = _l0;
		_l0 = candle.LowPrice;

		_t4 = _t3;
		_t3 = _t2;
		_t2 = _t1;
		_t1 = _t0;
		_t0 = candle.OpenTime;

		if (_bufferCount < 5)
		{
			_bufferCount++;
			return;
		}

		// Identify new fractal points once enough candles are available.
		if (IsUpFractal())
			RegisterFractal(new FractalPoint(FractalType.Up, _t2, _h2));

		if (IsDownFractal())
			RegisterFractal(new FractalPoint(FractalType.Down, _t2, _l2));
	}

	private void UpdateFractalLines()
	{
		// Build the sell line using the most recent up-down-up pattern.
		if (TryBuildLine(FractalType.Up, out var sellLine))
			_sellLine = sellLine;

		// Build the buy line using the most recent down-up-down pattern.
		if (TryBuildLine(FractalType.Down, out var buyLine))
			_buyLine = buyLine;
	}

	private bool IsUpFractal()
	{
		return _h2 >= _h3 && _h2 > _h4 && _h2 >= _h1 && _h2 > _h0;
	}

	private bool IsDownFractal()
	{
		return _l2 <= _l3 && _l2 < _l4 && _l2 <= _l1 && _l2 < _l0;
	}

	private void RegisterFractal(FractalPoint point)
	{
		// Skip duplicates that can appear on flat sequences.
		if (_fractal0 is { } latest && latest.Time == point.Time && latest.Type == point.Type)
			return;

		_fractal5 = _fractal4;
		_fractal4 = _fractal3;
		_fractal3 = _fractal2;
		_fractal2 = _fractal1;
		_fractal1 = _fractal0;
		_fractal0 = point;
	}

	private bool TryBuildLine(FractalType target, out FractalLine line)
	{
		line = default;
		FractalPoint? latest = null;
		FractalPoint? middle = null;
		FractalPoint? oldest = null;

		foreach (var item in EnumerateFractals())
		{
			if (item is not { } point)
				continue;

			if (latest is null)
			{
				if (point.Type == target)
					latest = point;
				continue;
			}

			if (middle is null)
			{
				if (point.Type != target)
					middle = point;
				continue;
			}

			if (point.Type == target)
			{
				oldest = point;
				break;
			}
		}

		if (latest is not { } latestPoint || middle is null || oldest is not { } oldestPoint)
			return false;

		if (latestPoint.Time == oldestPoint.Time)
			return false;

		line = new FractalLine(latestPoint, oldestPoint);
		return true;
	}

	private IEnumerable<FractalPoint?> EnumerateFractals()
	{
		yield return _fractal0;
		yield return _fractal1;
		yield return _fractal2;
		yield return _fractal3;
		yield return _fractal4;
		yield return _fractal5;
	}

	private bool CanIncreaseShort()
	{
		if (OrderVolume <= 0m || MaxPositions <= 0)
			return false;

		var lots = OrderVolume == 0m ? 0m : Math.Abs(Position) / OrderVolume;
		return lots < MaxPositions;
	}

	private bool CanIncreaseLong()
	{
		if (OrderVolume <= 0m || MaxPositions <= 0)
			return false;

		var lots = OrderVolume == 0m ? 0m : Math.Abs(Position) / OrderVolume;
		return lots < MaxPositions;
	}

	private decimal GetOffset()
	{
		var step = Security?.PriceStep ?? 1m;
		return step * LineOffsetSteps;
	}

	private static bool IsTouched(decimal price, ICandleMessage candle)
	{
		return price <= candle.HighPrice && price >= candle.LowPrice;
	}

	private static decimal GetLinePrice(FractalLine line, DateTimeOffset time)
	{
		return line.GetPrice(time);
	}
}
