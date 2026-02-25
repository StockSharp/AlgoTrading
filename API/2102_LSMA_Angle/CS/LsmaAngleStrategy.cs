using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// LSMA Angle based strategy.
/// Opens long when the LSMA slope rises above a threshold and short when it falls below.
/// </summary>
public class LsmaAngleStrategy : Strategy
{
	private readonly StrategyParam<int> _lsmaPeriod;
	private readonly StrategyParam<decimal> _slopeThreshold;
	private readonly StrategyParam<decimal> _stopLossPct;
	private readonly StrategyParam<decimal> _takeProfitPct;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevLsma;
	private decimal _prevSlope;

	public int LsmaPeriod { get => _lsmaPeriod.Value; set => _lsmaPeriod.Value = value; }
	public decimal SlopeThreshold { get => _slopeThreshold.Value; set => _slopeThreshold.Value = value; }
	public decimal StopLossPct { get => _stopLossPct.Value; set => _stopLossPct.Value = value; }
	public decimal TakeProfitPct { get => _takeProfitPct.Value; set => _takeProfitPct.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public LsmaAngleStrategy()
	{
		_lsmaPeriod = Param(nameof(LsmaPeriod), 25)
			.SetGreaterThanZero()
			.SetDisplay("LSMA Period", "LSMA calculation length", "Indicator");

		_slopeThreshold = Param(nameof(SlopeThreshold), 0.05m)
			.SetDisplay("Slope Threshold", "Percentage slope threshold", "Indicator");

		_stopLossPct = Param(nameof(StopLossPct), 2m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

		_takeProfitPct = Param(nameof(TakeProfitPct), 3m)
			.SetDisplay("Take Profit %", "Take profit percentage", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevLsma = null;
		_prevSlope = 0m;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var lsma = new LinearReg { Length = LsmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(lsma, (candle, lsmaValue) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (_prevLsma is null)
				{
					_prevLsma = lsmaValue;
					return;
				}

				// Calculate slope as percentage change
				var slope = _prevLsma.Value != 0 ? (lsmaValue - _prevLsma.Value) / _prevLsma.Value * 100m : 0m;

				var wasUp = _prevSlope > SlopeThreshold;
				var wasDown = _prevSlope < -SlopeThreshold;
				var isUp = slope > SlopeThreshold;
				var isDown = slope < -SlopeThreshold;

				if (!wasUp && isUp && Position <= 0)
				{
					if (Position < 0) BuyMarket();
					BuyMarket();
				}
				else if (!wasDown && isDown && Position >= 0)
				{
					if (Position > 0) SellMarket();
					SellMarket();
				}
				else if (wasUp && !isUp && Position > 0)
				{
					SellMarket();
				}
				else if (wasDown && !isDown && Position < 0)
				{
					BuyMarket();
				}

				_prevSlope = slope;
				_prevLsma = lsmaValue;
			})
			.Start();

		StartProtection(
			takeProfit: new Unit(TakeProfitPct, UnitTypes.Percent),
			stopLoss: new Unit(StopLossPct, UnitTypes.Percent),
			useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, lsma);
			DrawOwnTrades(area);
		}
	}
}
