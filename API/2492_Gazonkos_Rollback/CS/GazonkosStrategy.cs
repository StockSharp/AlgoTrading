using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Momentum breakout with rollback confirmation inspired by the gazonkos MT5 expert.
/// The strategy waits for a spread between two historical closes, then joins the trend after a pullback.
/// </summary>
public class GazonkosStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _rollback;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _delta;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _firstShift;
	private readonly StrategyParam<int> _secondShift;
	private readonly StrategyParam<int> _activeTrades;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _closeHistory = new();

	private int _state;
	private int _tradeDirection;
	private decimal _maxPrice;
	private decimal _minPrice;
	private bool _canTrade;
	private int _lastTradeHour;
	private int _lastSignalHour;
	private int _maxHistory;

	/// <summary>
	/// Take profit distance expressed in absolute price units.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Rollback distance that confirms the entry.
	/// </summary>
	public decimal Rollback
	{
		get => _rollback.Value;
		set => _rollback.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in absolute price units.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Minimum difference between historical closes to detect momentum.
	/// </summary>
	public decimal Delta
	{
		get => _delta.Value;
		set => _delta.Value = value;
	}

	/// <summary>
	/// Default volume for market orders.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Older bar shift used in the close difference calculation.
	/// </summary>
	public int FirstShift
	{
		get => _firstShift.Value;
		set => _firstShift.Value = value;
	}

	/// <summary>
	/// Recent bar shift used in the close difference calculation.
	/// </summary>
	public int SecondShift
	{
		get => _secondShift.Value;
		set => _secondShift.Value = value;
	}

	/// <summary>
	/// Maximum simultaneous trades counted in volume units.
	/// </summary>
	public int ActiveTrades
	{
		get => _activeTrades.Value;
		set => _activeTrades.Value = value;
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public GazonkosStrategy()
	{
		_takeProfit = Param(nameof(TakeProfit), 0.0016m)
			.SetDisplay("Take Profit", "Take profit distance in price units", "Risk Management")
			.SetCanOptimize(true);

		_rollback = Param(nameof(Rollback), 0.0016m)
			.SetDisplay("Rollback", "Required pullback before entering", "Signals")
			.SetCanOptimize(true);

		_stopLoss = Param(nameof(StopLoss), 0.004m)
			.SetDisplay("Stop Loss", "Stop loss distance in price units", "Risk Management")
			.SetCanOptimize(true);

		_delta = Param(nameof(Delta), 0.004m)
			.SetDisplay("Delta", "Minimum difference between closes", "Signals")
			.SetCanOptimize(true);

		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
			.SetDisplay("Trade Volume", "Default volume for market orders", "Orders")
			.SetCanOptimize(true);

		_firstShift = Param(nameof(FirstShift), 3)
			.SetDisplay("First Shift", "Older close shift for the comparison", "Signals")
			.SetCanOptimize(true);

		_secondShift = Param(nameof(SecondShift), 2)
			.SetDisplay("Second Shift", "Recent close shift for the comparison", "Signals")
			.SetCanOptimize(true);

		_activeTrades = Param(nameof(ActiveTrades), 1)
			.SetDisplay("Active Trades", "Maximum simultaneous trades", "Risk Management")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle series used for signals", "General");
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

		_state = 0;
		_tradeDirection = 0;
		_maxPrice = 0m;
		_minPrice = decimal.MaxValue;
		_canTrade = true;
		_lastTradeHour = -1;
		_lastSignalHour = -1;
		_closeHistory.Clear();
		UpdateHistorySize();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;
		UpdateHistorySize();

		StartProtection(
			takeProfit: new Unit(TakeProfit, UnitTypes.Absolute),
			stopLoss: new Unit(StopLoss, UnitTypes.Absolute),
			isStopTrailing: false,
			useMarketOrders: true);

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

		UpdateHistorySize();
		AddClose(candle.ClosePrice);

		var hour = candle.CloseTime.UtcDateTime.Hour;

		if (_state == 0)
		{
			// Evaluate if another trade can be started during the current hour.
			_canTrade = true;

			if (_lastTradeHour == hour)
				_canTrade = false;

			if (ActiveTrades > 0 && Volume > 0 && Math.Abs(Position) >= ActiveTrades * Volume)
				_canTrade = false;

			if (_canTrade)
				_state = 1;
		}

		if (_state == 1)
		{
			// Look for momentum using the difference between historical closes.
			if (!TryGetClose(FirstShift, out var closeFirst) || !TryGetClose(SecondShift, out var closeSecond))
				return;

			if (closeSecond - closeFirst > Delta)
			{
				_tradeDirection = 1;
				_maxPrice = candle.ClosePrice;
				_lastSignalHour = hour;
				_state = 2;
			}
			else if (closeFirst - closeSecond > Delta)
			{
				_tradeDirection = -1;
				_minPrice = candle.ClosePrice;
				_lastSignalHour = hour;
				_state = 2;
			}
		}

		if (_state == 2)
		{
			// Wait for a rollback confirmation during the same hour when the signal appeared.
			if (_lastSignalHour != hour)
			{
				ResetToIdle();
				return;
			}

			if (_tradeDirection == 1)
			{
				if (candle.HighPrice > _maxPrice)
					_maxPrice = candle.HighPrice;

				if (candle.LowPrice < _maxPrice - Rollback)
					_state = 3;
			}
			else if (_tradeDirection == -1)
			{
				if (candle.LowPrice < _minPrice)
					_minPrice = candle.LowPrice;

				if (candle.HighPrice > _minPrice + Rollback)
					_state = 3;
			}
		}

		if (_state == 3)
		{
			// Execute the trade after rollback confirmation.
			if (_tradeDirection == 1 && Position <= 0)
			{
				BuyMarket();
				_lastTradeHour = hour;
				ResetToIdle();
			}
			else if (_tradeDirection == -1 && Position >= 0)
			{
				SellMarket();
				_lastTradeHour = hour;
				ResetToIdle();
			}
		}
	}

	private void UpdateHistorySize()
	{
		var required = Math.Max(Math.Max(FirstShift, SecondShift) + 1, 1);

		if (_maxHistory == required)
			return;

		_maxHistory = required;

		if (_closeHistory.Count > _maxHistory)
			_closeHistory.RemoveRange(_maxHistory, _closeHistory.Count - _maxHistory);
	}

	private void AddClose(decimal close)
	{
		_closeHistory.Insert(0, close);

		if (_closeHistory.Count > _maxHistory)
			_closeHistory.RemoveAt(_closeHistory.Count - 1);
	}

	private bool TryGetClose(int shift, out decimal close)
	{
		close = 0m;

		if (shift < 0)
			return false;

		if (_closeHistory.Count <= shift)
			return false;

		close = _closeHistory[shift];
		return true;
	}

	private void ResetToIdle()
	{
		_state = 0;
		_tradeDirection = 0;
		_maxPrice = 0m;
		_minPrice = decimal.MaxValue;
		_canTrade = true;
		_lastSignalHour = -1;
	}
}
