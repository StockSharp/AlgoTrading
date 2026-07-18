using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on price position relative to SMA envelopes.
/// Buys when price is below the lower envelope, sells when above upper.
/// </summary>
public class JpalonsoModokiStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _smaPeriod;
	private readonly StrategyParam<decimal> _deviation;
	private readonly StrategyParam<Unit> _takeProfit;
	private readonly StrategyParam<Unit> _stopLoss;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int SmaPeriod { get => _smaPeriod.Value; set => _smaPeriod.Value = value; }
	public decimal Deviation { get => _deviation.Value; set => _deviation.Value = value; }
	public Unit TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public Unit StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }

	public JpalonsoModokiStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_smaPeriod = Param(nameof(SmaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("SMA Period", "Length of the moving average", "Envelopes");

		_deviation = Param(nameof(Deviation), 0.35m)
			.SetDisplay("Deviation %", "Envelope deviation from SMA in percent", "Envelopes");

		_takeProfit = Param(nameof(TakeProfit), new Unit(3000, UnitTypes.Absolute))
			.SetDisplay("Take Profit", "Take profit in points", "Risk Management");

		_stopLoss = Param(nameof(StopLoss), new Unit(5000, UnitTypes.Absolute))
			.SetDisplay("Stop Loss", "Stop loss in points", "Risk Management");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		StartProtection(takeProfit: TakeProfit, stopLoss: StopLoss);

		var sma = new SimpleMovingAverage { Length = SmaPeriod };

		SubscribeCandles(CandleType)
			.Bind(sma, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal ma)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var upper = ma * (1 + Deviation / 100m);
		var lower = ma * (1 - Deviation / 100m);
		var close = candle.ClosePrice;

		var buy = close <= lower;
		var sell = close >= upper;

		if (buy && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		else if (sell && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}
	}
}
