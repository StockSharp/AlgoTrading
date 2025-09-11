using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy using Heiken Ashi candles with Supertrend direction and ADX filter.
/// Long entry: bullish HA candle without lower wick, optional Supertrend uptrend and ADX confirmation.
/// Short entry: bearish HA candle without upper wick, optional Supertrend downtrend and ADX confirmation.
/// Exits on opposite signal or ATR trailing stop.
/// </summary>
public class HeikenAshiSupertrendAdxStrategy : Strategy
{
	private readonly StrategyParam<bool> _useSupertrend;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _supertrendMultiplier;
	private readonly StrategyParam<bool> _useAdxFilter;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<decimal> _trailAtrMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevHaOpen;
	private decimal _prevHaClose;
	private decimal? _trailStop;

	public HeikenAshiSupertrendAdxStrategy()
	{
		_useSupertrend = Param(nameof(UseSupertrend), true)
			.SetDisplay("Use Supertrend", "Use Supertrend for trade direction", "General");

		_atrPeriod = Param(nameof(AtrPeriod), 10)
			.SetDisplay("ATR Period", "ATR period for Supertrend", "Indicators");

		_supertrendMultiplier = Param(nameof(SupertrendMultiplier), 3m)
			.SetDisplay("Supertrend Multiplier", "Multiplier for Supertrend ATR", "Indicators");

		_useAdxFilter = Param(nameof(UseAdxFilter), false)
			.SetDisplay("Use ADX Filter", "Use ADX to confirm trend", "ADX");

		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetDisplay("ADX Period", "Period for ADX", "ADX");

		_adxThreshold = Param(nameof(AdxThreshold), 25m)
			.SetDisplay("ADX Threshold", "Minimum ADX to trade", "ADX");

		_trailAtrMultiplier = Param(nameof(TrailAtrMultiplier), 2m)
			.SetDisplay("ATR Trail Multiplier", "ATR multiplier for trailing stop", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}


	public bool UseSupertrend { get => _useSupertrend.Value; set => _useSupertrend.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal SupertrendMultiplier { get => _supertrendMultiplier.Value; set => _supertrendMultiplier.Value = value; }
	public bool UseAdxFilter { get => _useAdxFilter.Value; set => _useAdxFilter.Value = value; }
	public int AdxPeriod { get => _adxPeriod.Value; set => _adxPeriod.Value = value; }
	public decimal AdxThreshold { get => _adxThreshold.Value; set => _adxThreshold.Value = value; }
	public decimal TrailAtrMultiplier { get => _trailAtrMultiplier.Value; set => _trailAtrMultiplier.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevHaOpen = default;
		_prevHaClose = default;
		_trailStop = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var supertrend = new SuperTrend { Length = AtrPeriod, Multiplier = SupertrendMultiplier };
		var adx = new AverageDirectionalIndex { Length = AdxPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(supertrend, adx, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, supertrend);
			DrawIndicator(area, adx);
			DrawOwnTrades(area);
		}
	}


	private void ProcessCandle(ICandleMessage candle, IIndicatorValue supertrendValue, IIndicatorValue adxValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var supertrendTyped = (SuperTrendIndicatorValue)supertrendValue;
		var adxTyped = (AverageDirectionalIndexValue)adxValue;
		var atr = atrValue.ToDecimal();

		decimal haOpen, haClose, haHigh, haLow;
		if (_prevHaOpen == 0)
		{
			haOpen = (candle.OpenPrice + candle.ClosePrice) / 2m;
			haClose = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m;
		}
		else
		{
			haOpen = (_prevHaOpen + _prevHaClose) / 2m;
			haClose = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m;
		}
		haHigh = Math.Max(candle.HighPrice, Math.Max(haOpen, haClose));
		haLow = Math.Min(candle.LowPrice, Math.Min(haOpen, haClose));

		var isGreen = haClose > haOpen;
		var isRed = haClose < haOpen;

		var step = Security.PriceStep ?? 1m;
		var threshold = step * 5m;
		var noBottomWick = Math.Abs(Math.Min(haOpen, haClose) - haLow) <= threshold;
		var noTopWick = Math.Abs(haHigh - Math.Max(haOpen, haClose)) <= threshold;

		var isUptrend = candle.ClosePrice > supertrendTyped.Value;
		var isDowntrend = candle.ClosePrice < supertrendTyped.Value;

		var isAdxBullish = adxTyped.Dx.Plus > adxTyped.Dx.Minus && adxTyped.MovingAverage > AdxThreshold;
		var isAdxBearish = adxTyped.Dx.Minus > adxTyped.Dx.Plus && adxTyped.MovingAverage > AdxThreshold;

		if (Position > 0)
		{
			var stop = candle.ClosePrice - atr * TrailAtrMultiplier;
			_trailStop = _trailStop.HasValue ? Math.Max(_trailStop.Value, stop) : stop;
			if (candle.LowPrice <= _trailStop.Value)
			{
				SellMarket(Math.Abs(Position));
				_trailStop = null;
				LogInfo($"Exit long: trailing stop at {_trailStop}");
			}
		}
		else if (Position < 0)
		{
			var stop = candle.ClosePrice + atr * TrailAtrMultiplier;
			_trailStop = _trailStop.HasValue ? Math.Min(_trailStop.Value, stop) : stop;
			if (candle.HighPrice >= _trailStop.Value)
			{
				BuyMarket(Math.Abs(Position));
				_trailStop = null;
				LogInfo($"Exit short: trailing stop at {_trailStop}");
			}
		}

		if (Position == 0)
		{
			if (isGreen && noBottomWick && (!UseSupertrend || isUptrend) && (!UseAdxFilter || isAdxBullish))
			{
				BuyMarket(Volume);
				_trailStop = candle.ClosePrice - atr * TrailAtrMultiplier;
				LogInfo("Buy signal");
			}
			else if (isRed && noTopWick && (!UseSupertrend || isDowntrend) && (!UseAdxFilter || isAdxBearish))
			{
				SellMarket(Volume);
				_trailStop = candle.ClosePrice + atr * TrailAtrMultiplier;
				LogInfo("Sell signal");
			}
		}
		else if (Position > 0 && isRed && noTopWick)
		{
			SellMarket(Math.Abs(Position));
			_trailStop = null;
			LogInfo("Exit long signal");
		}
		else if (Position < 0 && isGreen && noBottomWick)
		{
			BuyMarket(Math.Abs(Position));
			_trailStop = null;
			LogInfo("Exit short signal");
		}

		_prevHaOpen = haOpen;
		_prevHaClose = haClose;
	}
}
