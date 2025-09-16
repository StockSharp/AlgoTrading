using System;
using System.Collections.Generic;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that combines Renko trend detection with RSI pullbacks.
/// Uses a three-bar breakout structure for entries and attaches stop-loss and take-profit levels.
/// </summary>
public class RenkoLineBreakVsRsiStrategy : Strategy
{
	private enum TrendState
	{
		None,
		Up,
		Down,
		ToUp,
		ToDown
	}

	private readonly StrategyParam<decimal> _boxSize;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiShift;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _indentFromHighLow;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeStrengthIndex _rsi;
	private DataType _renkoType;

	private TrendState _trendState = TrendState.None;
	private bool _renkoHasPrev;
	private bool _renkoPrevBull;

	private decimal _prevHigh1;
	private decimal _prevHigh2;
	private decimal _prevHigh3;
	private decimal _prevLow1;
	private decimal _prevLow2;
	private decimal _prevLow3;
	private int _historyCount;

	private bool? _pendingIsBuy;
	private bool _plannedTakeProfitEnabled;
	private bool _hasPlannedPrices;
	private decimal _plannedEntryPrice;
	private decimal _plannedStopPrice;
	private decimal _plannedTakeProfitPrice;

	private decimal? _activeStopPrice;
	private decimal? _activeTakeProfitPrice;

	private decimal _lastPosition;

	/// <summary>
	/// Renko brick size in price units.
	/// </summary>
	public decimal BoxSize
	{
		get => _boxSize.Value;
		set => _boxSize.Value = value;
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
	/// Distance from the RSI midpoint (50) to generate pullback signals.
	/// </summary>
	public decimal RsiShift
	{
		get => _rsiShift.Value;
		set => _rsiShift.Value = value;
	}

	/// <summary>
	/// Take-profit distance in price units from the planned entry price.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Additional indent applied to breakout and stop-loss levels.
	/// </summary>
	public decimal IndentFromHighLow
	{
		get => _indentFromHighLow.Value;
		set => _indentFromHighLow.Value = value;
	}

	/// <summary>
	/// Order volume used for entries.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Time-based candle type used for RSI and breakout calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="RenkoLineBreakVsRsiStrategy"/> parameters.
	/// </summary>
	public RenkoLineBreakVsRsiStrategy()
	{
		_boxSize = Param(nameof(BoxSize), 500m)
		.SetGreaterThanZero()
		.SetDisplay("Renko Box Size", "Renko brick size in price units", "Renko")
		.SetCanOptimize(true)
		.SetOptimize(100m, 1000m, 100m);

		_rsiPeriod = Param(nameof(RsiPeriod), 4)
		.SetGreaterThanZero()
		.SetDisplay("RSI Period", "Relative Strength Index period", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(2, 20, 1);

		_rsiShift = Param(nameof(RsiShift), 20m)
		.SetGreaterThanZero()
		.SetDisplay("RSI Shift", "Distance from the 50 level to detect pullbacks", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(10m, 40m, 5m);

		_takeProfit = Param(nameof(TakeProfit), 1000m)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit", "Take profit distance in price units", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(200m, 2000m, 200m);

		_indentFromHighLow = Param(nameof(IndentFromHighLow), 50m)
		.SetGreaterThanZero()
		.SetDisplay("Indent", "Indent applied to breakout and stop levels", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(10m, 200m, 10m);

		_volume = Param(nameof(Volume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Volume", "Trading volume for orders", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe used for RSI and breakouts", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		_renkoType ??= DataType.Create(typeof(RenkoCandleMessage), new RenkoCandleArg
		{
			BuildFrom = RenkoBuildFrom.Points,
			BoxSize = BoxSize
		});

		return [(Security, CandleType), (Security, _renkoType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_rsi = null;
		_renkoType = null;

		_trendState = TrendState.None;
		_renkoHasPrev = false;
		_renkoPrevBull = false;

		_prevHigh1 = 0m;
		_prevHigh2 = 0m;
		_prevHigh3 = 0m;
		_prevLow1 = 0m;
		_prevLow2 = 0m;
		_prevLow3 = 0m;
		_historyCount = 0;

		ResetPendingPlan();
		ResetActiveTargets();

		_lastPosition = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_rsi = new RelativeStrengthIndex
		{
			Length = RsiPeriod
		};

		var timeSubscription = SubscribeCandles(CandleType);
		timeSubscription
		.Bind(_rsi, ProcessTimeCandle)
		.Start();

		var renkoSubscription = SubscribeCandles(_renkoType);
		renkoSubscription
		.Bind(ProcessRenkoCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, timeSubscription);
			DrawIndicator(area, _rsi);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessRenkoCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var isBull = candle.ClosePrice > candle.OpenPrice;
		var isBear = candle.ClosePrice < candle.OpenPrice;

		if (!_renkoHasPrev)
		{
			// Store the very first renko brick direction and wait for the next one to define a trend state.
			_renkoPrevBull = isBull;
			_renkoHasPrev = true;
			_trendState = TrendState.None;
			return;
		}

		if (isBull)
		{
			_trendState = _renkoPrevBull ? TrendState.Up : TrendState.ToUp;
			_renkoPrevBull = true;
		}
		else if (isBear)
		{
			_trendState = _renkoPrevBull ? TrendState.ToDown : TrendState.Down;
			_renkoPrevBull = false;
		}
		else
		{
			// Flat bricks keep the previous trend state.
		}
	}

	private void ProcessTimeCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var canTrade = IsFormedAndOnlineAndAllowTrading();
		var hasRsi = _rsi?.IsFormed == true && rsiValue >= 0m;

		CheckPendingActivation();

		ManagePosition(candle, rsiValue, hasRsi);

		if (canTrade && Position == 0)
		{
			TryPlaceEntry(rsiValue, hasRsi);
		}
		else if (!canTrade && Position == 0 && _pendingIsBuy != null)
		{
			// Cancel pending orders when trading is not allowed.
			CancelActiveOrders();
			ResetPendingPlan();
		}

		UpdateHistory(candle);
		_lastPosition = Position;
	}

	private void ManagePosition(ICandleMessage candle, decimal rsiValue, bool hasRsi)
	{
		var position = Position;

		if (position > 0m)
		{
			// Long position management.
			if (_pendingIsBuy != null)
			ResetPendingPlan();

			if (_activeTakeProfitPrice.HasValue && candle.HighPrice >= _activeTakeProfitPrice.Value)
			{
				SellMarket(position);
				ResetActiveTargets();
				return;
			}

			if (_activeStopPrice.HasValue && candle.LowPrice <= _activeStopPrice.Value)
			{
				SellMarket(position);
				ResetActiveTargets();
				return;
			}

			if (_trendState == TrendState.ToDown)
			{
				SellMarket(position);
				ResetActiveTargets();
				return;
			}

			if (hasRsi && rsiValue > 50m + RsiShift)
			{
				SellMarket(position);
				ResetActiveTargets();
			}
		}
		else if (position < 0m)
		{
			// Short position management.
			if (_pendingIsBuy != null)
			ResetPendingPlan();

			var absPosition = Math.Abs(position);

			if (_activeTakeProfitPrice.HasValue && candle.LowPrice <= _activeTakeProfitPrice.Value)
			{
				BuyMarket(absPosition);
				ResetActiveTargets();
				return;
			}

			if (_activeStopPrice.HasValue && candle.HighPrice >= _activeStopPrice.Value)
			{
				BuyMarket(absPosition);
				ResetActiveTargets();
				return;
			}

			if (_trendState == TrendState.ToUp)
			{
				BuyMarket(absPosition);
				ResetActiveTargets();
				return;
			}

			if (hasRsi && rsiValue < 50m - RsiShift)
			{
				BuyMarket(absPosition);
				ResetActiveTargets();
			}
		}
		else
		{
			// No position -> clear active stop/target remnants.
			if (_activeStopPrice.HasValue || _activeTakeProfitPrice.HasValue)
			ResetActiveTargets();
		}
	}

	private void TryPlaceEntry(decimal rsiValue, bool hasRsi)
	{
		if (_trendState == TrendState.ToDown || _trendState == TrendState.ToUp)
		{
			if (_pendingIsBuy != null)
			{
				CancelActiveOrders();
				ResetPendingPlan();
			}

			return;
		}

		if (_historyCount < 3 || !hasRsi)
		return;

		var indent = IndentFromHighLow;
		var takeProfitDistance = TakeProfit;

		if (_trendState == TrendState.Up && rsiValue <= 50m - RsiShift)
		{
			var entryPrice = _prevHigh3 + indent;
			var stopPrice = Math.Min(_prevLow1, Math.Min(_prevLow2, _prevLow3)) - indent;

			if (entryPrice > 0m && stopPrice > 0m && entryPrice > stopPrice)
			{
				var takeProfitPrice = takeProfitDistance > 0m ? entryPrice + takeProfitDistance : (decimal?)null;
				PlacePendingOrder(true, entryPrice, stopPrice, takeProfitPrice);
			}
		}
		else if (_trendState == TrendState.Down && rsiValue >= 50m + RsiShift)
		{
			var entryPrice = _prevLow3 - indent;
			var stopPrice = Math.Max(_prevHigh1, Math.Max(_prevHigh2, _prevHigh3)) + indent;

			if (entryPrice > 0m && stopPrice > 0m && entryPrice < stopPrice)
			{
				var takeProfitPrice = takeProfitDistance > 0m ? entryPrice - takeProfitDistance : (decimal?)null;
				PlacePendingOrder(false, entryPrice, stopPrice, takeProfitPrice);
			}
		}
	}

	private void PlacePendingOrder(bool isBuy, decimal entryPrice, decimal stopPrice, decimal? takeProfitPrice)
	{
		// Avoid duplicate registrations if the pending order already matches the desired levels.
		if (_pendingIsBuy == isBuy && _hasPlannedPrices &&
		entryPrice == _plannedEntryPrice && stopPrice == _plannedStopPrice &&
		((takeProfitPrice == null && !_plannedTakeProfitEnabled) ||
		(takeProfitPrice != null && _plannedTakeProfitEnabled && takeProfitPrice.Value == _plannedTakeProfitPrice)))
		{
			return;
		}

		CancelActiveOrders();
		ResetPendingPlan();

		var volume = Volume;

		if (isBuy)
		{
			BuyStop(volume, entryPrice);
		}
		else
		{
			SellStop(volume, entryPrice);
		}

		_pendingIsBuy = isBuy;
		_hasPlannedPrices = true;
		_plannedEntryPrice = entryPrice;
		_plannedStopPrice = stopPrice;
		_plannedTakeProfitEnabled = takeProfitPrice != null;
		_plannedTakeProfitPrice = takeProfitPrice ?? 0m;
	}

	private void CheckPendingActivation()
	{
		if (_pendingIsBuy == null || !_hasPlannedPrices)
		return;

		if (_pendingIsBuy.Value && _lastPosition <= 0m && Position > 0m)
		{
			ActivatePlannedTargets();
		}
		else if (!_pendingIsBuy.Value && _lastPosition >= 0m && Position < 0m)
		{
			ActivatePlannedTargets();
		}
	}

	private void ActivatePlannedTargets()
	{
		_activeStopPrice = _plannedStopPrice;
		_activeTakeProfitPrice = _plannedTakeProfitEnabled ? _plannedTakeProfitPrice : null;

		ResetPendingPlan();
	}

	private void UpdateHistory(ICandleMessage candle)
	{
		_prevHigh3 = _prevHigh2;
		_prevHigh2 = _prevHigh1;
		_prevHigh1 = candle.HighPrice;

		_prevLow3 = _prevLow2;
		_prevLow2 = _prevLow1;
		_prevLow1 = candle.LowPrice;

		if (_historyCount < 3)
		{
			_historyCount++;
		}
	}

	private void ResetPendingPlan()
	{
		_pendingIsBuy = null;
		_hasPlannedPrices = false;
		_plannedEntryPrice = 0m;
		_plannedStopPrice = 0m;
		_plannedTakeProfitPrice = 0m;
		_plannedTakeProfitEnabled = false;
	}

	private void ResetActiveTargets()
	{
		_activeStopPrice = null;
		_activeTakeProfitPrice = null;
	}
}
