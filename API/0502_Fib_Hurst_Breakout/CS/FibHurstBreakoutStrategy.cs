using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Fibonacci levels with Hurst exponent filter.
/// Enters long when price crosses above 61.8% level and Hurst > 0.5.
/// Enters short when price crosses below 38.2% level and Hurst < 0.5.
/// </summary>
public class FibHurstBreakoutStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _hurstPeriod;
	private readonly StrategyParam<int> _maxTradesPerDay;
	private readonly StrategyParam<int> _maxTotalTrades;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<decimal> _riskReward;
	
	private HurstExponent _dailyHurstIndicator;
	private decimal _dailyHurst;
	private decimal _fib382;
	private decimal _fib618;
	private decimal _prevClose;
	private int _tradesToday;
	private int _globalTrades;
	private DateTime _currentDay;
	
	/// <summary>
	/// Candle type for main timeframe.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Period for Hurst exponent calculation.
	/// </summary>
	public int HurstPeriod
	{
		get => _hurstPeriod.Value;
		set => _hurstPeriod.Value = value;
	}
	
	/// <summary>
	/// Maximum trades per day.
	/// </summary>
	public int MaxTradesPerDay
	{
		get => _maxTradesPerDay.Value;
		set => _maxTradesPerDay.Value = value;
	}
	
	/// <summary>
	/// Maximum trades overall.
	/// </summary>
	public int MaxTotalTrades
	{
		get => _maxTotalTrades.Value;
		set => _maxTotalTrades.Value = value;
	}
	
	/// <summary>
	/// Risk percent for stop-loss.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}
	
	/// <summary>
	/// Risk-reward multiplier for take-profit.
	/// </summary>
	public decimal RiskReward
	{
		get => _riskReward.Value;
		set => _riskReward.Value = value;
	}
	
	/// <summary>
	/// Initialize <see cref="FibHurstBreakoutStrategy"/>.
	/// </summary>
	public FibHurstBreakoutStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles for strategy", "Parameters");
		
		_hurstPeriod = Param(nameof(HurstPeriod), 50)
		.SetGreaterThanZero()
		.SetDisplay("Hurst Period", "Period for Hurst exponent", "Parameters");
		
		_maxTradesPerDay = Param(nameof(MaxTradesPerDay), 5)
		.SetGreaterThanZero()
		.SetDisplay("Max Trades Per Day", "Maximum number of trades per day", "Risk");
		
		_maxTotalTrades = Param(nameof(MaxTotalTrades), 510)
		.SetGreaterThanZero()
		.SetDisplay("Max Total Trades", "Maximum number of trades overall", "Risk");
		
		_riskPercent = Param(nameof(RiskPercent), 2m)
		.SetDisplay("Risk Percent", "Stop-loss percent per trade", "Risk");
		
		_riskReward = Param(nameof(RiskReward), 2m)
		.SetDisplay("Risk Reward", "Take profit multiplier", "Risk");
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
		_dailyHurstIndicator = null;
		_dailyHurst = 0;
		_fib382 = 0;
		_fib618 = 0;
		_prevClose = 0;
		_tradesToday = 0;
		_globalTrades = 0;
		_currentDay = default;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		_dailyHurstIndicator = new HurstExponent { Length = HurstPeriod };
		
		var mainSub = SubscribeCandles(CandleType);
		mainSub.Bind(ProcessCandle).Start();
		
		var dailySub = SubscribeCandles(TimeSpan.FromDays(1).TimeFrame());
		dailySub.Bind(_dailyHurstIndicator, ProcessDaily).Start();
		
		StartProtection(
		takeProfit: new Unit(RiskPercent * RiskReward, UnitTypes.Percent),
		stopLoss: new Unit(RiskPercent, UnitTypes.Percent)
		);
	}
	
	private void ProcessDaily(ICandleMessage candle, decimal hurstValue)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		var high = candle.HighPrice;
		var low = candle.LowPrice;
		var range = high - low;
		_fib382 = low + 0.382m * range;
		_fib618 = low + 0.618m * range;
		_dailyHurst = hurstValue;
	}
	
	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		var day = candle.OpenTime.Date;
		if (_currentDay != day)
		{
			_currentDay = day;
			_tradesToday = 0;
		}
		
		var crossUp = _prevClose <= _fib618 && candle.ClosePrice > _fib618;
		var crossDown = _prevClose >= _fib382 && candle.ClosePrice < _fib382;
		
		if (_dailyHurst > 0.5m && crossUp && Position <= 0 && _tradesToday < MaxTradesPerDay && _globalTrades < MaxTotalTrades)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_tradesToday++;
			_globalTrades++;
		}
		else if (_dailyHurst < 0.5m && crossDown && Position >= 0 && _tradesToday < MaxTradesPerDay && _globalTrades < MaxTotalTrades)
		{
			SellMarket(Volume + Math.Abs(Position));
			_tradesToday++;
			_globalTrades++;
		}
		
		_prevClose = candle.ClosePrice;
	}
}

