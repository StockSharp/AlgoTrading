using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bounce Strength Index strategy.
/// Uses a custom indicator to measure bounce strength within a price range.
/// Opens long positions when the indicator turns upward and short positions when it turns downward.
/// </summary>
public class BounceStrengthIndexStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rangePeriod;
	private readonly StrategyParam<int> _slowing;
	private readonly StrategyParam<int> _avgPeriod;
	
	private BounceStrengthIndex _bsi = null!;
	private decimal? _prevValue;
	private bool? _prevRising;
	
	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Period for searching highest and lowest values.
	/// </summary>
	public int RangePeriod
	{
		get => _rangePeriod.Value;
		set => _rangePeriod.Value = value;
	}
	
	/// <summary>
	/// Fast smoothing period.
	/// </summary>
	public int Slowing
	{
		get => _slowing.Value;
		set => _slowing.Value = value;
	}
	
	/// <summary>
	/// Slow smoothing period.
	/// </summary>
	public int AvgPeriod
	{
		get => _avgPeriod.Value;
		set => _avgPeriod.Value = value;
	}
	
	/// <summary>
	/// Initializes a new instance of the <see cref="BounceStrengthIndexStrategy"/> class.
	/// </summary>
	public BounceStrengthIndexStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles to use", "General");
		_rangePeriod = Param(nameof(RangePeriod), 20)
		.SetDisplay("Range Period", "Period for highest and lowest search", "Indicator");
		_slowing = Param(nameof(Slowing), 3)
		.SetDisplay("Slowing", "Fast smoothing period", "Indicator");
		_avgPeriod = Param(nameof(AvgPeriod), 3)
		.SetDisplay("Average Period", "Slow smoothing period", "Indicator");
	}
	
	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	=> [(Security, CandleType)];
	
	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevValue = null;
		_prevRising = null;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		StartProtection();
		
		_bsi = new BounceStrengthIndex
		{
			RangePeriod = RangePeriod,
			Slowing = Slowing,
			AvgPeriod = AvgPeriod,
		};
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_bsi, ProcessCandle)
		.Start();
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal bsiValue)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (_prevValue is decimal prev)
		{
			var rising = bsiValue > prev;
			
			if (rising && _prevRising != true)
			{
				if (Position < 0)
				BuyMarket(Math.Abs(Position));
				
				if (Position <= 0)
				BuyMarket();
			}
			else if (!rising && _prevRising != false)
			{
				if (Position > 0)
				SellMarket(Position);
				
				if (Position >= 0)
				SellMarket();
			}
			
			_prevRising = rising;
		}
		
		_prevValue = bsiValue;
	}
}

/// <summary>
/// Simplified Bounce Strength Index indicator.
/// Calculates the position of the close price within the recent range
/// and applies double smoothing to produce a momentum-like value.
/// </summary>
public class BounceStrengthIndex : BaseIndicator<decimal>
{
	/// <summary>
	/// Period for highest and lowest search.
	/// </summary>
	public int RangePeriod { get; set; } = 20;
	
	/// <summary>
	/// Fast smoothing period.
	/// </summary>
	public int Slowing { get; set; } = 3;
	
	/// <summary>
	/// Slow smoothing period.
	/// </summary>
	public int AvgPeriod { get; set; } = 3;
	
	private readonly Highest _highest = new();
	private readonly Lowest _lowest = new();
	private readonly SimpleMovingAverage _fastSma = new();
	private readonly SimpleMovingAverage _slowSma = new();
	
	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		if (input is not ICandleMessage candle || candle.State != CandleStates.Finished)
		return new DecimalIndicatorValue(this, default, input.Time);
		
		_highest.Length = RangePeriod;
		_lowest.Length = RangePeriod;
		_fastSma.Length = Slowing;
		_slowSma.Length = AvgPeriod;
		
		var high = _highest.Process(input).GetValue<decimal>();
		var low = _lowest.Process(input).GetValue<decimal>();
		var range = high - low;
		
		if (range <= 0)
		return new DecimalIndicatorValue(this, default, input.Time);
		
		var pos = (candle.ClosePrice - low) / range * 100m;
		var neg = (high - candle.ClosePrice) / range * 100m;
		var diff = pos - neg;
		
		var fast = _fastSma.Process(new DecimalIndicatorValue(_fastSma, diff, input.Time)).GetValue<decimal>();
		var slow = _slowSma.Process(new DecimalIndicatorValue(_slowSma, fast, input.Time)).GetValue<decimal>();
		
		return new DecimalIndicatorValue(this, slow, input.Time);
	}
}
