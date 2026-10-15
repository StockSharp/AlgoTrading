using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// NQ Phantom Scalper Pro strategy based on VWAP bands with optional volume and trend filters.
/// </summary>
public class NqPhantomScalperProStrategy : Strategy
{
	private readonly StrategyParam<decimal> _band1Mult;
	private readonly StrategyParam<decimal> _atrStopMult;
	private readonly StrategyParam<bool> _useTrendFilter;
	private readonly StrategyParam<int> _trendLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal _stopPrice;
	private DateTimeOffset _lastSignal = DateTimeOffset.MinValue;

	public decimal Band1Mult
	{
		get => _band1Mult.Value;
		set => _band1Mult.Value = value;
	}

	public decimal AtrStopMult
	{
		get => _atrStopMult.Value;
		set => _atrStopMult.Value = value;
	}

	public bool UseTrendFilter
	{
		get => _useTrendFilter.Value;
		set => _useTrendFilter.Value = value;
	}

	public int TrendLength
	{
		get => _trendLength.Value;
		set => _trendLength.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public NqPhantomScalperProStrategy()
	{
		_band1Mult = Param(nameof(Band1Mult), 1.0m)
			.SetGreaterThanZero();

		_atrStopMult = Param(nameof(AtrStopMult), 1.0m)
			.SetGreaterThanZero();

		_useTrendFilter = Param(nameof(UseTrendFilter), false);

		_trendLength = Param(nameof(TrendLength), 50)
			.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_entryPrice = 0m;
		_stopPrice = 0m;
		_lastSignal = DateTimeOffset.MinValue;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_entryPrice = 0m;
		_stopPrice = 0m;
		_lastSignal = DateTimeOffset.MinValue;

		var vwap = new VolumeWeightedMovingAverage();
		var atr = new AverageTrueRange { Length = 14 };
		var std = new StandardDeviation { Length = 20 };
		var trendEma = new ExponentialMovingAverage { Length = TrendLength };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.BindEx(vwap, atr, std, trendEma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, vwap);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue vwapValue, IIndicatorValue atrValue, IIndicatorValue stdValue, IIndicatorValue trendValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!vwapValue.IsFinal || !vwapValue.IsFormed || !atrValue.IsFormed || !stdValue.IsFormed)
			return;

		var vwapVal = vwapValue.ToDecimal();
		var atr = atrValue.ToDecimal();
		var stdVal = stdValue.ToDecimal();
		var trendVal = trendValue.ToDecimal();

		if (atr <= 0 || stdVal <= 0)
			return;

		var upper1 = vwapVal + stdVal * Band1Mult;
		var lower1 = vwapVal - stdVal * Band1Mult;

		var trendOkLong = !UseTrendFilter || candle.ClosePrice > trendVal;
		var trendOkShort = !UseTrendFilter || candle.ClosePrice < trendVal;

		var cooldown = TimeSpan.FromMinutes(360);

		if (candle.OpenTime - _lastSignal < cooldown)
			return;

		// Exit logic - only on stop loss
		if (Position > 0 && _stopPrice > 0 && candle.ClosePrice <= _stopPrice)
		{
			SellMarket();
			_stopPrice = 0m;
			_lastSignal = candle.OpenTime;
			return;
		}

		if (Position < 0 && _stopPrice > 0 && candle.ClosePrice >= _stopPrice)
		{
			BuyMarket();
			_stopPrice = 0m;
			_lastSignal = candle.OpenTime;
			return;
		}

		// Entry logic
		if (Position <= 0 && candle.ClosePrice > upper1 && trendOkLong)
		{
			BuyMarket();
			_entryPrice = candle.ClosePrice;
			_stopPrice = _entryPrice - atr * AtrStopMult * 3;
			_lastSignal = candle.OpenTime;
		}
		else if (Position >= 0 && candle.ClosePrice < lower1 && trendOkShort)
		{
			SellMarket();
			_entryPrice = candle.ClosePrice;
			_stopPrice = _entryPrice + atr * AtrStopMult * 3;
			_lastSignal = candle.OpenTime;
		}
	}
}
