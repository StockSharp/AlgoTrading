using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MOC Delta MOO Entry strategy.
/// Uses previous day's afternoon volume delta to trade next morning.
/// </summary>
public class MocDeltaMooEntryStrategy : Strategy
{
	private readonly StrategyParam<int> _takeProfitTicks;
	private readonly StrategyParam<int> _stopLossTicks;
	private readonly StrategyParam<decimal> _deltaThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _dailyVolume;
	private decimal? _afternoonHigh;
	private decimal? _afternoonLow;
	private decimal _afternoonBuyVol;
	private decimal _afternoonSellVol;
	private bool _afternoonTracking;
	private decimal? _mocDeltaPct;

	private SMA _sma15 = null!;
	private SMA _sma30 = null!;
	/// <summary>
	/// Take profit in ticks.
	/// </summary>
	public int TakeProfitTicks
	{
		get => _takeProfitTicks.Value;
		set => _takeProfitTicks.Value = value;
	}

	/// <summary>
	/// Stop loss in ticks.
	/// </summary>
	public int StopLossTicks
	{
		get => _stopLossTicks.Value;
		set => _stopLossTicks.Value = value;
	}

	/// <summary>
	/// Delta percentage threshold.
	/// </summary>
	public decimal DeltaThreshold
	{
		get => _deltaThreshold.Value;
		set => _deltaThreshold.Value = value;
	}

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	/// <summary>
	/// Constructor.
	/// </summary>
	public MocDeltaMooEntryStrategy()
	{
		_takeProfitTicks = Param(nameof(TakeProfitTicks), 20)
			.SetDisplay("Take Profit (Ticks)", "Take profit in ticks", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(10, 40, 5);

		_stopLossTicks = Param(nameof(StopLossTicks), 10)
			.SetDisplay("Stop Loss (Ticks)", "Stop loss in ticks", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 5);

		_deltaThreshold = Param(nameof(DeltaThreshold), 2m)
			.SetDisplay("Delta % Threshold", "MOC delta percentage threshold", "General")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 5m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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

		_dailyVolume = 0m;
		_afternoonHigh = null;
		_afternoonLow = null;
		_afternoonBuyVol = 0m;
		_afternoonSellVol = 0m;
		_afternoonTracking = false;
		_mocDeltaPct = null;
	}
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var tickSize = Security.PriceStep ?? 1m;
		StartProtection(
			takeProfit: new Unit(tickSize * TakeProfitTicks, UnitTypes.Absolute),
			stopLoss: new Unit(tickSize * StopLossTicks, UnitTypes.Absolute),
			isStopTrailing: false,
			useMarketOrders: true);

		_sma15 = new SMA { Length = 15 };
		_sma30 = new SMA { Length = 30 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _sma15);
			DrawIndicator(area, _sma30);
			DrawOwnTrades(area);
		}
	}
	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var time = candle.OpenTime;
		var hour = time.Hour;
		var minute = time.Minute;
		var open = candle.OpenPrice;

		if (hour == 17 && minute == 0)
			_dailyVolume = 0m;

		_dailyVolume += candle.TotalVolume;

		var inAfternoonRange = hour == 14 && minute >= 50 && minute <= 55;
		var endAfternoonBar = hour == 14 && minute == 55;

		if (inAfternoonRange)
		{
			var bull = candle.ClosePrice > open;
			var bear = candle.ClosePrice < open;

			_afternoonHigh = _afternoonTracking ? Math.Max(_afternoonHigh ?? candle.HighPrice, candle.HighPrice) : candle.HighPrice;
			_afternoonLow = _afternoonTracking ? Math.Min(_afternoonLow ?? candle.LowPrice, candle.LowPrice) : candle.LowPrice;
			_afternoonBuyVol = _afternoonTracking ? _afternoonBuyVol + (bull ? candle.TotalVolume : 0m) : (bull ? candle.TotalVolume : 0m);
			_afternoonSellVol = _afternoonTracking ? _afternoonSellVol + (bear ? candle.TotalVolume : 0m) : (bear ? candle.TotalVolume : 0m);
			_afternoonTracking = true;
		}

		if (endAfternoonBar)
		{
			var delta = _afternoonBuyVol - _afternoonSellVol;
			_mocDeltaPct = _dailyVolume > 0m ? (delta / _dailyVolume) * 100m : null;

			_afternoonHigh = null;
			_afternoonLow = null;
			_afternoonBuyVol = 0m;
			_afternoonSellVol = 0m;
			_afternoonTracking = false;
		}

		var sma15Value = _sma15.Process(open).ToDecimal();
		var sma30Value = _sma30.Process(open).ToDecimal();

		if (!_sma15.IsFormed || !_sma30.IsFormed)
			return;

		var is830 = hour == 8 && minute == 30;
		var bullishMoc = _mocDeltaPct is decimal moc && moc > DeltaThreshold;
		var bearishMoc = _mocDeltaPct is decimal mocNeg && mocNeg < -DeltaThreshold;
		var validLong = open > sma15Value && open > sma30Value;
		var validShort = open < sma15Value && open < sma30Value;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (is830 && bullishMoc && validLong && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (is830 && bearishMoc && validShort && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}

		var closeTime = hour == 14 && minute == 50;
		if (closeTime && Position != 0)
			CloseAll();
	}
}
