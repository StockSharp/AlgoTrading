using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that emulates the Caudate X Period Candle TM Plus expert advisor.
/// The strategy classifies smoothed candles into body and tail types and reacts to the resulting color codes.
/// </summary>
public class CaudateXPeriodCandleTmPlusStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<SmoothingMethod> _smoothingMethod;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<int> _maPhase;
	private readonly StrategyParam<bool> _openLong;
	private readonly StrategyParam<bool> _openShort;
	private readonly StrategyParam<bool> _closeLong;
	private readonly StrategyParam<bool> _closeShort;
	private readonly StrategyParam<bool> _enableTimeExit;
	private readonly StrategyParam<int> _timeExitMinutes;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;

	private IIndicator _openAverage = null!;
	private IIndicator _highAverage = null!;
	private IIndicator _lowAverage = null!;
	private IIndicator _closeAverage = null!;
	private Highest _highest = null!;
	private Lowest _lowest = null!;

	private readonly List<int> _signalHistory = new();
	private DateTimeOffset? _positionEntryTime;

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public CaudateXPeriodCandleTmPlusStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Time frame used for calculations", "General");

		_period = Param(nameof(Period), 5)
			.SetGreaterThanZero()
			.SetDisplay("Donchian Period", "Lookback window for the candle range", "Indicator");

		_signalBar = Param(nameof(SignalBar), 1)
			.SetGreaterOrEqual(0)
			.SetDisplay("Signal Bar", "Number of bars to delay signal evaluation", "Indicator");

		_smoothingMethod = Param(nameof(SmoothingMethod), SmoothingMethod.Jjma)
			.SetDisplay("Smoothing Method", "Moving average applied to price components", "Indicator");

		_maLength = Param(nameof(MaLength), 3)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "Length of the smoothing filter", "Indicator");

		_maPhase = Param(nameof(MaPhase), 100)
			.SetDisplay("MA Phase", "Reserved for compatibility with the original JJMA phase", "Indicator");

		_openLong = Param(nameof(EnableLongEntries), true)
			.SetDisplay("Enable Long Entries", "Allow opening long positions", "Trading");

		_openShort = Param(nameof(EnableShortEntries), true)
			.SetDisplay("Enable Short Entries", "Allow opening short positions", "Trading");

		_closeLong = Param(nameof(EnableLongExits), true)
			.SetDisplay("Enable Long Exits", "Allow closing long positions by signal", "Trading");

		_closeShort = Param(nameof(EnableShortExits), true)
			.SetDisplay("Enable Short Exits", "Allow closing short positions by signal", "Trading");

		_enableTimeExit = Param(nameof(EnableTimeExit), true)
			.SetDisplay("Enable Time Exit", "Close positions after a fixed holding time", "Risk");

		_timeExitMinutes = Param(nameof(TimeExitMinutes), 480)
			.SetGreaterThanZero()
			.SetDisplay("Time Exit (minutes)", "Holding time before a forced exit", "Risk");

		_stopLossPoints = Param(nameof(StopLossPoints), 1000m)
			.SetGreaterOrEqual(0m)
			.SetDisplay("Stop Loss (points)", "Protective stop distance in price steps", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000m)
			.SetGreaterOrEqual(0m)
			.SetDisplay("Take Profit (points)", "Protective profit target distance in price steps", "Risk");
	}

	/// <summary>
	/// Candle type for the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Lookback period for the smoothed Donchian channel.
	/// </summary>
	public int Period
	{
		get => _period.Value;
		set => _period.Value = value;
	}

	/// <summary>
	/// Number of bars to shift the signal evaluation.
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	/// <summary>
	/// Moving average type used to smooth price components.
	/// </summary>
	public SmoothingMethod SmoothingMethod
	{
		get => _smoothingMethod.Value;
		set => _smoothingMethod.Value = value;
	}

	/// <summary>
	/// Length of the smoothing moving average.
	/// </summary>
	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	/// <summary>
	/// Placeholder for JJMA phase parameter.
	/// </summary>
	public int MaPhase
	{
		get => _maPhase.Value;
		set => _maPhase.Value = value;
	}

	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool EnableLongEntries
	{
		get => _openLong.Value;
		set => _openLong.Value = value;
	}

	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool EnableShortEntries
	{
		get => _openShort.Value;
		set => _openShort.Value = value;
	}

	/// <summary>
	/// Allow closing long positions based on signals.
	/// </summary>
	public bool EnableLongExits
	{
		get => _closeLong.Value;
		set => _closeLong.Value = value;
	}

	/// <summary>
	/// Allow closing short positions based on signals.
	/// </summary>
	public bool EnableShortExits
	{
		get => _closeShort.Value;
		set => _closeShort.Value = value;
	}

	/// <summary>
	/// Enable the maximum holding time exit rule.
	/// </summary>
	public bool EnableTimeExit
	{
		get => _enableTimeExit.Value;
		set => _enableTimeExit.Value = value;
	}

	/// <summary>
	/// Maximum holding time in minutes before forcing an exit.
	/// </summary>
	public int TimeExitMinutes
	{
		get => _timeExitMinutes.Value;
		set => _timeExitMinutes.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in price steps.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in price steps.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
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

		_signalHistory.Clear();
		_positionEntryTime = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Create separate smoothing filters for each candle component.
		_openAverage = CreateMovingAverage(SmoothingMethod, MaLength);
		_highAverage = CreateMovingAverage(SmoothingMethod, MaLength);
		_lowAverage = CreateMovingAverage(SmoothingMethod, MaLength);
		_closeAverage = CreateMovingAverage(SmoothingMethod, MaLength);

		_highest = new Highest { Length = Period };
		_lowest = new Lowest { Length = Period };

		_signalHistory.Clear();
		_positionEntryTime = null;

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

		var stopLossOffset = TryGetPriceOffset(StopLossPoints);
		var takeProfitOffset = TryGetPriceOffset(TakeProfitPoints);

		StartProtection(
			stopLoss: stopLossOffset.HasValue ? new Unit(stopLossOffset.Value, UnitTypes.Absolute) : null,
			takeProfit: takeProfitOffset.HasValue ? new Unit(takeProfitOffset.Value, UnitTypes.Absolute) : null);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Update smoothed price components for the finished candle.
		var openValue = _openAverage.Process(candle.OpenPrice, candle.CloseTime, true);
		var highValue = _highAverage.Process(candle.HighPrice, candle.CloseTime, true);
		var lowValue = _lowAverage.Process(candle.LowPrice, candle.CloseTime, true);
		var closeValue = _closeAverage.Process(candle.ClosePrice, candle.CloseTime, true);

		if (!_openAverage.IsFormed || !_highAverage.IsFormed || !_lowAverage.IsFormed || !_closeAverage.IsFormed)
			return;

		var smoothedOpen = openValue.ToDecimal();
		var smoothedHigh = highValue.ToDecimal();
		var smoothedLow = lowValue.ToDecimal();
		var smoothedClose = closeValue.ToDecimal();

		// Feed the Donchian style bands with the smoothed extremes.
		var highestValue = _highest.Process(smoothedHigh, candle.CloseTime, true);
		var lowestValue = _lowest.Process(smoothedLow, candle.CloseTime, true);

		if (!_highest.IsFormed || !_lowest.IsFormed)
			return;

		var rangeHigh = highestValue.ToDecimal();
		var rangeLow = lowestValue.ToDecimal();

		// Ensure the bands always include the candle body just like the original indicator.
		var adjustedHigh = Math.Max(rangeHigh, Math.Max(smoothedOpen, smoothedClose));
		var adjustedLow = Math.Min(rangeLow, Math.Min(smoothedOpen, smoothedClose));

		var colorCode = CalculateColor(smoothedOpen, smoothedClose, adjustedHigh, adjustedLow);

		_signalHistory.Insert(0, colorCode);
		var maxHistory = Math.Max(SignalBar + 1, 3);
		if (_signalHistory.Count > maxHistory)
			_signalHistory.RemoveAt(_signalHistory.Count - 1);

		if (_signalHistory.Count <= SignalBar)
			return;

		var value = _signalHistory[SignalBar];

		if (EnableTimeExit)
			CloseByTimeIfNeeded(candle);

		var position = Position;

		// Manage existing long positions before considering new entries.
		if (position > 0 && EnableLongExits && value > 3)
		{
			SellMarket(position);
			_positionEntryTime = null;
			position = Position;
		}

		// Manage existing short positions before considering new entries.
		if (position < 0 && EnableShortExits && value < 3)
		{
			BuyMarket(Math.Abs(position));
			_positionEntryTime = null;
			position = Position;
		}

		var canTrade = IsFormedAndOnlineAndAllowTrading();
		if (!canTrade)
			return;

		// Detect opportunities to open long positions based on color codes 0 or 1.
		if (EnableLongEntries && value < 2 && position <= 0)
		{
			var volume = Volume + Math.Abs(position);
			BuyMarket(volume);
			_positionEntryTime = candle.CloseTime;
			return;
		}

		// Detect opportunities to open short positions based on color codes 5 or 6.
		if (EnableShortEntries && value > 4 && position >= 0)
		{
			var volume = Volume + Math.Abs(position);
			SellMarket(volume);
			_positionEntryTime = candle.CloseTime;
		}
	}

	private void CloseByTimeIfNeeded(ICandleMessage candle)
	{
		if (_positionEntryTime is null || Position == 0)
			return;

		var holding = candle.CloseTime - _positionEntryTime.Value;
		var limit = TimeSpan.FromMinutes(TimeExitMinutes);

		if (holding < limit)
			return;

		// Close the position when the maximum holding period is exceeded.
		if (Position > 0)
		{
			SellMarket(Position);
		}
		else if (Position < 0)
		{
			BuyMarket(Math.Abs(Position));
		}

		_positionEntryTime = null;
	}

	private static int CalculateColor(decimal open, decimal close, decimal high, decimal low)
	{
		var half = (high + low) / 2m;
		var bullish = close >= open;

		var color = bullish ? 2 : 4;

		if (open < half && close < half)
		{
			// Candle body sits near the bottom of the range creating an upper tail.
			color = open >= close ? 6 : 5;
		}
		else if (open > half && close > half)
		{
			// Candle body sits near the top of the range creating a lower tail.
			color = open <= close ? 0 : 1;
		}

		return color;
	}

	private IIndicator CreateMovingAverage(SmoothingMethod method, int length)
	{
		length = Math.Max(1, length);

		return method switch
		{
			SmoothingMethod.Sma => new SimpleMovingAverage { Length = length },
			SmoothingMethod.Ema => new ExponentialMovingAverage { Length = length },
			SmoothingMethod.Smma => new SmoothedMovingAverage { Length = length },
			SmoothingMethod.Lwma => new WeightedMovingAverage { Length = length },
			SmoothingMethod.Jjma => new JurikMovingAverage { Length = length },
			SmoothingMethod.Ama => new KaufmanAdaptiveMovingAverage { Length = length },
			_ => new ExponentialMovingAverage { Length = length },
		};
	}

	private decimal? TryGetPriceOffset(decimal points)
	{
		if (points <= 0)
			return null;

		var step = Security?.PriceStep;
		if (step is null || step.Value <= 0)
			return points;

		return points * step.Value;
	}

	/// <summary>
	/// Supported smoothing methods for the strategy.
	/// </summary>
	public enum SmoothingMethod
	{
		/// <summary>
		/// Simple moving average.
		/// </summary>
		Sma,

		/// <summary>
		/// Exponential moving average.
		/// </summary>
		Ema,

		/// <summary>
		/// Smoothed moving average (RMA).
		/// </summary>
		Smma,

		/// <summary>
		/// Linear weighted moving average.
		/// </summary>
		Lwma,

		/// <summary>
		/// Jurik moving average approximation of JJMA.
		/// </summary>
		Jjma,

		/// <summary>
		/// Kaufman adaptive moving average.
		/// </summary>
		Ama
	}
}
