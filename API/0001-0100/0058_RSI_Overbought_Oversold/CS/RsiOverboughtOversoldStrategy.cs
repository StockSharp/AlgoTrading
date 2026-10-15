using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSI overbought/oversold reversal with neutral exits.
/// Buys while RSI is below the oversold level, sells while it is above the overbought level.
/// Exits at the neutral level and uses native actual-fill percent protection.
/// </summary>
public class RsiOverboughtOversoldStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _oversoldLevel;
	private readonly StrategyParam<int> _overboughtLevel;
	private readonly StrategyParam<int> _neutralLevel;
	private readonly StrategyParam<decimal> _stopLossPercent;

	private Order _pendingOrder;

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int OversoldLevel { get => _oversoldLevel.Value; set => _oversoldLevel.Value = value; }
	public int OverboughtLevel { get => _overboughtLevel.Value; set => _overboughtLevel.Value = value; }
	public int NeutralLevel { get => _neutralLevel.Value; set => _neutralLevel.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="RsiOverboughtOversoldStrategy"/>.
	/// </summary>
	public RsiOverboughtOversoldStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 14).SetGreaterThanZero()
			.SetDisplay("RSI Period", "Period for RSI calculation", "Indicators")
			.SetOptimize(10, 20, 2);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
		_oversoldLevel = Param(nameof(OversoldLevel), 30).SetRange(0, 100)
			.SetDisplay("Oversold Threshold", "Enter long while RSI is below this level.", "Indicators");
		_overboughtLevel = Param(nameof(OverboughtLevel), 70).SetRange(0, 100)
			.SetDisplay("Overbought Threshold", "Enter short while RSI is above this level.", "Indicators");
		_neutralLevel = Param(nameof(NeutralLevel), 50).SetRange(0, 100)
			.SetDisplay("Exit Level", "Close on return to the neutral level.", "Indicators");
		_stopLossPercent = Param(nameof(StopLossPercent), 2m).SetNotNegative()
			.SetDisplay("Stop Loss (%)", "Actual-fill percent stop; zero disables it.", "Protection");
		OrderRegistering += order => _pendingOrder = order;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (Security, DataType.Level1)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_pendingOrder = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		if (!(OversoldLevel < NeutralLevel && NeutralLevel < OverboughtLevel))
			throw new InvalidOperationException("OversoldLevel must be below NeutralLevel, which must be below OverboughtLevel.");

		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		StartProtection(new Unit(), new Unit(StopLossPercent, UnitTypes.Percent), useMarketOrders: true, isLocalStop: true);
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Bind(ObserveProtectionQuote).Start();
		}

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, rsi);
			DrawOwnTrades(area);
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// Native protection runs before the callback, including between finished candles.
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_pendingOrder is not null && _pendingOrder.State is not (OrderStates.Done or OrderStates.Failed))
			return;

		if (rsiValue < OversoldLevel && Position <= 0m)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
		}
		else if (rsiValue > OverboughtLevel && Position >= 0m)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
		}
		else if (Position > 0m && rsiValue >= NeutralLevel)
			SellMarket(Position);
		else if (Position < 0m && rsiValue <= NeutralLevel)
			BuyMarket(Math.Abs(Position));
	}
}
