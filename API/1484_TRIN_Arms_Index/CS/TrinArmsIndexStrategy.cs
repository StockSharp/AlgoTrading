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
/// Strategy inspired by TRIN Arms Index concept.
/// Uses RSI oversold conditions with volatility-based stop loss.
/// Goes long when RSI drops below threshold (extreme oversold), exits on recovery or stop.
/// </summary>
public class TrinArmsIndexStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiEntry;
	private readonly StrategyParam<decimal> _rsiExit;
	private readonly StrategyParam<decimal> _stopLossMultiplier;
	private readonly StrategyParam<int> _volatilityLookback;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;

	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public decimal RsiEntry { get => _rsiEntry.Value; set => _rsiEntry.Value = value; }
	public decimal RsiExit { get => _rsiExit.Value; set => _rsiExit.Value = value; }
	public decimal StopLossMultiplier { get => _stopLossMultiplier.Value; set => _stopLossMultiplier.Value = value; }
	public int VolatilityLookback { get => _volatilityLookback.Value; set => _volatilityLookback.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TrinArmsIndexStrategy()
	{
		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "Indicators");

		_rsiEntry = Param(nameof(RsiEntry), 30m)
			.SetDisplay("RSI Entry", "RSI level for entry (oversold)", "Indicators");

		_rsiExit = Param(nameof(RsiExit), 60m)
			.SetDisplay("RSI Exit", "RSI level for exit", "Indicators");

		_stopLossMultiplier = Param(nameof(StopLossMultiplier), 3.4m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss Mult", "Multiplier for volatility stop", "Risk");

		_volatilityLookback = Param(nameof(VolatilityLookback), 24)
			.SetGreaterThanZero()
			.SetDisplay("Vol Lookback", "StdDev lookback", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_entryPrice = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var stdDev = new StandardDeviation { Length = VolatilityLookback };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(rsi, stdDev, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiVal, decimal stdVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Exit logic
		if (Position > 0)
		{
			var stopPrice = _entryPrice - StopLossMultiplier * stdVal;
			if (candle.ClosePrice <= stopPrice || rsiVal > RsiExit)
			{
				SellMarket();
				_entryPrice = 0;
			}
		}
		else if (Position < 0)
		{
			var stopPrice = _entryPrice + StopLossMultiplier * stdVal;
			if (candle.ClosePrice >= stopPrice || rsiVal < (100m - RsiExit))
			{
				BuyMarket();
				_entryPrice = 0;
			}
		}

		// Entry logic - oversold bounce (long) or overbought fade (short)
		if (Position == 0)
		{
			if (rsiVal < RsiEntry)
			{
				BuyMarket();
				_entryPrice = candle.ClosePrice;
			}
			else if (rsiVal > (100m - RsiEntry))
			{
				SellMarket();
				_entryPrice = candle.ClosePrice;
			}
		}
	}
}
