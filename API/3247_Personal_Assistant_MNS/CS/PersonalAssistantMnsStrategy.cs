using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Personal Assistant MNS strategy: automated RSI-based entries with SMA trend filter.
/// Buys when RSI exits oversold in an uptrend, sells when RSI exits overbought in a downtrend.
/// </summary>
public class PersonalAssistantMnsStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _smaPeriod;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<decimal> _rsiOversold;

	private decimal? _prevRsi;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	public int SmaPeriod
	{
		get => _smaPeriod.Value;
		set => _smaPeriod.Value = value;
	}

	public decimal RsiOverbought
	{
		get => _rsiOverbought.Value;
		set => _rsiOverbought.Value = value;
	}

	public decimal RsiOversold
	{
		get => _rsiOversold.Value;
		set => _rsiOversold.Value = value;
	}

	public PersonalAssistantMnsStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI lookback", "Indicators");

		_smaPeriod = Param(nameof(SmaPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("SMA Period", "SMA trend filter period", "Indicators");

		_rsiOverbought = Param(nameof(RsiOverbought), 70m)
			.SetDisplay("RSI Overbought", "Overbought level", "Signals");

		_rsiOversold = Param(nameof(RsiOversold), 30m)
			.SetDisplay("RSI Oversold", "Oversold level", "Signals");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevRsi = null;

		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var sma = new SimpleMovingAverage { Length = SmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsi, decimal sma)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;
		var uptrend = close > sma;
		var downtrend = close < sma;

		if (_prevRsi.HasValue)
		{
			// Buy: RSI crosses up from oversold in uptrend
			if (uptrend && _prevRsi.Value < RsiOversold && rsi >= RsiOversold && Position <= 0)
			{
				BuyMarket();
			}
			// Sell: RSI crosses down from overbought in downtrend
			else if (downtrend && _prevRsi.Value > RsiOverbought && rsi <= RsiOverbought && Position >= 0)
			{
				SellMarket();
			}
		}

		_prevRsi = rsi;
	}
}
