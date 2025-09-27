using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that recreates the RenkoLiveChart_v3.2 expert by building synthetic Renko bricks from candle history and live ticks.
/// The strategy does not place orders; it focuses on generating Renko blocks for visualization or downstream processing.
/// </summary>
public class RenkoLiveChartStrategy : Strategy
{
	private readonly StrategyParam<int> _brickSizeSteps;
	private readonly StrategyParam<int> _brickOffsetSteps;
	private readonly StrategyParam<bool> _useWicks;
	private readonly StrategyParam<bool> _emulateLineChart;
	private readonly StrategyParam<bool> _useShortSymbolName;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _boxSize;
	private decimal _offset;
	private decimal _priceTolerance;

	private decimal _prevLow;
	private decimal _prevHigh;
	private decimal _prevOpen;
	private decimal _prevClose;

	private decimal _currentOpen;
	private decimal _currentClose;
	private decimal _currentVolume;
	private decimal _upWick;
	private decimal _downWick;

	private bool _initialized;
	private DateTimeOffset _prevTime;
	private string _seriesName = string.Empty;

	/// <summary>
	/// Size of a Renko brick expressed in price steps of the security.
	/// </summary>
	public int BrickSizeSteps
	{
		get => _brickSizeSteps.Value;
		set => _brickSizeSteps.Value = value;
	}

	/// <summary>
	/// Offset applied to the first Renko brick in price steps.
	/// </summary>
	public int BrickOffsetSteps
	{
		get => _brickOffsetSteps.Value;
		set => _brickOffsetSteps.Value = value;
	}

	/// <summary>
	/// Enables wick handling so highs and lows extend beyond brick bodies when price overshoots.
	/// </summary>
	public bool UseWicks
	{
		get => _useWicks.Value;
		set => _useWicks.Value = value;
	}

	/// <summary>
	/// When enabled the strategy continuously logs the state of the active brick to emulate the live chart behaviour.
	/// </summary>
	public bool EmulateLineChart
	{
		get => _emulateLineChart.Value;
		set => _emulateLineChart.Value = value;
	}

	/// <summary>
	/// Uses a shortened six-character symbol alias to mirror the original "StrangeSymbolName" option.
	/// </summary>
	public bool UseShortSymbolName
	{
		get => _useShortSymbolName.Value;
		set => _useShortSymbolName.Value = value;
	}

	/// <summary>
	/// Candle type that seeds the historical Renko calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="RenkoLiveChartStrategy"/> class.
	/// </summary>
	public RenkoLiveChartStrategy()
	{
		_brickSizeSteps = Param(nameof(BrickSizeSteps), 10)
		.SetGreaterThanZero()
		.SetDisplay("Brick Size", "Renko brick height in price steps", "Renko")
		.SetCanOptimize(true)
		.SetOptimize(5, 40, 1);

		_brickOffsetSteps = Param(nameof(BrickOffsetSteps), 0)
		.SetDisplay("Brick Offset", "Offset applied to the first brick (steps)", "Renko");

		_useWicks = Param(nameof(UseWicks), true)
		.SetDisplay("Use Wicks", "Extend bricks with wicks when price overshoots", "Renko");

		_emulateLineChart = Param(nameof(EmulateLineChart), true)
		.SetDisplay("Emulate Line Chart", "Log active brick updates in real time", "Visualization");

		_useShortSymbolName = Param(nameof(UseShortSymbolName), false)
		.SetDisplay("Short Symbol", "Trim the symbol name to six characters", "Visualization");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Candles used to seed the Renko history", "Data");
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

		_initialized = false;
		_prevLow = 0m;
		_prevHigh = 0m;
		_prevOpen = 0m;
		_prevClose = 0m;
		_currentOpen = 0m;
		_currentClose = 0m;
		_currentVolume = 0m;
		_upWick = 0m;
		_downWick = decimal.MaxValue;
		_prevTime = default;
		_seriesName = string.Empty;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
		step = 0.0001m;

		_boxSize = BrickSizeSteps * step;
		_offset = BrickOffsetSteps * step;
		_priceTolerance = Math.Max(step / 2m, step / 10m);

		if (_boxSize <= 0m)
		{
			LogError("Brick size must be greater than zero.");
			Stop();
			// Abort the strategy if the brick definition is invalid.
			return;
		}

		if (Math.Abs(_offset) >= _boxSize)
		{
			LogError("Absolute brick offset must be smaller than the brick size.");
			Stop();
			// Enforce the same offset constraint as the MQL expert.
			return;
		}

		_upWick = 0m;
		_downWick = decimal.MaxValue;
		_currentVolume = 0m;
		_initialized = false;
		_seriesName = BuildSeriesName();
		// Subscribe to historical candles and live trades just like the original MT4 script did.

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();

		SubscribeTicks()
		.Bind(ProcessTrade)
		.Start();

		LogInfo($"{_seriesName}: Renko conversion started with brick size {_boxSize} and offset {_offset}.");
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		// Ignore unfinished candles because Renko updates rely on completed ranges.
		return;

		EnsureInitialized(candle.ClosePrice, candle.CloseTime);
		// The very first candle sets the initial Renko grid anchors.

		var volume = candle.TotalVolume ?? 0m;

		_currentVolume += volume;
		_upWick = Math.Max(_upWick, candle.HighPrice);
		_downWick = Math.Min(_downWick, candle.LowPrice);

		var upTrend = candle.HighPrice + candle.LowPrice > _prevHigh + _prevLow;
		// Copy the MQL heuristic that decides whether to process lows or highs first.

		while (upTrend && ShouldFormDownBrick(candle.LowPrice))
		{
			FormDownBrick(candle.CloseTime);
		}

		while (ShouldFormUpBrick(candle.HighPrice))
		{
			FormUpBrick(candle.CloseTime);
		}

		while (!upTrend && ShouldFormDownBrick(candle.LowPrice))
		{
			FormDownBrick(candle.CloseTime);
		}

		UpdateActiveBrick(candle.ClosePrice, candle.CloseTime);
	}

	private void ProcessTrade(ITickTradeMessage trade)
	{
		var price = trade.Price;

		var time = trade.ServerTime;
		EnsureInitialized(price, time);
		// Live ticks also trigger the initial anchor so the strategy can run without history.

		var volume = trade.TradeVolume ?? 1m;
		// Approximate tick volume when the exchange does not report real values.

		_currentVolume += volume;
		_upWick = Math.Max(_upWick, price);
		_downWick = Math.Min(_downWick, price);

		if (ShouldFormUpBrick(price))
		{
			FormUpBrick(time);
		}
		else if (ShouldFormDownBrick(price))
		{
			FormDownBrick(time);
		}
		else
		{
			UpdateActiveBrick(price, time);
		}
	}

	private void EnsureInitialized(decimal price, DateTimeOffset time)
	{
		if (_initialized)
		return;

		var anchor = _offset + Math.Floor(price / _boxSize) * _boxSize;
		// Align the first brick with the configured offset and the nearest Renko grid level.

		while (price < anchor - _priceTolerance)
		// Slide the anchor down until the current price fits inside the brick.
		{
			anchor -= _boxSize;
		}

		while (price > anchor + _boxSize + _priceTolerance)
		// Slide the anchor up when the offset pushed the level below the market price.
		{
			anchor += _boxSize;
		}

		_prevLow = anchor;
		_prevHigh = anchor + _boxSize;
		_prevOpen = _prevLow;
		_prevClose = _prevHigh;
		_currentOpen = price;
		_currentClose = price;
		_currentVolume = 0m;
		_upWick = Math.Max(_prevHigh, price);
		_downWick = Math.Min(_prevLow, price);
		_prevTime = time;
		_initialized = true;

		LogInfo($"{_seriesName}: initial brick seeded between {_prevLow} and {_prevHigh} using price {price}.");
	}

	private bool ShouldFormUpBrick(decimal price)
	{
		return price > _prevHigh + _priceTolerance;
	}

	private bool ShouldFormDownBrick(decimal price)
	{
		return price < _prevLow - _priceTolerance;
	}

	private void FormUpBrick(DateTimeOffset time)
	{
		_prevHigh += _boxSize;
		_prevLow += _boxSize;
		_prevOpen = _prevLow;
		_prevClose = _prevHigh;

		var low = UseWicks && _downWick < _prevLow ? _downWick : _prevLow;
	// Preserve the lower shadow when wick processing is enabled.
		var high = _prevHigh;
		var close = _prevClose;
		var open = _prevOpen;

		time = EnsureMonotonicTime(time);
		EmitBrick(time, open, high, low, close, _currentVolume, true);

		_upWick = 0m;
		_downWick = decimal.MaxValue;
		_currentVolume = 0m;
		_currentOpen = close;
		_currentClose = close;
	}

	private void FormDownBrick(DateTimeOffset time)
	{
		_prevHigh -= _boxSize;
		_prevLow -= _boxSize;
		_prevOpen = _prevHigh;
		_prevClose = _prevLow;

		var high = UseWicks && _upWick > _prevHigh ? _upWick : _prevHigh;
	// Preserve the upper shadow to mirror the MT4 ShowWicks flag.
		var low = _prevLow;
		var close = _prevClose;
		var open = _prevOpen;

		time = EnsureMonotonicTime(time);
		EmitBrick(time, open, high, low, close, _currentVolume, false);

		_upWick = 0m;
		_downWick = decimal.MaxValue;
		_currentVolume = 0m;
		_currentOpen = close;
		_currentClose = close;
	}

	private DateTimeOffset EnsureMonotonicTime(DateTimeOffset time)
	{
		if (time <= _prevTime)
		time = _prevTime + TimeSpan.FromMilliseconds(1);

		_prevTime = time;
		return time;
	}

	private void EmitBrick(DateTimeOffset time, decimal open, decimal high, decimal low, decimal close, decimal volume, bool isUp)
	{
		var direction = isUp ? "up" : "down";
		// Log every finished brick so users can compare the sequence with the MT4 offline chart.
		LogInfo($"{_seriesName}: {direction} brick open={open}, high={high}, low={low}, close={close}, volume={volume}, time={time:O}.");
	}

	private void UpdateActiveBrick(decimal price, DateTimeOffset time)
	{
		_currentOpen = price >= _prevHigh
		? _prevHigh
		: price <= _prevLow
		? _prevLow
		: price;

		_currentClose = price;

		var high = UseWicks && _upWick > _prevHigh ? _upWick : _prevHigh;
		var low = UseWicks && _downWick < _prevLow ? _downWick : _prevLow;
		// Keep wick extremes while the brick is still forming.

		if (EmulateLineChart)
		{
			LogDebug($"{_seriesName}: active brick update open={_currentOpen}, high={high}, low={low}, close={_currentClose}, volume={_currentVolume}, time={time:O}.");
		// Emulate the continuous refresh that the MT4 offline chart provided.
		}
	}

	private string BuildSeriesName()
	{
		var id = Security?.Id ?? "Unknown";
		var alias = UseShortSymbolName && id.Length > 6 ? id[..6] : id;
		return $"Renko-{alias}";
	}
}
