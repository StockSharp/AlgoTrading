using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Four Bar Momentum Reversal Strategy - enters long after consecutive closes below the close from a number of bars ago and exits on breakout above previous high.
/// </summary>
public class FourBarMomentumReversalStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _buyThreshold;
	private readonly StrategyParam<int> _lookback;
	private readonly StrategyParam<DateTimeOffset> _startTime;
	private readonly StrategyParam<DateTimeOffset> _endTime;

	private Shift _shift;
	private int _aboveCount;
	private int _belowCount;
	private decimal _prevHigh;

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Number of consecutive closes below reference needed to buy.
	/// </summary>
	public int BuyThreshold { get => _buyThreshold.Value; set => _buyThreshold.Value = value; }

	/// <summary>
	/// Number of bars to look back for reference close.
	/// </summary>
	public int Lookback { get => _lookback.Value; set => _lookback.Value = value; }

	/// <summary>
	/// Start time for trading window.
	/// </summary>
	public DateTimeOffset StartTime { get => _startTime.Value; set => _startTime.Value = value; }

	/// <summary>
	/// End time for trading window.
	/// </summary>
	public DateTimeOffset EndTime { get => _endTime.Value; set => _endTime.Value = value; }

	/// <summary>
	/// Constructor.
	/// </summary>
	public FourBarMomentumReversalStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_buyThreshold = Param(nameof(BuyThreshold), 4)
			.SetGreaterThanZero()
			.SetDisplay("Buy Threshold", "Consecutive closes below reference to trigger buy", "Strategy")
			.SetCanOptimize(true)
			.SetOptimize(2, 10, 1);

		_lookback = Param(nameof(Lookback), 4)
			.SetGreaterThanZero()
			.SetDisplay("Lookback", "Number of bars to compare", "Strategy")
			.SetCanOptimize(true)
			.SetOptimize(1, 10, 1);

		_startTime = Param(nameof(StartTime), new DateTimeOffset(2014, 1, 1, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("Start Time", "Only trade after this time", "Time Settings");

		_endTime = Param(nameof(EndTime), new DateTimeOffset(2099, 1, 1, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("End Time", "Stop trading after this time", "Time Settings");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_shift = new Shift { Length = Lookback };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_shift, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal pastClose)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_shift.IsFormed)
			return;

		if (candle.ClosePrice > pastClose)
		{
			_aboveCount++;
			_belowCount = 0;
		}
		else if (candle.ClosePrice < pastClose)
		{
			_belowCount++;
			_aboveCount = 0;
		}
		else
		{
			_aboveCount = 0;
			_belowCount = 0;
		}

		var inWindow = candle.OpenTime >= StartTime && candle.OpenTime <= EndTime;

		if (_belowCount >= BuyThreshold && inWindow && Position <= 0)
		{
			RegisterOrder(CreateOrder(Sides.Buy, candle.ClosePrice, Volume));
		}

		if (Position > 0 && candle.ClosePrice > _prevHigh)
		{
			RegisterOrder(CreateOrder(Sides.Sell, candle.ClosePrice, Math.Abs(Position)));
		}

		_prevHigh = candle.HighPrice;
	}
}
