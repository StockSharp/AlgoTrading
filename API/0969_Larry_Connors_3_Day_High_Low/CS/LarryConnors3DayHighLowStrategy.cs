using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Larry Connors 3 Day High/Low strategy.
/// Buys after three consecutive lower highs and lows below a short moving average while price is above a long moving average.
/// Exits when price crosses above the short moving average.
/// </summary>
public class LarryConnors3DayHighLowStrategy : Strategy
{
	private readonly StrategyParam<int> _longMaLength;
	private readonly StrategyParam<int> _shortMaLength;
	private readonly StrategyParam<DataType> _candleType;

	private SMA _longSma;
	private SMA _shortSma;

	private int _barCount;
	private decimal _high1;
	private decimal _high2;
	private decimal _high3;
	private decimal _low1;
	private decimal _low2;
	private decimal _low3;

	/// <summary>
	/// Long moving average period.
	/// </summary>
	public int LongMaLength
	{
		get => _longMaLength.Value;
		set => _longMaLength.Value = value;
	}

	/// <summary>
	/// Short moving average period.
	/// </summary>
	public int ShortMaLength
	{
		get => _shortMaLength.Value;
		set => _shortMaLength.Value = value;
	}

	/// <summary>
	/// Type of candles to use.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public LarryConnors3DayHighLowStrategy()
	{
		_longMaLength = Param(nameof(LongMaLength), 200)
			.SetGreaterThanZero()
			.SetDisplay("Long MA Length", "Period of the long moving average", "General")
			.SetCanOptimize(true)
			.SetOptimize(100, 300, 50);

		_shortMaLength = Param(nameof(ShortMaLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Short MA Length", "Period of the short moving average", "General")
			.SetCanOptimize(true)
			.SetOptimize(3, 10, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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

		_barCount = 0;
		_high1 = _high2 = _high3 = 0m;
		_low1 = _low2 = _low3 = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_longSma = new SMA { Length = LongMaLength };
		_shortSma = new SMA { Length = ShortMaLength };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(_longSma, _shortSma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _longSma);
			DrawIndicator(area, _shortSma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal longMa, decimal shortMa)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var canTrade = IsFormedAndOnlineAndAllowTrading();

		if (canTrade && _longSma.IsFormed && _shortSma.IsFormed && _barCount >= 3)
		{
			var condition1 = candle.ClosePrice > longMa;
			var condition2 = candle.ClosePrice < shortMa;
			var condition3 = _high2 < _high3 && _low2 < _low3;
			var condition4 = _high1 < _high2 && _low1 < _low2;
			var condition5 = candle.HighPrice < _high1 && candle.LowPrice < _low1;

			if (condition1 && condition2 && condition3 && condition4 && condition5 && Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
			}
			else if (candle.ClosePrice > shortMa && Position > 0)
			{
				SellMarket(Math.Abs(Position));
			}
		}

		_barCount++;
		_high3 = _high2;
		_high2 = _high1;
		_high1 = candle.HighPrice;

		_low3 = _low2;
		_low2 = _low1;
		_low1 = candle.LowPrice;
	}
}
