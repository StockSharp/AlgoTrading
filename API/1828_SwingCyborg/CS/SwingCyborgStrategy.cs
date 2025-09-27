
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

using StockSharp.Algo;

namespace StockSharp.Samples.Strategies;

public class SwingCyborgStrategy : Strategy
{
	private readonly StrategyParam<TrendTypes> _trendPrediction;
	private readonly StrategyParam<TrendTimeframes> _trendTimeframe;
	private readonly StrategyParam<DateTimeOffset> _trendStart;
	private readonly StrategyParam<DateTimeOffset> _trendEnd;
	private readonly StrategyParam<Aggressiveness> _aggressiveness;
	
	private DataType _candleType;
	private int _takeProfitSteps;
	private int _stopLossSteps;
	
	
	/// <summary>
	/// Expected trend direction.
	/// </summary>
	public TrendTypes TrendPrediction
	{
	get => _trendPrediction.Value;
	set => _trendPrediction.Value = value;
	}
	
	/// <summary>
	/// Timeframe of expected trend.
	/// </summary>
	public TrendTimeframes TrendTimeframe
	{
	get => _trendTimeframe.Value;
	set => _trendTimeframe.Value = value;
	}
	
	/// <summary>
	/// Beginning of the expected trend.
	/// </summary>
	public DateTimeOffset TrendStart
	{
	get => _trendStart.Value;
	set => _trendStart.Value = value;
	}
	
	/// <summary>
	/// End of the expected trend.
	/// </summary>
	public DateTimeOffset TrendEnd
	{
	get => _trendEnd.Value;
	set => _trendEnd.Value = value;
	}
	
	/// <summary>
	/// Money management preset.
	/// </summary>
	public Aggressiveness Aggressiveness
	{
	get => _aggressiveness.Value;
	set => _aggressiveness.Value = value;
	}
	
	/// <summary>
	/// Initializes <see cref="SwingCyborgStrategy"/>.
	/// </summary>
	public SwingCyborgStrategy()
	{
	
	_trendPrediction = Param(nameof(TrendPrediction), TrendTypes.Uptrend)
	.SetDisplay("Trend Prediction", "Expected trend direction", "General");
	
	_trendTimeframe = Param(nameof(TrendTimeframe), TrendTimeframes.H1)
	.SetDisplay("Trend Timeframe", "Timeframe of expected trend", "General");
	
	_trendStart = Param(nameof(TrendStart), new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero))
	.SetDisplay("Trend Start", "Beginning of the expected trend", "General");
	
	_trendEnd = Param(nameof(TrendEnd), new DateTimeOffset(2099, 1, 1, 0, 0, 0, TimeSpan.Zero))
	.SetDisplay("Trend End", "End of the expected trend", "General");
	
	_aggressiveness = Param(nameof(Aggressiveness), Aggressiveness.Medium)
	.SetDisplay("Aggressiveness", "Money management preset", "Risk");
	}
	
	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
	_candleType = TrendTimeframes switch
	{
	TrendTimeframes.M30 => TimeSpan.FromMinutes(30).TimeFrame(),
	TrendTimeframes.H1 => TimeSpan.FromHours(1).TimeFrame(),
	TrendTimeframes.H4 => TimeSpan.FromHours(4).TimeFrame(),
	_ => TimeSpan.FromHours(1).TimeFrame()
	};
	return [(Security, _candleType)];
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	base.OnStarted(time);
	
	(_takeProfitSteps, _stopLossSteps) = Aggressiveness switch
	{
	Aggressiveness.Low => (300, 200),
	Aggressiveness.Medium => (500, 250),
	Aggressiveness.High => (600, 300),
	_ => (500, 250)
	};
	
	var rsi = new RelativeStrengthIndex { Length = 14 };
	var subscription = SubscribeCandles(_candleType);
	subscription.Bind(rsi, ProcessCandle).Start();
	
	StartProtection(new Unit(_takeProfitSteps, UnitTypes.Step), new Unit(_stopLossSteps, UnitTypes.Step));
	
	var area = CreateChartArea();
	if (area != null)
	{
	DrawCandles(area, subscription);
	DrawIndicator(area, rsi);
	DrawOwnTrades(area);
	}
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
	if (candle.State != CandleStates.Finished)
	return;
	
	var currentTime = candle.OpenTime;
	
	if (currentTime < TrendStart || currentTime > TrendEnd)
	return;
	
	if (!IsFormedAndOnlineAndAllowTrading())
	return;
	
	if (Position == 0)
	{
	if (TrendPrediction == TrendTypes.Uptrend && rsiValue <= 65m)
	{
	BuyMarket(Volume);
	}
	else if (TrendPrediction == TrendTypes.Downtrend && rsiValue >= 35m)
	{
	SellMarket(Volume);
	}
	}
	}
	
	/// <summary>
	/// Expected trend direction.
	/// </summary>
	public enum TrendTypes
	{
	Uptrend,
	Downtrend
	}
	
	/// <summary>
	/// Timeframe of expected trend.
	/// </summary>
	public enum TrendTimeframes
	{
	M30,
	H1,
	H4
	}
	
	/// <summary>
	/// Money management preset.
	/// </summary>
	public enum Aggressiveness
	{
	Low,
	Medium,
	High
	}
	}
