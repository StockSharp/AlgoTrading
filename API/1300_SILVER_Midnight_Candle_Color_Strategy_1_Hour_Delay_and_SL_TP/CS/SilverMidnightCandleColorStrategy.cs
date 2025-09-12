using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;
using StockSharp.BusinessEntities;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trades at 1:00 AM New York time based on the previous day's midnight candle color.
/// Long position has 57-tick take profit and 200-tick stop loss.
/// Short position has 48-tick take profit and 200-tick stop loss.
/// </summary>
public class SilverMidnightCandleColorStrategy : Strategy
{
	private readonly StrategyParam<int> _longTakeProfitTicks;
	private readonly StrategyParam<int> _shortTakeProfitTicks;
	private readonly StrategyParam<int> _stopLossTicks;
	private readonly StrategyParam<int> _timezoneOffset;
	private readonly StrategyParam<DataType> _candleType;

	private bool? _midnightIsGreen;
	private decimal? _prevOpen;
	private decimal? _prevClose;
	private decimal _tickSize;

	/// <summary>
	/// Take profit distance in ticks for long positions.
	/// </summary>
	public int LongTakeProfitTicks
	{
		get => _longTakeProfitTicks.Value;
		set => _longTakeProfitTicks.Value = value;
	}

	/// <summary>
	/// Take profit distance in ticks for short positions.
	/// </summary>
	public int ShortTakeProfitTicks
	{
		get => _shortTakeProfitTicks.Value;
		set => _shortTakeProfitTicks.Value = value;
	}

	/// <summary>
	/// Stop loss distance in ticks.
	/// </summary>
	public int StopLossTicks
	{
		get => _stopLossTicks.Value;
		set => _stopLossTicks.Value = value;
	}

	/// <summary>
	/// Hours offset from UTC to New York time.
	/// </summary>
	public int TimezoneOffset
	{
		get => _timezoneOffset.Value;
		set => _timezoneOffset.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize strategy parameters.
	/// </summary>
	public SilverMidnightCandleColorStrategy()
	{
		_longTakeProfitTicks = Param(nameof(LongTakeProfitTicks), 57)
			.SetGreaterOrEqualZero()
			.SetDisplay("Long TP Ticks", "Take profit ticks for long entries", "Risk");

		_shortTakeProfitTicks = Param(nameof(ShortTakeProfitTicks), 48)
			.SetGreaterOrEqualZero()
			.SetDisplay("Short TP Ticks", "Take profit ticks for short entries", "Risk");

		_stopLossTicks = Param(nameof(StopLossTicks), 200)
			.SetGreaterOrEqualZero()
			.SetDisplay("Stop Loss Ticks", "Stop loss distance in ticks", "Risk");

		_timezoneOffset = Param(nameof(TimezoneOffset), -5)
			.SetDisplay("Timezone Offset", "Hours offset from UTC to New York", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_midnightIsGreen = null;
		_prevOpen = null;
		_prevClose = null;
	}


	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_tickSize = Security.PriceStep ?? 1m;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

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

		var nyTime = candle.OpenTime.UtcDateTime.AddHours(TimezoneOffset);

		if (nyTime.Hour == 0 && nyTime.Minute == 0)
		{
			if (_prevOpen is decimal pOpen && _prevClose is decimal pClose)
				_midnightIsGreen = pClose > pOpen;
		}
		else if (nyTime.Hour == 1 && nyTime.Minute == 0)
		{
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			if (_midnightIsGreen is bool isGreen)
			{
				var volume = Volume + Math.Abs(Position);

				if (isGreen && Position <= 0)
				{
					CancelActiveOrders();
					BuyMarket(volume);
					var tp = candle.ClosePrice + LongTakeProfitTicks * _tickSize;
					var sl = candle.ClosePrice - StopLossTicks * _tickSize;
					SellLimit(tp, volume);
					SellStop(sl, volume);
				}
				else if (!isGreen && Position >= 0)
				{
					CancelActiveOrders();
					SellMarket(volume);
					var tp = candle.ClosePrice - ShortTakeProfitTicks * _tickSize;
					var sl = candle.ClosePrice + StopLossTicks * _tickSize;
					BuyLimit(tp, volume);
					BuyStop(sl, volume);
				}
			}
		}

		_prevOpen = candle.OpenPrice;
		_prevClose = candle.ClosePrice;
	}
}
