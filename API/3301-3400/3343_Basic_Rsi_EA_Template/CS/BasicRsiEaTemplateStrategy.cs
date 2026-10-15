namespace StockSharp.Samples.Strategies;

using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Basic RSI template: buys when RSI is oversold, sells when RSI is overbought.
/// </summary>
public class BasicRsiEaTemplateStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _overboughtLevel;
	private readonly StrategyParam<decimal> _oversoldLevel;

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

	public decimal OverboughtLevel
	{
		get => _overboughtLevel.Value;
		set => _overboughtLevel.Value = value;
	}

	public decimal OversoldLevel
	{
		get => _oversoldLevel.Value;
		set => _oversoldLevel.Value = value;
	}

	public BasicRsiEaTemplateStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI calculation period", "Indicators");

		_overboughtLevel = Param(nameof(OverboughtLevel), 70m)
			.SetDisplay("Overbought Level", "RSI overbought threshold", "Indicators");

		_oversoldLevel = Param(nameof(OversoldLevel), 30m)
			.SetDisplay("Oversold Level", "RSI oversold threshold", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		decimal? prevRsi = null;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, (candle, rsiValue) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				if (prevRsi.HasValue)
				{
					var crossBelowOversold = prevRsi.Value >= OversoldLevel && rsiValue < OversoldLevel;
					var crossAboveOverbought = prevRsi.Value <= OverboughtLevel && rsiValue > OverboughtLevel;

					if (crossBelowOversold && Position <= 0)
						BuyMarket();
					else if (crossAboveOverbought && Position >= 0)
						SellMarket();
				}

				prevRsi = rsiValue;
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, rsi);
			DrawOwnTrades(area);
		}
	}
}
