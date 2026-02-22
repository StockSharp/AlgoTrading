using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class OscillatorEvaluatorStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _oversold;
	private readonly StrategyParam<decimal> _overbought;
	private readonly StrategyParam<DataType> _candleType;

	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public decimal Oversold { get => _oversold.Value; set => _oversold.Value = value; }
	public decimal Overbought { get => _overbought.Value; set => _overbought.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public OscillatorEvaluatorStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 14).SetGreaterThanZero();
		_oversold = Param(nameof(Oversold), 30m);
		_overbought = Param(nameof(Overbought), 70m);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var stoch = new StochasticOscillator();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(rsi, stoch, ProcessCandle)
			.Start();

		StartProtection(
			takeProfit: new Unit(2m, UnitTypes.Percent),
			stopLoss: new Unit(1m, UnitTypes.Percent));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue rsiValue, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!rsiValue.IsFinal || !rsiValue.IsFormed || !stochValue.IsFormed)
			return;

		var rsi = rsiValue.ToDecimal();

		// Get stoch K value
		decimal stochK = 50;
		var complexStoch = stochValue as IComplexIndicatorValue;
		if (complexStoch != null)
		{
			var vals = complexStoch.InnerValues.Select(v => v.Value.ToDecimal()).ToArray();
			if (vals.Length >= 1)
				stochK = vals[0];
		}

		if (rsi < Oversold && stochK < Oversold && Position <= 0)
		{
			CancelActiveOrders();
			if (Position < 0) BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
		}
		else if (rsi > Overbought && stochK > Overbought && Position >= 0)
		{
			CancelActiveOrders();
			if (Position > 0) SellMarket(Math.Abs(Position));
			SellMarket(Volume);
		}

		if (Position > 0 && rsi > 50 && stochK > 50)
			SellMarket(Math.Abs(Position));
		else if (Position < 0 && rsi < 50 && stochK < 50)
			BuyMarket(Math.Abs(Position));
	}
}
