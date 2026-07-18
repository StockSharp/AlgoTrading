using System;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// January barometer strategy generalized to any month.
/// Measures the return over the first N candles of each evaluation period,
/// then goes long if bullish or short if bearish for the remainder.
/// Re-evaluates at the start of each new period.
/// </summary>
public class JanuaryBarometerStrategy : Strategy
{
	private readonly StrategyParam<int> _measureCandles;
	private readonly StrategyParam<int> _periodCandles;
	private readonly StrategyParam<DataType> _candleType;

	private int _candleCount;
	private decimal _periodOpen;
	private decimal _measureClose;
	private bool _measured;

	/// <summary>
	/// Number of candles in the measurement (barometer) window.
	/// </summary>
	public int MeasureCandles
	{
		get => _measureCandles.Value;
		set => _measureCandles.Value = value;
	}

	/// <summary>
	/// Total candles per evaluation period before resetting.
	/// </summary>
	public int PeriodCandles
	{
		get => _periodCandles.Value;
		set => _periodCandles.Value = value;
	}

	/// <summary>
	/// The type of candles to use for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public JanuaryBarometerStrategy()
	{
		_measureCandles = Param(nameof(MeasureCandles), 50)
			.SetGreaterThanZero()
			.SetDisplay("Measure Candles", "Number of candles for barometer measurement", "General");

		_periodCandles = Param(nameof(PeriodCandles), 200)
			.SetGreaterThanZero()
			.SetDisplay("Period Candles", "Total candles per evaluation period", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_candleCount = 0;
		_periodOpen = 0m;
		_measureClose = 0m;
		_measured = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_candleCount++;

		// Start of a new period
		if (_candleCount == 1)
		{
			_periodOpen = candle.OpenPrice;
			_measured = false;
		}

		// End of measurement window
		if (_candleCount == MeasureCandles && !_measured)
		{
			_measureClose = candle.ClosePrice;
			_measured = true;

			if (_periodOpen > 0m)
			{
				var barometerReturn = (_measureClose - _periodOpen) / _periodOpen;
				var bullish = barometerReturn > 0m;

				// Enter position based on barometer reading
				if (bullish && Position <= 0)
				{
					if (Position < 0)
						BuyMarket();

					BuyMarket();
				}
				else if (!bullish && Position >= 0)
				{
					if (Position > 0)
						SellMarket();

					SellMarket();
				}
			}
		}

		// End of period: close position and reset for next period
		if (_candleCount >= PeriodCandles)
		{
			if (Position > 0)
				SellMarket();
			else if (Position < 0)
				BuyMarket();

			_candleCount = 0;
			_periodOpen = 0m;
			_measureClose = 0m;
			_measured = false;
		}
	}
}
