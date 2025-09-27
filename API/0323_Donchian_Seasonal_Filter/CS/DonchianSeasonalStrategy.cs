using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Collections;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
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
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _seasonalDataCount;
	
	private DonchianChannels _donchian;
	private bool _isLongPosition;
	private bool _isShortPosition;
	
	// Seasonal data storage
	private readonly SynchronizedDictionary<Month, decimal> _monthlyReturns = [];
	
	// Simulated seasonal data count

	// Current values
	private decimal _upperBand;
	private decimal _lowerBand;
	private decimal _middleBand;
	private decimal _seasonalStrength;

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
		_donchianPeriod = Param(nameof(DonchianPeriod), 20)
			.SetDisplay("Donchian Period", "Donchian Channel period", "Donchian")
			.SetCanOptimize(true)
			.SetOptimize(10, 50, 5);
			
		_seasonalThreshold = Param(nameof(SeasonalThreshold), 0.5m)
			.SetDisplay("Seasonal Threshold", "Seasonal strength threshold for entry", "Seasonal")
			.SetCanOptimize(true)
			.SetOptimize(0.2m, 1.0m, 0.1m);

		_seasonalDataCount = Param(nameof(SeasonalDataCount), 5)
			.SetDisplay("Seasonal Years", "Years of seasonal data", "Seasonal")
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetOptimize(1, 10, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
			
		// Initialize monthly returns with neutral values
		foreach (Month month in Enum.GetValues(typeof(Month)))
		{
			_monthlyReturns[month] = 0m;
		}
		
		// Simulated historical seasonal data (in a real strategy, this would come from analysis of historical data)
		// These are example values that suggest certain months tend to be bullish or bearish
		_monthlyReturns[Month.January] = 0.8m;
		_monthlyReturns[Month.February] = 0.3m;
		_monthlyReturns[Month.March] = 0.6m;
		_monthlyReturns[Month.April] = 0.9m;
		_monthlyReturns[Month.May] = 0.2m;
		_monthlyReturns[Month.June] = -0.4m;
		_monthlyReturns[Month.July] = -0.2m;
		_monthlyReturns[Month.August] = -0.7m;
		_monthlyReturns[Month.September] = -0.9m;
		_monthlyReturns[Month.October] = -0.1m;
		_monthlyReturns[Month.November] = 0.5m;
		_monthlyReturns[Month.December] = 0.7m;
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

		_isLongPosition = false;
		_isShortPosition = false;
		_upperBand = 0;
		_middleBand = 0;
		_lowerBand = 0;
		_seasonalStrength = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

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

		var donchianTyped = (DonchianChannelsValue)donchianValue;

		if (donchianTyped.UpperBand is not decimal upperBand ||
			donchianTyped.LowerBand is not decimal lowerBand ||
			donchianTyped.Middle is not decimal middleBand)
		{
			return;
		}

		// Save current Donchian Channel values
		_upperBand = upperBand;
		_middleBand = middleBand;
		_lowerBand = lowerBand;
		
		// Calculate seasonal strength for current month
		UpdateSeasonalStrength(candle.OpenTime);
		
		if (!IsFormedAndOnlineAndAllowTrading())
			return;
			
		// Trading logic
		// Buy when price breaks above upper band and seasonal strength is positive (above threshold)
		if (candle.ClosePrice > _upperBand && _seasonalStrength > SeasonalThreshold && Position <= 0)
		{
			BuyMarket(Volume);
			LogInfo($"Buy Signal: Price {candle.ClosePrice:F2} > Upper Band {_upperBand:F2}, Seasonal Strength {_seasonalStrength:F2}");
			_isLongPosition = true;
			_isShortPosition = false;
		}
		// Sell when price breaks below lower band and seasonal strength is negative (below negative threshold)
		else if (candle.ClosePrice < _lowerBand && _seasonalStrength < -SeasonalThreshold && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			LogInfo($"Sell Signal: Price {candle.ClosePrice:F2} < Lower Band {_lowerBand:F2}, Seasonal Strength {_seasonalStrength:F2}");
			_isLongPosition = false;
			_isShortPosition = true;
		}
		// Exit long position when price falls below middle band
		else if (_isLongPosition && candle.ClosePrice < _middleBand)
		{
			SellMarket(Position);
			LogInfo($"Exit Long: Price {candle.ClosePrice:F2} fell below Middle Band {_middleBand:F2}");
			_isLongPosition = false;
		}
		// Exit short position when price rises above middle band
		else if (_isShortPosition && candle.ClosePrice > _middleBand)
		{
			BuyMarket(Math.Abs(Position));
			LogInfo($"Exit Short: Price {candle.ClosePrice:F2} rose above Middle Band {_middleBand:F2}");
			_isShortPosition = false;
		}
	}
	
	private void UpdateSeasonalStrength(DateTimeOffset time)
	{
		// Get current month
		Month currentMonth = (Month)time.Month;
		
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
	private enum Month
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