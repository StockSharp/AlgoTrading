using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Combined RSI, Stochastic, and Moving Average strategy.
/// The moving average defines the trend direction.
/// Entries occur on RSI and Stochastic oversold/overbought signals in the trend direction.
/// Exits trigger when oscillators leave extreme zones.
/// </summary>
public class RsiStochasticMaStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiUpperLevel;
	private readonly StrategyParam<decimal> _rsiLowerLevel;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _stochKPeriod;
	private readonly StrategyParam<int> _stochDPeriod;
	private readonly StrategyParam<decimal> _stochUpperLevel;
	private readonly StrategyParam<decimal> _stochLowerLevel;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<DataType> _candleType;
	
	private RelativeStrengthIndex _rsi;
	private SimpleMovingAverage _ma;
	private StochasticOscillator _stochastic;
	
	/// <summary>
	/// RSI calculation period.
	/// </summary>
	public int RsiPeriod
	{
	get => _rsiPeriod.Value;
	set => _rsiPeriod.Value = value;
	}
	
	/// <summary>
	/// RSI level considered overbought.
	/// </summary>
	public decimal RsiUpperLevel
	{
	get => _rsiUpperLevel.Value;
	set => _rsiUpperLevel.Value = value;
	}
	
	/// <summary>
	/// RSI level considered oversold.
	/// </summary>
	public decimal RsiLowerLevel
	{
	get => _rsiLowerLevel.Value;
	set => _rsiLowerLevel.Value = value;
	}
	
	/// <summary>
	/// Moving average period.
	/// </summary>
	public int MaPeriod
	{
	get => _maPeriod.Value;
	set => _maPeriod.Value = value;
	}
	
	/// <summary>
	/// Stochastic %K period.
	/// </summary>
	public int StochKPeriod
	{
	get => _stochKPeriod.Value;
	set => _stochKPeriod.Value = value;
	}
	
	/// <summary>
	/// Stochastic %D smoothing period.
	/// </summary>
	public int StochDPeriod
	{
	get => _stochDPeriod.Value;
	set => _stochDPeriod.Value = value;
	}
	
	/// <summary>
	/// Stochastic overbought level.
	/// </summary>
	public decimal StochUpperLevel
	{
	get => _stochUpperLevel.Value;
	set => _stochUpperLevel.Value = value;
	}
	
	/// <summary>
	/// Stochastic oversold level.
	/// </summary>
	public decimal StochLowerLevel
	{
	get => _stochLowerLevel.Value;
	set => _stochLowerLevel.Value = value;
	}
	
	/// <summary>
	/// Trade volume.
	/// </summary>
	public decimal Volume
	{
	get => _volume.Value;
	set => _volume.Value = value;
	}
	
	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
	get => _candleType.Value;
	set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Constructor.
	/// </summary>
	public RsiStochasticMaStrategy()
	{
	_rsiPeriod = Param(nameof(RsiPeriod), 3)
	.SetDisplay("RSI Period", "RSI calculation period", "RSI")
	.SetCanOptimize(true)
	.SetOptimize(2, 14, 1);
	
	_rsiUpperLevel = Param(nameof(RsiUpperLevel), 80m)
	.SetDisplay("RSI Upper Level", "RSI overbought level", "RSI")
	.SetCanOptimize(true)
	.SetOptimize(60m, 90m, 5m);
	
	_rsiLowerLevel = Param(nameof(RsiLowerLevel), 20m)
	.SetDisplay("RSI Lower Level", "RSI oversold level", "RSI")
	.SetCanOptimize(true)
	.SetOptimize(10m, 40m, 5m);
	
	_maPeriod = Param(nameof(MaPeriod), 150)
	.SetDisplay("MA Period", "Moving average period", "Trend")
	.SetCanOptimize(true)
	.SetOptimize(50, 200, 10);
	
	_stochKPeriod = Param(nameof(StochKPeriod), 6)
	.SetDisplay("Stochastic K", "%K period", "Stochastic")
	.SetCanOptimize(true)
	.SetOptimize(5, 20, 1);
	
	_stochDPeriod = Param(nameof(StochDPeriod), 3)
	.SetDisplay("Stochastic D", "%D smoothing period", "Stochastic")
	.SetCanOptimize(true)
	.SetOptimize(2, 10, 1);
	
	_stochUpperLevel = Param(nameof(StochUpperLevel), 70m)
	.SetDisplay("Stochastic Upper", "Stochastic overbought level", "Stochastic")
	.SetCanOptimize(true)
	.SetOptimize(60m, 90m, 5m);
	
	_stochLowerLevel = Param(nameof(StochLowerLevel), 30m)
	.SetDisplay("Stochastic Lower", "Stochastic oversold level", "Stochastic")
	.SetCanOptimize(true)
	.SetOptimize(10m, 40m, 5m);
	
	_volume = Param(nameof(Volume), 1m)
	.SetDisplay("Volume", "Order volume", "General");
	
	_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
	.SetDisplay("Candle Type", "Candle type", "General");
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
	
	_rsi = new RelativeStrengthIndex { Length = RsiPeriod };
	_ma = new SimpleMovingAverage { Length = MaPeriod };
	_stochastic = new StochasticOscillator
	{
	K = { Length = StochKPeriod },
	D = { Length = StochDPeriod }
	};
	
	var subscription = SubscribeCandles(CandleType);
	subscription
	.BindEx(_ma, _rsi, _stochastic, ProcessCandle)
	.Start();
	
	StartProtection();
	}
	
	private void ProcessCandle(ICandleMessage candle, IIndicatorValue maValue, IIndicatorValue rsiValue, IIndicatorValue stochValue)
	{
	if (candle.State != CandleStates.Finished)
	return;
	
	if (!IsFormedAndOnlineAndAllowTrading())
	return;
	
	var ma = maValue.ToDecimal();
	var rsi = rsiValue.ToDecimal();
	var stochTyped = (StochasticOscillatorValue)stochValue;
	
	if (stochTyped.K is not decimal k || stochTyped.D is not decimal d)
	return;
	
	var price = candle.ClosePrice;
	
	var isUpTrend = price > ma;
	var isDownTrend = price < ma;
	
	if (isUpTrend && rsi < RsiLowerLevel && k < StochLowerLevel && d < StochLowerLevel && Position <= 0)
	{
	BuyMarket(Volume + Math.Abs(Position));
	}
	else if (isDownTrend && rsi > RsiUpperLevel && k > StochUpperLevel && d > StochUpperLevel && Position >= 0)
	{
	SellMarket(Volume + Math.Abs(Position));
	}
	else if (Position > 0 && (k > StochUpperLevel || rsi > RsiUpperLevel))
	{
	SellMarket(Math.Abs(Position));
	}
	else if (Position < 0 && (k < StochLowerLevel || rsi < RsiLowerLevel))
	{
	BuyMarket(Math.Abs(Position));
	}
	}
	}
