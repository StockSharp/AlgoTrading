
using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class SwingCyborgStrategy : Strategy
{
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<TrendType> _trendPrediction;
	private readonly StrategyParam<TrendTimeframe> _trendTimeframe;
	private readonly StrategyParam<DateTimeOffset> _trendStart;
	private readonly StrategyParam<DateTimeOffset> _trendEnd;
	private readonly StrategyParam<Aggressiveness> _aggressiveness;
	
	private DataType _candleType;
	private int _takeProfitSteps;
	private int _stopLossSteps;
	
	/// <summary>
	/// Order volume.
	/// </summary>
	public decimal Volume
	{
	get => _volume.Value;
	set => _volume.Value = value;
	}
	
	/// <summary>
	/// Expected trend direction.
	/// </summary>
	public TrendType TrendPrediction
	{
	get => _trendPrediction.Value;
	set => _trendPrediction.Value = value;
	}
	
	/// <summary>
	/// Timeframe of expected trend.
	/// </summary>
	public TrendTimeframe TrendTimeframe
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
	_volume = Param(nameof(Volume), 0.1m)
	.SetGreaterThanZero()
	.SetDisplay("Volume", "Order volume", "Trading");
	
	_trendPrediction = Param(nameof(TrendPrediction), TrendType.Uptrend)
	.SetDisplay("Trend Prediction", "Expected trend direction", "General");
	
	_trendTimeframe = Param(nameof(TrendTimeframe), TrendTimeframe.H1)
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
	_candleType = TrendTimeframe switch
	{
	TrendTimeframe.M30 => TimeSpan.FromMinutes(30).TimeFrame(),
	TrendTimeframe.H1 => TimeSpan.FromHours(1).TimeFrame(),
	TrendTimeframe.H4 => TimeSpan.FromHours(4).TimeFrame(),
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
	if (TrendPrediction == TrendType.Uptrend && rsiValue <= 65m)
	{
	BuyMarket(Volume);
	}
	else if (TrendPrediction == TrendType.Downtrend && rsiValue >= 35m)
	{
	SellMarket(Volume);
	}
	}
	}
	
	/// <summary>
	/// Expected trend direction.
	/// </summary>
	public enum TrendType
	{
	Uptrend,
	Downtrend
	}
	
	/// <summary>
	/// Timeframe of expected trend.
	/// </summary>
	public enum TrendTimeframe
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
