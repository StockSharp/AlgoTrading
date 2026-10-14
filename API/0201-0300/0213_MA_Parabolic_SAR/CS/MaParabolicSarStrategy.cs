using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MA Parabolic SAR strategy.
/// A close above both the MaPeriod simple moving average and the Parabolic SAR goes long and a close below both goes short, reversing
/// an opposite position. The SAR is the trailing stop: a long closes when price closes below it and a short when price closes above it,
/// and a percent stop limits the loss.
/// </summary>
public class MaParabolicSarStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<decimal> _sarStep;
	private readonly StrategyParam<decimal> _sarMaxStep;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Period of the simple moving average.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Acceleration factor of the SAR.
	/// </summary>
	public decimal SarStep
	{
		get => _sarStep.Value;
		set => _sarStep.Value = value;
	}

	/// <summary>
	/// Maximum acceleration factor of the SAR.
	/// </summary>
	public decimal SarMaxStep
	{
		get => _sarMaxStep.Value;
		set => _sarMaxStep.Value = value;
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
	public MaParabolicSarStrategy()
	{
		_maPeriod = Param(nameof(MaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Period of the simple moving average", "Indicators");

		_sarStep = Param(nameof(SarStep), 0.02m)
			.SetGreaterThanZero()
			.SetDisplay("SAR Step", "Acceleration factor of the SAR", "Indicators");

		_sarMaxStep = Param(nameof(SarMaxStep), 0.2m)
			.SetGreaterThanZero()
			.SetDisplay("SAR Max Step", "Maximum acceleration factor of the SAR", "Indicators");

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

		var ma = new SimpleMovingAverage { Length = MaPeriod };
		var sar = new ParabolicSar
		{
			Acceleration = SarStep,
			AccelerationMax = SarMaxStep,
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(ma, sar, ProcessCandle)
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
			DrawIndicator(area, ma);
			DrawIndicator(area, sar);
			DrawOwnTrades(area);
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// The high-level handler activates native protection before this callback, also between signal bars.
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue maValue, IIndicatorValue sarValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// The first SAR value is formed but empty.
		if (!maValue.IsFormed || !sarValue.IsFormed || sarValue.IsEmpty)
			return;

		var ma = maValue.GetValue<decimal>();
		var sar = sarValue.GetValue<decimal>();
		var close = candle.ClosePrice;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (close > ma && close > sar && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (close < ma && close < sar && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (Position > 0 && close < sar)
			SellMarket(Position);
		else if (Position < 0 && close > sar)
			BuyMarket(-Position);
	}
}
