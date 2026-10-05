using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MA CCI strategy.
/// A close above the MaPeriod SMA with CCI below OversoldLevel goes long and a close below it with CCI above OverboughtLevel goes short,
/// reversing an opposite position. The position closes once CCI returns to the zero line, and a percent stop limits the loss.
/// </summary>
public class MaCciStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<decimal> _overboughtLevel;
	private readonly StrategyParam<decimal> _oversoldLevel;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Period of the trend SMA.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Period of CCI.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// CCI level for shorts.
	/// </summary>
	public decimal OverboughtLevel
	{
		get => _overboughtLevel.Value;
		set => _overboughtLevel.Value = value;
	}

	/// <summary>
	/// CCI level for longs.
	/// </summary>
	public decimal OversoldLevel
	{
		get => _oversoldLevel.Value;
		set => _oversoldLevel.Value = value;
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
	public MaCciStrategy()
	{
		_maPeriod = Param(nameof(MaPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Period of the trend SMA", "Indicators");

		_cciPeriod = Param(nameof(CciPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "Period of CCI", "Indicators");

		_overboughtLevel = Param(nameof(OverboughtLevel), 100m)
			.SetDisplay("Overbought Level", "CCI level for shorts", "Indicators");

		_oversoldLevel = Param(nameof(OversoldLevel), -100m)
			.SetDisplay("Oversold Level", "CCI level for longs", "Indicators");

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

		var sma = new SimpleMovingAverage { Length = MaPeriod };
		var cci = new CommodityChannelIndex { Length = CciPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(sma, cci, ProcessCandle)
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
			DrawIndicator(area, sma);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
			{
				DrawIndicator(oscillators, cci);
			}
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// The high-level handler activates native protection before this callback, also between signal bars.
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue smaValue, IIndicatorValue cciValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!smaValue.IsFormed || !cciValue.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var ma = smaValue.GetValue<decimal>();
		var cci = cciValue.GetValue<decimal>();
		var close = candle.ClosePrice;

		if (close > ma && cci < OversoldLevel && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (close < ma && cci > OverboughtLevel && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (Position > 0 && cci >= 0)
			SellMarket(Position);
		else if (Position < 0 && cci <= 0)
			BuyMarket(-Position);
	}
}
