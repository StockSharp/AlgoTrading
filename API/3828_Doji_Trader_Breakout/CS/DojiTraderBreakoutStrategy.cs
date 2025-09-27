namespace StockSharp.Samples.Strategies;

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

/// <summary>
/// Strategy based on the DojiTrader MQL expert.
/// Waits for a recent doji candle and trades on the breakout of its high or low.
/// </summary>
public class DojiTraderBreakoutStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _takeProfitSteps;
	private readonly StrategyParam<int> _stopLossSteps;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _dojiHigh;
	private decimal? _dojiLow;
	private int _pendingDirection;
	private decimal? _triggerPrice;
	private decimal? _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takePrice;

	private ICandleMessage _prevCandle;
	private ICandleMessage _prev2Candle;
	private ICandleMessage _prev3Candle;

	/// <summary>
	/// Initializes a new instance of <see cref="DojiTraderBreakoutStrategy"/>.
	/// </summary>
	public DojiTraderBreakoutStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Order volume", "Volume used for market orders.", "Trading");

		_takeProfitSteps = Param(nameof(TakeProfitSteps), 15)
			.SetDisplay("Take profit (steps)", "Distance to take profit in price steps.", "Risk");

		_stopLossSteps = Param(nameof(StopLossSteps), 50)
			.SetDisplay("Stop loss (steps)", "Distance to stop loss in price steps.", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle type", "Timeframe for signal detection.", "General");
	}

	/// <summary>
	/// Volume used for market orders.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Distance to take profit in price steps.
	/// </summary>
	public int TakeProfitSteps
	{
		get => _takeProfitSteps.Value;
		set => _takeProfitSteps.Value = value;
	}

	/// <summary>
	/// Distance to stop loss in price steps.
	/// </summary>
	public int StopLossSteps
	{
		get => _stopLossSteps.Value;
		set => _stopLossSteps.Value = value;
	}

	/// <summary>
	/// Candle type used for analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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
		_dojiHigh = null;
		_dojiLow = null;
		_pendingDirection = 0;
		_triggerPrice = null;
		_entryPrice = null;
		_stopPrice = null;
		_takePrice = null;
		_prevCandle = null;
		_prev2Candle = null;
		_prev3Candle = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = OrderVolume;
		StartProtection();

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

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			UpdateHistory(candle);
			return;
		}

		var previous = _prevCandle;
		// Use the previous completed candle (MQL Close[1]) for decisions.

		if (Position > 0)
		{
			// Manage long exits before evaluating new entries.
			CheckLongExit(previous);
		}
		else if (Position < 0)
		{
			// Manage short exits before evaluating new entries.
			CheckShortExit(previous);
		}

		if (!IsTradingHour(candle.CloseTime))
		{
			// Outside trading hours, only maintain state and skip entry logic.
			UpdateHistory(candle);
			return;
		}

		if (Position == 0)
		{
			// Flat state: look for doji signals and possible breakout entries.
			// If no valid doji remains within the last three candles, reset the pending breakout.
			UpdateDoji();

			if (_pendingDirection == 0 && _dojiHigh.HasValue && _dojiLow.HasValue && previous != null)
			{
				var prevClose = previous.ClosePrice;
				// Determine breakout direction once per doji using the candle after the doji.
				if (prevClose > _dojiHigh.Value)
				{
					_pendingDirection = 1;
					_triggerPrice = prevClose;
				}
				else if (prevClose < _dojiLow.Value)
				{
					_pendingDirection = -1;
					_triggerPrice = prevClose;
				}
			}

			if (_pendingDirection == 1 && _triggerPrice.HasValue && candle.ClosePrice > _triggerPrice.Value)
			{
				// Market entry above the trigger price mirrors the original Ask check.
				EnterLong(candle);
			}
			else if (_pendingDirection == -1 && _triggerPrice.HasValue && candle.ClosePrice < _triggerPrice.Value)
			{
				// Market entry below the trigger price mirrors the original Bid check.
				EnterShort(candle);
			}
		}

		UpdateHistory(candle);
	}

	private void CheckLongExit(ICandleMessage previous)
	{
		if (previous == null)
			return;

		// Exit triggers emulate the MQL4 order closing conditions.
		var shouldClose = false;

		if (_dojiLow.HasValue && previous.ClosePrice < _dojiLow.Value)
			shouldClose = true;

		if (_stopPrice.HasValue && previous.LowPrice <= _stopPrice.Value)
			shouldClose = true;

		if (_takePrice.HasValue && previous.HighPrice >= _takePrice.Value)
			shouldClose = true;

		if (!shouldClose)
			return;

		ClosePosition();
		_pendingDirection = 0;
		_triggerPrice = null;
		_entryPrice = null;
		_stopPrice = null;
		_takePrice = null;
	}

	private void CheckShortExit(ICandleMessage previous)
	{
		if (previous == null)
			return;

		// Exit triggers emulate the MQL4 order closing conditions.
		var shouldClose = false;

		if (_dojiHigh.HasValue && previous.ClosePrice > _dojiHigh.Value)
			shouldClose = true;

		if (_stopPrice.HasValue && previous.HighPrice >= _stopPrice.Value)
			shouldClose = true;

		if (_takePrice.HasValue && previous.LowPrice <= _takePrice.Value)
			shouldClose = true;

		if (!shouldClose)
			return;

		ClosePosition();
		_pendingDirection = 0;
		_triggerPrice = null;
		_entryPrice = null;
		_stopPrice = null;
		_takePrice = null;
	}

	private void EnterLong(ICandleMessage candle)
	{
		if (!CanTrade())
			return;

		var step = GetPriceStep();
		// Compute synthetic risk levels based on the configured step distances.
		BuyMarket(OrderVolume);

		_entryPrice = candle.ClosePrice;
		_stopPrice = StopLossSteps > 0 ? _entryPrice - step * StopLossSteps : null;
		_takePrice = TakeProfitSteps > 0 ? _entryPrice + step * TakeProfitSteps : null;
		_pendingDirection = 0;
		_triggerPrice = null;
	}

	private void EnterShort(ICandleMessage candle)
	{
		if (!CanTrade())
			return;

		var step = GetPriceStep();
		// Compute synthetic risk levels based on the configured step distances.
		SellMarket(OrderVolume);

		_entryPrice = candle.ClosePrice;
		_stopPrice = StopLossSteps > 0 ? _entryPrice + step * StopLossSteps : null;
		_takePrice = TakeProfitSteps > 0 ? _entryPrice - step * TakeProfitSteps : null;
		_pendingDirection = 0;
		_triggerPrice = null;
	}

	private bool CanTrade()
	{
		return OrderVolume > 0 && Portfolio != null && Security != null;
	}

	private void UpdateDoji()
	{
		if (_prev2Candle != null && IsDoji(_prev2Candle))
		{
			// Prefer the most recent doji that is two candles back.
			_dojiHigh = _prev2Candle.HighPrice;
			_dojiLow = _prev2Candle.LowPrice;
			return;
		}

		if (_prev3Candle != null && IsDoji(_prev3Candle))
		{
			// Allow a doji up to three candles back, matching the original window.
			_dojiHigh = _prev3Candle.HighPrice;
			_dojiLow = _prev3Candle.LowPrice;
			return;
		}

		_dojiHigh = null;
		_dojiLow = null;
		_pendingDirection = 0;
		_triggerPrice = null;
	}

	private void UpdateHistory(ICandleMessage candle)
	{
		_prev3Candle = _prev2Candle;
		_prev2Candle = _prevCandle;
		_prevCandle = candle;
	}

	private static bool IsDoji(ICandleMessage candle)
	{
		// Strict equality matches the original EA comparison.
		return candle.OpenPrice == candle.ClosePrice;
	}

	private bool IsTradingHour(DateTimeOffset time)
	{
		var hour = time.Hour;
		return hour >= 8 && hour < 17;
	}

	private decimal GetPriceStep()
	{
		return Security?.PriceStep ?? 1m;
	}
}

