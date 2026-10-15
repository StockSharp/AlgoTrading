using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Nifty 50 5-minute breakout strategy.
/// A long opens when the close breaks above the highest high of the previous LookbackPeriod candles and above the upper Bollinger band
/// while DEMA is above the session VWAP; a short uses the lowest low, the lower band and DEMA below VWAP. An opposite signal reverses
/// the position, and a fixed stop of StopLossPoints price steps closes it otherwise.
/// </summary>
public class Nifty505mintStrategy : Strategy
{
	private readonly StrategyParam<int> _demaPeriod;
	private readonly StrategyParam<int> _bollingerLength;
	private readonly StrategyParam<decimal> _bollingerStdDev;
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _highest;
	private Lowest _lowest;
	private decimal? _prevHighest;
	private decimal? _prevLowest;
	private DateTime _vwapDate;
	private decimal _vwapPriceVolume;
	private decimal _vwapVolume;

	/// <summary>
	/// DEMA period.
	/// </summary>
	public int DemaPeriod
	{
		get => _demaPeriod.Value;
		set => _demaPeriod.Value = value;
	}

	/// <summary>
	/// Bollinger Bands period.
	/// </summary>
	public int BollingerLength
	{
		get => _bollingerLength.Value;
		set => _bollingerLength.Value = value;
	}

	/// <summary>
	/// Bollinger Bands standard deviation multiplier.
	/// </summary>
	public decimal BollingerStdDev
	{
		get => _bollingerStdDev.Value;
		set => _bollingerStdDev.Value = value;
	}

	/// <summary>
	/// Previous candles whose high or low has to be broken.
	/// </summary>
	public int LookbackPeriod
	{
		get => _lookbackPeriod.Value;
		set => _lookbackPeriod.Value = value;
	}

	/// <summary>
	/// Stop loss distance in price steps.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
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
	/// Constructor.
	/// </summary>
	public Nifty505mintStrategy()
	{
		_demaPeriod = Param(nameof(DemaPeriod), 6)
			.SetGreaterThanZero()
			.SetDisplay("DEMA Period", "DEMA period", "Indicators");

		_bollingerLength = Param(nameof(BollingerLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Length", "Bollinger Bands period", "Indicators");

		_bollingerStdDev = Param(nameof(BollingerStdDev), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger StdDev", "Bollinger Bands standard deviation multiplier", "Indicators");

		_lookbackPeriod = Param(nameof(LookbackPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Lookback Period", "Previous candles whose high or low has to be broken", "Signals");

		_stopLossPoints = Param(nameof(StopLossPoints), 25m)
			.SetNotNegative()
			.SetDisplay("Stop Loss Points", "Stop loss distance in price steps", "Risk");

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
		ResetState();
	}

	private void ResetState()
	{
		_highest = null;
		_lowest = null;
		_prevHighest = null;
		_prevLowest = null;
		_vwapDate = default;
		_vwapPriceVolume = 0m;
		_vwapVolume = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		_highest = new Highest { Length = LookbackPeriod };
		_lowest = new Lowest { Length = LookbackPeriod };

		var dema = new DoubleExponentialMovingAverage { Length = DemaPeriod };
		var bollinger = new BollingerBands { Length = BollingerLength, Width = BollingerStdDev };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(dema, bollinger, ProcessCandle)
			.Start();

		var step = Security?.PriceStep ?? 1m;
		StartProtection(new Unit(), StopLossPoints > 0 ? new Unit(StopLossPoints * step, UnitTypes.Absolute) : new Unit(), useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, dema);
			DrawIndicator(area, bollinger);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue demaValue, IIndicatorValue bollingerValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Session VWAP restarts every UTC day.
		var date = candle.OpenTime.Date;
		if (date != _vwapDate)
		{
			_vwapDate = date;
			_vwapPriceVolume = 0m;
			_vwapVolume = 0m;
		}

		var typical = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m;
		_vwapPriceVolume += typical * candle.TotalVolume;
		_vwapVolume += candle.TotalVolume;

		// The breakout levels are measured on the candles before this one.
		var prevHighest = _prevHighest;
		var prevLowest = _prevLowest;

		var highestValue = _highest.Process(candle.HighPrice, candle.ServerTime, true);
		var lowestValue = _lowest.Process(candle.LowPrice, candle.ServerTime, true);

		if (_highest.IsFormed && _lowest.IsFormed)
		{
			_prevHighest = highestValue.ToDecimal();
			_prevLowest = lowestValue.ToDecimal();
		}

		if (!demaValue.IsFormed || !bollingerValue.IsFormed || prevHighest is not decimal lastHigh || prevLowest is not decimal lastLow)
			return;

		if (bollingerValue is not IBollingerBandsValue { UpBand: decimal upper, LowBand: decimal lower })
			return;

		if (!IsFormedAndOnlineAndAllowTrading() || _vwapVolume <= 0)
			return;

		var close = candle.ClosePrice;
		var dema = demaValue.GetValue<decimal>();
		var vwap = _vwapPriceVolume / _vwapVolume;

		if (close > lastHigh && close > upper && dema > vwap && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (close < lastLow && close < lower && dema < vwap && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}
