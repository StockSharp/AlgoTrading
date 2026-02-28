using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Open Time Daily Window: trades during a specific time window using
/// EMA direction for entry. Closes position at the end of window.
/// </summary>
public class OpenTimeDailyWindowStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _tradeHour;
	private readonly StrategyParam<int> _windowMinutes;
	private readonly StrategyParam<int> _closeHour;

	private decimal _prevEma;
	private decimal _entryPrice;

	public OpenTimeDailyWindowStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe.", "General");

		_emaLength = Param(nameof(EmaLength), 20)
			.SetDisplay("EMA Length", "EMA period for direction.", "Indicators");

		_tradeHour = Param(nameof(TradeHour), 10)
			.SetDisplay("Trade Hour", "Hour when trading window opens (UTC).", "Schedule");

		_windowMinutes = Param(nameof(WindowMinutes), 120)
			.SetDisplay("Window Minutes", "Duration of trading window.", "Schedule");

		_closeHour = Param(nameof(CloseHour), 20)
			.SetDisplay("Close Hour", "Hour to close positions (UTC).", "Schedule");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	public int TradeHour
	{
		get => _tradeHour.Value;
		set => _tradeHour.Value = value;
	}

	public int WindowMinutes
	{
		get => _windowMinutes.Value;
		set => _windowMinutes.Value = value;
	}

	public int CloseHour
	{
		get => _closeHour.Value;
		set => _closeHour.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevEma = 0;
		_entryPrice = 0;

		var ema = new ExponentialMovingAverage { Length = EmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var hour = candle.OpenTime.Hour;
		var minute = candle.OpenTime.Minute;
		var totalMinutes = hour * 60 + minute;
		var tradeStart = TradeHour * 60;
		var tradeEnd = tradeStart + WindowMinutes;
		var closeStart = CloseHour * 60;

		var close = candle.ClosePrice;

		// Close position at close hour
		if (Position != 0 && totalMinutes >= closeStart && totalMinutes < closeStart + 30)
		{
			if (Position > 0)
				SellMarket();
			else
				BuyMarket();
			_entryPrice = 0;
		}

		if (_prevEma == 0)
		{
			_prevEma = emaVal;
			return;
		}

		// Trade within window
		var inWindow = totalMinutes >= tradeStart && totalMinutes < tradeEnd;

		if (Position == 0 && inWindow)
		{
			var emaRising = emaVal > _prevEma;
			var emaFalling = emaVal < _prevEma;

			if (emaRising && close > emaVal)
			{
				_entryPrice = close;
				BuyMarket();
			}
			else if (emaFalling && close < emaVal)
			{
				_entryPrice = close;
				SellMarket();
			}
		}

		_prevEma = emaVal;
	}
}
