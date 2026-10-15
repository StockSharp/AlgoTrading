using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Keltner Williams R strategy.
/// The Keltner Channel is the EmaPeriod EMA plus and minus KeltnerMultiplier times the AtrPeriod ATR. A close below the lower band with
/// Williams %R below WilliamsROversold goes long and a close above the upper band with %R above WilliamsROverbought goes short, reversing
/// an opposite position. A long closes once price returns to the middle band and a short likewise, and a percent stop limits the loss.
/// </summary>
public class KeltnerWilliamsRStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<decimal> _keltnerMultiplier;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<int> _williamsRPeriod;
	private readonly StrategyParam<decimal> _williamsROversold;
	private readonly StrategyParam<decimal> _williamsROverbought;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Period of the channel EMA.
	/// </summary>
	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier of the channel width.
	/// </summary>
	public decimal KeltnerMultiplier
	{
		get => _keltnerMultiplier.Value;
		set => _keltnerMultiplier.Value = value;
	}

	/// <summary>
	/// Period of the channel ATR.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Period of Williams %R.
	/// </summary>
	public int WilliamsRPeriod
	{
		get => _williamsRPeriod.Value;
		set => _williamsRPeriod.Value = value;
	}

	/// <summary>
	/// Williams %R level for longs.
	/// </summary>
	public decimal WilliamsROversold
	{
		get => _williamsROversold.Value;
		set => _williamsROversold.Value = value;
	}

	/// <summary>
	/// Williams %R level for shorts.
	/// </summary>
	public decimal WilliamsROverbought
	{
		get => _williamsROverbought.Value;
		set => _williamsROverbought.Value = value;
	}

	/// <summary>
	/// Stop loss percentage from entry price.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public KeltnerWilliamsRStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "Period of the channel EMA", "Keltner");

		_keltnerMultiplier = Param(nameof(KeltnerMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Keltner Multiplier", "ATR multiplier of the channel width", "Keltner");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "Period of the channel ATR", "Keltner");

		_williamsRPeriod = Param(nameof(WilliamsRPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Williams %R Period", "Period of Williams %R", "Williams %R");

		_williamsROversold = Param(nameof(WilliamsROversold), -80m)
			.SetDisplay("Williams %R Oversold", "Williams %R level for longs", "Williams %R");

		_williamsROverbought = Param(nameof(WilliamsROverbought), -20m)
			.SetDisplay("Williams %R Overbought", "Williams %R level for shorts", "Williams %R");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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

		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };
		var williams = new WilliamsR { Length = WilliamsRPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(ema, atr, williams, ProcessCandle)
			.Start();

		StartProtection(new Unit(), new Unit(StopLossPercent, UnitTypes.Percent), useMarketOrders: true, isLocalStop: true);

		// The stop has to see prices between candles, not only at their close.
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Bind(ObserveProtectionQuote).Start();
		}

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
			{
				DrawIndicator(oscillators, williams);
			}
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// The high-level handler activates native protection before this callback, also between signal bars.
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue emaValue, IIndicatorValue atrValue, IIndicatorValue williamsValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!emaValue.IsFormed || !atrValue.IsFormed || !williamsValue.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var middle = emaValue.GetValue<decimal>();
		var atr = atrValue.GetValue<decimal>();
		var williams = williamsValue.GetValue<decimal>();
		var upper = middle + KeltnerMultiplier * atr;
		var lower = middle - KeltnerMultiplier * atr;
		var close = candle.ClosePrice;

		if (close < lower && williams < WilliamsROversold && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (close > upper && williams > WilliamsROverbought && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (Position > 0 && close >= middle)
			SellMarket(Position);
		else if (Position < 0 && close <= middle)
			BuyMarket(-Position);
	}
}
