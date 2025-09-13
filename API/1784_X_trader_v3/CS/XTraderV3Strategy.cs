using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// X Trader V3 strategy based on two median price moving averages.
/// </summary>
public class XTraderV3Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _ma1Period;
	private readonly StrategyParam<int> _ma2Period;
	private readonly StrategyParam<TimeSpan> _startTime;
	private readonly StrategyParam<TimeSpan> _endTime;
	private readonly StrategyParam<bool> _allowBuy;
	private readonly StrategyParam<bool> _allowSell;
	private readonly StrategyParam<bool> _closeOnReverse;
	private readonly StrategyParam<int> _takeProfitTicks;
	private readonly StrategyParam<int> _stopLossTicks;

	private SimpleMovingAverage _ma1;
	private SimpleMovingAverage _ma2;

	private decimal _ma1Prev;
	private decimal _ma1Prev2;
	private decimal _ma2Prev;
	private decimal _ma2Prev2;

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Period of the first moving average.
	/// </summary>
	public int Ma1Period { get => _ma1Period.Value; set => _ma1Period.Value = value; }

	/// <summary>
	/// Period of the second moving average.
	/// </summary>
	public int Ma2Period { get => _ma2Period.Value; set => _ma2Period.Value = value; }

	/// <summary>
	/// Start time of the trading window.
	/// </summary>
	public TimeSpan StartTime { get => _startTime.Value; set => _startTime.Value = value; }

	/// <summary>
	/// End time of the trading window.
	/// </summary>
	public TimeSpan EndTime { get => _endTime.Value; set => _endTime.Value = value; }

	/// <summary>
	/// Allow long positions.
	/// </summary>
	public bool AllowBuy { get => _allowBuy.Value; set => _allowBuy.Value = value; }

	/// <summary>
	/// Allow short positions.
	/// </summary>
	public bool AllowSell { get => _allowSell.Value; set => _allowSell.Value = value; }

	/// <summary>
	/// Close position when opposite signal appears.
	/// </summary>
	public bool CloseOnReverseSignal { get => _closeOnReverse.Value; set => _closeOnReverse.Value = value; }

	/// <summary>
	/// Take profit in ticks.
	/// </summary>
	public int TakeProfitTicks { get => _takeProfitTicks.Value; set => _takeProfitTicks.Value = value; }

	/// <summary>
	/// Stop loss in ticks.
	/// </summary>
	public int StopLossTicks { get => _stopLossTicks.Value; set => _stopLossTicks.Value = value; }

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public XTraderV3Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for candles", "General");

		_ma1Period = Param(nameof(Ma1Period), 16)
			.SetDisplay("MA1 Period", "Period for first moving average", "Indicators")
			.SetCanOptimize(true);

		_ma2Period = Param(nameof(Ma2Period), 1)
			.SetDisplay("MA2 Period", "Period for second moving average", "Indicators")
			.SetCanOptimize(true);

		_startTime = Param(nameof(StartTime), TimeSpan.Zero)
			.SetDisplay("Start Time", "Trading window start", "Time");

		_endTime = Param(nameof(EndTime), new TimeSpan(23, 59, 0))
			.SetDisplay("End Time", "Trading window end", "Time");

		_allowBuy = Param(nameof(AllowBuy), true)
			.SetDisplay("Allow Buy", "Enable long trades", "Trading");

		_allowSell = Param(nameof(AllowSell), true)
			.SetDisplay("Allow Sell", "Enable short trades", "Trading");

		_closeOnReverse = Param(nameof(CloseOnReverseSignal), true)
			.SetDisplay("Close On Reverse", "Close on opposite signal", "Trading");

		_takeProfitTicks = Param(nameof(TakeProfitTicks), 150)
			.SetDisplay("Take Profit Ticks", "Take profit in ticks", "Risk")
			.SetCanOptimize(true);

		_stopLossTicks = Param(nameof(StopLossTicks), 100)
			.SetDisplay("Stop Loss Ticks", "Stop loss in ticks", "Risk")
			.SetCanOptimize(true);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ma1 = new SimpleMovingAverage { Length = Ma1Period };
		_ma2 = new SimpleMovingAverage { Length = Ma2Period };

		SubscribeCandles(CandleType)
			.Bind(ProcessCandle)
			.Start();

		var step = Security.PriceStep ?? 1m;
		StartProtection(
			takeProfit: new Unit(TakeProfitTicks * step, UnitTypes.Point),
			stopLoss: new Unit(StopLossTicks * step, UnitTypes.Point));
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var time = candle.OpenTime.TimeOfDay;
		if (time < StartTime || time > EndTime)
			return;

		var median = (candle.HighPrice + candle.LowPrice) / 2m;
		var ma1 = _ma1.Process(median).ToDecimal();
		var ma2 = _ma2.Process(median).ToDecimal();

		var buySignal = ma1 < ma2 && _ma1Prev < _ma2Prev && _ma1Prev2 > _ma2Prev2;
		var sellSignal = ma1 > ma2 && _ma1Prev > _ma2Prev && _ma1Prev2 < _ma2Prev2;
		var closeBuy = ma1 > ma2 && _ma1Prev > _ma2Prev && _ma1Prev2 < _ma2Prev2;
		var closeSell = ma1 < ma2 && _ma1Prev < _ma2Prev && _ma1Prev2 > _ma2Prev2;

		if (buySignal && Position <= 0 && AllowBuy)
		{
			if (CloseOnReverseSignal && Position < 0)
				BuyMarket(-Position);
			BuyMarket();
		}
		else if (sellSignal && Position >= 0 && AllowSell)
		{
			if (CloseOnReverseSignal && Position > 0)
				SellMarket(Position);
			SellMarket();
		}
		else if (CloseOnReverseSignal)
		{
			if (closeBuy && Position > 0)
				SellMarket(Position);
			else if (closeSell && Position < 0)
				BuyMarket(-Position);
		}

		_ma1Prev2 = _ma1Prev;
		_ma1Prev = ma1;
		_ma2Prev2 = _ma2Prev;
		_ma2Prev = ma2;
	}
}
