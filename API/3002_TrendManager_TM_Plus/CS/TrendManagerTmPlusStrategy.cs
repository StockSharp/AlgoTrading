namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Trend following strategy based on the TrendManager indicator.
/// Calculates two configurable moving averages and acts on their distance.
/// </summary>
public class TrendManagerTmPlusStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<SmoothingMethod> _fastMethod;
	private readonly StrategyParam<SmoothingMethod> _slowMethod;
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<decimal> _dvLimit;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<bool> _allowLongEntries;
	private readonly StrategyParam<bool> _allowShortEntries;
	private readonly StrategyParam<bool> _closeLongOnOpposite;
	private readonly StrategyParam<bool> _closeShortOnOpposite;
	private readonly StrategyParam<bool> _useTimeExpiration;
	private readonly StrategyParam<TimeSpan> _maxPositionAge;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<decimal> _stopLossDistance;
	private readonly StrategyParam<decimal> _takeProfitDistance;

	private IIndicator _fastMa = null!;
	private IIndicator _slowMa = null!;
	private readonly List<int> _colorHistory = new();

	private DateTimeOffset? _entryTime;
	private decimal? _entryPrice;

	/// <summary>
	/// Supported smoothing methods.
	/// </summary>
	public enum SmoothingMethod
	{
		Simple,
		Exponential,
		Smoothed,
		Weighted,
		Jurik,
		Adaptive
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public TrendManagerTmPlusStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for signals", "General");

		_fastMethod = Param(nameof(FastMethod), SmoothingMethod.Simple)
			.SetDisplay("Fast MA Method", "Smoothing for fast line", "Indicator");

		_slowMethod = Param(nameof(SlowMethod), SmoothingMethod.Simple)
			.SetDisplay("Slow MA Method", "Smoothing for slow line", "Indicator");

		_fastLength = Param(nameof(FastLength), 23)
			.SetGreaterThanZero()
			.SetDisplay("Fast Length", "Period for fast moving average", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(5, 60, 5);

		_slowLength = Param(nameof(SlowLength), 84)
			.SetGreaterThanZero()
			.SetDisplay("Slow Length", "Period for slow moving average", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(20, 200, 10);

		_dvLimit = Param(nameof(DvLimit), 0.0007m)
			.SetGreaterThanZero()
			.SetDisplay("Distance Threshold", "Minimum fast-slow distance to trigger a signal", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(0.0001m, 0.01m, 0.0001m);

		_signalBar = Param(nameof(SignalBar), 1)
			.SetGreaterOrEqualZero()
			.SetDisplay("Signal Bar", "Bars ago used to confirm a new signal", "Indicator");

		_allowLongEntries = Param(nameof(AllowLongEntries), true)
			.SetDisplay("Allow Long Entries", "Enable buying when an up trend appears", "Trading");

		_allowShortEntries = Param(nameof(AllowShortEntries), true)
			.SetDisplay("Allow Short Entries", "Enable selling when a down trend appears", "Trading");

		_closeLongOnOpposite = Param(nameof(CloseLongOnOppositeSignal), true)
			.SetDisplay("Close Long", "Close longs on opposite signals", "Trading");

		_closeShortOnOpposite = Param(nameof(CloseShortOnOppositeSignal), true)
			.SetDisplay("Close Short", "Close shorts on opposite signals", "Trading");

		_useTimeExpiration = Param(nameof(UseTimeExpiration), true)
			.SetDisplay("Use Time Exit", "Enable time-based exit", "Risk");

		_maxPositionAge = Param(nameof(MaxPositionAge), TimeSpan.FromMinutes(12000))
			.SetDisplay("Max Position Age", "Maximum holding time", "Risk");

		_orderVolume = Param(nameof(OrderVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Volume used for market orders", "Trading");

		_stopLossDistance = Param(nameof(StopLossDistance), 0m)
			.SetDisplay("Stop Loss Distance", "Price distance for protective stop (0 disables)", "Risk");

		_takeProfitDistance = Param(nameof(TakeProfitDistance), 0m)
			.SetDisplay("Take Profit Distance", "Price distance for profit target (0 disables)", "Risk");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public SmoothingMethod FastMethod
	{
		get => _fastMethod.Value;
		set => _fastMethod.Value = value;
	}

	public SmoothingMethod SlowMethod
	{
		get => _slowMethod.Value;
		set => _slowMethod.Value = value;
	}

	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	public decimal DvLimit
	{
		get => _dvLimit.Value;
		set => _dvLimit.Value = value;
	}

	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	public bool AllowLongEntries
	{
		get => _allowLongEntries.Value;
		set => _allowLongEntries.Value = value;
	}

	public bool AllowShortEntries
	{
		get => _allowShortEntries.Value;
		set => _allowShortEntries.Value = value;
	}

	public bool CloseLongOnOppositeSignal
	{
		get => _closeLongOnOpposite.Value;
		set => _closeLongOnOpposite.Value = value;
	}

	public bool CloseShortOnOppositeSignal
	{
		get => _closeShortOnOpposite.Value;
		set => _closeShortOnOpposite.Value = value;
	}

	public bool UseTimeExpiration
	{
		get => _useTimeExpiration.Value;
		set => _useTimeExpiration.Value = value;
	}

	public TimeSpan MaxPositionAge
	{
		get => _maxPositionAge.Value;
		set => _maxPositionAge.Value = value;
	}

	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	public decimal StopLossDistance
	{
		get => _stopLossDistance.Value;
		set => _stopLossDistance.Value = value;
	}

	public decimal TakeProfitDistance
	{
		get => _takeProfitDistance.Value;
		set => _takeProfitDistance.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastMa = CreateMovingAverage(FastMethod, FastLength);
		_slowMa = CreateMovingAverage(SlowMethod, SlowLength);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fastMa, _slowMa, ProcessCandle)
			.Start();

		StartProtection();
	}

	private IIndicator CreateMovingAverage(SmoothingMethod method, int length)
	{
		// Map the selected smoothing method to a StockSharp indicator implementation.
		return method switch
		{
			SmoothingMethod.Simple => new SimpleMovingAverage { Length = length },
			SmoothingMethod.Exponential => new ExponentialMovingAverage { Length = length },
			SmoothingMethod.Smoothed => new SmoothedMovingAverage { Length = length },
			SmoothingMethod.Weighted => new WeightedMovingAverage { Length = length },
			SmoothingMethod.Jurik => new JurikMovingAverage { Length = length },
			SmoothingMethod.Adaptive => new KaufmanAdaptiveMovingAverage { Length = length },
			_ => new SimpleMovingAverage { Length = length },
		};
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_fastMa.IsFormed || !_slowMa.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Calculate the difference between fast and slow averages.
		var diff = fastValue - slowValue;
		var color = 3;

		if (diff >= DvLimit)
			color = 0;
		else if (diff <= -DvLimit)
			color = 1;

		_colorHistory.Add(color);

		var maxHistory = Math.Max(10, SignalBar + 3);
		if (_colorHistory.Count > maxHistory)
			_colorHistory.RemoveRange(0, _colorHistory.Count - maxHistory);

		HandleRiskManagement(candle);

		if (_colorHistory.Count < SignalBar + 2)
			return;

		var signalIndex = SignalBar + 1;
		var colorAtSignal = _colorHistory[^signalIndex];
		var previousColor = _colorHistory[^ (signalIndex + 1)];

		// Close opposite positions on signal if allowed.
		if (colorAtSignal == 0 && CloseShortOnOppositeSignal && Position < 0)
			CloseShort();
		else if (colorAtSignal == 1 && CloseLongOnOppositeSignal && Position > 0)
			CloseLong();

		// Open new positions only when a fresh signal appears.
		if (colorAtSignal == 0 && previousColor != 0 && AllowLongEntries && Position <= 0)
			OpenLong(candle.ClosePrice, candle.CloseTime);
		else if (colorAtSignal == 1 && previousColor != 1 && AllowShortEntries && Position >= 0)
			OpenShort(candle.ClosePrice, candle.CloseTime);
	}

	private void HandleRiskManagement(ICandleMessage candle)
	{
		if (Position > 0)
		{
			if (ShouldExitLong(candle))
				CloseLong();
		}
		else if (Position < 0)
		{
			if (ShouldExitShort(candle))
				CloseShort();
		}

		if (Position == 0)
		{
			_entryTime = null;
			_entryPrice = null;
		}
	}

	private bool ShouldExitLong(ICandleMessage candle)
	{
		if (UseTimeExpiration && _entryTime is DateTimeOffset entryTime)
		{
			if (candle.CloseTime - entryTime >= MaxPositionAge)
				return true;
		}

		if (_entryPrice is not decimal entryPrice)
			return false;

		if (StopLossDistance > 0m && candle.LowPrice <= entryPrice - StopLossDistance)
			return true;

		if (TakeProfitDistance > 0m && candle.HighPrice >= entryPrice + TakeProfitDistance)
			return true;

		return false;
	}

	private bool ShouldExitShort(ICandleMessage candle)
	{
		if (UseTimeExpiration && _entryTime is DateTimeOffset entryTime)
		{
			if (candle.CloseTime - entryTime >= MaxPositionAge)
				return true;
		}

		if (_entryPrice is not decimal entryPrice)
			return false;

		if (StopLossDistance > 0m && candle.HighPrice >= entryPrice + StopLossDistance)
			return true;

		if (TakeProfitDistance > 0m && candle.LowPrice <= entryPrice - TakeProfitDistance)
			return true;

		return false;
	}

	private void OpenLong(decimal price, DateTimeOffset time)
	{
		if (OrderVolume <= 0m)
			return;

		// Enter long position at market price.
		BuyMarket(OrderVolume);
		_entryPrice = price;
		_entryTime = time;
	}

	private void OpenShort(decimal price, DateTimeOffset time)
	{
		if (OrderVolume <= 0m)
			return;

		// Enter short position at market price.
		SellMarket(OrderVolume);
		_entryPrice = price;
		_entryTime = time;
	}

	private void CloseLong()
	{
		var volume = Position;
		if (volume <= 0m)
			return;

		// Close existing long position.
		SellMarket(volume);
		_entryPrice = null;
		_entryTime = null;
	}

	private void CloseShort()
	{
		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return;

		// Close existing short position.
		BuyMarket(volume);
		_entryPrice = null;
		_entryTime = null;
	}
}
