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
/// Countertrend strategy based on the Open Source Forex oscillator.
/// </summary>
public class OsfCountertrendStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _volumePerPoint;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeStrengthIndex _rsi = null!;
	private int _cooldown;
	private decimal _longTarget;
	private decimal _shortTarget;

	/// <summary>
	/// RSI period used to approximate the original OSF oscillator.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Volume traded per RSI point away from equilibrium (50 level).
	/// </summary>
	public decimal VolumePerPoint
	{
		get => _volumePerPoint.Value;
		set => _volumePerPoint.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in instrument points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Number of finished candles to wait before a new signal can trigger.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="OsfCountertrendStrategy"/>.
	/// </summary>
	public OsfCountertrendStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
		.SetRange(2, 200)
		.SetCanOptimize(true)
		.SetDisplay("RSI Period", "RSI length used in oscillator", "General");

		_volumePerPoint = Param(nameof(VolumePerPoint), 0.01m)
		.SetRange(0.001m, 1m)
		.SetCanOptimize(true)
		.SetDisplay("Volume per Point", "Order volume per RSI point from 50", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 150m)
		.SetRange(0m, 1000m)
		.SetCanOptimize(true)
		.SetDisplay("Take Profit", "Distance to take profit in points", "Risk");

		_cooldownBars = Param(nameof(CooldownBars), 5)
		.SetRange(0, 50)
		.SetCanOptimize(true)
		.SetDisplay("Cooldown Bars", "Finished candles to wait after trading", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Data series for processing", "General");
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

		_cooldown = 0;
		_longTarget = 0m;
		_shortTarget = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_rsi = new RelativeStrengthIndex
		{
			Length = RsiPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_rsi, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!_rsi.IsFormed)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		// Track active positions for manual take-profit handling.
		if (Position > 0 && _longTarget > 0m && TakeProfitPoints > 0m)
		{
			if (candle.LowPrice <= _longTarget)
			{
				SellMarket(Math.Abs(Position));
				_longTarget = 0m;
			}
		}
		else if (Position < 0 && _shortTarget > 0m && TakeProfitPoints > 0m)
		{
			if (candle.HighPrice >= _shortTarget)
			{
				BuyMarket(Math.Abs(Position));
				_shortTarget = 0m;
			}
		}

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		var diff = rsiValue - 50m;
		if (diff == 0m)
		return;

		var absDiff = Math.Abs(diff);
		var volume = absDiff * VolumePerPoint;
		if (volume <= 0m)
		return;

		var step = Security?.PriceStep ?? 1m;

		if (diff > 0m && Position <= 0m)
		{
			// RSI above 50: countertrend short trade sized by oscillator distance.
			var volumeToSell = volume + Math.Max(0m, Position);
			if (volumeToSell <= 0m)
			return;

			SellMarket(volumeToSell);

			_shortTarget = TakeProfitPoints > 0m
			? candle.ClosePrice - step * TakeProfitPoints
			: 0m;
			_longTarget = 0m;
			_cooldown = CooldownBars;
		}
		else if (diff < 0m && Position >= 0m)
		{
			// RSI below 50: countertrend long trade sized by oscillator distance.
			var volumeToBuy = volume + Math.Max(0m, -Position);
			if (volumeToBuy <= 0m)
			return;

			BuyMarket(volumeToBuy);

			_longTarget = TakeProfitPoints > 0m
			? candle.ClosePrice + step * TakeProfitPoints
			: 0m;
			_shortTarget = 0m;
			_cooldown = CooldownBars;
		}
	}
}

