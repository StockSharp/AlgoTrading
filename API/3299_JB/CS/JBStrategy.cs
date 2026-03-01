using System;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// JB strategy: Bollinger Bands + SMA trend.
/// Buys when close is below lower BB and above SMA.
/// Sells when close is above upper BB and below SMA.
/// </summary>
public class JbStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _bbPeriod;
	private readonly StrategyParam<int> _smaPeriod;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int BbPeriod
	{
		get => _bbPeriod.Value;
		set => _bbPeriod.Value = value;
	}

	public int SmaPeriod
	{
		get => _smaPeriod.Value;
		set => _smaPeriod.Value = value;
	}

	public JbStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_bbPeriod = Param(nameof(BbPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("BB Period", "Bollinger Bands period", "Indicators");

		_smaPeriod = Param(nameof(SmaPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("SMA Period", "SMA period", "Indicators");
	}

	private decimal _smaValue;

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var bb = new BollingerBands { Length = BbPeriod };
		var sma = new SimpleMovingAverage { Length = SmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, OnSma);

		subscription
			.BindEx(bb, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bb);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void OnSma(ICandleMessage candle, decimal smaValue)
	{
		_smaValue = smaValue;
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bbVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var bb = (BollingerBandsValue)bbVal;
		if (bb.UpBand is not decimal upper || bb.LowBand is not decimal lower)
			return;

		var close = candle.ClosePrice;

		if (close < lower && Position <= 0)
		{
			BuyMarket();
		}
		else if (close > upper && Position >= 0)
		{
			SellMarket();
		}
	}
}
