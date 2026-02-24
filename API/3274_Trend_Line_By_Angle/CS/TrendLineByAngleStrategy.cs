using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trend Line By Angle strategy: Linear Regression slope with SMA filter.
/// Buys when linear regression rises and close is above SMA.
/// Sells when linear regression falls and close is below SMA.
/// </summary>
public class TrendLineByAngleStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _lrPeriod;
	private readonly StrategyParam<int> _smaPeriod;

	private decimal? _prevLr;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int LrPeriod
	{
		get => _lrPeriod.Value;
		set => _lrPeriod.Value = value;
	}

	public int SmaPeriod
	{
		get => _smaPeriod.Value;
		set => _smaPeriod.Value = value;
	}

	public TrendLineByAngleStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_lrPeriod = Param(nameof(LrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("LR Period", "Linear Regression period", "Indicators");

		_smaPeriod = Param(nameof(SmaPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("SMA Period", "SMA trend filter period", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevLr = null;

		var lr = new LinearReg { Length = LrPeriod };
		var sma = new SimpleMovingAverage { Length = SmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(lr, sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal lrValue, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;

		if (_prevLr.HasValue)
		{
			var lrRising = lrValue > _prevLr.Value;
			var lrFalling = lrValue < _prevLr.Value;

			if (lrRising && close > smaValue && Position <= 0)
			{
				BuyMarket();
			}
			else if (lrFalling && close < smaValue && Position >= 0)
			{
				SellMarket();
			}
		}

		_prevLr = lrValue;
	}
}
