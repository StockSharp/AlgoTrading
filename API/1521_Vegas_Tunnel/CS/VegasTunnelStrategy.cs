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
/// Vegas Tunnel strategy using multiple EMAs.
/// Goes long when price is above the tunnel (EMA 144/169) with fast EMA confirmation.
/// Uses StdDev-based stops and risk/reward targets.
/// </summary>
public class VegasTunnelStrategy : Strategy
{
	private readonly StrategyParam<decimal> _riskRewardRatio;
	private readonly StrategyParam<decimal> _stopMult;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _stopPrice;
	private decimal _takePrice;

	public decimal RiskRewardRatio { get => _riskRewardRatio.Value; set => _riskRewardRatio.Value = value; }
	public decimal StopMult { get => _stopMult.Value; set => _stopMult.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public VegasTunnelStrategy()
	{
		_riskRewardRatio = Param(nameof(RiskRewardRatio), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Risk/Reward", "Risk to reward ratio", "General");

		_stopMult = Param(nameof(StopMult), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Mult", "StdDev multiplier for stop", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_stopPrice = 0;
		_takePrice = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var emaFast = new ExponentialMovingAverage { Length = 12 };
		var emaSlow = new ExponentialMovingAverage { Length = 144 };
		var emaTunnel = new ExponentialMovingAverage { Length = 169 };
		var stdDev = new StandardDeviation { Length = 14 };

		_stopPrice = 0;
		_takePrice = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(emaFast, emaSlow, emaTunnel, stdDev, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, emaFast);
			DrawIndicator(area, emaSlow);
			DrawIndicator(area, emaTunnel);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal tunnel, decimal stdVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Exit management
		if (Position > 0 && _stopPrice > 0)
		{
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takePrice)
			{
				SellMarket();
				_stopPrice = 0;
				_takePrice = 0;
			}
		}
		else if (Position < 0 && _stopPrice > 0)
		{
			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takePrice)
			{
				BuyMarket();
				_stopPrice = 0;
				_takePrice = 0;
			}
		}

		// Tunnel direction
		var tunnelUp = slow < tunnel;
		var tunnelDown = slow > tunnel;

		var longCond = candle.ClosePrice > slow && candle.ClosePrice > tunnel && tunnelUp &&
			fast > slow && fast > tunnel;
		var shortCond = candle.ClosePrice < slow && candle.ClosePrice < tunnel && tunnelDown &&
			fast < slow && fast < tunnel;

		if (longCond && Position <= 0 && stdVal > 0)
		{
			BuyMarket();
			var entry = candle.ClosePrice;
			_stopPrice = entry - StopMult * stdVal;
			_takePrice = entry + (entry - _stopPrice) * RiskRewardRatio;
		}
		else if (shortCond && Position >= 0 && stdVal > 0)
		{
			SellMarket();
			var entry = candle.ClosePrice;
			_stopPrice = entry + StopMult * stdVal;
			_takePrice = entry - (_stopPrice - entry) * RiskRewardRatio;
		}
	}
}
