using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// X Trader Strategy - contrarian moving average cross with fixed profit and loss.
/// </summary>
public class XTraderStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _ma1Period;
	private readonly StrategyParam<int> _ma2Period;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;

	private SimpleMovingAverage _ma1;
	private SimpleMovingAverage _ma2;

	private decimal _ma1Prev;
	private decimal _ma1Prev2;
	private decimal _ma2Prev;
	private decimal _ma2Prev2;

	/// <summary>
	/// Candle type to process.
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
	/// Take profit size in points.
	/// </summary>
	public decimal TakeProfitPoints { get => _takeProfitPoints.Value; set => _takeProfitPoints.Value = value; }

	/// <summary>
	/// Stop loss size in points.
	/// </summary>
	public decimal StopLossPoints { get => _stopLossPoints.Value; set => _stopLossPoints.Value = value; }

	public XTraderStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_ma1Period = Param(nameof(Ma1Period), 16)
			.SetGreaterThanZero()
			.SetDisplay("MA1 Period", "Period of the first moving average", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(5, 50, 5);

		_ma2Period = Param(nameof(Ma2Period), 1)
			.SetGreaterThanZero()
			.SetDisplay("MA2 Period", "Period of the second moving average", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(1, 20, 1);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 150m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take profit size in points", "Risk");

		_stopLossPoints = Param(nameof(StopLossPoints), 100m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop loss size in points", "Risk");
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
		_ma1Prev = 0m;
		_ma1Prev2 = 0m;
		_ma2Prev = 0m;
		_ma2Prev2 = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ma1 = new SimpleMovingAverage { Length = Ma1Period };
		_ma2 = new SimpleMovingAverage { Length = Ma2Period };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_ma1, _ma2, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ma1);
			DrawIndicator(area, _ma2);
			DrawOwnTrades(area);
		}

		StartProtection(
			takeProfit: new Unit(TakeProfitPoints, UnitTypes.Absolute),
			stopLoss: new Unit(StopLossPoints, UnitTypes.Absolute));
	}

	private void ProcessCandle(ICandleMessage candle, decimal ma1Value, decimal ma2Value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_ma1.IsFormed || !_ma2.IsFormed)
		{
			UpdatePrevValues(ma1Value, ma2Value);
			return;
		}

		var sellSignal = ma1Value > ma2Value && _ma1Prev > _ma2Prev && _ma1Prev2 < _ma2Prev2;
		var buySignal = ma1Value < ma2Value && _ma1Prev < _ma2Prev && _ma1Prev2 > _ma2Prev2;

		if (sellSignal && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));

		if (buySignal && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));

		UpdatePrevValues(ma1Value, ma2Value);
	}

	private void UpdatePrevValues(decimal ma1, decimal ma2)
	{
		_ma1Prev2 = _ma1Prev;
		_ma1Prev = ma1;
		_ma2Prev2 = _ma2Prev;
		_ma2Prev = ma2;
	}
}
