using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on VWAP with EMA trend filter and ATR risk management.
/// </summary>
public class VwapProV21Strategy : Strategy
{
	private readonly StrategyParam<int> _emaFastPeriod;
	private readonly StrategyParam<int> _emaSlowPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _tpMultiplier;
	private readonly StrategyParam<decimal> _slMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _trendValue;
	private decimal _takeProfit;
	private decimal _stopLoss;

	/// <summary>
	/// Fast EMA period.
	/// </summary>
	public int EmaFastPeriod
	{
		get => _emaFastPeriod.Value;
		set => _emaFastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period.
	/// </summary>
	public int EmaSlowPeriod
	{
		get => _emaSlowPeriod.Value;
		set => _emaSlowPeriod.Value = value;
	}

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier for take profit.
	/// </summary>
	public decimal TakeProfitAtrMultiplier
	{
		get => _tpMultiplier.Value;
		set => _tpMultiplier.Value = value;
	}

	/// <summary>
	/// ATR multiplier for stop loss.
	/// </summary>
	public decimal StopLossAtrMultiplier
	{
		get => _slMultiplier.Value;
		set => _slMultiplier.Value = value;
	}

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="VwapProV21Strategy"/>.
	/// </summary>
	public VwapProV21Strategy()
	{
		_emaFastPeriod = Param(nameof(EmaFastPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("EMA Fast", "Fast EMA period", "Indicators");

		_emaSlowPeriod = Param(nameof(EmaSlowPeriod), 21)
			.SetGreaterThanZero()
			.SetDisplay("EMA Slow", "Slow EMA period", "Indicators");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR period", "Indicators");

		_tpMultiplier = Param(nameof(TakeProfitAtrMultiplier), 0.7m)
			.SetGreaterOrEqual(0m)
			.SetDisplay("TP ATR Multiplier", "ATR multiplier for take profit", "Risk Management");

		_slMultiplier = Param(nameof(StopLossAtrMultiplier), 1.4m)
			.SetGreaterOrEqual(0m)
			.SetDisplay("SL ATR Multiplier", "ATR multiplier for stop loss", "Risk Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
		yield return (Security, TimeSpan.FromHours(1).TimeFrame());
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_trendValue = 0m;
		_takeProfit = 0m;
		_stopLoss = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var emaFast = new ExponentialMovingAverage { Length = EmaFastPeriod };
		var emaSlow = new ExponentialMovingAverage { Length = EmaSlowPeriod };
		var vwap = new VolumeWeightedMovingAverage();
		var atr = new AverageTrueRange { Length = AtrPeriod };

		var trendEma = new ExponentialMovingAverage { Length = 50 };
		var trendSubscription = SubscribeCandles(TimeSpan.FromHours(1).TimeFrame());
		trendSubscription
			.Bind(trendEma, ProcessTrend)
			.Start();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(emaFast, emaSlow, vwap, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, emaFast);
			DrawIndicator(area, emaSlow);
			DrawIndicator(area, vwap);
			DrawIndicator(area, atr);
			DrawOwnTrades(area);
		}
	}

	private void ProcessTrend(ICandleMessage candle, decimal trendEma)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_trendValue = trendEma;
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaFast, decimal emaSlow, decimal vwap, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var trendOk = 0;
		if (candle.ClosePrice > _trendValue)
			trendOk = 1;
		else if (candle.ClosePrice < _trendValue)
			trendOk = -1;

		var longCond = candle.ClosePrice > emaFast && emaFast > emaSlow && candle.ClosePrice > vwap && trendOk == 1;
		var shortCond = candle.ClosePrice < emaFast && emaFast < emaSlow && candle.ClosePrice < vwap && trendOk == -1;

		if (longCond && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			_takeProfit = candle.ClosePrice + atr * TakeProfitAtrMultiplier;
			_stopLoss = candle.ClosePrice - atr * StopLossAtrMultiplier;
		}
		else if (shortCond && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			_takeProfit = candle.ClosePrice - atr * TakeProfitAtrMultiplier;
			_stopLoss = candle.ClosePrice + atr * StopLossAtrMultiplier;
		}

		if (Position > 0)
		{
			if (candle.HighPrice >= _takeProfit || candle.LowPrice <= _stopLoss)
				SellMarket(Math.Abs(Position));
		}
		else if (Position < 0)
		{
			if (candle.LowPrice <= _takeProfit || candle.HighPrice >= _stopLoss)
				BuyMarket(Math.Abs(Position));
		}
	}
}
