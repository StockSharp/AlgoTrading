using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that monitors confirmed COVID-19 cases and trades based on growth ratio.
/// </summary>
public class CovidStatisticsTrackerStrategy : Strategy
{
	private readonly StrategyParam<string> _region;
	private readonly StrategyParam<int> _lookback;
	private readonly StrategyParam<DataType> _candleType;
	
	private decimal? _prev1;
	private decimal? _prev2;
	
	/// <summary>
	/// Region code used to build the ticker id.
	/// </summary>
	public string Region
	{
		get => _region.Value;
		set => _region.Value = value;
	}
	
	/// <summary>
	/// Number of candles for growth calculation.
	/// </summary>
	public int Lookback
	{
		get => _lookback.Value;
		set => _lookback.Value = value;
	}
	
	/// <summary>
	/// Candle type used for analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Initializes a new instance of <see cref="CovidStatisticsTrackerStrategy"/>.
	/// </summary>
	public CovidStatisticsTrackerStrategy()
	{
		_region = Param(nameof(Region), "US")
		.SetDisplay("Region", "Region code for COVID data", "General");
		
		_lookback = Param(nameof(Lookback), 2)
		.SetGreaterThanZero()
		.SetDisplay("Lookback", "Candles for growth ratio", "General");
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe for analysis", "General");
	}
	
	private Security CreateSecurity() => new() { Id = $"COVID19:CONFIRMED_{Region}" };
	
	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(CreateSecurity(), CandleType)];
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		Security = CreateSecurity();
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();
	}
	
	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		var current = candle.ClosePrice;
		
		if (_prev1 is null)
		{
			_prev1 = current;
			return;
		}
		
		if (_prev2 is null)
		{
			_prev2 = _prev1;
			_prev1 = current;
			return;
		}
		
		var denom = _prev1.Value - _prev2.Value;
		if (denom == 0)
		{
			_prev2 = _prev1;
			_prev1 = current;
			return;
		}
		
		var growth = (current - _prev1.Value) / denom;
		
		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prev2 = _prev1;
			_prev1 = current;
			return;
		}
		
		if (growth > 1m && Position <= 0)
		SellMarket(Volume + Math.Abs(Position));
		else if (growth < 1m && Position >= 0)
		BuyMarket(Volume + Math.Abs(Position));
		
		_prev2 = _prev1;
		_prev1 = current;
	}
}
