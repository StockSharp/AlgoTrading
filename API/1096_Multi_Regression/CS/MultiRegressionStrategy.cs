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
/// Strategy that trades based on price crossing a moving average with StdDev-based bounds.
/// </summary>
public class MultiRegressionStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _riskMultiplier;
	private readonly StrategyParam<int> _signalCooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	public int Length { get => _length.Value; set => _length.Value = value; }
	public decimal RiskMultiplier { get => _riskMultiplier.Value; set => _riskMultiplier.Value = value; }
	public int SignalCooldownBars { get => _signalCooldownBars.Value; set => _signalCooldownBars.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MultiRegressionStrategy()
	{
		_length = Param(nameof(Length), 20)
			.SetGreaterThanZero()
			.SetDisplay("Length", "SMA and StdDev period", "Regression");
		_riskMultiplier = Param(nameof(RiskMultiplier), 2m)
			.SetDisplay("Risk Multiplier", "StdDev multiplier for bounds", "Risk");
		_signalCooldownBars = Param(nameof(SignalCooldownBars), 8)
			.SetGreaterThanZero()
			.SetDisplay("Signal Cooldown", "Bars to wait between reversals", "Risk");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "Common");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SMA { Length = Length };
		var std = new StandardDeviation { Length = Length };

		var prevClose = 0m;
		var prevUpper = 0m;
		var prevLower = 0m;
		var initialized = false;
		var cooldownRemaining = 0;

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(sma, std, (candle, smaVal, stdVal) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				if (cooldownRemaining > 0)
					cooldownRemaining--;

				var price = candle.ClosePrice;

				if (!initialized)
				{
					prevClose = price;
					prevUpper = smaVal + stdVal * RiskMultiplier;
					prevLower = smaVal - stdVal * RiskMultiplier;
					initialized = true;
					return;
				}

				var upperBound = smaVal + stdVal * RiskMultiplier;
				var lowerBound = smaVal - stdVal * RiskMultiplier;
				var longEntry = prevClose < prevLower && price >= lowerBound;
				var shortEntry = prevClose > prevUpper && price <= upperBound;
				var longExit = Position > 0 && (price >= smaVal || price >= upperBound);
				var shortExit = Position < 0 && (price <= smaVal || price <= lowerBound);

				if (longExit)
				{
					SellMarket();
					cooldownRemaining = SignalCooldownBars;
				}
				else if (shortExit)
				{
					BuyMarket(Math.Abs(Position));
					cooldownRemaining = SignalCooldownBars;
				}
				else if (cooldownRemaining == 0 && longEntry && Position <= 0)
				{
					BuyMarket(Volume + Math.Abs(Position));
					cooldownRemaining = SignalCooldownBars;
				}
				else if (cooldownRemaining == 0 && shortEntry && Position >= 0)
				{
					SellMarket(Volume + Math.Abs(Position));
					cooldownRemaining = SignalCooldownBars;
				}

				prevClose = price;
				prevUpper = upperBound;
				prevLower = lowerBound;
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}
}
