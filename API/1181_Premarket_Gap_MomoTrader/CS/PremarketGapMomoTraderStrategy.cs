using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trades a single long breakout after a gap up during the premarket.
/// Position size depends on candle body percent.
/// </summary>
public class PremarketGapMomoTraderStrategy : Strategy
{
	private readonly StrategyParam<decimal> _minGainPct;
	private readonly StrategyParam<int> _minVolume;
	private readonly StrategyParam<bool> _useSession;
	private readonly StrategyParam<DataType> _candleType;
	
	private bool _isTradedToday;
	private bool _inMomentumTrade;
	private decimal _lastCandleVolume;
	private decimal _previousClose;
	private DateTime _currentDate;
	
	/// <summary>
	/// Minimum percent gain from previous close to enter.
	/// </summary>
	public decimal MinGainPct
	{
		get => _minGainPct.Value;
		set => _minGainPct.Value = value;
	}
	
	/// <summary>
	/// Minimum volume required for entry.
	/// </summary>
	public int MinVolume
	{
		get => _minVolume.Value;
		set => _minVolume.Value = value;
	}
	
	/// <summary>
	/// Restrict trading to premarket session 04:00–09:30.
	/// </summary>
	public bool UseSession
	{
		get => _useSession.Value;
		set => _useSession.Value = value;
	}
	
	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Initializes a new instance of the <see cref="PremarketGapMomoTraderStrategy"/>.
	/// </summary>
	public PremarketGapMomoTraderStrategy()
	{
		_minGainPct = Param(nameof(MinGainPct), 5m)
		.SetDisplay("Min % Gain for Entry", "Minimum percent gain from previous close", "General")
		.SetCanOptimize(true)
		.SetOptimize(1m, 10m, 1m);
		
		_minVolume = Param(nameof(MinVolume), 15000)
		.SetGreaterThanZero()
		.SetDisplay("Min Volume for Entry", "Minimum candle volume required", "General")
		.SetCanOptimize(true)
		.SetOptimize(1000, 50000, 1000);
		
		_useSession = Param(nameof(UseSession), true)
		.SetDisplay("Restrict to Premarket", "Trade only during premarket 04:00–09:30", "General");
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_isTradedToday = false;
		_inMomentumTrade = false;
		_lastCandleVolume = 0m;
		_previousClose = 0m;
		_currentDate = default;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
		
		StartProtection();
	}
	
	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		var candleDate = candle.OpenTime.Date;
		
		if (_currentDate != candleDate)
		{
			_isTradedToday = false;
			_currentDate = candleDate;
		}
		
		if (_previousClose == 0m)
		{
			_previousClose = candle.ClosePrice;
			return;
		}
		
		var prevClose = _previousClose;
		var candleMovePct = prevClose == 0m ? 0m : ((candle.ClosePrice - prevClose) / prevClose) * 100m;
		var candleRange = candle.HighPrice - candle.LowPrice;
		var candleBody = Math.Abs(candle.ClosePrice - candle.OpenPrice);
		var bodyPct = candleRange == 0m ? 0m : (candleBody / candleRange) * 100m;
		var isGreen = candle.ClosePrice > candle.OpenPrice;
		var enoughVolume = candle.TotalVolume > MinVolume;
		var bodyPosSizePercent = bodyPct >= 90m ? 100m :
		bodyPct >= 85m ? 50m :
		bodyPct >= 75m ? 25m : 0m;
		
		var time = candle.OpenTime.TimeOfDay;
		var isPreMarket = time >= new TimeSpan(4, 0, 0) && time < new TimeSpan(9, 30, 0);
		var validSession = UseSession ? isPreMarket : true;
		var enterCondition = candleMovePct >= MinGainPct && isGreen && enoughVolume && bodyPosSizePercent > 0m;
		var enterTrade = enterCondition && validSession && !_isTradedToday;
		
		if (enterTrade)
		{
			var portfolioValue = Portfolio.CurrentValue ?? 0m;
			var qty = Math.Floor((portfolioValue * bodyPosSizePercent / 100m) / candle.ClosePrice);
			if (qty > 0m)
			{
				BuyMarket(qty);
				_isTradedToday = true;
				_inMomentumTrade = true;
				_lastCandleVolume = candle.TotalVolume;
			}
		}
		else if (Position > 0)
		{
			if (_inMomentumTrade)
			{
				var nextBarGreen = isGreen;
				var volumeIncreasing = candle.TotalVolume > _lastCandleVolume;
				
				if (nextBarGreen && volumeIncreasing)
				{
					_lastCandleVolume = candle.TotalVolume;
				}
				else
				{
					SellMarket(Position);
					_inMomentumTrade = false;
				}
			}
			else
			{
				_inMomentumTrade = true;
				_lastCandleVolume = candle.TotalVolume;
			}
		}
		else
		{
			_inMomentumTrade = false;
			_lastCandleVolume = 0m;
		}
		
		_previousClose = candle.ClosePrice;
	}
}
