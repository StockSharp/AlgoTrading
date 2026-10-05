namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// RSI + 1200 Strategy.
/// An EMA of EmaLength bars is calculated on MtfTimeframe candles. A long opens when RSI crosses above RsiOversold and the close
/// is at most 1% above that EMA; a short opens when RSI crosses below RsiOverbought and the close is no more than 1% below it. Longs close once RSI rises above RsiOverbought and shorts once it falls below RsiOversold.
/// A stop at StopLossPercent (a fraction, 0.10 = 10%) from the entry limits the loss.
/// </summary>
public class RsiPlus1200Strategy : Strategy
{
	private const decimal EmaSlack = 0.01m;

	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<DataType> _mtfTimeframe;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeStrengthIndex _rsi;
	private ExponentialMovingAverage _ema;
	private decimal? _prevRsi;
	private decimal? _mtfEma;

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// RSI overbought level.
	/// </summary>
	public decimal RsiOverbought
	{
		get => _rsiOverbought.Value;
		set => _rsiOverbought.Value = value;
	}

	/// <summary>
	/// RSI oversold level.
	/// </summary>
	public decimal RsiOversold
	{
		get => _rsiOversold.Value;
		set => _rsiOversold.Value = value;
	}

	/// <summary>
	/// EMA period on the higher time frame.
	/// </summary>
	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	/// <summary>
	/// Higher time frame of the EMA.
	/// </summary>
	public DataType MtfTimeframe
	{
		get => _mtfTimeframe.Value;
		set => _mtfTimeframe.Value = value;
	}

	/// <summary>
	/// Stop loss as a fraction of the entry price (0.10 = 10%).
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Candle type of the RSI signals.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public RsiPlus1200Strategy()
	{
		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "Indicators");

		_rsiOverbought = Param(nameof(RsiOverbought), 72m)
			.SetDisplay("RSI Overbought", "RSI overbought level", "Indicators");

		_rsiOversold = Param(nameof(RsiOversold), 28m)
			.SetDisplay("RSI Oversold", "RSI oversold level", "Indicators");

		_emaLength = Param(nameof(EmaLength), 150)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "EMA period on the higher time frame", "Indicators");

		_mtfTimeframe = Param(nameof(MtfTimeframe), TimeSpan.FromMinutes(120).TimeFrame())
			.SetDisplay("MTF Timeframe", "Higher time frame of the EMA", "General");

		_stopLossPercent = Param(nameof(StopLossPercent), 0.10m)
			.SetNotNegative()
			.SetDisplay("Stop Loss", "Stop loss as a fraction of the entry price (0.10 = 10%)", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Candle type of the RSI signals", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType), (Security, MtfTimeframe)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevRsi = null;
		_mtfEma = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevRsi = null;
		_mtfEma = null;

		_rsi = new RelativeStrengthIndex { Length = RsiLength };
		_ema = new ExponentialMovingAverage { Length = EmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rsi, ProcessCandle)
			.Start();

		SubscribeCandles(MtfTimeframe)
			.Bind(_ema, ProcessMtfCandle)
			.Start();

		if (StopLossPercent > 0m)
		{
			StartProtection(new Unit(), new Unit(StopLossPercent * 100m, UnitTypes.Percent), useMarketOrders: true, isLocalStop: true);

			// The stop has to see prices between candles, not only at their close.
			foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
			{
				var quotes = new Subscription(DataType.Level1, Security);
				quotes.MarketData.BuildField = field;
				SubscribeLevel1(quotes).Bind(ObserveProtectionQuote).Start();
			}
		}

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
				DrawIndicator(oscillators, _rsi);
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// The high-level handler activates native protection before this callback, also between signal bars.
	}

	private void ProcessMtfCandle(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished || !_ema.IsFormed)
			return;

		_mtfEma = emaValue;
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsi)
	{
		if (candle.State != CandleStates.Finished || !_rsi.IsFormed)
			return;

		var prevRsi = _prevRsi;
		_prevRsi = rsi;

		if (prevRsi is not decimal previous || _mtfEma is not decimal ema)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;
		var crossUpOversold = previous <= RsiOversold && rsi > RsiOversold;
		var crossDownOverbought = previous >= RsiOverbought && rsi < RsiOverbought;

		if (crossUpOversold && close <= ema * (1m + EmaSlack) && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (crossDownOverbought && close >= ema * (1m - EmaSlack) && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (Position > 0 && rsi > RsiOverbought)
			SellMarket(Position);
		else if (Position < 0 && rsi < RsiOversold)
			BuyMarket(-Position);
	}
}
