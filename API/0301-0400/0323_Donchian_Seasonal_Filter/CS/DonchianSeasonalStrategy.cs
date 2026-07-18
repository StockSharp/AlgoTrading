using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Donchian Channels with seasonal filter.
/// </summary>
public class DonchianSeasonalStrategy : Strategy
{
	private readonly StrategyParam<int> _donchianPeriod;
	private readonly StrategyParam<decimal> _seasonalThreshold;
	private readonly StrategyParam<int> _signalCooldownBars;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _seasonalDataCount;
	
	private DonchianChannels _donchian = null!;
	
	// Seasonal data storage
	private readonly SynchronizedDictionary<Months, decimal> _monthlyReturns = [];
	
	private decimal _seasonalStrength;
	private decimal? _previousUpperBand;
	private decimal? _previousLowerBand;
	private decimal? _previousMiddleBand;
	private decimal? _previousClosePrice;
	private int _cooldownRemaining;

	/// <summary>
	/// Donchian Channel period.
	/// </summary>
	public int DonchianPeriod
	{
		get => _donchianPeriod.Value;
		set => _donchianPeriod.Value = value;
	}
	
	/// <summary>
	/// Seasonal strength threshold for entry.
	/// </summary>
	public decimal SeasonalThreshold
	{
		get => _seasonalThreshold.Value;
		set => _seasonalThreshold.Value = value;
	}

	/// <summary>
	/// Number of years used for seasonal analysis.
	/// </summary>
	public int SeasonalDataCount
	{
		get => _seasonalDataCount.Value;
		set => _seasonalDataCount.Value = value;
	}

	/// <summary>
	/// Number of closed candles to wait before allowing the next entry.
	/// </summary>
	public int SignalCooldownBars
	{
		get => _signalCooldownBars.Value;
		set => _signalCooldownBars.Value = value;
	}

	/// <summary>
	/// Candle type to use for the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="DonchianSeasonalStrategy"/>.
	/// </summary>
	public DonchianSeasonalStrategy()
	{
		_donchianPeriod = Param(nameof(DonchianPeriod), 40)
			.SetDisplay("Donchian Period", "Donchian Channel period", "Donchian")
			
			.SetOptimize(10, 50, 5);
			
		_seasonalThreshold = Param(nameof(SeasonalThreshold), 0.5m)
			.SetDisplay("Seasonal Threshold", "Seasonal strength threshold for entry", "Seasonal")
			
			.SetOptimize(0.2m, 1.0m, 0.1m);

		_seasonalDataCount = Param(nameof(SeasonalDataCount), 5)
			.SetDisplay("Seasonal Years", "Years of seasonal data", "Seasonal")
			.SetGreaterThanZero()
			
			.SetOptimize(1, 10, 1);

		_signalCooldownBars = Param(nameof(SignalCooldownBars), 12)
			.SetNotNegative()
			.SetDisplay("Signal Cooldown Bars", "Closed candles to wait before a new breakout entry", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
			
		// Initialize monthly returns with neutral values
		foreach (Months month in Enum.GetValues(typeof(Months)))
		{
			_monthlyReturns[month] = 0m;
		}
		
		// Simulated historical seasonal data (in a real strategy, this would come from analysis of historical data)
		// These are example values that suggest certain months tend to be bullish or bearish
		_monthlyReturns[Months.January] = 0.8m;
		_monthlyReturns[Months.February] = 0.3m;
		_monthlyReturns[Months.March] = 0.6m;
		_monthlyReturns[Months.April] = 0.9m;
		_monthlyReturns[Months.May] = 0.2m;
		_monthlyReturns[Months.June] = -0.4m;
		_monthlyReturns[Months.July] = -0.2m;
		_monthlyReturns[Months.August] = -0.7m;
		_monthlyReturns[Months.September] = -0.9m;
		_monthlyReturns[Months.October] = -0.1m;
		_monthlyReturns[Months.November] = 0.5m;
		_monthlyReturns[Months.December] = 0.7m;
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

		_seasonalStrength = 0;
		_previousUpperBand = null;
		_previousLowerBand = null;
		_previousMiddleBand = null;
		_previousClosePrice = null;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		// Create Donchian Channel indicator
		_donchian = new DonchianChannels
		{
			Length = DonchianPeriod
		};

		// Create subscription and bind indicator
		var subscription = SubscribeCandles(CandleType);
		
		subscription
			.BindEx(_donchian, ProcessCandle)
			.Start();

		// Setup chart visualization if available
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _donchian);
			DrawOwnTrades(area);
		}
		
		// Setup position protection
		StartProtection(
			new Unit(2, UnitTypes.Percent), 
			new Unit(2, UnitTypes.Percent)
		);
	}
	
	private void ProcessCandle(ICandleMessage candle, IIndicatorValue donchianValue)
	{
		// Skip unfinished candles
		if (candle.State != CandleStates.Finished)
			return;

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		var donchianTyped = (DonchianChannelsValue)donchianValue;

		if (donchianTyped.UpperBand is not decimal upperBand ||
			donchianTyped.LowerBand is not decimal lowerBand ||
			donchianTyped.Middle is not decimal middleBand)
		{
			return;
		}

		// Calculate seasonal strength for current month
		UpdateSeasonalStrength(candle.OpenTime);

		if (_previousUpperBand is null || _previousLowerBand is null || _previousMiddleBand is null || _previousClosePrice is null)
		{
			_previousUpperBand = upperBand;
			_previousLowerBand = lowerBand;
			_previousMiddleBand = middleBand;
			_previousClosePrice = candle.ClosePrice;
			return;
		}
		
		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_previousUpperBand = upperBand;
			_previousLowerBand = lowerBand;
			_previousMiddleBand = middleBand;
			_previousClosePrice = candle.ClosePrice;
			return;
		}

		var previousUpperBand = _previousUpperBand.Value;
		var previousLowerBand = _previousLowerBand.Value;
		var previousMiddleBand = _previousMiddleBand.Value;
		var previousClosePrice = _previousClosePrice.Value;
			
		// Trading logic
		// Donchian channels include the current bar, so the breakout must be checked against the previous channel.
		if (Position > 0 && candle.ClosePrice < previousMiddleBand)
		{
			SellMarket(Position);
			_cooldownRemaining = SignalCooldownBars;
		}
		else if (Position < 0 && candle.ClosePrice > previousMiddleBand)
		{
			BuyMarket(-Position);
			_cooldownRemaining = SignalCooldownBars;
		}
		else if (_cooldownRemaining == 0 &&
			previousClosePrice <= previousUpperBand &&
			candle.ClosePrice > previousUpperBand &&
			_seasonalStrength > SeasonalThreshold &&
			Position <= 0)
		{
			BuyMarket(Volume + (Position < 0 ? -Position : 0m));
			_cooldownRemaining = SignalCooldownBars;
		}
		else if (_cooldownRemaining == 0 &&
			previousClosePrice >= previousLowerBand &&
			candle.ClosePrice < previousLowerBand &&
			_seasonalStrength < -SeasonalThreshold &&
			Position >= 0)
		{
			SellMarket(Volume + (Position > 0 ? Position : 0m));
			_cooldownRemaining = SignalCooldownBars;
		}

		_previousUpperBand = upperBand;
		_previousLowerBand = lowerBand;
		_previousMiddleBand = middleBand;
		_previousClosePrice = candle.ClosePrice;
	}
	
	private void UpdateSeasonalStrength(DateTimeOffset time)
	{
		// Get current month
		Months currentMonth = (Months)time.Month;
		
		// Get historical return for this month
		_seasonalStrength = _monthlyReturns[currentMonth];
		
		// Log seasonal information at the beginning of each month
		if (time.Day == 1)
		{
			LogInfo($"Monthly Seasonal Data: {currentMonth} has historical strength of {_seasonalStrength:F2} over {SeasonalDataCount} years");
		}
	}
	
	/// <summary>
	/// Enumeration for months of the year.
	/// </summary>
	private enum Months
	{
		January = 1,
		February,
		March,
		April,
		May,
		June,
		July,
		August,
		September,
		October,
		November,
		December
	}
}
