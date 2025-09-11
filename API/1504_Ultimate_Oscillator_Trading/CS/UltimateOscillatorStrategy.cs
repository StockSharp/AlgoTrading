using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trading strategy based on the Ultimate Oscillator.
/// Buys when the oscillator drops below a threshold and exits when price breaks the previous high.
/// </summary>
public class UltimateOscillatorStrategy : Strategy
{
	private readonly StrategyParam<int> _shortPeriod;
	private readonly StrategyParam<int> _mediumPeriod;
	private readonly StrategyParam<int> _longPeriod;
	private readonly StrategyParam<decimal> _buyThreshold;
	private readonly StrategyParam<DataType> _candleType;
	
	private decimal _previousHigh;
	private bool _isFirst = true;
	
	/// <summary>
	/// Short-term period for oscillator.
	/// </summary>
	public int ShortPeriod
	{
		get => _shortPeriod.Value;
		set => _shortPeriod.Value = value;
	}
	
	/// <summary>
	/// Medium-term period for oscillator.
	/// </summary>
	public int MediumPeriod
	{
		get => _mediumPeriod.Value;
		set => _mediumPeriod.Value = value;
	}
	
	/// <summary>
	/// Long-term period for oscillator.
	/// </summary>
	public int LongPeriod
	{
		get => _longPeriod.Value;
		set => _longPeriod.Value = value;
	}
	
	/// <summary>
	/// Buy threshold for the oscillator.
	/// </summary>
	public decimal BuyThreshold
	{
		get => _buyThreshold.Value;
		set => _buyThreshold.Value = value;
	}
	
	/// <summary>
	/// Candle type for processing.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public UltimateOscillatorStrategy()
	{
		_shortPeriod = Param(nameof(ShortPeriod), 6)
		.SetGreaterThanZero()
		.SetDisplay("Short Period", "Short-term period", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(4, 10, 2);
		
		_mediumPeriod = Param(nameof(MediumPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("Medium Period", "Medium-term period", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(8, 20, 2);
		
		_longPeriod = Param(nameof(LongPeriod), 20)
		.SetGreaterThanZero()
		.SetDisplay("Long Period", "Long-term period", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(12, 30, 2);
		
		_buyThreshold = Param(nameof(BuyThreshold), 30m)
		.SetNotNegative()
		.SetDisplay("Buy Threshold", "Buy when oscillator below", "Trading")
		.SetCanOptimize(true)
		.SetOptimize(20m, 40m, 5m);
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");
	}
	
	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		var uo = new UltimateOscillator
		{
			ShortPeriod = ShortPeriod,
			MediumPeriod = MediumPeriod,
			LongPeriod = LongPeriod
			};
			
			var subscription = SubscribeCandles(CandleType);
			subscription
			.Bind(uo, ProcessCandle)
			.Start();
			
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, uo);
				DrawOwnTrades(area);
			}
		}
		
		private void ProcessCandle(ICandleMessage candle, decimal uoValue)
		{
			if (candle.State != CandleStates.Finished)
			return;
			
			if (!IsFormedAndOnlineAndAllowTrading())
			return;
			
			if (_isFirst)
			{
				_previousHigh = candle.HighPrice;
				_isFirst = false;
				return;
			}
			
			if (uoValue < BuyThreshold && Position <= 0)
			{
				var volume = Volume + Math.Abs(Position);
				BuyMarket(volume);
			}
			else if (Position > 0 && candle.ClosePrice > _previousHigh)
			{
				SellMarket(Math.Abs(Position));
			}
			
			_previousHigh = candle.HighPrice;
		}
	}
	
