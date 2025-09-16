using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy combining EMA and ADX for trend confirmation.
/// </summary>
public class SimpleMaAdxEaStrategy : Strategy
{
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _emaPrev1;
	private decimal _emaPrev2;
	private decimal _prevClose;
	private bool _isInitialized;

	/// <summary>
	/// ADX period.
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
	/// Minimum ADX value to allow trading.
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
	/// Order volume.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
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
		_adxPeriod = Param(nameof(AdxPeriod), 8)
			.SetDisplay("ADX Period", "Period for ADX calculation", "Indicators")
			.SetCanOptimize(true);

		_maPeriod = Param(nameof(MaPeriod), 8)
			.SetDisplay("MA Period", "EMA calculation period", "Indicators")
			.SetCanOptimize(true);

		_adxThreshold = Param(nameof(AdxThreshold), 22m)
			.SetDisplay("ADX Threshold", "Minimum ADX level", "Indicators")
			.SetCanOptimize(true);

		_stopLoss = Param(nameof(StopLoss), 30m)
			.SetDisplay("Stop Loss", "Stop loss in price units", "Risk Management")
			.SetCanOptimize(true);

		_takeProfit = Param(nameof(TakeProfit), 100m)
			.SetDisplay("Take Profit", "Take profit in price units", "Risk Management")
			.SetCanOptimize(true);

		_volume = Param(nameof(Volume), 0.1m)
			.SetDisplay("Volume", "Order volume", "General")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var ema = new ExponentialMovingAverage { Length = MaPeriod };
		var adx = new AverageDirectionalIndex { Length = AdxPeriod };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.BindEx(adx, ema, ProcessCandle)
			.Start();

		StartProtection(
			stopLoss: new Unit(StopLoss, UnitTypes.Absolute),
			takeProfit: new Unit(TakeProfit, UnitTypes.Absolute),
			useMarketOrders: true
		);
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue adxValue, IIndicatorValue emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!adxValue.IsFinal || !emaValue.IsFinal)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var adx = (AverageDirectionalIndexValue)adxValue;
		var adxMain = adx.MovingAverage;
		var plusDi = adx.Dx.Plus;
		var minusDi = adx.Dx.Minus;

		var ema = emaValue.GetValue<decimal>();

		if (!_isInitialized)
		{
			_emaPrev1 = ema;
			_prevClose = candle.ClosePrice;
			_isInitialized = true;
			return;
		}

		var emaPrev1 = _emaPrev1;
		var emaPrev2 = _emaPrev2;
		var prevClose = _prevClose;

		_emaPrev2 = emaPrev1;
		_emaPrev1 = ema;
		_prevClose = candle.ClosePrice;

		var buyCond1 = ema > emaPrev1 && emaPrev1 > emaPrev2;
		var buyCond2 = prevClose > emaPrev1;
		var buyCond3 = adxMain > AdxThreshold;
		var buyCond4 = plusDi > minusDi;

		if (buyCond1 && buyCond2 && buyCond3 && buyCond4 && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
		}

		var sellCond1 = ema < emaPrev1 && emaPrev1 < emaPrev2;
		var sellCond2 = prevClose < emaPrev1;
		var sellCond3 = adxMain > AdxThreshold;
		var sellCond4 = plusDi < minusDi;

		if (sellCond1 && sellCond2 && sellCond3 && sellCond4 && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
		}
	}
}

