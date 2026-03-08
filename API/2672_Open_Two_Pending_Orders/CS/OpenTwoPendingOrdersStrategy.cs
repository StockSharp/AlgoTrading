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
/// Strategy that simulates placing both buy stop and sell stop orders around the current price.
/// It uses candle-based breakout detection and manages the resulting position
/// with fixed stop loss, take profit and optional trailing stop levels.
/// </summary>
public class OpenTwoPendingOrdersStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<decimal> _entryOffsetPoints;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _pendingBuyPrice;
	private decimal? _pendingSellPrice;
	private decimal? _entryPrice;
	private decimal? _stopLevel;
	private decimal? _takeLevel;
	private decimal _highestSinceEntry;
	private decimal _lowestSinceEntry;

	/// <summary>
	/// Stop loss distance expressed in price steps.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in price steps.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in price steps.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Distance in price steps used to place the pending entries away from the current price.
	/// </summary>
	public decimal EntryOffsetPoints
	{
		get => _entryOffsetPoints.Value;
		set => _entryOffsetPoints.Value = value;
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="OpenTwoPendingOrdersStrategy"/>.
	/// </summary>
	public OpenTwoPendingOrdersStrategy()
	{
		_stopLossPoints = Param(nameof(StopLossPoints), 5m)
			.SetDisplay("Stop Loss (steps)", "Stop loss distance in price steps", "Risk")
			.SetOptimize(20m, 300m, 20m);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 10m)
			.SetDisplay("Take Profit (steps)", "Take profit distance in price steps", "Risk")
			.SetOptimize(50m, 600m, 50m);

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 3m)
			.SetDisplay("Trailing Stop (steps)", "Trailing stop distance in price steps", "Risk")
			.SetOptimize(10m, 200m, 10m);

		_entryOffsetPoints = Param(nameof(EntryOffsetPoints), 2m)
			.SetDisplay("Entry Offset (steps)", "Offset from close for pending entries", "Execution")
			.SetOptimize(10m, 150m, 10m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

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

		var step = GetStep();

		// Manage existing position
		if (Position != 0 && _entryPrice.HasValue)
		{
			ManagePosition(candle, step);

			// If position was closed, reset and set up new pending entries
			if (Position == 0)
			{
				ResetState();
				SetupPendingEntries(candle.ClosePrice, step);
			}
			return;
		}

		// Check pending entries
		if (_pendingBuyPrice.HasValue && _pendingSellPrice.HasValue)
		{
			// Buy stop triggered: price went up to pending buy level
			if (candle.HighPrice >= _pendingBuyPrice.Value)
			{
				BuyMarket();
				var entryPrice = _pendingBuyPrice.Value;
				_pendingBuyPrice = null;
				_pendingSellPrice = null;
				InitializePositionLevels(true, entryPrice, step);
				return;
			}

			// Sell stop triggered: price went down to pending sell level
			if (candle.LowPrice <= _pendingSellPrice.Value)
			{
				SellMarket();
				var entryPrice = _pendingSellPrice.Value;
				_pendingBuyPrice = null;
				_pendingSellPrice = null;
				InitializePositionLevels(false, entryPrice, step);
				return;
			}
		}
		else
		{
			// No pending entries, set up new ones
			SetupPendingEntries(candle.ClosePrice, step);
		}
	}

	private void SetupPendingEntries(decimal currentPrice, decimal step)
	{
		var offset = EntryOffsetPoints * step;
		_pendingBuyPrice = currentPrice + offset;
		_pendingSellPrice = currentPrice - offset;
	}

	private void InitializePositionLevels(bool isLong, decimal entryPrice, decimal step)
	{
		_entryPrice = entryPrice;
		_highestSinceEntry = entryPrice;
		_lowestSinceEntry = entryPrice;

		_stopLevel = StopLossPoints > 0m
			? entryPrice + (isLong ? -StopLossPoints : StopLossPoints) * step
			: null;

		_takeLevel = TakeProfitPoints > 0m
			? entryPrice + (isLong ? TakeProfitPoints : -TakeProfitPoints) * step
			: null;
	}

	private void ManagePosition(ICandleMessage candle, decimal step)
	{
		if (Position > 0)
		{
			_highestSinceEntry = Math.Max(_highestSinceEntry, candle.HighPrice);

			if (_stopLevel.HasValue && candle.LowPrice <= _stopLevel.Value)
			{
				SellMarket();
				return;
			}

			if (_takeLevel.HasValue && candle.HighPrice >= _takeLevel.Value)
			{
				SellMarket();
				return;
			}

			UpdateTrailingStop(true, step);
		}
		else if (Position < 0)
		{
			_lowestSinceEntry = Math.Min(_lowestSinceEntry, candle.LowPrice);

			if (_stopLevel.HasValue && candle.HighPrice >= _stopLevel.Value)
			{
				BuyMarket();
				return;
			}

			if (_takeLevel.HasValue && candle.LowPrice <= _takeLevel.Value)
			{
				BuyMarket();
				return;
			}

			UpdateTrailingStop(false, step);
		}
	}

	private void UpdateTrailingStop(bool isLong, decimal step)
	{
		if (TrailingStopPoints <= 0m || _entryPrice == null)
			return;

		var trailingDistance = TrailingStopPoints * step;
		if (trailingDistance <= 0m)
			return;

		if (isLong)
		{
			if (_highestSinceEntry - _entryPrice.Value >= trailingDistance)
			{
				var desiredStop = _highestSinceEntry - trailingDistance;
				if (_stopLevel == null || desiredStop > _stopLevel.Value)
					_stopLevel = desiredStop;
			}
		}
		else
		{
			if (_entryPrice.Value - _lowestSinceEntry >= trailingDistance)
			{
				var desiredStop = _lowestSinceEntry + trailingDistance;
				if (_stopLevel == null || desiredStop < _stopLevel.Value)
					_stopLevel = desiredStop;
			}
		}
	}

	private void ResetState()
	{
		_pendingBuyPrice = null;
		_pendingSellPrice = null;
		_entryPrice = null;
		_stopLevel = null;
		_takeLevel = null;
		_highestSinceEntry = 0m;
		_lowestSinceEntry = 0m;
	}

	private decimal GetStep()
	{
		var step = Security?.PriceStep ?? 0m;
		return step > 0m ? step : 0.01m;
	}
}
