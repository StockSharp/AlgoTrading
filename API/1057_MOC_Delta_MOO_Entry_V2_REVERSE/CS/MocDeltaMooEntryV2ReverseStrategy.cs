namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

public class MocDeltaMooEntryV2ReverseStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _tpTicks;
	private readonly StrategyParam<int> _slTicks;
	private readonly StrategyParam<decimal> _deltaThreshold;

	private SimpleMovingAverage _sma15;
	private SimpleMovingAverage _sma30;

	private decimal _dailyVolume;
	private decimal _afternoonBuyVol;
	private decimal _afternoonSellVol;
	private bool _afternoonTracking;
	private decimal? _savedMocDeltaPct;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int TpTicks
	{
		get => _tpTicks.Value;
		set => _tpTicks.Value = value;
	}

	public int SlTicks
	{
		get => _slTicks.Value;
		set => _slTicks.Value = value;
	}

	public decimal DeltaThreshold
	{
		get => _deltaThreshold.Value;
		set => _deltaThreshold.Value = value;
	}

	public MocDeltaMooEntryV2ReverseStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_tpTicks = Param(nameof(TpTicks), 20)
			.SetDisplay("Take Profit Ticks", "Take profit in ticks", "Risk")
			.SetGreaterThanZero();

		_slTicks = Param(nameof(SlTicks), 10)
			.SetDisplay("Stop Loss Ticks", "Stop loss in ticks", "Risk")
			.SetGreaterThanZero();

		_deltaThreshold = Param(nameof(DeltaThreshold), 2m)
			.SetDisplay("Delta % Threshold", "Delta percent threshold", "General")
			.SetRange(0.1m, 10m);
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();

		_dailyVolume = 0m;
		_afternoonBuyVol = 0m;
		_afternoonSellVol = 0m;
		_afternoonTracking = false;
		_savedMocDeltaPct = null;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_sma15 = new SimpleMovingAverage { Length = 15 };
		_sma30 = new SimpleMovingAverage { Length = 30 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		var openTime = candle.OpenTime;

		if (openTime.Hour == 8 && openTime.Minute == 30)
			_dailyVolume = 0m;

		_dailyVolume += candle.Volume;

		var inAfternoon = openTime.Hour == 14 && openTime.Minute >= 50 && openTime.Minute <= 55;

		if (inAfternoon)
		{
			_afternoonBuyVol = _afternoonTracking
				? _afternoonBuyVol + (candle.ClosePrice > candle.OpenPrice ? candle.Volume : 0m)
				: (candle.ClosePrice > candle.OpenPrice ? candle.Volume : 0m);

			_afternoonSellVol = _afternoonTracking
				? _afternoonSellVol + (candle.ClosePrice < candle.OpenPrice ? candle.Volume : 0m)
				: (candle.ClosePrice < candle.OpenPrice ? candle.Volume : 0m);

			_afternoonTracking = true;
		}

		if (openTime.Hour == 14 && openTime.Minute == 55 && _afternoonTracking)
		{
			var mocDelta = _afternoonBuyVol - _afternoonSellVol;
			_savedMocDeltaPct = _dailyVolume > 0m ? mocDelta / _dailyVolume * 100m : (decimal?)null;
			_afternoonBuyVol = 0m;
			_afternoonSellVol = 0m;
			_afternoonTracking = false;
		}

		if (candle.State != CandleStates.Finished)
			return;

		var sma15Value = _sma15.Process(new CandleIndicatorValue(candle, candle.OpenPrice));
		var sma30Value = _sma30.Process(new CandleIndicatorValue(candle, candle.OpenPrice));

		if (!sma15Value.IsFinal || !sma30Value.IsFinal)
			return;

		var sma15 = sma15Value.GetValue<decimal>();
		var sma30 = sma30Value.GetValue<decimal>();

		var is830 = openTime.Hour == 8 && openTime.Minute == 30;
		var bearishMoc = _savedMocDeltaPct is decimal pct1 && pct1 > DeltaThreshold;
		var bullishMoc = _savedMocDeltaPct is decimal pct2 && pct2 < -DeltaThreshold;

		var validLong = candle.OpenPrice > sma15 && candle.OpenPrice > sma30 && sma15 > sma30;
		var validShort = candle.OpenPrice < sma15 && candle.OpenPrice < sma30 && sma15 < sma30;

		var step = Security.MinPriceStep;

		if (is830 && bullishMoc && validLong && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			var entry = candle.ClosePrice;
			BuyMarket(volume);
			SellLimit(volume, entry + step * TpTicks);
			SellStop(volume, entry - step * SlTicks);
		}
		else if (is830 && bearishMoc && validShort && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			var entry = candle.ClosePrice;
			SellMarket(volume);
			BuyLimit(volume, entry - step * TpTicks);
			BuyStop(volume, entry + step * SlTicks);
		}

		if (openTime.Hour == 14 && openTime.Minute == 50 && Position != 0)
			CloseAll();
	}
}
