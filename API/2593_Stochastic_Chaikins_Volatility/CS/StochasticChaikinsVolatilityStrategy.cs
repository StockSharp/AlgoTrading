using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy converted from the "Stochastic Chaikin's Volatility" MQL expert advisor.
/// Combines a smoothed Chaikin volatility measure with a stochastic oscillator style normalization.
/// </summary>
public class StochasticChaikinsVolatilityStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<SmoothMethod> _primaryMethod;
	private readonly StrategyParam<int> _primaryLength;
	private readonly StrategyParam<SmoothMethod> _secondaryMethod;
	private readonly StrategyParam<int> _secondaryLength;
	private readonly StrategyParam<int> _stochasticLength;
	private readonly StrategyParam<int> _signalShift;
	private readonly StrategyParam<bool> _allowLongEntry;
	private readonly StrategyParam<bool> _allowShortEntry;
	private readonly StrategyParam<bool> _allowLongExit;
	private readonly StrategyParam<bool> _allowShortExit;
	private readonly StrategyParam<decimal> _highLevel;
	private readonly StrategyParam<decimal> _middleLevel;
	private readonly StrategyParam<decimal> _lowLevel;
	
	private LengthIndicator<decimal> _primarySmoother = null!;
	private LengthIndicator<decimal> _secondarySmoother = null!;
	private readonly Queue<decimal> _volatilityWindow = new();
	private readonly List<decimal> _mainHistory = new();
	
	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Smoothing method applied to the high-low spread.
	/// </summary>
	public SmoothMethod PrimaryMethod
	{
		get => _primaryMethod.Value;
		set => _primaryMethod.Value = value;
	}
	
	/// <summary>
	/// Length of the primary smoothing moving average.
	/// </summary>
	public int PrimaryLength
	{
		get => _primaryLength.Value;
		set => _primaryLength.Value = value;
	}
	
	/// <summary>
	/// Smoothing method applied to the stochastic ratio.
	/// </summary>
	public SmoothMethod SecondaryMethod
	{
		get => _secondaryMethod.Value;
		set => _secondaryMethod.Value = value;
	}
	
	/// <summary>
	/// Length of the secondary smoothing moving average.
	/// </summary>
	public int SecondaryLength
	{
		get => _secondaryLength.Value;
		set => _secondaryLength.Value = value;
	}
	
	/// <summary>
	/// Lookback for calculating the stochastic style normalization.
	/// </summary>
	public int StochasticLength
	{
		get => _stochasticLength.Value;
		set => _stochasticLength.Value = value;
	}
	
	/// <summary>
	/// Number of completed candles used as signal shift.
	/// </summary>
	public int SignalShift
	{
		get => _signalShift.Value;
		set => _signalShift.Value = value;
	}
	
	/// <summary>
	/// Enable opening of long positions.
	/// </summary>
	public bool AllowLongEntry
	{
		get => _allowLongEntry.Value;
		set => _allowLongEntry.Value = value;
	}
	
	/// <summary>
	/// Enable opening of short positions.
	/// </summary>
	public bool AllowShortEntry
	{
		get => _allowShortEntry.Value;
		set => _allowShortEntry.Value = value;
	}
	
	/// <summary>
	/// Enable closing of long positions on indicator reversal.
	/// </summary>
	public bool AllowLongExit
	{
		get => _allowLongExit.Value;
		set => _allowLongExit.Value = value;
	}
	
	/// <summary>
	/// Enable closing of short positions on indicator reversal.
	/// </summary>
	public bool AllowShortExit
	{
		get => _allowShortExit.Value;
		set => _allowShortExit.Value = value;
	}
	
	/// <summary>
	/// Upper visual level for the oscillator.
	/// </summary>
	public decimal HighLevel
	{
		get => _highLevel.Value;
		set => _highLevel.Value = value;
	}
	
	/// <summary>
	/// Middle visual level for the oscillator.
	/// </summary>
	public decimal MiddleLevel
	{
		get => _middleLevel.Value;
		set => _middleLevel.Value = value;
	}
	
	/// <summary>
	/// Lower visual level for the oscillator.
	/// </summary>
	public decimal LowLevel
	{
		get => _lowLevel.Value;
		set => _lowLevel.Value = value;
	}
	
	/// <summary>
	/// Initializes a new instance of the <see cref="StochasticChaikinsVolatilityStrategy"/> class.
	/// </summary>
	public StochasticChaikinsVolatilityStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe used for indicator calculations", "General");
		
		_primaryMethod = Param(nameof(PrimaryMethod), SmoothMethod.Sma)
		.SetDisplay("Primary Method", "Smoothing applied to high-low spread", "Indicator")
		.SetCanOptimize(true);
		
		_primaryLength = Param(nameof(PrimaryLength), 10)
		.SetGreaterThanZero()
		.SetDisplay("Primary Length", "Periods for primary smoothing", "Indicator")
		.SetCanOptimize(true);
		
		_secondaryMethod = Param(nameof(SecondaryMethod), SmoothMethod.Jurik)
		.SetDisplay("Secondary Method", "Smoothing applied to stochastic ratio", "Indicator")
		.SetCanOptimize(true);
		
		_secondaryLength = Param(nameof(SecondaryLength), 5)
		.SetGreaterThanZero()
		.SetDisplay("Secondary Length", "Periods for secondary smoothing", "Indicator")
		.SetCanOptimize(true);
		
		_stochasticLength = Param(nameof(StochasticLength), 5)
		.SetGreaterThanZero()
		.SetDisplay("Stochastic Length", "Lookback for highest-lowest range", "Indicator")
		.SetCanOptimize(true);
		
		_signalShift = Param(nameof(SignalShift), 1)
		.SetGreaterThanZero()
		.SetDisplay("Signal Shift", "Completed candles offset for signals", "Trading")
		.SetCanOptimize(true);
		
		_allowLongEntry = Param(nameof(AllowLongEntry), true)
		.SetDisplay("Allow Long Entry", "Enable opening of buy trades", "Trading");
		
		_allowShortEntry = Param(nameof(AllowShortEntry), true)
		.SetDisplay("Allow Short Entry", "Enable opening of sell trades", "Trading");
		
		_allowLongExit = Param(nameof(AllowLongExit), true)
		.SetDisplay("Allow Long Exit", "Enable closing longs on reversal", "Trading");
		
		_allowShortExit = Param(nameof(AllowShortExit), true)
		.SetDisplay("Allow Short Exit", "Enable closing shorts on reversal", "Trading");
		
		_highLevel = Param(nameof(HighLevel), 300m)
		.SetDisplay("High Level", "Upper visual threshold", "Visualization");
		
		_middleLevel = Param(nameof(MiddleLevel), 50m)
		.SetDisplay("Middle Level", "Middle visual threshold", "Visualization");
		
		_lowLevel = Param(nameof(LowLevel), -300m)
		.SetDisplay("Low Level", "Lower visual threshold", "Visualization");
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
		_volatilityWindow.Clear();
		_mainHistory.Clear();
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		_primarySmoother = CreateSmoother(PrimaryMethod, PrimaryLength);
		_secondarySmoother = CreateSmoother(SecondaryMethod, SecondaryLength);
		
		var subscription = SubscribeCandles(CandleType);
		subscription.WhenNew(ProcessCandle).Start();
		
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
		
		var diff = candle.HighPrice - candle.LowPrice;
		var smoothedValue = _primarySmoother.Process(diff);
		if (!smoothedValue.IsFinal)
		return;
		
		var smoothedDiff = smoothedValue.ToDecimal();
		UpdateQueue(_volatilityWindow, smoothedDiff, StochasticLength);
		
		if (_volatilityWindow.Count < StochasticLength)
		return;
		
		decimal highest = decimal.MinValue;
		decimal lowest = decimal.MaxValue;
		foreach (var value in _volatilityWindow)
		{
			if (value > highest)
			highest = value;
			if (value < lowest)
			lowest = value;
		}
		
		var priceStep = Security?.PriceStep ?? 0.0001m;
		if (priceStep <= 0m)
		priceStep = 0.0001m;
		
		var range = highest - lowest;
		var denominator = range < priceStep ? priceStep : range;
		var normalized = denominator == 0m ? 0m : (smoothedDiff - lowest) / denominator;
		if (normalized < 0m)
		normalized = 0m;
		else if (normalized > 1m)
		normalized = 1m;
		
		var scaled = normalized * 100m;
		var stochasticValue = _secondarySmoother.Process(scaled);
		if (!stochasticValue.IsFinal)
		return;
		
		var main = stochasticValue.ToDecimal();
		AddHistory(main);
		
		var minHistory = SignalShift + 3;
		if (_mainHistory.Count < minHistory)
		return;
		
		var idx = SignalShift;
		var value0 = _mainHistory[idx];
		var value1 = _mainHistory[idx + 1];
		var value2 = _mainHistory[idx + 2];
		
		var buyClose = AllowLongExit && value1 < value2;
		var sellClose = AllowShortExit && value1 > value2;
		var buyOpen = AllowLongEntry && value1 > value2 && value0 <= value1;
		var sellOpen = AllowShortEntry && value1 < value2 && value0 >= value1;
		
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		if (Position > 0m && buyClose)
		{
			SellMarket(Math.Abs(Position));
		}
		else if (Position < 0m && sellClose)
		{
			BuyMarket(Math.Abs(Position));
		}
		
		if (buyOpen && Position <= 0m)
		{
			CancelActiveOrders();
			var volume = Volume + (Position < 0m ? Math.Abs(Position) : 0m);
			if (volume > 0m)
			BuyMarket(volume);
		}
		else if (sellOpen && Position >= 0m)
		{
			CancelActiveOrders();
			var volume = Volume + (Position > 0m ? Math.Abs(Position) : 0m);
			if (volume > 0m)
			SellMarket(volume);
		}
	}
	
	private void AddHistory(decimal value)
	{
		_mainHistory.Insert(0, value);
		var maxSize = SignalShift + 3;
		if (_mainHistory.Count > maxSize)
		_mainHistory.RemoveRange(maxSize, _mainHistory.Count - maxSize);
	}
	
	private static void UpdateQueue(Queue<decimal> queue, decimal value, int length)
	{
		queue.Enqueue(value);
		while (queue.Count > length)
		queue.Dequeue();
	}
	
	private static LengthIndicator<decimal> CreateSmoother(SmoothMethod method, int length)
	{
		return method switch
		{
			SmoothMethod.Sma => new SimpleMovingAverage { Length = length },
			SmoothMethod.Ema => new ExponentialMovingAverage { Length = length },
			SmoothMethod.Smma => new SmoothedMovingAverage { Length = length },
			SmoothMethod.Lwma => new WeightedMovingAverage { Length = length },
			SmoothMethod.Jurik => new JurikMovingAverage { Length = length },
			_ => new SimpleMovingAverage { Length = length },
		};
	}
	
	/// <summary>
	/// Available smoothing methods supported by the strategy.
	/// </summary>
	public enum SmoothMethod
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
		/// Smoothed moving average (RMA/SMMA).
		/// </summary>
		Smma,
		
		/// <summary>
		/// Linear weighted moving average.
		/// </summary>
		Lwma,
		
		/// <summary>
		/// Jurik moving average approximation.
		/// </summary>
		Jurik
	}
}
