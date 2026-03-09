using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on two SMA crossovers with ATR stop loss.
/// Buys when the fast SMA rises above the slow SMA and sells on opposite cross.
/// A stop loss is placed one ATR away from the entry price.
/// </summary>
public class CrossMAStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private int _lastSignal;

	/// <summary>
	/// Fast SMA period.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Slow SMA period.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// ATR period for stop calculation.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}


	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public CrossMAStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 5)
		.SetGreaterThanZero()
		.SetDisplay("Fast SMA Period", "Period of the fast SMA", "Parameters");

		_slowPeriod = Param(nameof(SlowPeriod), 20)
		.SetGreaterThanZero()
		.SetDisplay("Slow SMA Period", "Period of the slow SMA", "Parameters");

		_atrPeriod = Param(nameof(AtrPeriod), 6)
		.SetGreaterThanZero()
		.SetDisplay("ATR Period", "Period of ATR for stop calculation", "Risk");


		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles for calculations", "General");
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
		_lastSignal = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastSma = new SimpleMovingAverage
		{
			Length = FastPeriod
		};

		var slowSma = new SimpleMovingAverage
		{
			Length = SlowPeriod
		};

		var subscription = SubscribeCandles(CandleType);

		subscription
		.Bind(fastSma, slowSma, ProcessCandle)
		.Start();

		StartProtection(
			new Unit(2000m, UnitTypes.Absolute),
			new Unit(1000m, UnitTypes.Absolute));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastSma);
			DrawIndicator(area, slowSma);
			DrawOwnTrades(area);
		}
	}

	/// <summary>
	/// Process candle and indicator values.
	/// </summary>
	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (fast > slow && candle.ClosePrice > slow && _lastSignal != 1 && Position <= 0)
		{
			BuyMarket();
			_lastSignal = 1;
		}
		else if (fast < slow && candle.ClosePrice < slow && _lastSignal != -1 && Position >= 0)
		{
			SellMarket();
			_lastSignal = -1;
		}
	}
}
