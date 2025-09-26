using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the "SendCloseOrder" MetaTrader 4 expert advisor.
/// The strategy rebuilds fractal-based trendlines and trades their touch events.
/// </summary>
public class SendCloseOrderStrategy : Strategy
{
	private const decimal CloseOffsetPoints = 15m;
	private const decimal VolumeTolerance = 0.0000001m;
	private const int MaxStoredFractals = 64;
	private const decimal DefaultPriceStep = 0.0001m;

	private readonly StrategyParam<bool> _enableSellLine;
	private readonly StrategyParam<bool> _enableBuyLine;
	private readonly StrategyParam<bool> _enableCloseLongLine;
	private readonly StrategyParam<bool> _enableCloseShortLine;
	private readonly StrategyParam<int> _maxOrders;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<FractalPoint> _fractals = new();

	private LineInfo _sellLine;
	private LineInfo _buyLine;
	private LineInfo _closeLongLine;
	private LineInfo _closeShortLine;

	private decimal _h1;
	private decimal _h2;
	private decimal _h3;
	private decimal _h4;
	private decimal _h5;
	private decimal _l1;
	private decimal _l2;
	private decimal _l3;
	private decimal _l4;
	private decimal _l5;
	private DateTimeOffset _t1;
	private DateTimeOffset _t2;
	private DateTimeOffset _t3;
	private DateTimeOffset _t4;
	private DateTimeOffset _t5;

	private enum FractalKind
	{
		Up,
		Down,
	}

	private enum LineType
	{
		Sell,
		Buy,
		CloseLong,
		CloseShort,
	}

	private sealed class FractalPoint
	{
		public FractalPoint(FractalKind type, DateTimeOffset time, decimal price)
		{
			Type = type;
			Time = time;
			Price = price;
		}

		public FractalKind Type { get; }

		public DateTimeOffset Time { get; }

		public decimal Price { get; set; }
	}

	private sealed class LineInfo
	{
		public LineInfo(LineType type, DateTimeOffset firstTime, decimal firstPrice, DateTimeOffset secondTime, decimal secondPrice)
		{
			Type = type;
			FirstTime = firstTime;
			FirstPrice = firstPrice;
			SecondTime = secondTime;
			SecondPrice = secondPrice;
		}

		public LineType Type { get; }

		public DateTimeOffset FirstTime { get; }

		public decimal FirstPrice { get; }

		public DateTimeOffset SecondTime { get; }

		public decimal SecondPrice { get; }
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SendCloseOrderStrategy"/> class.
	/// </summary>
	public SendCloseOrderStrategy()
	{
		_enableSellLine = Param(nameof(EnableSellLine), true)
			.SetDisplay("Enable Sell Line", "Allow sell signals generated from the resistance trendline.", "Signals");

		_enableBuyLine = Param(nameof(EnableBuyLine), true)
			.SetDisplay("Enable Buy Line", "Allow buy signals generated from the support trendline.", "Signals");

		_enableCloseLongLine = Param(nameof(EnableCloseLongLine), true)
			.SetDisplay("Enable Close #1", "Allow closing positions when the upper exit line is touched.", "Signals");

		_enableCloseShortLine = Param(nameof(EnableCloseShortLine), true)
			.SetDisplay("Enable Close #2", "Allow closing positions when the lower exit line is touched.", "Signals");

		_maxOrders = Param(nameof(MaxOrders), 1)
			.SetGreaterThanZero()
			.SetDisplay("Max Orders", "Maximum stacked entries allowed at the same time.", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(1, 5, 1);

		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Volume per individual entry.", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle series used for fractal detection.", "Data");
	}

	/// <summary>
	/// Enables sell entries triggered by the resistance line.
	/// </summary>
	public bool EnableSellLine
	{
		get => _enableSellLine.Value;
		set => _enableSellLine.Value = value;
	}

	/// <summary>
	/// Enables buy entries triggered by the support line.
	/// </summary>
	public bool EnableBuyLine
	{
		get => _enableBuyLine.Value;
		set => _enableBuyLine.Value = value;
	}

	/// <summary>
	/// Enables the upper exit line that closes long positions.
	/// </summary>
	public bool EnableCloseLongLine
	{
		get => _enableCloseLongLine.Value;
		set => _enableCloseLongLine.Value = value;
	}

	/// <summary>
	/// Enables the lower exit line that closes short positions.
	/// </summary>
	public bool EnableCloseShortLine
	{
		get => _enableCloseShortLine.Value;
		set => _enableCloseShortLine.Value = value;
	}

	/// <summary>
	/// Maximum number of stacked entries allowed simultaneously.
	/// </summary>
	public int MaxOrders
	{
		get => _maxOrders.Value;
		set => _maxOrders.Value = value;
	}

	/// <summary>
	/// Volume of a single trade when opening a new position.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Candle type used to build the fractal structure.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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

		_fractals.Clear();
		_sellLine = null;
		_buyLine = null;
		_closeLongLine = null;
		_closeShortLine = null;

		_h1 = _h2 = _h3 = _h4 = _h5 = 0m;
		_l1 = _l2 = _l3 = _l4 = _l5 = 0m;
		_t1 = _t2 = _t3 = _t4 = _t5 = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Shift the five-candle window used for Bill Williams fractals.
		ShiftWindow(candle);
		DetectFractals();

		var priceStep = GetPriceStep();
		UpdateLines(priceStep);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var signal = GetTriggeredLine(candle, priceStep);
		if (signal is null)
			return;

		if (signal == LineType.CloseLong || signal == LineType.CloseShort)
		{
			if (Position != 0m)
			{
				// Flatten any existing position when a close line is touched.
				ClosePosition();
			}

			return;
		}

		if (TradeVolume <= 0m)
			return;

		var maxVolume = MaxOrders * TradeVolume;
		if (maxVolume <= 0m)
			return;

		var absPosition = Math.Abs(Position);

		if (signal == LineType.Buy)
		{
			var hasShort = Position < 0m;
			var hasCapacity = Position >= 0m && absPosition + TradeVolume <= maxVolume + VolumeTolerance;

			if (!hasShort && !hasCapacity)
				return;

			var volume = TradeVolume + (hasShort ? absPosition : 0m);
			if (volume > 0m)
			{
				// Offset shorts first, then stack the requested long entry.
				BuyMarket(volume);
			}
		}
		else if (signal == LineType.Sell)
		{
			var hasLong = Position > 0m;
			var hasCapacity = Position <= 0m && absPosition + TradeVolume <= maxVolume + VolumeTolerance;

			if (!hasLong && !hasCapacity)
				return;

			var volume = TradeVolume + (hasLong ? absPosition : 0m);
			if (volume > 0m)
			{
				// Offset longs first, then stack the requested short entry.
				SellMarket(volume);
			}
		}
	}

	private void ShiftWindow(ICandleMessage candle)
	{
		_h1 = _h2;
		_h2 = _h3;
		_h3 = _h4;
		_h4 = _h5;
		_h5 = candle.HighPrice;

		_l1 = _l2;
		_l2 = _l3;
		_l3 = _l4;
		_l4 = _l5;
		_l5 = candle.LowPrice;

		_t1 = _t2;
		_t2 = _t3;
		_t3 = _t4;
		_t4 = _t5;
		_t5 = candle.OpenTime;
	}

	private void DetectFractals()
	{
		if (_t1 == default || _t2 == default || _t3 == default || _t4 == default || _t5 == default)
			return;

		// Bill Williams fractal requires the middle candle to dominate both sides.
		var upFractal = _h3 >= _h2 && _h3 > _h1 && _h3 >= _h4 && _h3 > _h5;
		if (upFractal)
			RegisterFractal(FractalKind.Up, _t3, _h3);

		var downFractal = _l3 <= _l2 && _l3 < _l1 && _l3 <= _l4 && _l3 < _l5;
		if (downFractal)
			RegisterFractal(FractalKind.Down, _t3, _l3);
	}

	private void RegisterFractal(FractalKind kind, DateTimeOffset time, decimal price)
	{
		if (_fractals.Count > 0)
		{
			var last = _fractals[^1];
			if (last.Time == time && last.Type == kind)
			{
				// Update the price if the same candle produced another evaluation.
				last.Price = price;
				return;
			}
		}

		_fractals.Add(new FractalPoint(kind, time, price));

		if (_fractals.Count > MaxStoredFractals)
			_fractals.RemoveAt(0);
	}

	private void UpdateLines(decimal priceStep)
	{
		if (EnableSellLine || EnableCloseLongLine)
		{
			if (TryBuildLine(FractalKind.Up, FractalKind.Down, out var first, out var last))
			{
				_sellLine = EnableSellLine ? new LineInfo(LineType.Sell, first.Time, first.Price, last.Time, last.Price) : null;

				var offset = CloseOffsetPoints * priceStep;
				_closeLongLine = EnableCloseLongLine
					? new LineInfo(LineType.CloseLong, first.Time, first.Price + offset, last.Time, last.Price + offset)
					: null;
			}
			else
			{
				_sellLine = null;
				_closeLongLine = null;
			}
		}
		else
		{
			_sellLine = null;
			_closeLongLine = null;
		}

		if (EnableBuyLine || EnableCloseShortLine)
		{
			if (TryBuildLine(FractalKind.Down, FractalKind.Up, out var first, out var last))
			{
				_buyLine = EnableBuyLine ? new LineInfo(LineType.Buy, first.Time, first.Price, last.Time, last.Price) : null;

				var offset = CloseOffsetPoints * priceStep;
				_closeShortLine = EnableCloseShortLine
					? new LineInfo(LineType.CloseShort, first.Time, first.Price - offset, last.Time, last.Price - offset)
					: null;
			}
			else
			{
				_buyLine = null;
				_closeShortLine = null;
			}
		}
		else
		{
			_buyLine = null;
			_closeShortLine = null;
		}
	}

	private bool TryBuildLine(FractalKind outerKind, FractalKind middleKind, out FractalPoint first, out FractalPoint last)
	{
		first = default!;
		last = default!;

		for (var i = _fractals.Count - 1; i >= 0; i--)
		{
			var candidateLast = _fractals[i];
			if (candidateLast.Type != outerKind)
				continue;

			var middleIndex = i - 1;
			while (middleIndex >= 0 && _fractals[middleIndex].Type != middleKind)
				middleIndex--;

			if (middleIndex < 0)
				continue;

			var firstIndex = middleIndex - 1;
			while (firstIndex >= 0 && _fractals[firstIndex].Type != outerKind)
				firstIndex--;

			if (firstIndex < 0)
				continue;

			var candidateFirst = _fractals[firstIndex];

			if (candidateFirst.Time >= candidateLast.Time)
				continue;

			first = candidateFirst;
			last = candidateLast;
			return true;
		}

		return false;
	}

	private LineType? GetTriggeredLine(ICandleMessage candle, decimal priceStep)
	{
		var evaluationTime = candle.CloseTime != default ? candle.CloseTime : candle.OpenTime;
		var tolerance = priceStep * 2m;
		if (tolerance <= 0m)
			tolerance = priceStep > 0m ? priceStep : DefaultPriceStep;

		foreach (var line in EnumerateLinesByPriority())
		{
			if (line is null)
				continue;

			var projectedPrice = ProjectPrice(line, evaluationTime);
			if (projectedPrice is null)
				continue;

			var price = projectedPrice.Value;

			// Treat the touch as valid when the candle's range envelops the projected price.
			if (price >= candle.LowPrice - tolerance && price <= candle.HighPrice + tolerance)
				return line.Type;
		}

		return null;
	}

	private IEnumerable<LineInfo> EnumerateLinesByPriority()
	{
		yield return _closeLongLine;
		yield return _closeShortLine;
		yield return _sellLine;
		yield return _buyLine;
	}

	private decimal? ProjectPrice(LineInfo line, DateTimeOffset time)
	{
		var start = line.FirstTime;
		var end = line.SecondTime;

		var totalTicks = end.Ticks - start.Ticks;
		if (totalTicks == 0)
			return null;

		var offsetTicks = time.Ticks - start.Ticks;
		var slope = (line.SecondPrice - line.FirstPrice) / totalTicks;
		return line.FirstPrice + slope * offsetTicks;
	}

	private decimal GetPriceStep()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			step = DefaultPriceStep;

		return step;
	}
}
