using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy demonstrating how to handle earnings, split and dividend information from news feed.
/// The strategy listens for news and logs corporate action events.
/// </summary>
public class EarningsSplitsDividendsStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Candle type for auxiliary processing.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="EarningsSplitsDividendsStrategy"/> class.
	/// </summary>
	public EarningsSplitsDividendsStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Connector.SubscribeMarketData(Security, MarketDataTypes.News);

		SubscribeCandles(CandleType)
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;
	}

	/// <inheritdoc />
	protected override void OnProcessMessage(Message message)
	{
		base.OnProcessMessage(message);

		if (message.Type != MessageTypes.News)
			return;

		var news = (NewsMessage)message;
		var text = (news.Headline + " " + news.Story)?.ToLowerInvariant();

		if (text == null)
			return;

		if (text.Contains("earning"))
		{
			LogInfo($"Earnings event at {news.ServerTime:O}");
		}
		else if (text.Contains("split"))
		{
			LogInfo($"Split event at {news.ServerTime:O}");
		}
		else if (text.Contains("dividend"))
		{
			LogInfo($"Dividend event at {news.ServerTime:O}");
		}
	}
}
