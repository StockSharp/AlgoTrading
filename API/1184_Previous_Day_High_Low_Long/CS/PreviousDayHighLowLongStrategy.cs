using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Goes long when price breaks the previous day's high or low with ADX confirmation.
/// </summary>
public class PreviousDayHighLowLongStrategy : Strategy
{
	private readonly StrategyParam<decimal> _maxProfit;
	private readonly StrategyParam<decimal> _maxStopLoss;
	private readonly StrategyParam<int> _adxLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevLwadx;
	private decimal _entryPrice;
	private decimal _checkpoint;
	private decimal _stop;
	private decimal _prevClose;

	private decimal _prevDayHigh;
	private decimal _prevDayLow;

	/// <summary>
	/// Maximum profit target.
	/// </summary>
	public decimal MaxProfit { get => _maxProfit.Value; set => _maxProfit.Value = value; }

	/// <summary>
	/// Maximum stop loss.
	/// </summary>
	public decimal MaxStopLoss { get => _maxStopLoss.Value; set => _maxStopLoss.Value = value; }

	/// <summary>
	/// ADX length.
	/// </summary>
	public int AdxLength { get => _adxLength.Value; set => _adxLength.Value = value; }

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="PreviousDayHighLowLongStrategy"/> class.
	/// </summary>
	public PreviousDayHighLowLongStrategy()
	{
		_maxProfit = Param(nameof(MaxProfit), 150m)
			.SetDisplay("Max Profit", "Maximum profit in absolute currency", "Risk")
			.SetCanOptimize(true);

		_maxStopLoss = Param(nameof(MaxStopLoss), 15m)
			.SetDisplay("Max Stop Loss", "Maximum stop loss in absolute currency", "Risk")
			.SetCanOptimize(true);

		_adxLength = Param(nameof(AdxLength), 11)
			.SetDisplay("ADX Length", "Period for ADX indicator", "Indicators")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (Security, TimeSpan.FromDays(1).TimeFrame())];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevLwadx = default;
		_entryPrice = default;
		_checkpoint = default;
		_stop = default;
		_prevClose = default;
		_prevDayHigh = default;
		_prevDayLow = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		StartProtection();

		var adx = new AverageDirectionalIndex { Length = AdxLength };

		var candleSub = SubscribeCandles(CandleType);
		candleSub
			.BindEx(adx, ProcessCandle)
			.Start();

		var dailySub = SubscribeCandles(TimeSpan.FromDays(1).TimeFrame());
		dailySub
			.Bind(ProcessDaily)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, candleSub);
			DrawIndicator(area, adx);
			DrawOwnTrades(area);
		}
	}

	private void ProcessDaily(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_prevDayHigh = candle.HighPrice;
		_prevDayLow = candle.LowPrice;
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue adxValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var time = candle.OpenTime.TimeOfDay;
		var openTime = new TimeSpan(9, 30, 0);
		var closeTime = new TimeSpan(15, 10, 0);

		var adx = (AverageDirectionalIndexValue)adxValue;
		if (adx.MovingAverage is not decimal adxMa)
			return;

		var plus = adx.Dx.Plus;
		var minus = adx.Dx.Minus;

		var lwadx = (adxMa - 15m) * 2.5m;
		var strengthUp = lwadx > 11m && plus > minus && lwadx > _prevLwadx;

		var crossHigh = _prevClose <= _prevDayHigh && candle.ClosePrice > _prevDayHigh;
		var crossLow = _prevClose <= _prevDayLow && candle.ClosePrice > _prevDayLow;
		var longCondition = (crossHigh || crossLow) && strengthUp && time >= openTime && time <= closeTime;

		if (Position == 0)
		{
			if (longCondition)
			{
				BuyMarket();
				_entryPrice = candle.ClosePrice;
				_checkpoint = _entryPrice;
				_stop = Math.Min(MaxStopLoss, MaxProfit * 0.4m);
			}
		}
		else
		{
			var t1 = MaxProfit * 0.3m;
			var t2 = MaxProfit * 0.5m;
			var t3 = MaxProfit * 0.8m;

			if (candle.HighPrice >= _entryPrice + t1 && _checkpoint < _entryPrice + t1)
			{
				_checkpoint = _entryPrice + t1;
				_stop = _checkpoint - _entryPrice;
			}
			if (candle.HighPrice >= _entryPrice + t2 && _checkpoint < _entryPrice + t2)
			{
				_checkpoint = _entryPrice + t2;
				_stop = _checkpoint - _entryPrice - t1;
			}
			if (candle.HighPrice >= _entryPrice + t3 && _checkpoint < _entryPrice + t3)
			{
				_checkpoint = _entryPrice + MaxProfit;
				_stop = _checkpoint - _entryPrice - t2;
			}

			var stopPrice = _checkpoint - _stop;
			var takePrice = _entryPrice + MaxProfit;

			if (candle.LowPrice <= stopPrice || candle.HighPrice >= takePrice || time > closeTime)
			{
				ClosePosition();
				_entryPrice = 0m;
				_checkpoint = 0m;
				_stop = 0m;
			}
		}

		_prevClose = candle.ClosePrice;
		_prevLwadx = lwadx;
	}
}
