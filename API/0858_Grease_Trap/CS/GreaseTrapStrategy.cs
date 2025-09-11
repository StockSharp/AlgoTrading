using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Grease Trap strategy based on crossover of two moving averages.
/// </summary>
public class GreaseTrapStrategy : Strategy
{
	private readonly StrategyParam<int> _length1;
	private readonly StrategyParam<int> _length2;
	private readonly StrategyParam<decimal> _longProfit;
	private readonly StrategyParam<decimal> _shortProfit;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _sma1 = null!;
	private SimpleMovingAverage _sma2 = null!;
	private Order _tpOrder = null!;
	private decimal _prevAvg1;
	private decimal _prevAvg2;
	private bool _hasPrev;

	public GreaseTrapStrategy()
	{
		_length1 = Param(nameof(Length1), 9)
			.SetGreaterThanZero()
			.SetDisplay("Length 1", "Elements for first average", "Parameters");

		_length2 = Param(nameof(Length2), 14)
			.SetGreaterThanZero()
			.SetDisplay("Length 2", "Elements for second average", "Parameters");

		_longProfit = Param(nameof(LongProfit), 0.02m)
			.SetGreaterThanZero()
			.SetDisplay("Long Profit %", "Profit target for long positions", "Trading");

		_shortProfit = Param(nameof(ShortProfit), 0.02m)
			.SetGreaterThanZero()
			.SetDisplay("Short Profit %", "Profit target for short positions", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for strategy", "General");
	}

	/// <summary>
	/// Length for the first average.
	/// </summary>
	public int Length1 { get => _length1.Value; set => _length1.Value = value; }

	/// <summary>
	/// Length for the second average.
	/// </summary>
	public int Length2 { get => _length2.Value; set => _length2.Value = value; }

	/// <summary>
	/// Profit target percentage for long positions.
	/// </summary>
	public decimal LongProfit { get => _longProfit.Value; set => _longProfit.Value = value; }

	/// <summary>
	/// Profit target percentage for short positions.
	/// </summary>
	public decimal ShortProfit { get => _shortProfit.Value; set => _shortProfit.Value = value; }

	/// <summary>
	/// Candle type for subscription.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_tpOrder = null!;
		_prevAvg1 = 0m;
		_prevAvg2 = 0m;
		_hasPrev = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_sma1 = new SimpleMovingAverage { Length = Length1 };
		_sma2 = new SimpleMovingAverage { Length = Length2 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_sma1, _sma2, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _sma1);
			DrawIndicator(area, _sma2);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal avg1, decimal avg2)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!_hasPrev)
		{
		_prevAvg1 = avg1;
		_prevAvg2 = avg2;
		_hasPrev = true;
		return;
		}

		var crossUp = _prevAvg1 <= _prevAvg2 && avg1 > avg2;
		var crossDown = _prevAvg1 >= _prevAvg2 && avg1 < avg2;

		if (crossUp && Position <= 0)
		{
		BuyMarket(Volume + Math.Abs(Position));
		RegisterTakeProfit(true, candle.ClosePrice);
		}
		else if (crossDown && Position >= 0)
		{
		SellMarket(Volume + Math.Abs(Position));
		RegisterTakeProfit(false, candle.ClosePrice);
		}

		_prevAvg1 = avg1;
		_prevAvg2 = avg2;
	}

	private void RegisterTakeProfit(bool isLong, decimal entryPrice)
	{
		if (_tpOrder != null && _tpOrder.State == OrderStates.Active)
		CancelOrder(_tpOrder);

		var target = isLong
		? entryPrice * (1 + LongProfit)
		: entryPrice * (1 - ShortProfit);

		_tpOrder = isLong
		? SellLimit(Volume, target)
		: BuyLimit(Volume, target);
	}
}
