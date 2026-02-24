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
/// CCI and RSI crossover strategy.
/// Buys when CCI is positive and RSI above 50 on bullish candle.
/// Sells when CCI is negative and RSI below 50 on bearish candle.
/// </summary>
public class CciComaStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _maPeriod;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int CciPeriod { get => _cciPeriod.Value; set => _cciPeriod.Value = value; }
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public int MaPeriod { get => _maPeriod.Value; set => _maPeriod.Value = value; }

	public CciComaStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_cciPeriod = Param(nameof(CciPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "CCI calculation length", "Indicators");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI calculation length", "Indicators");

		_maPeriod = Param(nameof(MaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Trend MA period", "Indicators");
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

		var cci = new CommodityChannelIndex { Length = CciPeriod };
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var sma = new SimpleMovingAverage { Length = MaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(cci, rsi, sma, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal cci, decimal rsi, decimal sma)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var bullishCandle = candle.ClosePrice > candle.OpenPrice;
		var bearishCandle = candle.ClosePrice < candle.OpenPrice;
		var trendUp = candle.ClosePrice > sma;
		var trendDown = candle.ClosePrice < sma;

		var longSignal = cci >= 0m && rsi > 50m && bullishCandle && trendUp;
		var shortSignal = cci <= 0m && rsi < 50m && bearishCandle && trendDown;

		if (Position > 0 && shortSignal)
		{
			SellMarket();
			SellMarket();
		}
		else if (Position < 0 && longSignal)
		{
			BuyMarket();
			BuyMarket();
		}
		else if (Position == 0)
		{
			if (longSignal)
				BuyMarket();
			else if (shortSignal)
				SellMarket();
		}
	}
}
