using System;
using System.Collections.Generic;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// NY Opening Range Breakout with retest.
/// </summary>
public class NyOrbCpStrategy : Strategy
{
	private readonly StrategyParam<decimal> _minRangePoints;
	private readonly StrategyParam<decimal> _riskReward;
	private readonly StrategyParam<int> _maxTradesPerSession;
	private readonly StrategyParam<decimal> _maxDailyLoss;
	private readonly StrategyParam<DataType> _candleType;
	
	private SimpleMovingAverage _volumeSma;
	private ExponentialMovingAverage _ema;
	private VolumeWeightedMovingAverage _vwap;
	
	private decimal? _nyHigh;
	private decimal? _nyLow;
	private bool _nyRangeDone;
	private int _nyTradeCount;
	private decimal _dailyProfit;
	private decimal _prevPnL;
	private DateTime _currentDate;
	private decimal _stopPrice;
	private decimal _takePrice;
	
	/// <summary>
	/// Minimum NY range size.
	/// </summary>
	public decimal MinRangePoints
	{
		get => _minRangePoints.Value;
		set => _minRangePoints.Value = value;
	}
	
	/// <summary>
	/// Risk/reward ratio.
	/// </summary>
	public decimal RiskReward
	{
		get => _riskReward.Value;
		set => _riskReward.Value = value;
	}
	
	/// <summary>
	/// Max trades per session.
	/// </summary>
	public int MaxTradesPerSession
	{
		get => _maxTradesPerSession.Value;
		set => _maxTradesPerSession.Value = value;
	}
	
	/// <summary>
	/// Max daily loss.
	/// </summary>
	public decimal MaxDailyLoss
	{
		get => _maxDailyLoss.Value;
		set => _maxDailyLoss.Value = value;
	}
	
	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	public NyOrbCpStrategy()
	{
		_minRangePoints = Param(nameof(MinRangePoints), 60m)
		.SetDisplay("Minimum NY Range (points)", string.Empty, "Strategy Parameters")
		.SetCanOptimize(true);
		
		_riskReward = Param(nameof(RiskReward), 3m)
		.SetDisplay("Risk/Reward Ratio", string.Empty, "Strategy Parameters")
		.SetCanOptimize(true);
		
		_maxTradesPerSession = Param(nameof(MaxTradesPerSession), 3)
		.SetDisplay("Max Trades per Session", string.Empty, "Strategy Parameters")
		.SetCanOptimize(true);
		
		_maxDailyLoss = Param(nameof(MaxDailyLoss), -1000m)
		.SetDisplay("Max Daily Loss", string.Empty, "Strategy Parameters")
		.SetCanOptimize(true);
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", string.Empty, "Strategy Parameters");
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
		
		_nyHigh = null;
		_nyLow = null;
		_nyRangeDone = false;
		_nyTradeCount = 0;
		_dailyProfit = 0m;
		_prevPnL = 0m;
		_currentDate = default;
		_stopPrice = 0m;
		_takePrice = 0m;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		_ema = new ExponentialMovingAverage { Length = 200 };
		_vwap = new VolumeWeightedMovingAverage();
		_volumeSma = new SimpleMovingAverage { Length = 20 };
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_ema, _vwap, ProcessCandle)
		.Start();
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal ema, decimal vwap)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		var realizedPnL = PnL;
		if (realizedPnL != _prevPnL)
		{
			_dailyProfit += realizedPnL - _prevPnL;
			_prevPnL = realizedPnL;
		}
		
		var date = candle.OpenTime.Date;
		if (date != _currentDate)
		{
			_nyHigh = null;
			_nyLow = null;
			_nyRangeDone = false;
			_nyTradeCount = 0;
			_dailyProfit = 0m;
			_currentDate = date;
		}
		
		var hour = candle.OpenTime.Hour;
		var minute = candle.OpenTime.Minute;
		
		var nyInRange = hour == 9 && minute >= 30 && minute < 45;
		var nyRangeEnd = hour == 9 && minute == 45;
		
		if (nyInRange)
		{
			_nyHigh = _nyHigh is null ? candle.HighPrice : Math.Max(_nyHigh.Value, candle.HighPrice);
			_nyLow = _nyLow is null ? candle.LowPrice : Math.Min(_nyLow.Value, candle.LowPrice);
		}
		
		if (nyRangeEnd && !_nyRangeDone && _nyHigh != null && _nyLow != null)
		_nyRangeDone = true;
		
		var volumeAvg = _volumeSma.Process(candle.TotalVolume, candle.OpenTime, true).ToDecimal();
		if (!_volumeSma.IsFormed || _nyHigh is null || _nyLow is null)
		return;
		
		var nyRangeSize = _nyHigh.Value - _nyLow.Value;
		var validRange = _nyRangeDone && nyRangeSize >= MinRangePoints;
		
		var longBreakout = validRange && candle.HighPrice > _nyHigh;
		var longRetest = longBreakout && candle.LowPrice <= _nyHigh && candle.ClosePrice > _nyHigh;
		
		var shortBreakout = validRange && candle.LowPrice < _nyLow;
		var shortRetest = shortBreakout && candle.HighPrice >= _nyLow && candle.ClosePrice < _nyLow;
		
		var volumeFilter = candle.TotalVolume > volumeAvg;
		var trendUp = candle.ClosePrice > ema && candle.ClosePrice > vwap;
		var trendDown = candle.ClosePrice < ema && candle.ClosePrice < vwap;
		var canTrade = _dailyProfit > MaxDailyLoss;
		
		var longCondition = longRetest && _nyTradeCount < MaxTradesPerSession && trendUp && volumeFilter && canTrade;
		var shortCondition = shortRetest && _nyTradeCount < MaxTradesPerSession && trendDown && volumeFilter && canTrade;
		
		if (longCondition)
		{
			BuyMarket();
			_nyTradeCount++;
			_stopPrice = candle.ClosePrice - nyRangeSize * 0.33m;
			_takePrice = candle.ClosePrice + nyRangeSize * 0.33m * RiskReward;
		}
		else if (shortCondition)
		{
			SellMarket();
			_nyTradeCount++;
			_stopPrice = candle.ClosePrice + nyRangeSize * 0.33m;
			_takePrice = candle.ClosePrice - nyRangeSize * 0.33m * RiskReward;
		}
		
		if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takePrice)
			SellMarket();
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takePrice)
			BuyMarket();
		}
	}
}
