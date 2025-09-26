using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader expert JK_BullP_AutoTrader2.
/// Uses Bulls Power momentum to sell fading bullish spikes and buy dips into negative territory.
/// Adds basic risk controls, a configurable trailing stop, and candle-based execution.
/// </summary>
public class JkBullPAutoTraderStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _ema;
	private decimal? _previousBullsPower;
	private decimal? _lastBullsPower;
	private decimal _pipSize;
	private decimal? _entryPrice;
	private decimal? _longTrailingStop;
	private decimal? _shortTrailingStop;
	private decimal _previousPosition;
	private decimal? _lastTradePrice;
	private Sides? _lastTradeSide;

	/// <summary>
	/// Order size used for market entries.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Length of the EMA used by Bulls Power calculations.
	/// </summary>
	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	/// <summary>
	/// Candle type used to drive indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="JkBullPAutoTraderStrategy"/> class.
	/// </summary>
	public JkBullPAutoTraderStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 8.5m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Market order size for entries", "Trading");

		_takeProfitPips = Param(nameof(TakeProfitPips), 500m)
			.SetGreaterThanOrEqualZero()
			.SetDisplay("Take Profit (pips)", "Take profit distance expressed in pips", "Risk");

		_stopLossPips = Param(nameof(StopLossPips), 20m)
			.SetGreaterThanOrEqualZero()
			.SetDisplay("Stop Loss (pips)", "Stop loss distance expressed in pips", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 10m)
			.SetGreaterThanOrEqualZero()
			.SetDisplay("Trailing Stop (pips)", "Trailing distance used to protect profits", "Risk");

		_emaPeriod = Param(nameof(EmaPeriod), 13)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "Length of the EMA used by Bulls Power", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle type used for indicator calculations", "Data");
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

		// Clear cached indicator state and trailing information.
		_ema = null;
		_previousBullsPower = null;
		_lastBullsPower = null;
		_pipSize = 0m;
		_entryPrice = null;
		_longTrailingStop = null;
		_shortTrailingStop = null;
		_previousPosition = 0m;
		_lastTradePrice = null;
		_lastTradeSide = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Cache pip size so the original point-based inputs map to price offsets.
		_pipSize = GetPipSize();

		// EMA replicates the smoothing that underlies the Bulls Power indicator.
		_ema = new ExponentialMovingAverage
		{
			Length = EmaPeriod
		};

		// Subscribe to candles and obtain EMA values alongside price action.
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_ema, ProcessCandle)
			.Start();

		// Use the configured lot size for all market orders.
		Volume = OrderVolume;

		var stopLoss = StopLossPips > 0m && _pipSize > 0m
			? new Unit(StopLossPips * _pipSize, UnitTypes.Absolute)
			: null;
		var takeProfit = TakeProfitPips > 0m && _pipSize > 0m
			? new Unit(TakeProfitPips * _pipSize, UnitTypes.Absolute)
			: null;

		// Activate protective mechanics to emulate the fixed stop and target.
		if (stopLoss != null || takeProfit != null)
		{
			StartProtection(stopLoss: stopLoss, takeProfit: takeProfit, useMarketOrders: true);
		}
		else
		{
			StartProtection();
		}
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade.Trade != null)
		{
			// Remember the latest execution price to synchronize entry tracking.
			_lastTradePrice = trade.Trade.Price;
		}

		_lastTradeSide = trade.Order.Direction;
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		// Detect new entries and store the price baseline for trailing decisions.
		if (_previousPosition == 0m && Position != 0m)
		{
			if (Position > 0m && _lastTradeSide == Sides.Buy)
			{
				_entryPrice = _lastTradePrice;
				_longTrailingStop = null;
				_shortTrailingStop = null;
			}
			else if (Position < 0m && _lastTradeSide == Sides.Sell)
			{
				_entryPrice = _lastTradePrice;
				_longTrailingStop = null;
				_shortTrailingStop = null;
			}
		}
		else if (Position == 0m && _previousPosition != 0m)
		{
			// Reset trailing context after a complete exit.
			_entryPrice = null;
			_longTrailingStop = null;
			_shortTrailingStop = null;
		}

		_previousPosition = Position;
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Manage the trailing stop before evaluating fresh entry signals.
		if (UpdateTrailing(candle))
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_ema is null || !_ema.IsFormed)
			return;

		// Bulls Power equals the distance between candle highs and the EMA baseline.
		var bullsPower = candle.HighPrice - emaValue;

		if (_lastBullsPower.HasValue)
			_previousBullsPower = _lastBullsPower;

		_lastBullsPower = bullsPower;

		if (_previousBullsPower is not decimal previous)
			return;

		// Sell when bullish pressure weakens but still stays above zero.
		if (previous > bullsPower && bullsPower > 0m && Position == 0m)
		{
			SellMarket();
			return;
		}

		// Buy when Bulls Power drops below zero, anticipating a rebound.
		if (bullsPower < 0m && Position == 0m)
		{
			BuyMarket();
		}
	}

	private bool UpdateTrailing(ICandleMessage candle)
	{
		if (TrailingStopPips <= 0m || _entryPrice is null || _pipSize <= 0m)
			return false;

		var trailingDistance = TrailingStopPips * _pipSize;
		if (trailingDistance <= 0m)
			return false;

		var entryPrice = _entryPrice.Value;
		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return false;

		if (Position > 0m)
		{
			// Trail the stop underneath the latest close once profit exceeds the threshold.
			var candidate = candle.ClosePrice - trailingDistance;
			if (candle.ClosePrice - entryPrice > trailingDistance)
			{
				if (!_longTrailingStop.HasValue || candidate > _longTrailingStop.Value)
					_longTrailingStop = candidate;
			}

			if (_longTrailingStop.HasValue && candle.LowPrice <= _longTrailingStop.Value)
			{
				SellMarket(volume);
				_entryPrice = null;
				_longTrailingStop = null;
				_shortTrailingStop = null;
				return true;
			}
		}
		else if (Position < 0m)
		{
			// Trail the stop above price for shorts when gains surpass the trailing distance.
			var candidate = candle.ClosePrice + trailingDistance;
			if (entryPrice - candle.ClosePrice > trailingDistance)
			{
				if (!_shortTrailingStop.HasValue || candidate < _shortTrailingStop.Value)
					_shortTrailingStop = candidate;
			}

			if (_shortTrailingStop.HasValue && candle.HighPrice >= _shortTrailingStop.Value)
			{
				BuyMarket(volume);
				_entryPrice = null;
				_longTrailingStop = null;
				_shortTrailingStop = null;
				return true;
			}
		}

		return false;
	}

	private decimal GetPipSize()
	{
		var security = Security;
		if (security?.PriceStep is decimal step && step > 0m)
			return step;

		return 1m;
	}
}
