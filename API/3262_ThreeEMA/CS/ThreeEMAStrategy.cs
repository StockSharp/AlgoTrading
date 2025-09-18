namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Triple exponential moving average crossover strategy.
/// </summary>
public class ThreeEMAStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _mediumPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;

	private ExponentialMovingAverage _fastEma;
	private ExponentialMovingAverage _mediumEma;
	private ExponentialMovingAverage _slowEma;

	public ThreeEMAStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle type", "Time frame of candles used for analysis.", "General");

		_fastPeriod = Param(nameof(FastPeriod), 5)
		.SetDisplay("Fast EMA", "Length of the fast EMA.", "Parameters")
		.SetCanOptimize(true);

		_mediumPeriod = Param(nameof(MediumPeriod), 12)
		.SetDisplay("Medium EMA", "Length of the medium EMA.", "Parameters")
		.SetCanOptimize(true);

		_slowPeriod = Param(nameof(SlowPeriod), 24)
		.SetDisplay("Slow EMA", "Length of the slow EMA.", "Parameters")
		.SetCanOptimize(true);

		_stopLossPoints = Param(nameof(StopLossPoints), 400m)
		.SetDisplay("Stop loss", "Protective distance in points.", "Risk")
		.SetCanOptimize(true);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 900m)
		.SetDisplay("Take profit", "Target distance in points.", "Risk")
		.SetCanOptimize(true);
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	public int MediumPeriod
	{
		get => _mediumPeriod.Value;
		set => _mediumPeriod.Value = value;
	}

	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
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

		if (FastPeriod <= 0 || MediumPeriod <= 0 || SlowPeriod <= 0)
		{
			throw new InvalidOperationException("EMA periods must be positive.");
		}

		if (FastPeriod >= MediumPeriod)
		{
			throw new InvalidOperationException("Medium EMA period must be greater than fast EMA period.");
		}

		if (MediumPeriod >= SlowPeriod)
		{
			throw new InvalidOperationException("Slow EMA period must be greater than medium EMA period.");
		}

		_fastEma = new ExponentialMovingAverage { Length = FastPeriod };
		_mediumEma = new ExponentialMovingAverage { Length = MediumPeriod };
		_slowEma = new ExponentialMovingAverage { Length = SlowPeriod };

		// Subscribe to candle data and feed EMA indicators.
		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_fastEma, _mediumEma, _slowEma, ProcessCandle)
		.Start();

		// Attach indicators to chart if UI is available.
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastEma);
			DrawIndicator(area, _mediumEma);
			DrawIndicator(area, _slowEma);
			DrawOwnTrades(area);
		}

		// Enable protective stop loss and take profit distances.
		var step = Security.PriceStep ?? 1m;
		Unit? takeProfitUnit = TakeProfitPoints > 0m ? new Unit(TakeProfitPoints * step, UnitTypes.Point) : null;
		Unit? stopLossUnit = StopLossPoints > 0m ? new Unit(StopLossPoints * step, UnitTypes.Point) : null;

		if (takeProfitUnit != null || stopLossUnit != null)
		{
			StartProtection(takeProfit: takeProfitUnit, stopLoss: stopLossUnit);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal mediumValue, decimal slowValue)
	{
		// Work only with completed candles to avoid noise.
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var isBullishStack = fastValue > mediumValue && mediumValue > slowValue;
		var isBearishStack = fastValue < mediumValue && mediumValue < slowValue;

		if (isBullishStack && Position <= 0)
		{
			// Close shorts (if any) and go long.
			CancelActiveOrders();
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (isBearishStack && Position >= 0)
		{
			// Close longs (if any) and go short.
			CancelActiveOrders();
			SellMarket(Volume + Math.Abs(Position));
		}
	}
}
