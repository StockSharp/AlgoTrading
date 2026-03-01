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
/// Pin bar reversal strategy with ATR based stops and targets.
/// </summary>
public class PinBarReversalStrategy : Strategy
{
	private readonly StrategyParam<int> _trendLength;
	private readonly StrategyParam<decimal> _maxBodyPct;
	private readonly StrategyParam<decimal> _minWickPct;
	private readonly StrategyParam<int> __atrLength;
	private readonly StrategyParam<decimal> __stopMultiplier;
	private readonly StrategyParam<decimal> __takeMultiplier;
	private readonly StrategyParam<decimal> __minAtr;
	private readonly StrategyParam<DataType> __candleType;

	private decimal __stopLevel;
	private decimal __profitLevel;

	/// <summary>
	/// Period for trend SMA.
	/// </summary>
	public int TrendLength
	{
		get => _trendLength.Value;
		set => _trendLength.Value = value;
	}

	/// <summary>
	/// Maximum body percent of candle range.
	/// </summary>
	public decimal MaxBodyPct
	{
		get => _maxBodyPct.Value;
		set => _maxBodyPct.Value = value;
	}

	/// <summary>
	/// Minimum wick percent of candle range.
	/// </summary>
	public decimal MinWickPct
	{
		get => _minWickPct.Value;
		set => _minWickPct.Value = value;
	}

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrLength
	{
		get => __atrLength.Value;
		set => __atrLength.Value = value;
	}

	/// <summary>
	/// Stop loss ATR multiplier.
	/// </summary>
	public decimal StopMultiplier
	{
		get => __stopMultiplier.Value;
		set => __stopMultiplier.Value = value;
	}

	/// <summary>
	/// Take profit ATR multiplier.
	/// </summary>
	public decimal TakeMultiplier
	{
		get => __takeMultiplier.Value;
		set => __takeMultiplier.Value = value;
	}

	/// <summary>
	/// Minimum ATR value to allow entry.
	/// </summary>
	public decimal MinAtr
	{
		get => __minAtr.Value;
		set => __minAtr.Value = value;
	}

	/// <summary>
	/// Working candle type.
	/// </summary>
	public DataType CandleType
	{
		get => __candleType.Value;
		set => __candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="PinBarReversalStrategy"/>.
	/// </summary>
	public PinBarReversalStrategy()
	{
			_trendLength = Param(nameof(TrendLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("Trend SMA Length", "Period for trend SMA", "General")
			;

			_maxBodyPct = Param(nameof(MaxBodyPct), 0.30m)
			.SetRange(0.1m, 0.5m)
			.SetDisplay("Max Body %", "Maximum body as % of range", "Pattern")
			;

			_minWickPct = Param(nameof(MinWickPct), 0.66m)
			.SetRange(0.5m, 0.9m)
			.SetDisplay("Min Wick %", "Minimum wick as % of range", "Pattern")
			;

		__atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR period", "Risk")
			;

		__stopMultiplier = Param(nameof(StopMultiplier), 1m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Stop Mult", "Stop loss ATR multiplier", "Risk");

		__takeMultiplier = Param(nameof(TakeMultiplier), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Take Mult", "Take profit ATR multiplier", "Risk");

		__minAtr = Param(nameof(MinAtr), 0.0015m)
			.SetGreaterThanZero()
			.SetDisplay("Min ATR", "Minimum ATR to allow entry", "Risk");

		__candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Working candle timeframe", "General");
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
		__stopLevel = 0m;
		__profitLevel = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var trendSma = new SimpleMovingAverage { Length = TrendLength };
		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(trendSma, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, trendSma);
			DrawIndicator(area, atr);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var range = candle.HighPrice - candle.LowPrice;
		if (range <= 0m)
		return;

		var body = Math.Abs(candle.ClosePrice - candle.OpenPrice);
		var lowerWick = Math.Min(candle.OpenPrice, candle.ClosePrice) - candle.LowPrice;
		var upperWick = candle.HighPrice - Math.Max(candle.OpenPrice, candle.ClosePrice);

		var inUpTrend = candle.ClosePrice > smaValue;
		var inDownTrend = candle.ClosePrice < smaValue;

		var bullPin = inUpTrend && body <= MaxBodyPct * range && lowerWick >= MinWickPct * range;
		var bearPin = inDownTrend && body <= MaxBodyPct * range && upperWick >= MinWickPct * range;

		if (Position == 0 && atrValue > MinAtr)
		{
			if (bullPin)
			{
				BuyMarket();
				__stopLevel = candle.LowPrice - atrValue * StopMultiplier;
				__profitLevel = candle.ClosePrice + atrValue * TakeMultiplier;
			}
			else if (bearPin)
			{
				SellMarket();
				__stopLevel = candle.HighPrice + atrValue * StopMultiplier;
				__profitLevel = candle.ClosePrice - atrValue * TakeMultiplier;
			}
		}
		else if (Position > 0)
		{
			if (candle.LowPrice <= __stopLevel || candle.HighPrice >= __profitLevel)
			SellMarket(Position);
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= __stopLevel || candle.LowPrice <= __profitLevel)
			BuyMarket(Math.Abs(Position));
		}
	}
}
