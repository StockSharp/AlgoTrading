using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;
	
	/// <summary>
	/// Strategy that trades box breakouts with RSI divergence and moving average trend filter.
	/// </summary>
	public class KaitoBoxWithRsiDivStrategy : Strategy
	{
	private readonly StrategyParam<int> _boxLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _ma20Period;
	private readonly StrategyParam<int> _ma50Period;
	private readonly StrategyParam<int> _ma100Period;
	private readonly StrategyParam<int> _ma200Period;
	private readonly StrategyParam<DataType> _candleType;
	
	private decimal? _prevLow;
	private decimal? _prevLowRsi;
	private decimal? _prevHigh;
	private decimal? _prevHighRsi;
	
	/// <summary>
	/// Length of the box range for high/low detection.
	/// </summary>
	public int BoxLength
	{
	get => _boxLength.Value;
	set => _boxLength.Value = value;
	}
	
	/// <summary>
	/// Length of the RSI indicator.
	/// </summary>
	public int RsiLength
	{
	get => _rsiLength.Value;
	set => _rsiLength.Value = value;
	}
	
	/// <summary>
	/// Period for the 20-bar moving average.
	/// </summary>
	public int Ma20Period
	{
	get => _ma20Period.Value;
	set => _ma20Period.Value = value;
	}
	
	/// <summary>
	/// Period for the 50-bar moving average.
	/// </summary>
	public int Ma50Period
	{
	get => _ma50Period.Value;
	set => _ma50Period.Value = value;
	}
	
	/// <summary>
	/// Period for the 100-bar moving average.
	/// </summary>
	public int Ma100Period
	{
	get => _ma100Period.Value;
	set => _ma100Period.Value = value;
	}
	
	/// <summary>
	/// Period for the 200-bar moving average.
	/// </summary>
	public int Ma200Period
	{
	get => _ma200Period.Value;
	set => _ma200Period.Value = value;
	}
	
	/// <summary>
	/// Type of candles used by the strategy.
	/// </summary>
	public DataType CandleType
	{
	get => _candleType.Value;
	set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public KaitoBoxWithRsiDivStrategy()
	{
	_boxLength = Param(nameof(BoxLength), 3)
	.SetDisplay("Box Length", "Length of the box range", "General")
	.SetCanOptimize(true)
	.SetOptimize(2, 10, 1);
	
	_rsiLength = Param(nameof(RsiLength), 2)
	.SetDisplay("RSI Length", "Length of RSI", "General")
	.SetCanOptimize(true)
	.SetOptimize(2, 14, 1);
	
	_ma20Period = Param(nameof(Ma20Period), 20)
	.SetDisplay("MA20 Period", "Short moving average period", "Trend");
	
	_ma50Period = Param(nameof(Ma50Period), 50)
	.SetDisplay("MA50 Period", "Medium moving average period", "Trend");
	
	_ma100Period = Param(nameof(Ma100Period), 100)
	.SetDisplay("MA100 Period", "Medium moving average period", "Trend");
	
	_ma200Period = Param(nameof(Ma200Period), 200)
	.SetDisplay("MA200 Period", "Long moving average period", "Trend");
	
	_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(3).TimeFrame())
	.SetDisplay("Candle Type", "Timeframe for candles", "General");
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
	
	StartProtection();
	
	var highest = new Highest { Length = BoxLength };
	var lowest = new Lowest { Length = BoxLength };
	var rsi = new RelativeStrengthIndex { Length = RsiLength };
	var ma20 = new SimpleMovingAverage { Length = Ma20Period };
	var ma50 = new SimpleMovingAverage { Length = Ma50Period };
	var ma100 = new SimpleMovingAverage { Length = Ma100Period };
	var ma200 = new SimpleMovingAverage { Length = Ma200Period };
	
	var subscription = SubscribeCandles(CandleType);
	subscription
	.Bind(highest, lowest, rsi, ma20, ma50, ma100, ma200, ProcessCandle)
	.Start();
	
	var area = CreateChartArea();
	if (area != null)
	{
	DrawCandles(area, subscription);
	DrawIndicator(area, ma20);
	DrawIndicator(area, ma50);
	DrawIndicator(area, ma100);
	DrawIndicator(area, ma200);
	DrawOwnTrades(area);
	}
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal highestHigh, decimal lowestLow, decimal rsiValue, decimal ma20, decimal ma50, decimal ma100, decimal ma200)
	{
	if (candle.State != CandleStates.Finished)
	return;
	
	if (!IsFormedAndOnlineAndAllowTrading())
	return;
	
	const decimal rsiOverbought = 80m;
	const decimal rsiOversold = 13m;
	
	bool downTrend = ma20 < ma200 && ma50 < ma200 && ma100 < ma200;
	bool upTrend = ma20 > ma200 && ma50 > ma200 && ma100 > ma200;
	
	bool bullishDiv = _prevLow is decimal prevLow && _prevLowRsi is decimal prevLowRsi &&
	candle.LowPrice < prevLow && rsiValue > prevLowRsi && rsiValue < rsiOversold;
	
	bool bearishDiv = _prevHigh is decimal prevHigh && _prevHighRsi is decimal prevHighRsi &&
	candle.HighPrice > prevHigh && rsiValue < prevHighRsi && rsiValue > rsiOverbought;
	
	bool longSignal = candle.ClosePrice <= lowestLow && bullishDiv;
	bool shortSignal = candle.ClosePrice >= highestHigh && bearishDiv;
	
	if (downTrend && longSignal && Position <= 0)
	BuyMarket();
	
	if (upTrend && shortSignal && Position > 0)
	SellMarket(Position);
	
	_prevLow = lowestLow;
	_prevLowRsi = rsiValue;
	_prevHigh = highestHigh;
	_prevHighRsi = rsiValue;
	}
	}
	
