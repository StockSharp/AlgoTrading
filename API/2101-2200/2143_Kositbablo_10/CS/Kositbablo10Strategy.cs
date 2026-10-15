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
/// Strategy based on RSI and EMA signals.
/// Buys when RSI is oversold and EMA cross is bearish (mean-reversion).
/// Sells when RSI is overbought and EMA cross is bearish.
/// </summary>
public class Kositbablo10Strategy : Strategy
{
	private readonly StrategyParam<int> _rsiBuyPeriod;
	private readonly StrategyParam<int> _rsiSellPeriod;
	private readonly StrategyParam<int> _emaLongPeriod;
	private readonly StrategyParam<int> _emaShortPeriod;
	private readonly StrategyParam<decimal> _stopLossPct;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// RSI period for buy signals.
	/// </summary>
	public int RsiBuyPeriod
	{
		get => _rsiBuyPeriod.Value;
		set => _rsiBuyPeriod.Value = value;
	}

	/// <summary>
	/// RSI period for sell signals.
	/// </summary>
	public int RsiSellPeriod
	{
		get => _rsiSellPeriod.Value;
		set => _rsiSellPeriod.Value = value;
	}

	/// <summary>
	/// Long EMA period.
	/// </summary>
	public int EmaLongPeriod
	{
		get => _emaLongPeriod.Value;
		set => _emaLongPeriod.Value = value;
	}

	/// <summary>
	/// Short EMA period.
	/// </summary>
	public int EmaShortPeriod
	{
		get => _emaShortPeriod.Value;
		set => _emaShortPeriod.Value = value;
	}

	/// <summary>
	/// Stop loss percentage.
	/// </summary>
	public decimal StopLossPct
	{
		get => _stopLossPct.Value;
		set => _stopLossPct.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public Kositbablo10Strategy()
	{
		_rsiBuyPeriod = Param(nameof(RsiBuyPeriod), 5)
			.SetDisplay("RSI Buy Period", "RSI period for buy signals", "Indicators");
		_rsiSellPeriod = Param(nameof(RsiSellPeriod), 20)
			.SetDisplay("RSI Sell Period", "RSI period for sell signals", "Indicators");
		_emaLongPeriod = Param(nameof(EmaLongPeriod), 20)
			.SetDisplay("EMA Long", "Long EMA period", "Indicators");
		_emaShortPeriod = Param(nameof(EmaShortPeriod), 5)
			.SetDisplay("EMA Short", "Short EMA period", "Indicators");
		_stopLossPct = Param(nameof(StopLossPct), 1m)
			.SetDisplay("Stop Loss %", "Stop loss percent", "Risk")
			.SetGreaterThanZero();
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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

		var rsiBuy = new RelativeStrengthIndex { Length = RsiBuyPeriod };
		var rsiSell = new RelativeStrengthIndex { Length = RsiSellPeriod };
		var emaLong = new ExponentialMovingAverage { Length = EmaLongPeriod };
		var emaShort = new ExponentialMovingAverage { Length = EmaShortPeriod };

		StartProtection(
			takeProfit: new Unit(2m, UnitTypes.Percent),
			stopLoss: new Unit(StopLossPct, UnitTypes.Percent));

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsiBuy, rsiSell, emaLong, emaShort, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiBuy, decimal rsiSell, decimal emaLong, decimal emaShort)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var buyCond = rsiBuy < 48 && emaLong > emaShort;
		var sellCond = rsiSell > 60 && emaLong > emaShort;

		if (buyCond && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		else if (sellCond && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}
	}
}
