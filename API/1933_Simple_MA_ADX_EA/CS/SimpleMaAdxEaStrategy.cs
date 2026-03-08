using System;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy combining EMA direction with a slower trend filter.
/// </summary>
public class SimpleMaAdxEaStrategy : Strategy
{
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private readonly ExponentialMovingAverage _ema = new();
	private readonly ExponentialMovingAverage _trendMa = new();

	private decimal _emaPrev1;
	private decimal _emaPrev2;
	private decimal _trendPrev;
	private decimal _prevClose;
	private bool _isInitialized;
	private int _barsSinceTrade;

	/// <summary>
	/// Trend filter period.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// EMA period.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Minimum percentage distance between the fast and slow averages.
	/// </summary>
	public decimal AdxThreshold
	{
		get => _adxThreshold.Value;
		set => _adxThreshold.Value = value;
	}

	/// <summary>
	/// Stop loss in price units.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take profit in price units.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Bars to wait after a completed trade.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
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
	/// Initializes a new instance of the <see cref="SimpleMaAdxEaStrategy"/> class.
	/// </summary>
	public SimpleMaAdxEaStrategy()
	{
		_adxPeriod = Param(nameof(AdxPeriod), 21)
			.SetDisplay("Trend Period", "Period for trend confirmation", "Indicators");

		_maPeriod = Param(nameof(MaPeriod), 8)
			.SetDisplay("MA Period", "EMA calculation period", "Indicators");

		_adxThreshold = Param(nameof(AdxThreshold), 0.05m)
			.SetDisplay("Trend Threshold", "Minimum average distance in percent", "Indicators");

		_stopLoss = Param(nameof(StopLoss), 400m)
			.SetDisplay("Stop Loss", "Stop loss in price units", "Risk Management");

		_takeProfit = Param(nameof(TakeProfit), 1200m)
			.SetDisplay("Take Profit", "Take profit in price units", "Risk Management");

		_cooldownBars = Param(nameof(CooldownBars), 2)
			.SetDisplay("Cooldown Bars", "Bars to wait after a completed trade", "Risk Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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

		_ema.Reset();
		_trendMa.Reset();
		_emaPrev1 = 0m;
		_emaPrev2 = 0m;
		_trendPrev = 0m;
		_prevClose = 0m;
		_isInitialized = false;
		_barsSinceTrade = CooldownBars;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_ema.Length = MaPeriod;
		_trendMa.Length = AdxPeriod;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection(
			stopLoss: new Unit(StopLoss, UnitTypes.Absolute),
			takeProfit: new Unit(TakeProfit, UnitTypes.Absolute),
			useMarketOrders: true);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var ema = _ema.Process(new DecimalIndicatorValue(_ema, candle.ClosePrice, candle.OpenTime)).ToDecimal();
		var trendMa = _trendMa.Process(new DecimalIndicatorValue(_trendMa, candle.ClosePrice, candle.OpenTime)).ToDecimal();

		if (!_ema.IsFormed || !_trendMa.IsFormed || trendMa == 0m)
			return;

		if (_barsSinceTrade < CooldownBars)
			_barsSinceTrade++;

		if (!_isInitialized)
		{
			_emaPrev2 = ema;
			_emaPrev1 = ema;
			_trendPrev = trendMa;
			_prevClose = candle.ClosePrice;
			_isInitialized = true;
			return;
		}

		var distancePercent = Math.Abs(ema - trendMa) / trendMa * 100m;
		var buyCond1 = ema > _emaPrev1 && _emaPrev1 >= _emaPrev2;
		var buyCond2 = _prevClose > _emaPrev1 && candle.ClosePrice > trendMa;
		var buyCond3 = trendMa >= _trendPrev && distancePercent >= AdxThreshold;
		var sellCond1 = ema < _emaPrev1 && _emaPrev1 <= _emaPrev2;
		var sellCond2 = _prevClose < _emaPrev1 && candle.ClosePrice < trendMa;
		var sellCond3 = trendMa <= _trendPrev && distancePercent >= AdxThreshold;

		if (_barsSinceTrade >= CooldownBars)
		{
			if (buyCond1 && buyCond2 && buyCond3 && Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
				_barsSinceTrade = 0;
			}
			else if (sellCond1 && sellCond2 && sellCond3 && Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
				_barsSinceTrade = 0;
			}
		}

		_emaPrev2 = _emaPrev1;
		_emaPrev1 = ema;
		_trendPrev = trendMa;
		_prevClose = candle.ClosePrice;
	}
}
