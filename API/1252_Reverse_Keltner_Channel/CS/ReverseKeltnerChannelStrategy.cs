using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Reverse Keltner Channel strategy with optional ADX filter.
/// Enters when price crosses back inside the channel and targets the opposite band.
/// </summary>
public class ReverseKeltnerChannelStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<decimal> _stopLossFactor;
	private readonly StrategyParam<int> _adxLength;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<bool> _useAdxFilter;
	private readonly StrategyParam<bool> _weakTrendOnly;
	private readonly StrategyParam<DataType> _candleType;

	private KeltnerChannels _keltner = null!;
	private AverageDirectionalIndex _adx = null!;

	private decimal _prevClose;
	private decimal _prevUpper;
	private decimal _prevLower;

	private decimal _longTarget;
	private decimal _longStop;
	private decimal _shortTarget;
	private decimal _shortStop;

	/// <summary>
	/// EMA period for channel calculation.
	/// </summary>
	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }

	/// <summary>
	/// ATR period for channel calculation.
	/// </summary>
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }

	/// <summary>
	/// ATR multiplier for channel width.
	/// </summary>
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }

	/// <summary>
	/// Fraction of channel width used for stop loss.
	/// </summary>
	public decimal StopLossFactor { get => _stopLossFactor.Value; set => _stopLossFactor.Value = value; }

	/// <summary>
	/// ADX period.
	/// </summary>
	public int AdxLength { get => _adxLength.Value; set => _adxLength.Value = value; }

	/// <summary>
	/// ADX threshold separating weak and strong trends.
	/// </summary>
	public decimal AdxThreshold { get => _adxThreshold.Value; set => _adxThreshold.Value = value; }

	/// <summary>
	/// Enable ADX filter.
	/// </summary>
	public bool UseAdxFilter { get => _useAdxFilter.Value; set => _useAdxFilter.Value = value; }

	/// <summary>
	/// Enter only when ADX indicates weak trend.
	/// </summary>
	public bool WeakTrendOnly { get => _weakTrendOnly.Value; set => _weakTrendOnly.Value = value; }

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of <see cref="ReverseKeltnerChannelStrategy"/>.
	/// </summary>
	public ReverseKeltnerChannelStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 20)
			.SetDisplay("EMA Period", "EMA length for Keltner Channel", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 50, 5);

		_atrPeriod = Param(nameof(AtrPeriod), 10)
			.SetDisplay("ATR Period", "ATR length for Keltner Channel", "Indicators");

		_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
			.SetDisplay("ATR Multiplier", "ATR multiplier for channel width", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(1m, 3m, 0.5m);

		_stopLossFactor = Param(nameof(StopLossFactor), 0.5m)
			.SetDisplay("Stop Loss Factor", "Fraction of channel width for stop loss", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 1m, 0.1m);

		_adxLength = Param(nameof(AdxLength), 14)
			.SetDisplay("ADX Length", "Period for Average Directional Index", "Indicators");

		_adxThreshold = Param(nameof(AdxThreshold), 25m)
			.SetDisplay("ADX Threshold", "Value separating weak and strong trends", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10m, 40m, 5m);

		_useAdxFilter = Param(nameof(UseAdxFilter), true)
			.SetDisplay("Use ADX Filter", "Enable ADX trend strength filter", "Filters");

		_weakTrendOnly = Param(nameof(WeakTrendOnly), true)
			.SetDisplay("Enter Only in Weak Trends", "If true trades occur only when ADX below threshold", "Filters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		_keltner = null!;
		_adx = null!;
		_prevClose = _prevUpper = _prevLower = default;
		_longTarget = _longStop = _shortTarget = _shortStop = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_keltner = new KeltnerChannels
		{
			Length = EmaPeriod,
			Multiplier = AtrMultiplier
		};

		_adx = new AverageDirectionalIndex { Length = AdxLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_keltner, _adx, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _keltner);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal middle, decimal upper, decimal lower, decimal adxValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading() || !_keltner.IsFormed || !_adx.IsFormed)
		{
			_prevClose = candle.ClosePrice;
			_prevUpper = upper;
			_prevLower = lower;
			return;
		}

		var crossedAboveLower = _prevClose < _prevLower && candle.ClosePrice > lower;
		var crossedBelowUpper = _prevClose > _prevUpper && candle.ClosePrice < upper;

		var channelWidth = upper - lower;
		var halfWidth = channelWidth * StopLossFactor;

		var adxFilterPassed = !UseAdxFilter || (WeakTrendOnly ? adxValue < AdxThreshold : adxValue >= AdxThreshold);

		if (Position <= 0 && crossedAboveLower && adxFilterPassed)
		{
			CancelActiveOrders();
			BuyMarket(Volume + Math.Abs(Position));
			_longTarget = upper;
			_longStop = candle.ClosePrice - halfWidth;
		}
		else if (Position >= 0 && crossedBelowUpper && adxFilterPassed)
		{
			CancelActiveOrders();
			SellMarket(Volume + Math.Abs(Position));
			_shortTarget = lower;
			_shortStop = candle.ClosePrice + halfWidth;
		}
		else if (Position > 0)
		{
			if (candle.ClosePrice >= _longTarget || candle.ClosePrice <= _longStop)
				SellMarket(Position);
		}
		else if (Position < 0)
		{
			if (candle.ClosePrice <= _shortTarget || candle.ClosePrice >= _shortStop)
				BuyMarket(Math.Abs(Position));
		}

		_prevClose = candle.ClosePrice;
		_prevUpper = upper;
		_prevLower = lower;
	}
}
