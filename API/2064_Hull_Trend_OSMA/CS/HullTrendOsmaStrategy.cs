using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Hull Trend OSMA indicator.
/// Opens long when oscillator rises twice in a row, short when it falls twice in a row.
/// </summary>
public class HullTrendOsmaStrategy : Strategy
{
	private readonly StrategyParam<int> _hullPeriod;
	private readonly StrategyParam<int> _signalPeriod;
	private readonly StrategyParam<decimal> _takeProfitPct;
	private readonly StrategyParam<decimal> _stopLossPct;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prev1;
	private decimal? _prev2;

	public int HullPeriod { get => _hullPeriod.Value; set => _hullPeriod.Value = value; }
	public int SignalPeriod { get => _signalPeriod.Value; set => _signalPeriod.Value = value; }
	public decimal TakeProfitPct { get => _takeProfitPct.Value; set => _takeProfitPct.Value = value; }
	public decimal StopLossPct { get => _stopLossPct.Value; set => _stopLossPct.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public HullTrendOsmaStrategy()
	{
		_hullPeriod = Param(nameof(HullPeriod), 20)
			.SetDisplay("Hull Period", "Period for Hull Moving Average", "Indicators");

		_signalPeriod = Param(nameof(SignalPeriod), 5)
			.SetDisplay("Signal Period", "Period for signal SMA", "Indicators");

		_takeProfitPct = Param(nameof(TakeProfitPct), 3m)
			.SetDisplay("Take Profit %", "Take profit percentage", "Risk");

		_stopLossPct = Param(nameof(StopLossPct), 2m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_prev1 = null;
		_prev2 = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var hma = new HullMovingAverage { Length = HullPeriod };
		var signal = new ExponentialMovingAverage { Length = SignalPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(hma, signal, ProcessCandle)
			.Start();

		StartProtection(
			takeProfit: new Unit(TakeProfitPct, UnitTypes.Percent),
			stopLoss: new Unit(StopLossPct, UnitTypes.Percent),
			useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, hma);
			DrawIndicator(area, signal);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal hmaValue, decimal signalValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var osma = hmaValue - signalValue;

		if (_prev1 is null || _prev2 is null)
		{
			_prev2 = _prev1;
			_prev1 = osma;
			return;
		}

		var prev = _prev1.Value;
		var prevPrev = _prev2.Value;

		var isRising = prev > prevPrev && osma >= prev;
		var isFalling = prev < prevPrev && osma <= prev;

		if (isRising && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		else if (isFalling && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}

		_prev2 = _prev1;
		_prev1 = osma;
	}
}
