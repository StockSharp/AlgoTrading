using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Evaluates the trend of a moving average relative to a price channel.
/// Goes long on positive trend score and short on negative score.
/// </summary>
public class TrendFollowingMovingAveragesStrategy : Strategy
{
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<int> _trendPeriod;
	private readonly StrategyParam<decimal> _trendRate;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Moving average length.
	/// </summary>
	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	/// <summary>
	/// Period to evaluate trend strength.
	/// </summary>
	public int TrendPeriod
	{
		get => _trendPeriod.Value;
		set => _trendPeriod.Value = value;
	}

	/// <summary>
	/// Channel width rate in percent.
	/// </summary>
	public decimal TrendRate
	{
		get => _trendRate.Value;
		set => _trendRate.Value = value;
	}

	/// <summary>
	/// Candle type for processing.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public TrendFollowingMovingAveragesStrategy()
	{
		_maLength = Param(nameof(MaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "Moving average period", "Parameters");

		_trendPeriod = Param(nameof(TrendPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Trend Period", "Bars to check trend", "Parameters");

		_trendRate = Param(nameof(TrendRate), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trend Rate %", "Channel width rate", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candles for strategy", "General");
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

		var ma = new SimpleMovingAverage { Length = MaLength };
		var highestMa = new Highest { Length = TrendPeriod };
		var lowestMa = new Lowest { Length = TrendPeriod };
		var highestClose = new Highest { Length = 280 };
		var lowestClose = new Lowest { Length = 280 };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(ma, (candle, maValue) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				var hh = highestMa.Process(new DecimalIndicatorValue(highestMa, maValue, candle.OpenTime)).ToDecimal();
				var ll = lowestMa.Process(new DecimalIndicatorValue(lowestMa, maValue, candle.OpenTime)).ToDecimal();
				var hc = highestClose.Process(new DecimalIndicatorValue(highestClose, candle.ClosePrice, candle.OpenTime)).ToDecimal();
				var lc = lowestClose.Process(new DecimalIndicatorValue(lowestClose, candle.ClosePrice, candle.OpenTime)).ToDecimal();

				var diff = Math.Abs(hh - ll);
				var chan = (hc - lc) * (TrendRate / 100m);

				decimal trend = 0m;
				if (diff > chan)
				{
					if (maValue > ll + chan)
						trend = 1m;
					else if (maValue < hh - chan)
						trend = -1m;
				}

				var score = chan == 0 ? 0 : trend * diff / chan;

				if (score > 0 && Position <= 0)
					BuyMarket(Volume + Math.Abs(Position));
				else if (score < 0 && Position >= 0)
					SellMarket(Volume + Math.Abs(Position));
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ma);
			DrawOwnTrades(area);
		}
	}
}
