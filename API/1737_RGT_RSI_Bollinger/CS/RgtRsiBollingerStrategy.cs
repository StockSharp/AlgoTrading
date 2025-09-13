using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSI with Bollinger Bands strategy.
/// Buys when RSI is oversold below the lower band and sells when RSI is overbought above the upper band.
/// Includes stop-loss and trailing stop management based on price steps.
/// </summary>
public class RgtRsiBollingerStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _rsiHigh;
	private readonly StrategyParam<int> _rsiLow;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _minProfitPips;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal _stopPrice;
	private bool _isLong;

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }

	/// <summary>
	/// Overbought RSI level.
	/// </summary>
	public int RsiHigh { get => _rsiHigh.Value; set => _rsiHigh.Value = value; }

	/// <summary>
	/// Oversold RSI level.
	/// </summary>
	public int RsiLow { get => _rsiLow.Value; set => _rsiLow.Value = value; }

	/// <summary>
	/// Initial stop-loss distance in pips.
	/// </summary>
	public int StopLossPips { get => _stopLossPips.Value; set => _stopLossPips.Value = value; }

	/// <summary>
	/// Trailing stop distance in pips.
	/// </summary>
	public int TrailingStopPips { get => _trailingStopPips.Value; set => _trailingStopPips.Value = value; }

	/// <summary>
	/// Minimum profit in pips before trailing activates.
	/// </summary>
	public int MinProfitPips { get => _minProfitPips.Value; set => _minProfitPips.Value = value; }

	/// <summary>
	/// Order volume.
	/// </summary>
	public decimal VolumeParam { get => _volume.Value; set => _volume.Value = value; }

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initialize strategy parameters.
	/// </summary>
	public RgtRsiBollingerStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 8)
			.SetDisplay("RSI Period", "RSI calculation period", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_rsiHigh = Param(nameof(RsiHigh), 90)
			.SetDisplay("RSI High", "Overbought RSI level", "Indicator");

		_rsiLow = Param(nameof(RsiLow), 10)
			.SetDisplay("RSI Low", "Oversold RSI level", "Indicator");

		_stopLossPips = Param(nameof(StopLossPips), 70)
			.SetDisplay("Stop Loss (pips)", "Initial stop-loss distance in pips", "Risk Management");

		_trailingStopPips = Param(nameof(TrailingStopPips), 35)
			.SetDisplay("Trailing Stop (pips)", "Trailing stop distance in pips", "Risk Management");

		_minProfitPips = Param(nameof(MinProfitPips), 30)
			.SetDisplay("Min Profit (pips)", "Minimum profit before trailing", "Risk Management");

		_volume = Param(nameof(Volume), 1m)
			.SetDisplay("Volume", "Order volume", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "Data");
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
		_entryPrice = 0;
		_stopPrice = 0;
		_isLong = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var bb = new BollingerBands { Length = 20, Width = 2m };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, bb, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, rsi);
			DrawIndicator(area, bb);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue, decimal middle, decimal upper, decimal lower)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var step = Security?.PriceStep ?? 1m;

		if (Position == 0)
		{
			if (rsiValue < RsiLow && candle.ClosePrice < lower)
			{
				BuyMarket(VolumeParam);
				_entryPrice = candle.ClosePrice;
				_stopPrice = _entryPrice - StopLossPips * step;
				_isLong = true;
			}
			else if (rsiValue > RsiHigh && candle.ClosePrice > upper)
			{
				SellMarket(VolumeParam);
				_entryPrice = candle.ClosePrice;
				_stopPrice = _entryPrice + StopLossPips * step;
				_isLong = false;
			}
		}
		else if (_isLong && Position > 0)
		{
			var profit = candle.ClosePrice - _entryPrice;
			var trigger = (TrailingStopPips + MinProfitPips) * step;
			if (profit > trigger)
			{
				var newStop = candle.ClosePrice - TrailingStopPips * step;
				if (newStop > _stopPrice)
				_stopPrice = newStop;
			}

			if (candle.LowPrice <= _stopPrice)
			SellMarket(Position);
		}
		else if (!_isLong && Position < 0)
		{
			var profit = _entryPrice - candle.ClosePrice;
			var trigger = (TrailingStopPips + MinProfitPips) * step;
			if (profit > trigger)
			{
				var newStop = candle.ClosePrice + TrailingStopPips * step;
				if (newStop < _stopPrice || _stopPrice == 0)
				_stopPrice = newStop;
			}

			if (candle.HighPrice >= _stopPrice)
			BuyMarket(Math.Abs(Position));
		}
	}
}
