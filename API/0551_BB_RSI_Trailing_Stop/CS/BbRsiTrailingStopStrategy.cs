using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bollinger Bands and RSI strategy with conditional trailing stop.
/// </summary>
public class BbRsiTrailingStopStrategy : Strategy
{
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _trailOffsetPoints;
	private readonly StrategyParam<decimal> _trailStopPoints;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _trailingPrice;
	private bool _trailingActive;
	private int _cooldown;

	/// <summary>
	/// Bollinger Bands period.
	/// </summary>
	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	/// <summary>
	/// Bollinger Bands deviation multiplier.
	/// </summary>
	public decimal BollingerDeviation
	{
		get => _bollingerDeviation.Value;
		set => _bollingerDeviation.Value = value;
	}

	/// <summary>
	/// RSI calculation period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// RSI overbought level.
	/// </summary>
	public decimal RsiOverbought
	{
		get => _rsiOverbought.Value;
		set => _rsiOverbought.Value = value;
	}

	/// <summary>
	/// RSI oversold level.
	/// </summary>
	public decimal RsiOversold
	{
		get => _rsiOversold.Value;
		set => _rsiOversold.Value = value;
	}

	/// <summary>
	/// Initial stop loss in price points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Profit required to activate trailing stop.
	/// </summary>
	public decimal TrailOffsetPoints
	{
		get => _trailOffsetPoints.Value;
		set => _trailOffsetPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in price points.
	/// </summary>
	public decimal TrailStopPoints
	{
		get => _trailStopPoints.Value;
		set => _trailStopPoints.Value = value;
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
	/// Initializes strategy parameters.
	/// </summary>
	public BbRsiTrailingStopStrategy()
	{
		_bollingerPeriod = Param(nameof(BollingerPeriod), 40)
			.SetDisplay("Bollinger Period", "Period for Bollinger Bands", "Indicators")
			
			.SetOptimize(10, 50, 5);

		_bollingerDeviation = Param(nameof(BollingerDeviation), 2.5m)
			.SetDisplay("Bollinger Deviation", "Deviation multiplier", "Indicators")
			
			.SetOptimize(1m, 3m, 0.5m);

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetDisplay("RSI Period", "RSI calculation period", "Indicators")
			
			.SetOptimize(7, 21, 7);

		_rsiOverbought = Param(nameof(RsiOverbought), 70m)
			.SetDisplay("RSI Overbought", "Overbought level", "Indicators")

			.SetOptimize(50m, 80m, 5m);

		_rsiOversold = Param(nameof(RsiOversold), 30m)
			.SetDisplay("RSI Oversold", "Oversold level", "Indicators")

			.SetOptimize(20m, 40m, 5m);

		_stopLossPoints = Param(nameof(StopLossPoints), 3000m)
			.SetDisplay("Stop Loss Points", "Initial stop loss in points", "Risk Management")

			.SetOptimize(20m, 100m, 10m);

		_trailOffsetPoints = Param(nameof(TrailOffsetPoints), 2000m)
			.SetDisplay("Trail Offset Points", "Profit to activate trailing stop", "Risk Management")

			.SetOptimize(50m, 150m, 10m);

		_trailStopPoints = Param(nameof(TrailStopPoints), 1500m)
			.SetDisplay("Trail Stop Points", "Trailing stop distance", "Risk Management")

			.SetOptimize(20m, 80m, 10m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
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
		_entryPrice = 0;
		_stopPrice = 0;
		_trailingPrice = 0;
		_trailingActive = false;
		_cooldown = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var bollinger = new BollingerBands
		{
		Length = BollingerPeriod,
		Width = BollingerDeviation
		};

		var rsi = new RelativeStrengthIndex
		{
		Length = RsiPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(bollinger, rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bollinger);
			DrawIndicator(area, rsi);
			DrawOwnTrades(area);
		}
	}

	private void ResetStops()
	{
		_entryPrice = 0;
		_stopPrice = 0;
		_trailingPrice = 0;
		_trailingActive = false;
		_cooldown = 100;
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bollingerValue, IIndicatorValue rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (bollingerValue is not BollingerBandsValue bb ||
			bb.UpBand is not decimal upper ||
			bb.LowBand is not decimal lower ||
			!rsiValue.IsFormed)
			return;

		var rsi = rsiValue.GetValue<decimal>();

		if (_cooldown > 0)
			_cooldown--;

		if (Position == 0 && _cooldown == 0)
		{
			if (candle.LowPrice < lower && rsi < RsiOversold)
			{
				BuyMarket();
				_entryPrice = candle.ClosePrice;
				_stopPrice = _entryPrice - StopLossPoints;
				_cooldown = 100;
			}
			else if (candle.HighPrice > upper && rsi > RsiOverbought)
			{
				SellMarket();
				_entryPrice = candle.ClosePrice;
				_stopPrice = _entryPrice + StopLossPoints;
				_cooldown = 100;
			}
		}
		else if (Position > 0)
		{
			if (!_trailingActive && candle.ClosePrice - _entryPrice >= TrailOffsetPoints)
			{
				_trailingActive = true;
				_trailingPrice = candle.ClosePrice - TrailStopPoints;
			}

			if (_trailingActive)
			{
				var newLevel = candle.ClosePrice - TrailStopPoints;
				if (newLevel > _trailingPrice)
					_trailingPrice = newLevel;
			}

			// Exit on stop or trailing stop
			if (candle.LowPrice <= _stopPrice || (_trailingActive && candle.LowPrice <= _trailingPrice))
			{
				SellMarket();
				ResetStops();
			}
		}
		else
		{
			if (!_trailingActive && _entryPrice - candle.ClosePrice >= TrailOffsetPoints)
			{
				_trailingActive = true;
				_trailingPrice = candle.ClosePrice + TrailStopPoints;
			}

			if (_trailingActive)
			{
				var newLevel = candle.ClosePrice + TrailStopPoints;
				if (newLevel < _trailingPrice || _trailingPrice == 0)
					_trailingPrice = newLevel;
			}

			// Exit on stop or trailing stop
			if (candle.HighPrice >= _stopPrice || (_trailingActive && candle.HighPrice >= _trailingPrice))
			{
				BuyMarket();
				ResetStops();
			}
		}
	}
}