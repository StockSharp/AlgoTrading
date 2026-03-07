using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the MultiTrend Signal indicator.
/// Builds an adaptive channel using Highest/Lowest and trades breakouts.
/// </summary>
public class ExpMultitrendSignalKvnStrategy : Strategy
{
	private readonly StrategyParam<decimal> _k;
	private readonly StrategyParam<int> _kPeriod;
	private readonly StrategyParam<decimal> _stopLossPct;
	private readonly StrategyParam<decimal> _takeProfitPct;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _maxHigh;
	private Lowest _minLow;
	private int _trend;

	public decimal K
	{
		get => _k.Value;
		set => _k.Value = value;
	}

	public int KPeriod
	{
		get => _kPeriod.Value;
		set => _kPeriod.Value = value;
	}

	public decimal StopLossPct
	{
		get => _stopLossPct.Value;
		set => _stopLossPct.Value = value;
	}

	public decimal TakeProfitPct
	{
		get => _takeProfitPct.Value;
		set => _takeProfitPct.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public ExpMultitrendSignalKvnStrategy()
	{
		_k = Param(nameof(K), 10m)
			.SetDisplay("K", "Percent of swing used for channel width", "Indicator");

		_kPeriod = Param(nameof(KPeriod), 20)
			.SetDisplay("K Period", "Base period for swing calculation", "Indicator")
			.SetGreaterThanZero();

		_stopLossPct = Param(nameof(StopLossPct), 2m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

		_takeProfitPct = Param(nameof(TakeProfitPct), 3m)
			.SetDisplay("Take Profit %", "Take profit percentage", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for calculation", "General");
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
		_maxHigh = default;
		_minLow = default;
		_trend = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_maxHigh = new Highest { Length = KPeriod };
		_minLow = new Lowest { Length = KPeriod };

		Indicators.Add(_maxHigh);
		Indicators.Add(_minLow);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection(
			takeProfit: new Unit(TakeProfitPct, UnitTypes.Percent),
			stopLoss: new Unit(StopLossPct, UnitTypes.Percent),
			useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var maxResult = _maxHigh.Process(candle);
		var minResult = _minLow.Process(candle);

		if (!maxResult.IsFormed || !minResult.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var ssMax = maxResult.ToDecimal();
		var ssMin = minResult.ToDecimal();

		var swing = (ssMax - ssMin) * K / 100m;
		var smin = ssMin + swing;
		var smax = ssMax - swing;

		if (candle.ClosePrice > smax)
		{
			if (_trend <= 0 && Position <= 0)
			{
				if (Position < 0) BuyMarket();
				BuyMarket();
			}
			_trend = 1;
		}
		else if (candle.ClosePrice < smin)
		{
			if (_trend >= 0 && Position >= 0)
			{
				if (Position > 0) SellMarket();
				SellMarket();
			}
			_trend = -1;
		}
	}
}
