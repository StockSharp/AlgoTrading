using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Buy the dip strategy based on consecutive bars below moving average.
/// </summary>
public class ConsecutiveBarsAboveBelowEMABuyTheDipStrategy : Strategy
{
	private readonly StrategyParam<int> _barsThreshold;
	private readonly StrategyParam<bool> _useEma;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DateTimeOffset> _startTime;
	private readonly StrategyParam<DateTimeOffset> _endTime;

	private int _aboveCount;
	private int _belowCount;
	private decimal _prevHigh;

	/// <summary>
	/// Number of consecutive bars to trigger entry.
	/// </summary>
	public int BarsThreshold
	{
		get => _barsThreshold.Value;
		set => _barsThreshold.Value = value;
	}

	/// <summary>
	/// Use EMA instead of SMA.
	/// </summary>
	public bool UseEma
	{
		get => _useEma.Value;
		set => _useEma.Value = value;
	}

	/// <summary>
	/// Moving average length.
	/// </summary>
	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
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
	/// Trading start time.
	/// </summary>
	public DateTimeOffset StartTime
	{
		get => _startTime.Value;
		set => _startTime.Value = value;
	}

	/// <summary>
	/// Trading end time.
	/// </summary>
	public DateTimeOffset EndTime
	{
		get => _endTime.Value;
		set => _endTime.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="ConsecutiveBarsAboveBelowEMABuyTheDipStrategy"/>.
	/// </summary>
	public ConsecutiveBarsAboveBelowEMABuyTheDipStrategy()
	{
		_barsThreshold = Param(nameof(BarsThreshold), 3)
			.SetGreaterThanZero()
			.SetDisplay("Consecutive Bars", "Number of consecutive bars below MA to buy", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(2, 5, 1);

		_useEma = Param(nameof(UseEma), true)
			.SetDisplay("Use EMA", "Use EMA instead of SMA", "MA Settings");

		_maLength = Param(nameof(MaLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "Moving average period", "MA Settings")
			.SetCanOptimize(true)
			.SetOptimize(3, 10, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_startTime = Param(nameof(StartTime), new DateTimeOffset(2014, 1, 1, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("Start Time", "Start of trading window", "Time Settings");

		_endTime = Param(nameof(EndTime), new DateTimeOffset(2099, 1, 1, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("End Time", "End of trading window", "Time Settings");
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
		_aboveCount = 0;
		_belowCount = 0;
		_prevHigh = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var ma = UseEma ? new EMA { Length = MaLength } : new SMA { Length = MaLength };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(ma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (candle.OpenTime < StartTime || candle.OpenTime > EndTime)
		{
			_prevHigh = candle.HighPrice;
			_aboveCount = 0;
			_belowCount = 0;
			return;
		}

		if (candle.ClosePrice > maValue)
		{
			_aboveCount++;
			_belowCount = 0;
		}
		else if (candle.ClosePrice < maValue)
		{
			_belowCount++;
			_aboveCount = 0;
		}
		else
		{
			_aboveCount = 0;
			_belowCount = 0;
		}

		if (_belowCount >= BarsThreshold && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
		}

		if (Position != 0 && candle.ClosePrice > _prevHigh)
		{
			if (Position > 0)
				SellMarket(Position);
			else if (Position < 0)
				BuyMarket(Math.Abs(Position));
		}

		_prevHigh = candle.HighPrice;
	}
}

