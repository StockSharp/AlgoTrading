namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Chaikin Oscillator strategy with flat filter.
/// Uses Bollinger Bands to detect flat market and trades on crossover of oscillator and its moving average.
/// </summary>
public class ChoWithFlatStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<MovingAverageTypes> _maType;
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _stdDeviation;
	private readonly StrategyParam<decimal> _flatThreshold;
	
	private decimal _previousCho;
	private decimal _previousSignal;
	private bool _isInitialized;
	
	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Fast period for Chaikin Oscillator.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}
	
	/// <summary>
	/// Slow period for Chaikin Oscillator.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}
	
	/// <summary>
	/// Period of the moving average applied to the oscillator.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}
	
	/// <summary>
	/// Moving average type for the signal line.
	/// </summary>
	public MovingAverageTypes MaType
	{
		get => _maType.Value;
		set => _maType.Value = value;
	}
	
	/// <summary>
	/// Bollinger Bands period used for flat detection.
	/// </summary>
	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}
	
	/// <summary>
	/// Standard deviation for Bollinger Bands.
	/// </summary>
	public decimal StdDeviation
	{
		get => _stdDeviation.Value;
		set => _stdDeviation.Value = value;
	}
	
	/// <summary>
	/// Threshold in points to detect flat market.
	/// </summary>
	public decimal FlatThreshold
	{
		get => _flatThreshold.Value;
		set => _flatThreshold.Value = value;
	}
	
	/// <summary>
	/// Constructor.
	/// </summary>
	public ChoWithFlatStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe for analysis", "General");
		
		_fastPeriod = Param(nameof(FastPeriod), 3)
		.SetGreaterThanZero()
		.SetDisplay("Fast Period", "Fast period for Chaikin Oscillator", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(2, 20, 1);
		
		_slowPeriod = Param(nameof(SlowPeriod), 10)
		.SetGreaterThanZero()
		.SetDisplay("Slow Period", "Slow period for Chaikin Oscillator", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(5, 40, 1);
		
		_maPeriod = Param(nameof(MaPeriod), 13)
		.SetGreaterThanZero()
		.SetDisplay("MA Period", "Period for signal moving average", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(5, 40, 1);
		
		_maType = Param(nameof(MaType), MovingAverageTypes.Simple)
		.SetDisplay("MA Type", "Moving average type", "Indicator");
		
		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
		.SetGreaterThanZero()
		.SetDisplay("Bollinger Period", "Period for Bollinger Bands", "Flat Filter");
		
		_stdDeviation = Param(nameof(StdDeviation), 2.0m)
		.SetGreaterThanZero()
		.SetDisplay("Std Deviation", "Deviation for Bollinger Bands", "Flat Filter");
		
		_flatThreshold = Param(nameof(FlatThreshold), 100m)
		.SetGreaterThanZero()
		.SetDisplay("Flat Threshold", "Minimum band width in points", "Flat Filter")
		.SetCanOptimize(true)
		.SetOptimize(10m, 200m, 10m);
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
		_previousCho = 0m;
		_previousSignal = 0m;
		_isInitialized = false;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		var cho = new ChaikinOscillator { ShortPeriod = FastPeriod, LongPeriod = SlowPeriod };
		var signalMa = new MovingAverage { Length = MaPeriod, Type = MaType };
		var bollinger = new BollingerBands { Length = BollingerPeriod, Width = StdDeviation };
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(cho, signalMa, bollinger, ProcessCandle)
		.Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, cho);
			DrawIndicator(area, signalMa);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal choValue, decimal signalValue, decimal middleBand, decimal upperBand, decimal lowerBand)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		if (!_isInitialized)
		{
			_previousCho = choValue;
			_previousSignal = signalValue;
			_isInitialized = true;
			return;
		}
		
		var bandWidth = upperBand - middleBand;
		var bandWidthInPoints = bandWidth / Security.PriceStep;
		if (bandWidthInPoints < FlatThreshold)
		{
			_previousCho = choValue;
			_previousSignal = signalValue;
			return;
		}
		
		var wasChoAbove = _previousCho > _previousSignal;
		var isChoAbove = choValue > signalValue;
		
		if (wasChoAbove && !isChoAbove)
		{
			if (Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (!wasChoAbove && isChoAbove)
		{
			if (Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		}
		
		_previousCho = choValue;
		_previousSignal = signalValue;
	}
}
