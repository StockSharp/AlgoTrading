using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Time-based breakout strategy. At start hour, detects breakout direction from previous candle range.
/// At stop hour, closes all positions.
/// </summary>
public class TwentyOneHourStrategy : Strategy
{
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _stopHour;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevHigh;
	private decimal _prevLow;
	private bool _hasPrev;
	private bool _tradedToday;
	private int _lastTradeDay;

	public TwentyOneHourStrategy()
	{
		_startHour = Param(nameof(StartHour), 10)
			.SetDisplay("Start Hour", "Hour to look for breakout entries.", "Schedule");

		_stopHour = Param(nameof(StopHour), 22)
			.SetDisplay("Stop Hour", "Hour to close positions.", "Schedule");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candles used for time tracking.", "General");
	}

	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	public int StopHour
	{
		get => _stopHour.Value;
		set => _stopHour.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevHigh = 0;
		_prevLow = 0;
		_hasPrev = false;
		_tradedToday = false;
		_lastTradeDay = -1;
	}

		protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevHigh = 0;
		_prevLow = 0;
		_hasPrev = false;
		_tradedToday = false;
		_lastTradeDay = -1;

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

		var hour = candle.OpenTime.Hour;
		var day = candle.OpenTime.DayOfYear;

		// Reset daily flag
		if (day != _lastTradeDay)
		{
			_tradedToday = false;
			_lastTradeDay = day;
		}

		// Close at stop hour
		if (hour >= StopHour && Position != 0)
		{
			if (Position > 0)
				SellMarket();
			else
				BuyMarket();
		}

		// Entry at start hour window
		if (hour >= StartHour && hour < StopHour && !_tradedToday && _hasPrev && Position == 0)
		{
			if (candle.ClosePrice > _prevHigh)
			{
				BuyMarket();
				_tradedToday = true;
			}
			else if (candle.ClosePrice < _prevLow)
			{
				SellMarket();
				_tradedToday = true;
			}
		}

		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;
		_hasPrev = true;
	}
}
