using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on RSI mean reversion.
/// Buys when RSI enters the oversold zone, sells when it enters the overbought zone.
/// Exits at the neutral level and uses native actual-fill percent protection.
/// </summary>
public class RsiReversionStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _oversoldThreshold;
	private readonly StrategyParam<decimal> _overboughtThreshold;
	private readonly StrategyParam<decimal> _exitLevel;
	private readonly StrategyParam<decimal> _stopLossPercent;

	private decimal _prevRsi;
	private bool _hasPrevValues;
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

	public decimal OversoldThreshold { get => _oversoldThreshold.Value; set => _oversoldThreshold.Value = value; }
	public decimal OverboughtThreshold { get => _overboughtThreshold.Value; set => _overboughtThreshold.Value = value; }
	public decimal ExitLevel { get => _exitLevel.Value; set => _exitLevel.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="RsiReversionStrategy"/>.
	/// </summary>
	public RsiReversionStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 14).SetGreaterThanZero()
			.SetDisplay("RSI Period", "Period for RSI calculation", "Indicators")
			.SetOptimize(10, 20, 2);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
		_oversoldThreshold = Param(nameof(OversoldThreshold), 30m).SetRange(0m, 100m)
			.SetDisplay("Oversold Threshold", "Enter long on a strict downward crossing.", "Indicators");
		_overboughtThreshold = Param(nameof(OverboughtThreshold), 70m).SetRange(0m, 100m)
			.SetDisplay("Overbought Threshold", "Enter short on a strict upward crossing.", "Indicators");
		_exitLevel = Param(nameof(ExitLevel), 50m).SetRange(0m, 100m)
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
		_prevRsi = default;
		_hasPrevValues = default;
		_pendingOrder = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		if (!(OversoldThreshold < ExitLevel && ExitLevel < OverboughtThreshold))
			throw new InvalidOperationException("OversoldThreshold must be below ExitLevel, which must be below OverboughtThreshold.");

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

		if (!_hasPrevValues)
		{
			_hasPrevValues = true;
			_prevRsi = rsiValue;
			return;
		}

		if (_pendingOrder is not null && _pendingOrder.State is not (OrderStates.Done or OrderStates.Failed))
		{
			_prevRsi = rsiValue;
			return;
		}

		// Fade entry INTO each extreme; this is not a crossing out of the extreme zone.
		if (_prevRsi >= OversoldThreshold && rsiValue < OversoldThreshold && Position <= 0m)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
		}
		else if (_prevRsi <= OverboughtThreshold && rsiValue > OverboughtThreshold && Position >= 0m)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
		}
		else if (Position > 0m && rsiValue >= ExitLevel)
			SellMarket(Position);
		else if (Position < 0m && rsiValue <= ExitLevel)
			BuyMarket(Math.Abs(Position));

		_prevRsi = rsiValue;
	}
}
