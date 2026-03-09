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
/// Breakout strategy converted from the Ambush MQL5 expert.
/// Enters on breakouts above/below previous candle range with trailing stop management.
/// </summary>
public class AmbushStrategy : Strategy
{
	private readonly StrategyParam<decimal> _indentationPoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<decimal> _trailingStepPoints;
	private readonly StrategyParam<decimal> _equityTakeProfit;
	private readonly StrategyParam<decimal> _equityStopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private ICandleMessage _previousCandle;
	private decimal _entryPrice;
	private decimal? _stopPrice;
	private decimal _priceStep;

	/// <summary>
	/// Distance from the market price for breakout detection, in points.
	/// </summary>
	public decimal IndentationPoints
	{
		get => _indentationPoints.Value;
		set => _indentationPoints.Value = value;
	}

	/// <summary>
	/// Trailing distance for stop orders, in points.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Trailing step added to the base trailing distance, in points.
	/// </summary>
	public decimal TrailingStepPoints
	{
		get => _trailingStepPoints.Value;
		set => _trailingStepPoints.Value = value;
	}

	/// <summary>
	/// Target equity profit that triggers position flattening.
	/// </summary>
	public decimal EquityTakeProfit
	{
		get => _equityTakeProfit.Value;
		set => _equityTakeProfit.Value = value;
	}

	/// <summary>
	/// Maximum equity drawdown allowed before flattening positions.
	/// </summary>
	public decimal EquityStopLoss
	{
		get => _equityStopLoss.Value;
		set => _equityStopLoss.Value = value;
	}

	/// <summary>
	/// Candle type used for breakout detection.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="AmbushStrategy"/> class.
	/// </summary>
	public AmbushStrategy()
	{
		_indentationPoints = Param(nameof(IndentationPoints), 10m)
			.SetNotNegative()
			.SetDisplay("Indentation (points)", "Distance from price for breakout", "Orders");

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 10m)
			.SetNotNegative()
			.SetDisplay("Trailing Stop (points)", "Base trailing distance", "Orders");

		_trailingStepPoints = Param(nameof(TrailingStepPoints), 1m)
			.SetNotNegative()
			.SetDisplay("Trailing Step (points)", "Additional trailing offset", "Orders");

		_equityTakeProfit = Param(nameof(EquityTakeProfit), 15m)
			.SetNotNegative()
			.SetDisplay("Equity Take Profit", "Flatten positions once this profit is reached", "Risk");

		_equityStopLoss = Param(nameof(EquityStopLoss), 5m)
			.SetNotNegative()
			.SetDisplay("Equity Stop Loss", "Flatten positions after this loss", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(6).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for breakout detection", "General");

		Volume = 1;
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

		_previousCandle = null;
		_entryPrice = 0m;
		_stopPrice = null;
		_priceStep = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_priceStep = Security?.PriceStep ?? 1m;
		if (_priceStep <= 0m) _priceStep = 1m;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Check equity targets.
		var pnl = PnL;
		if (EquityTakeProfit > 0m && pnl >= EquityTakeProfit)
		{
			FlattenPosition();
			_previousCandle = candle;
			return;
		}
		if (EquityStopLoss > 0m && pnl <= -EquityStopLoss)
		{
			FlattenPosition();
			_previousCandle = candle;
			return;
		}

		// Check trailing stop.
		if (Position > 0 && _stopPrice.HasValue && candle.LowPrice <= _stopPrice.Value)
		{
			SellMarket(Position);
			ResetTargets();
		}
		else if (Position < 0 && _stopPrice.HasValue && candle.HighPrice >= _stopPrice.Value)
		{
			BuyMarket(Math.Abs(Position));
			ResetTargets();
		}

		// Update trailing stop.
		UpdateTrailing(candle);

		// Entry logic - breakout above/below previous candle range.
		if (Position == 0 && _previousCandle != null)
		{
			var indentation = IndentationPoints * _priceStep;
			var buyLevel = _previousCandle.HighPrice + indentation;
			var sellLevel = _previousCandle.LowPrice - indentation;

			if (candle.HighPrice >= buyLevel)
			{
				BuyMarket(Volume);
				_entryPrice = candle.ClosePrice;
				var trailDist = (TrailingStopPoints + TrailingStepPoints) * _priceStep;
				_stopPrice = trailDist > 0m ? _entryPrice - trailDist : null;
			}
			else if (candle.LowPrice <= sellLevel)
			{
				SellMarket(Volume);
				_entryPrice = candle.ClosePrice;
				var trailDist = (TrailingStopPoints + TrailingStepPoints) * _priceStep;
				_stopPrice = trailDist > 0m ? _entryPrice + trailDist : null;
			}
		}

		_previousCandle = candle;
	}

	private void UpdateTrailing(ICandleMessage candle)
	{
		if (TrailingStopPoints <= 0m)
			return;

		var trailDist = (TrailingStopPoints + TrailingStepPoints) * _priceStep;
		if (trailDist <= 0m)
			return;

		if (Position > 0)
		{
			var newStop = candle.ClosePrice - trailDist;
			if (!_stopPrice.HasValue || newStop > _stopPrice.Value)
				_stopPrice = newStop;
		}
		else if (Position < 0)
		{
			var newStop = candle.ClosePrice + trailDist;
			if (!_stopPrice.HasValue || newStop < _stopPrice.Value)
				_stopPrice = newStop;
		}
	}

	private void FlattenPosition()
	{
		if (Position > 0)
			SellMarket(Position);
		else if (Position < 0)
			BuyMarket(Math.Abs(Position));
		ResetTargets();
	}

	private void ResetTargets()
	{
		_entryPrice = 0m;
		_stopPrice = null;
	}
}
