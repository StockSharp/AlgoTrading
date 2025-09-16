using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trend following strategy using multiple moving averages similar to a traffic light.
/// Enter long when fast average is above slower ones and price is above fast average.
/// Enter short when fast average is below slower ones and price is below fast average.
/// </summary>
public class TrafficLightStrategy : Strategy
{
	private readonly StrategyParam<int> _redMaPeriod;
	private readonly StrategyParam<int> _yellowMaPeriod;
	private readonly StrategyParam<int> _greenMaPeriod;
	private readonly StrategyParam<int> _blueMaPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _useBlueRange;
	private readonly StrategyParam<bool> _closeOnCross;
	private readonly StrategyParam<decimal> _takeProfitTicks;
	private readonly StrategyParam<decimal> _stopLossTicks;

	private ExponentialMovingAverage _blueHigh;
	private ExponentialMovingAverage _blueLow;
	private SimpleMovingAverage _redMa;
	private SimpleMovingAverage _yellowMa;
	private ExponentialMovingAverage _greenMa;

	/// <summary>
	/// Period for the red (slow) SMA.
	/// </summary>
	public int RedMaPeriod
	{
		get => _redMaPeriod.Value;
		set => _redMaPeriod.Value = value;
	}

	/// <summary>
	/// Period for the yellow (medium) SMA.
	/// </summary>
	public int YellowMaPeriod
	{
		get => _yellowMaPeriod.Value;
		set => _yellowMaPeriod.Value = value;
	}

	/// <summary>
	/// Period for the green (fast) EMA.
	/// </summary>
	public int GreenMaPeriod
	{
		get => _greenMaPeriod.Value;
		set => _greenMaPeriod.Value = value;
	}

	/// <summary>
	/// Period for the blue EMA channel.
	/// </summary>
	public int BlueMaPeriod
	{
		get => _blueMaPeriod.Value;
		set => _blueMaPeriod.Value = value;
	}

	/// <summary>
	/// Candle type for strategy calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Use blue EMA channel as entry zone instead of red/yellow range.
	/// </summary>
	public bool UseBlueRange
	{
		get => _useBlueRange.Value;
		set => _useBlueRange.Value = value;
	}

	/// <summary>
	/// Close positions when green EMA crosses the yellow SMA.
	/// </summary>
	public bool CloseOnCross
	{
		get => _closeOnCross.Value;
		set => _closeOnCross.Value = value;
	}

	/// <summary>
	/// Take profit in price steps.
	/// </summary>
	public decimal TakeProfitTicks
	{
		get => _takeProfitTicks.Value;
		set => _takeProfitTicks.Value = value;
	}

	/// <summary>
	/// Stop loss in price steps.
	/// </summary>
	public decimal StopLossTicks
	{
		get => _stopLossTicks.Value;
		set => _stopLossTicks.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public TrafficLightStrategy()
	{
		_redMaPeriod = Param(nameof(RedMaPeriod), 120)
						   .SetGreaterThanZero()
						   .SetDisplay("Red MA", "SMA period representing the slow trend", "Parameters")
						   .SetCanOptimize();

		_yellowMaPeriod = Param(nameof(YellowMaPeriod), 55)
							  .SetGreaterThanZero()
							  .SetDisplay("Yellow MA", "SMA period representing the medium trend", "Parameters")
							  .SetCanOptimize();

		_greenMaPeriod = Param(nameof(GreenMaPeriod), 5)
							 .SetGreaterThanZero()
							 .SetDisplay("Green MA", "EMA period representing the fast trend", "Parameters")
							 .SetCanOptimize();

		_blueMaPeriod = Param(nameof(BlueMaPeriod), 24)
							.SetGreaterThanZero()
							.SetDisplay("Blue MA", "EMA period for channel boundaries", "Parameters")
							.SetCanOptimize();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
						  .SetDisplay("Candle Type", "Timeframe for calculations", "General");

		_useBlueRange = Param(nameof(UseBlueRange), false)
							.SetDisplay("Use Blue Range", "Use blue EMA channel for entry zone", "Strategy");

		_closeOnCross = Param(nameof(CloseOnCross), true)
							.SetDisplay("Close On Cross", "Close position on green/yellow cross", "Strategy");

		_takeProfitTicks = Param(nameof(TakeProfitTicks), 120m)
							   .SetNotNegative()
							   .SetDisplay("Take Profit", "Take profit in price steps", "Protection");

		_stopLossTicks = Param(nameof(StopLossTicks), 60m)
							 .SetNotNegative()
							 .SetDisplay("Stop Loss", "Stop loss in price steps", "Protection");
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

		_blueHigh = new ExponentialMovingAverage { Length = BlueMaPeriod };
		_blueLow = new ExponentialMovingAverage { Length = BlueMaPeriod };
		_redMa = new SimpleMovingAverage { Length = RedMaPeriod };
		_yellowMa = new SimpleMovingAverage { Length = YellowMaPeriod };
		_greenMa = new ExponentialMovingAverage { Length = GreenMaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection(takeProfit: TakeProfitTicks > 0 ? new Unit(TakeProfitTicks, UnitTypes.Step) : new Unit(0m),
						stopLoss: StopLossTicks > 0 ? new Unit(StopLossTicks, UnitTypes.Step) : new Unit(0m));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _redMa);
			DrawIndicator(area, _yellowMa);
			DrawIndicator(area, _greenMa);
			DrawIndicator(area, _blueHigh);
			DrawIndicator(area, _blueLow);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var redVal = _redMa.Process(new CandleIndicatorValue(candle, candle.ClosePrice));
		var yellowVal = _yellowMa.Process(new CandleIndicatorValue(candle, candle.ClosePrice));
		var greenVal = _greenMa.Process(new CandleIndicatorValue(candle, candle.ClosePrice));
		var blueHighVal = _blueHigh.Process(new CandleIndicatorValue(candle, candle.HighPrice));
		var blueLowVal = _blueLow.Process(new CandleIndicatorValue(candle, candle.LowPrice));

		if (!redVal.IsFinal || !yellowVal.IsFinal || !greenVal.IsFinal || !blueHighVal.IsFinal || !blueLowVal.IsFinal)
			return;

		var red = redVal.GetValue<decimal>();
		var yellow = yellowVal.GetValue<decimal>();
		var green = greenVal.GetValue<decimal>();
		var blueHigh = blueHighVal.GetValue<decimal>();
		var blueLow = blueLowVal.GetValue<decimal>();

		var price = candle.ClosePrice;

		var inZone = UseBlueRange ? price < blueHigh && price > blueLow
								  : (price < red && price > yellow) || (price < yellow && price > red);

		if (Position == 0 && inZone)
		{
			if (green > blueHigh && blueHigh > yellow && yellow > red && price > green)
			{
				var volume = Volume + Math.Abs(Position);
				BuyMarket(volume);
			}
			else if (green < blueLow && blueLow < yellow && yellow < red && price < green)
			{
				var volume = Volume + Math.Abs(Position);
				SellMarket(volume);
			}
		}
		else if (CloseOnCross)
		{
			if (Position > 0 && green < yellow)
				ClosePosition();
			else if (Position < 0 && green > yellow)
				ClosePosition();
		}
	}
}
