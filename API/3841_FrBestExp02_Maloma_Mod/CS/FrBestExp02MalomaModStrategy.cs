using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// FrBestExp02 Maloma Mod strategy translated from MetaTrader.
/// Combines OsMA momentum with volume confirmation and a daily pivot filter.
/// Trades against recent fractal extremes when volume and oscillator conditions align.
/// </summary>
public class FrBestExp02MalomaModStrategy : Strategy
{
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<int> _trailingStopPoints;
	private readonly StrategyParam<decimal> _volumeThreshold;
	private readonly StrategyParam<int> _osmaFastPeriod;
	private readonly StrategyParam<int> _osmaSlowPeriod;
	private readonly StrategyParam<int> _osmaSignalPeriod;
	private readonly StrategyParam<int> _pivotWindow;
	private readonly StrategyParam<int> _minTradeIntervalSeconds;
	private readonly StrategyParam<DataType> _candleType;

	private MACD _macd = null!;

	private readonly ICandleMessage[] _history = new ICandleMessage[6];
	private int _historyCount;

	private decimal[] _highWindow = Array.Empty<decimal>();
	private decimal[] _lowWindow = Array.Empty<decimal>();
	private decimal[] _closeWindow = Array.Empty<decimal>();
	private int _windowIndex;
	private int _windowCount;
	private decimal _pivotPoint;
	private bool _pivotReady;

	private decimal? _previousOsma;
	private decimal? _previousVolume;
	private decimal? _previousPreviousVolume;
	private DateTimeOffset? _lastTradeTime;
	private decimal _pipSize;

	/// <summary>
	/// Initializes a new instance of the <see cref="FrBestExp02MalomaModStrategy"/> class.
	/// </summary>
	public FrBestExp02MalomaModStrategy()
	{
		_volume = Param(nameof(Volume), 1m)
			.SetDisplay("Volume", "Order volume used for new positions.", "Trading")
			.SetCanOptimize(true)
			.SetGreaterThanZero();

		_stopLossPoints = Param(nameof(StopLossPoints), 1000)
			.SetDisplay("Stop Loss (points)", "Protective stop distance expressed in points.", "Risk")
			.SetCanOptimize(true)
			.SetGreaterThanZero();

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 1000)
			.SetDisplay("Take Profit (points)", "Profit target distance expressed in points.", "Risk")
			.SetCanOptimize(true)
			.SetGreaterThanZero();

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 0)
			.SetDisplay("Trailing Stop (points)", "Trailing stop distance in points. Zero disables trailing.", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0, 1500, 50);

		_volumeThreshold = Param(nameof(VolumeThreshold), 50m)
			.SetDisplay("Volume Threshold", "Minimum previous candle volume required for signals.", "Filters")
			.SetCanOptimize(true)
			.SetGreaterThanZero();

		_osmaFastPeriod = Param(nameof(OsmaFastPeriod), 12)
			.SetDisplay("OsMA Fast Period", "Fast EMA period used by the MACD histogram.", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 1)
			.SetGreaterThanZero();

		_osmaSlowPeriod = Param(nameof(OsmaSlowPeriod), 26)
			.SetDisplay("OsMA Slow Period", "Slow EMA period used by the MACD histogram.", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 60, 1)
			.SetGreaterThanZero();

		_osmaSignalPeriod = Param(nameof(OsmaSignalPeriod), 9)
			.SetDisplay("OsMA Signal Period", "Signal line period for the MACD histogram.", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(3, 20, 1)
			.SetGreaterThanZero();

		_pivotWindow = Param(nameof(PivotWindow), 96)
			.SetDisplay("Pivot Window", "Number of finished candles used to build the session pivot.", "Filters")
			.SetCanOptimize(true)
			.SetOptimize(32, 144, 16)
			.SetGreaterThanZero();

		_minTradeIntervalSeconds = Param(nameof(MinTradeIntervalSeconds), 20)
			.SetDisplay("Min Trade Interval (sec)", "Minimum number of seconds between new entries.", "Filters")
			.SetCanOptimize(true)
			.SetOptimize(0, 1800, 60);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe used by the strategy.", "Data");
	}

	/// <summary>
	/// Trading volume per order.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in points.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance in points.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in points.
	/// </summary>
	public int TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Required volume on the previous candle.
	/// </summary>
	public decimal VolumeThreshold
	{
		get => _volumeThreshold.Value;
		set => _volumeThreshold.Value = value;
	}

	/// <summary>
	/// Fast EMA period used in the OsMA calculation.
	/// </summary>
	public int OsmaFastPeriod
	{
		get => _osmaFastPeriod.Value;
		set => _osmaFastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period used in the OsMA calculation.
	/// </summary>
	public int OsmaSlowPeriod
	{
		get => _osmaSlowPeriod.Value;
		set => _osmaSlowPeriod.Value = value;
	}

	/// <summary>
	/// Signal line period used in the OsMA calculation.
	/// </summary>
	public int OsmaSignalPeriod
	{
		get => _osmaSignalPeriod.Value;
		set => _osmaSignalPeriod.Value = value;
	}

	/// <summary>
	/// Number of finished candles included in the pivot computation.
	/// </summary>
	public int PivotWindow
	{
		get => _pivotWindow.Value;
		set => _pivotWindow.Value = value;
	}

	/// <summary>
	/// Minimum time between consecutive entries.
	/// </summary>
	public int MinTradeIntervalSeconds
	{
		get => _minTradeIntervalSeconds.Value;
		set => _minTradeIntervalSeconds.Value = value;
	}

	/// <summary>
	/// Candle type used for signal generation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override System.Collections.Generic.IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return new[] { (Security, CandleType) };
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		Array.Clear(_history, 0, _history.Length);
		_historyCount = 0;

		_highWindow = Array.Empty<decimal>();
		_lowWindow = Array.Empty<decimal>();
		_closeWindow = Array.Empty<decimal>();
		_windowIndex = 0;
		_windowCount = 0;
		_pivotPoint = 0m;
		_pivotReady = false;

		_previousOsma = null;
		_previousVolume = null;
		_previousPreviousVolume = null;
		_lastTradeTime = null;
		_pipSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_macd = new MACD
		{
			ShortPeriod = OsmaFastPeriod,
			LongPeriod = OsmaSlowPeriod,
			SignalPeriod = OsmaSignalPeriod
		};

		EnsurePivotCapacity();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_macd, ProcessCandle)
			.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal macdLine, decimal macdSignal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateHistory(candle);
		UpdatePivotData(candle);

		var osmaCurrent = macdLine - macdSignal;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			StoreState(candle, osmaCurrent);
			return;
		}

		if (!_pivotReady || !_previousOsma.HasValue || !_previousVolume.HasValue || !_previousPreviousVolume.HasValue)
		{
			StoreState(candle, osmaCurrent);
			return;
		}

		var previousOsma = _previousOsma.Value;
		var prevVolume = _previousVolume.Value;
		var prevPrevVolume = _previousPreviousVolume.Value;

		var hasLowFractal = HasLowFractal();
		var hasHighFractal = HasHighFractal();

		var sellSignal = hasLowFractal &&
			prevVolume > VolumeThreshold &&
			prevVolume > prevPrevVolume &&
			previousOsma > osmaCurrent &&
			previousOsma <= 0m &&
			osmaCurrent < 0m &&
			candle.ClosePrice > _pivotPoint;

		var buySignal = hasHighFractal &&
			prevVolume > VolumeThreshold &&
			prevVolume > prevPrevVolume &&
			previousOsma < osmaCurrent &&
			previousOsma >= 0m &&
			osmaCurrent > 0m &&
			candle.ClosePrice < _pivotPoint;

		var interval = MinTradeIntervalSeconds > 0 ? TimeSpan.FromSeconds(MinTradeIntervalSeconds) : TimeSpan.Zero;
		var canTradeByTime = interval == TimeSpan.Zero ||
			!_lastTradeTime.HasValue ||
			DateTimeOffset.Compare(candle.CloseTime, _lastTradeTime.Value + interval) >= 0;

		if (sellSignal && canTradeByTime && Position >= 0m && Volume > 0m)
		{
			EnterShort(candle.ClosePrice);
			_lastTradeTime = candle.CloseTime;
		}
		else if (buySignal && canTradeByTime && Position <= 0m && Volume > 0m)
		{
			EnterLong(candle.ClosePrice);
			_lastTradeTime = candle.CloseTime;
		}
		else
		{
			UpdateTrailing(candle.ClosePrice);
		}

		StoreState(candle, osmaCurrent);
	}

	private void EnterLong(decimal price)
	{
		var closingVolume = Position < 0m ? Math.Abs(Position) : 0m;
		var openingVolume = Volume;
		var totalVolume = closingVolume + openingVolume;
		if (totalVolume <= 0m)
			return;

		var resultingPosition = Position + totalVolume;
		BuyMarket(totalVolume);

		ApplyProtection(price, resultingPosition);
	}

	private void EnterShort(decimal price)
	{
		var closingVolume = Position > 0m ? Position : 0m;
		var openingVolume = Volume;
		var totalVolume = closingVolume + openingVolume;
		if (totalVolume <= 0m)
			return;

		var resultingPosition = Position - totalVolume;
		SellMarket(totalVolume);

		ApplyProtection(price, resultingPosition);
	}

	private void ApplyProtection(decimal referencePrice, decimal resultingPosition)
	{
		var stopDistance = ConvertPointsToPrice(StopLossPoints);
		var takeDistance = ConvertPointsToPrice(TakeProfitPoints);

		if (stopDistance > 0m)
			SetStopLoss(stopDistance, referencePrice, resultingPosition);

		if (takeDistance > 0m)
			SetTakeProfit(takeDistance, referencePrice, resultingPosition);
	}

	private void UpdateTrailing(decimal currentPrice)
	{
		var trailingDistance = ConvertPointsToPrice(TrailingStopPoints);
		if (trailingDistance <= 0m || Position == 0m)
			return;

		SetStopLoss(trailingDistance, currentPrice, Position);
	}

	private void StoreState(ICandleMessage candle, decimal osmaCurrent)
	{
		_previousPreviousVolume = _previousVolume;
		_previousVolume = candle.TotalVolume ?? candle.Volume ?? 0m;
		_previousOsma = osmaCurrent;
	}

	private void UpdateHistory(ICandleMessage candle)
	{
		for (var i = Math.Min(_historyCount, _history.Length - 1); i > 0; i--)
			_history[i] = _history[i - 1];

		_history[0] = candle;

		if (_historyCount < _history.Length)
			_historyCount++;
	}

	private bool HasHighFractal()
	{
		if (_historyCount < 6)
			return false;

		var c1 = _history[1];
		var c2 = _history[2];
		var c3 = _history[3];
		var c4 = _history[4];
		var c5 = _history[5];

		if (c1 is null || c2 is null || c3 is null || c4 is null || c5 is null)
			return false;

		return c3.HighPrice > c4.HighPrice &&
			c3.HighPrice > c5.HighPrice &&
			c3.HighPrice > c2.HighPrice &&
			c3.HighPrice > c1.HighPrice;
	}

	private bool HasLowFractal()
	{
		if (_historyCount < 6)
			return false;

		var c1 = _history[1];
		var c2 = _history[2];
		var c3 = _history[3];
		var c4 = _history[4];
		var c5 = _history[5];

		if (c1 is null || c2 is null || c3 is null || c4 is null || c5 is null)
			return false;

		return c3.LowPrice < c4.LowPrice &&
			c3.LowPrice < c5.LowPrice &&
			c3.LowPrice < c2.LowPrice &&
			c3.LowPrice < c1.LowPrice;
	}

	private void EnsurePivotCapacity()
	{
		var window = PivotWindow;
		if (window <= 0)
		{
			_highWindow = Array.Empty<decimal>();
			_lowWindow = Array.Empty<decimal>();
			_closeWindow = Array.Empty<decimal>();
			_windowIndex = 0;
			_windowCount = 0;
			_pivotReady = false;
			return;
		}

		if (_highWindow.Length == window)
			return;

		_highWindow = new decimal[window];
		_lowWindow = new decimal[window];
		_closeWindow = new decimal[window];
		_windowIndex = 0;
		_windowCount = 0;
		_pivotReady = false;
	}

	private void UpdatePivotData(ICandleMessage candle)
	{
		var window = PivotWindow;
		if (window <= 0)
		{
			_pivotReady = false;
			return;
		}

		if (_highWindow.Length != window)
			EnsurePivotCapacity();

		_highWindow[_windowIndex] = candle.HighPrice;
		_lowWindow[_windowIndex] = candle.LowPrice;
		_closeWindow[_windowIndex] = candle.ClosePrice;

		if (_windowCount < window)
			_windowCount++;

		_windowIndex++;
		if (_windowIndex >= window)
			_windowIndex = 0;

		if (_windowCount < window)
		{
			_pivotReady = false;
			return;
		}

		var highest = _highWindow[0];
		var lowest = _lowWindow[0];
		for (var i = 1; i < window; i++)
		{
			var high = _highWindow[i];
			if (high > highest)
				highest = high;

			var low = _lowWindow[i];
			if (low < lowest)
				lowest = low;
		}

		var oldestIndex = _windowIndex;
		var pivotClose = _closeWindow[oldestIndex];
		_pivotPoint = (highest + lowest + pivotClose) / 3m;
		_pivotReady = true;
	}

	private decimal ConvertPointsToPrice(int points)
	{
		if (points <= 0)
			return 0m;

		var pip = _pipSize > 0m ? _pipSize : (_pipSize = CalculatePipSize());
		if (pip <= 0m)
			return 0m;

		return pip * points;
	}

	private decimal CalculatePipSize()
	{
		var security = Security;
		if (security is null)
			return 0m;

		var step = security.PriceStep ?? 0m;
		if (step == 0m)
			return 0m;

		var decimals = security.Decimals;
		return decimals == 3 || decimals == 5 ? step * 10m : step;
	}
}
