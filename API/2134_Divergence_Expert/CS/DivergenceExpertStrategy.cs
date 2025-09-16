namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy trading RSI price divergences.
/// </summary>
public class DivergenceExpertStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DateTimeOffset> _startDate;
	private readonly StrategyParam<DateTimeOffset> _endDate;

	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _lastPriceHigh;
	private decimal _lastPriceLow;
	private decimal _lastRsiHigh;
	private decimal _lastRsiLow;

	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public DateTimeOffset StartDate
	{
		get => _startDate.Value;
		set => _startDate.Value = value;
	}

	public DateTimeOffset EndDate
	{
		get => _endDate.Value;
		set => _endDate.Value = value;
	}

	public DivergenceExpertStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetDisplay("RSI Period", "RSI calculation period", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 5);

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetDisplay("Stop Loss (%)", "Max risk per trade in percent", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_startDate = Param(nameof(StartDate), new DateTimeOffset(new DateTime(2017, 1, 1), TimeSpan.Zero))
			.SetDisplay("Start Date", "Backtest start date", "General");

		_endDate = Param(nameof(EndDate), new DateTimeOffset(new DateTime(2024, 7, 1), TimeSpan.Zero))
			.SetDisplay("End Date", "Backtest end date", "General");
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

		_entryPrice = default;
		_stopPrice = default;
		_lastPriceHigh = default;
		_lastPriceLow = default;
		_lastRsiHigh = default;
		_lastRsiLow = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var rsi = new RelativeStrengthIndex
		{
			Length = RsiPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var time = candle.OpenTime;
		var inRange = time >= StartDate && time <= EndDate;

		if (!inRange)
		{
			if (Position != 0)
				ClosePosition();

			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;

		// Track new highs for bearish divergence detection
		if (candle.HighPrice > _lastPriceHigh)
		{
			if (_lastPriceHigh != 0m && rsi < _lastRsiHigh && Position >= 0)
			{
				if (Position > 0)
					ClosePosition();

				SellMarket();
				_entryPrice = close;
				_stopPrice = _entryPrice * (1m + StopLossPercent / 100m);
			}

			_lastPriceHigh = candle.HighPrice;
			_lastRsiHigh = rsi;
		}

		// Track new lows for bullish divergence detection
		if (_lastPriceLow == 0m || candle.LowPrice < _lastPriceLow)
		{
			if (_lastPriceLow != 0m && rsi > _lastRsiLow && Position <= 0)
			{
				if (Position < 0)
					ClosePosition();

				BuyMarket();
				_entryPrice = close;
				_stopPrice = _entryPrice * (1m - StopLossPercent / 100m);
			}

			_lastPriceLow = candle.LowPrice;
			_lastRsiLow = rsi;
		}

		if (Position > 0 && candle.LowPrice <= _stopPrice)
			ClosePosition();
		else if (Position < 0 && candle.HighPrice >= _stopPrice)
			ClosePosition();
	}
}

