using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MOC Delta MOO Entry v2 strategy.
/// Uses afternoon volume delta to trade next day open.
/// </summary>
public class MocDeltaMooEntryV2Strategy : Strategy
{
	private readonly StrategyParam<int> _tpTicks;
	private readonly StrategyParam<int> _slTicks;
	private readonly StrategyParam<decimal> _deltaThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _dailyVolume;
	private decimal? _afternoonHigh;
	private decimal? _afternoonLow;
	private decimal _afternoonBuyVol;
	private decimal _afternoonSellVol;
	private bool _afternoonTracking;
	private decimal _mocDeltaPct;

	/// <summary>
	/// Take profit in ticks.
	/// </summary>
	public int TpTicks { get => _tpTicks.Value; set => _tpTicks.Value = value; }

	/// <summary>
	/// Stop loss in ticks.
	/// </summary>
	public int SlTicks { get => _slTicks.Value; set => _slTicks.Value = value; }

	/// <summary>
	/// Delta percentage threshold.
	/// </summary>
	public decimal DeltaThreshold { get => _deltaThreshold.Value; set => _deltaThreshold.Value = value; }

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of <see cref="MocDeltaMooEntryV2Strategy"/>.
	/// </summary>
	public MocDeltaMooEntryV2Strategy()
	{
		_tpTicks = Param(nameof(TpTicks), 20)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit Ticks", "Take profit distance in ticks", "Risk Management");

		_slTicks = Param(nameof(SlTicks), 10)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss Ticks", "Stop loss distance in ticks", "Risk Management");

		_deltaThreshold = Param(nameof(DeltaThreshold), 2m)
		.SetGreaterThanZero()
		.SetDisplay("Delta % Threshold", "Minimum delta percent", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Candles for calculation", "Parameters");
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

		_dailyVolume = 0m;
		_afternoonHigh = null;
		_afternoonLow = null;
		_afternoonBuyVol = 0m;
		_afternoonSellVol = 0m;
		_afternoonTracking = false;
		_mocDeltaPct = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var sma15 = new SimpleMovingAverage { Length = 15 };
		var sma30 = new SimpleMovingAverage { Length = 30 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma15, sma30, ProcessCandle)
			.Start();

		StartProtection(
			takeProfit: new Unit(TpTicks, UnitTypes.Step),
			stopLoss: new Unit(SlTicks, UnitTypes.Step));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma15);
			DrawIndicator(area, sma30);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal sma15Value, decimal sma30Value)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var time = candle.OpenTime;
		var hour = time.Hour;
		var minute = time.Minute;

		// reset daily volume at 08:30
		if (hour == 8 && minute == 30)
		_dailyVolume = 0m;

		_dailyVolume += candle.TotalVolume;

		var inAfternoonRange = hour == 14 && minute >= 50 && minute <= 55;
		if (inAfternoonRange)
		{
			if (_afternoonTracking)
			{
				_afternoonHigh = Math.Max(_afternoonHigh ?? candle.HighPrice, candle.HighPrice);
				_afternoonLow = Math.Min(_afternoonLow ?? candle.LowPrice, candle.LowPrice);
			}
			else
			{
				_afternoonHigh = candle.HighPrice;
				_afternoonLow = candle.LowPrice;
				_afternoonTracking = true;
			}

			if (candle.ClosePrice > candle.OpenPrice)
				_afternoonBuyVol += candle.TotalVolume;
			else if (candle.ClosePrice < candle.OpenPrice)
				_afternoonSellVol += candle.TotalVolume;
		}

		var endAfternoonBar = hour == 14 && minute == 55 && _afternoonTracking;
		if (endAfternoonBar)
		{
			var mocDelta = _afternoonBuyVol - _afternoonSellVol;
			_mocDeltaPct = _dailyVolume > 0 ? mocDelta / _dailyVolume * 100m : 0m;

			_afternoonHigh = null;
			_afternoonLow = null;
			_afternoonBuyVol = 0m;
			_afternoonSellVol = 0m;
			_afternoonTracking = false;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var is830 = hour == 8 && minute == 30;
		var bullishMoc = _mocDeltaPct > DeltaThreshold;
		var bearishMoc = _mocDeltaPct < -DeltaThreshold;

		var validLong = candle.OpenPrice > sma15Value && candle.OpenPrice > sma30Value && sma15Value > sma30Value;
		var validShort = candle.OpenPrice < sma15Value && candle.OpenPrice < sma30Value && sma15Value < sma30Value;

		if (is830)
		{
			if (bullishMoc && validLong && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
			else if (bearishMoc && validShort && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		}

		if (hour == 14 && minute == 50 && Position != 0)
		CloseAll("15:50 Close");
	}
}

