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
/// Strategy based on Ehlers CyclePeriod concept using EMA crossover as proxy.
/// Detects short-term cycles via fast/slow EMA and trades reversals.
/// </summary>
public class ExpCyclePeriodStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<bool> _buyPosOpen;
	private readonly StrategyParam<bool> _sellPosOpen;

	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _prevReady;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }
	public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public bool BuyPosOpen { get => _buyPosOpen.Value; set => _buyPosOpen.Value = value; }
	public bool SellPosOpen { get => _sellPosOpen.Value; set => _sellPosOpen.Value = value; }

	public ExpCyclePeriodStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(6).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_fastPeriod = Param(nameof(FastPeriod), 7)
			.SetDisplay("Fast Period", "Fast EMA period", "Indicator");

		_slowPeriod = Param(nameof(SlowPeriod), 21)
			.SetDisplay("Slow Period", "Slow EMA period", "Indicator");

		_takeProfit = Param(nameof(TakeProfit), 2000m)
			.SetDisplay("Take Profit", "Take profit in price", "Risk");

		_stopLoss = Param(nameof(StopLoss), 1000m)
			.SetDisplay("Stop Loss", "Stop loss in price", "Risk");

		_buyPosOpen = Param(nameof(BuyPosOpen), true)
			.SetDisplay("Buy Open", "Allow long entries", "Logic");

		_sellPosOpen = Param(nameof(SellPosOpen), true)
			.SetDisplay("Sell Open", "Allow short entries", "Logic");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		StartProtection(new Unit(TakeProfit, UnitTypes.Absolute), new Unit(StopLoss, UnitTypes.Absolute));

		var fastEma = new ExponentialMovingAverage { Length = FastPeriod };
		var slowEma = new ExponentialMovingAverage { Length = SlowPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastEma, slowEma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastEma);
			DrawIndicator(area, slowEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_prevReady)
		{
			_prevFast = fastValue;
			_prevSlow = slowValue;
			_prevReady = true;
			return;
		}

		// Crossover detection
		var prevDiff = _prevFast - _prevSlow;
		var currDiff = fastValue - slowValue;

		// Golden cross: fast crosses above slow
		if (prevDiff <= 0 && currDiff > 0)
		{
			if (BuyPosOpen && Position <= 0)
			{
				if (Position < 0)
					BuyMarket();
				BuyMarket();
			}
		}
		// Death cross: fast crosses below slow
		else if (prevDiff >= 0 && currDiff < 0)
		{
			if (SellPosOpen && Position >= 0)
			{
				if (Position > 0)
					SellMarket();
				SellMarket();
			}
		}

		_prevFast = fastValue;
		_prevSlow = slowValue;
	}
}
