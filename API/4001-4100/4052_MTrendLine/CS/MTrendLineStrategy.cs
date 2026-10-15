using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trend line strategy using linear regression slope for direction.
/// Enters long when slope is positive and price is above regression, short otherwise.
/// </summary>
public class MTrendLineStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _regressionLength;

	private decimal _prevSlope;
	private bool _hasPrev;

	public MTrendLineStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for analysis.", "General");

		_regressionLength = Param(nameof(RegressionLength), 20)
			.SetDisplay("Regression Length", "Length of the linear regression.", "Indicators");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int RegressionLength
	{
		get => _regressionLength.Value;
		set => _regressionLength.Value = value;
	}

	/// <inheritdoc />
	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevSlope = 0;
		_hasPrev = false;
	}

		protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevSlope = 0;
		_hasPrev = false;

		var lr = new LinearReg { Length = RegressionLength };
		var ema = new ExponentialMovingAverage { Length = RegressionLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(lr, ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, lr);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal lrValue, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Compute slope as difference between LR and EMA
		var slope = lrValue - emaValue;

		if (!_hasPrev)
		{
			_prevSlope = slope;
			_hasPrev = true;
			return;
		}

		var close = candle.ClosePrice;

		// Exit conditions
		if (Position > 0 && slope < 0 && _prevSlope >= 0)
		{
			SellMarket();
		}
		else if (Position < 0 && slope > 0 && _prevSlope <= 0)
		{
			BuyMarket();
		}

		// Entry conditions
		if (Position == 0)
		{
			if (slope > 0 && _prevSlope <= 0 && close > lrValue)
			{
				BuyMarket();
			}
			else if (slope < 0 && _prevSlope >= 0 && close < lrValue)
			{
				SellMarket();
			}
		}

		_prevSlope = slope;
	}
}
