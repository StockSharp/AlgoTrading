using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the Aeron JJN Scalper expert advisor.
/// </summary>
public class AeronJjnScalperEaStrategy : Strategy
{
	private const int AtrLength = 8;
	private const int HistoryDepth = 120;

	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<int> _resetMinutes;
	private readonly StrategyParam<decimal> _dojiDiff1Pips;
	private readonly StrategyParam<decimal> _dojiDiff2Pips;
	private readonly StrategyParam<DataType> _candleType;

	private AverageTrueRange _atr;

	private readonly List<CandleSnapshot> _history = new();

	private decimal _pipSize;
	private decimal _trailingStopDistance;
	private decimal _trailingStepDistance;
	private decimal _dojiDiff1;
	private decimal _dojiDiff2;

	private decimal? _pendingLongLevel;
	private decimal? _pendingShortLevel;
	private decimal? _pendingLongAtr;
	private decimal? _pendingShortAtr;
	private DateTimeOffset? _pendingLongExpiry;
	private DateTimeOffset? _pendingShortExpiry;

	private decimal? _entryPrice;
	private decimal? _longStopPrice;
	private decimal? _longTakePrice;
	private decimal? _shortStopPrice;
	private decimal? _shortTakePrice;

	private decimal _lastAtr;

	/// <summary>
	/// Trailing stop distance in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Trailing step distance in pips.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Expiration time for pending orders in minutes.
	/// </summary>
	public int ResetMinutes
	{
		get => _resetMinutes.Value;
		set => _resetMinutes.Value = value;
	}

	/// <summary>
	/// Minimum body size for the reversal candle (pips).
	/// </summary>
	public decimal DojiDiff1Pips
	{
		get => _dojiDiff1Pips.Value;
		set => _dojiDiff1Pips.Value = value;
	}

	/// <summary>
	/// Minimum body size for the reference candle (pips).
	/// </summary>
	public decimal DojiDiff2Pips
	{
		get => _dojiDiff2Pips.Value;
		set => _dojiDiff2Pips.Value = value;
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
	/// Create strategy instance.
	/// </summary>
	public AeronJjnScalperEaStrategy()
	{
		_trailingStopPips = Param(nameof(TrailingStopPips), 5m)
		.SetDisplay("Trailing Stop (pips)");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
		.SetDisplay("Trailing Step (pips)");

		_resetMinutes = Param(nameof(ResetMinutes), 10)
		.SetDisplay("Pending Expiry (minutes)");

		_dojiDiff1Pips = Param(nameof(DojiDiff1Pips), 10m)
		.SetDisplay("Doji Diff 1 (pips)");

		_dojiDiff2Pips = Param(nameof(DojiDiff2Pips), 4m)
		.SetDisplay("Doji Diff 2 (pips)");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type");

		Volume = 0.1m;
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

		_history.Clear();
		_pendingLongLevel = null;
		_pendingShortLevel = null;
		_pendingLongAtr = null;
		_pendingShortAtr = null;
		_pendingLongExpiry = null;
		_pendingShortExpiry = null;
		_entryPrice = null;
		_longStopPrice = null;
		_longTakePrice = null;
		_shortStopPrice = null;
		_shortTakePrice = null;
		_lastAtr = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		InitializePipSettings();

		_atr = new AverageTrueRange
		{
			Length = AtrLength
		};

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_atr, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _atr);
			DrawOwnTrades(area);
		}
	}

	private void InitializePipSettings()
	{
		_pipSize = Security?.PriceStep ?? 0m;
		if (_pipSize <= 0m)
		_pipSize = 1m;

		var decimals = Security?.Decimals;
		if (decimals == 3 || decimals == 5)
		_pipSize *= 10m;

		_trailingStopDistance = TrailingStopPips * _pipSize;
		_trailingStepDistance = TrailingStepPips * _pipSize;
		_dojiDiff1 = DojiDiff1Pips * _pipSize;
		_dojiDiff2 = DojiDiff2Pips * _pipSize;
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (_pipSize <= 0m)
		InitializePipSettings();

		_lastAtr = atrValue;

		var closeTime = candle.CloseTime;
		CancelExpiredPendings(closeTime);

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			UpdateHistory(candle);
			return;
		}

		ManageActivePosition(candle);
		TriggerPendings(candle);
		EvaluateSignals(candle);
		UpdateHistory(candle);
	}

	private void CancelExpiredPendings(DateTimeOffset closeTime)
	{
		if (_pendingLongExpiry.HasValue && closeTime >= _pendingLongExpiry.Value)
		{
			_pendingLongLevel = null;
			_pendingLongAtr = null;
			_pendingLongExpiry = null;
		}

		if (_pendingShortExpiry.HasValue && closeTime >= _pendingShortExpiry.Value)
		{
			_pendingShortLevel = null;
			_pendingShortAtr = null;
			_pendingShortExpiry = null;
		}
	}

	private void ManageActivePosition(ICandleMessage candle)
	{
		if (Position > 0)
		{
			ApplyTrailingForLong(candle);

			// Exit a long position when price reaches the trailing stop or the ATR based take profit.
			if (_longStopPrice.HasValue && candle.LowPrice <= _longStopPrice.Value)
			{
				SellMarket(Math.Abs(Position));
				ClearPositionState();
				return;
			}

			if (_longTakePrice.HasValue && candle.HighPrice >= _longTakePrice.Value)
			{
				SellMarket(Math.Abs(Position));
				ClearPositionState();
			}
		}
		else if (Position < 0)
		{
			ApplyTrailingForShort(candle);

			// Exit a short position when price reaches the trailing stop or the ATR based take profit.
			if (_shortStopPrice.HasValue && candle.HighPrice >= _shortStopPrice.Value)
			{
				BuyMarket(Math.Abs(Position));
				ClearPositionState();
				return;
			}

			if (_shortTakePrice.HasValue && candle.LowPrice <= _shortTakePrice.Value)
			{
				BuyMarket(Math.Abs(Position));
				ClearPositionState();
			}
		}
		else if (_entryPrice.HasValue)
		{
			// Reset state when the position is closed externally.
			ClearPositionState();
		}
	}

	private void ApplyTrailingForLong(ICandleMessage candle)
	{
		if (_trailingStopDistance <= 0m || !_entryPrice.HasValue)
		return;

		var move = candle.ClosePrice - _entryPrice.Value;
		if (move <= _trailingStopDistance + _trailingStepDistance)
		return;

		var threshold = candle.ClosePrice - (_trailingStopDistance + _trailingStepDistance);
		if (!_longStopPrice.HasValue || _longStopPrice.Value < threshold)
		_longStopPrice = candle.ClosePrice - _trailingStopDistance;
	}

	private void ApplyTrailingForShort(ICandleMessage candle)
	{
		if (_trailingStopDistance <= 0m || !_entryPrice.HasValue)
		return;

		var move = _entryPrice.Value - candle.ClosePrice;
		if (move <= _trailingStopDistance + _trailingStepDistance)
		return;

		var threshold = candle.ClosePrice + (_trailingStopDistance + _trailingStepDistance);
		if (!_shortStopPrice.HasValue || _shortStopPrice.Value > threshold)
		_shortStopPrice = candle.ClosePrice + _trailingStopDistance;
	}

	private void TriggerPendings(ICandleMessage candle)
	{
		if (_pendingLongLevel.HasValue && candle.HighPrice >= _pendingLongLevel.Value)
		{
			var volume = Volume + (Position < 0 ? Math.Abs(Position) : 0m);
			if (volume > 0m)
			{
				BuyMarket(volume);
				SetupLongPosition(_pendingLongLevel.Value, _pendingLongAtr);
			}

			_pendingLongLevel = null;
			_pendingLongAtr = null;
			_pendingLongExpiry = null;
		}

		if (_pendingShortLevel.HasValue && candle.LowPrice <= _pendingShortLevel.Value)
		{
			var volume = Volume + (Position > 0 ? Math.Abs(Position) : 0m);
			if (volume > 0m)
			{
				SellMarket(volume);
				SetupShortPosition(_pendingShortLevel.Value, _pendingShortAtr);
			}

			_pendingShortLevel = null;
			_pendingShortAtr = null;
			_pendingShortExpiry = null;
		}
	}

	private void EvaluateSignals(ICandleMessage candle)
	{
		if (_history.Count == 0)
		return;

		var prev = _history[0];

		// A bullish candle following a strong bearish candle creates a buy stop setup.
		if (candle.ClosePrice > candle.OpenPrice && prev.Open - prev.Close > _dojiDiff1)
		{
			TryCreateLongPending(candle);
		}
		// A bearish candle following a strong bullish candle creates a sell stop setup.
		else if (candle.ClosePrice < candle.OpenPrice && prev.Close - prev.Open > _dojiDiff1)
		{
			TryCreateShortPending(candle);
		}
	}

	private void TryCreateLongPending(ICandleMessage candle)
	{
		if (_pendingLongLevel.HasValue || Position > 0 || _lastAtr <= 0m)
		return;

		var lastBearish = FindLastBearishOpen();
		if (!lastBearish.HasValue)
		return;

		var minDistance = _pipSize;
		if (lastBearish.Value <= candle.ClosePrice + minDistance)
		return;

		_pendingLongLevel = lastBearish.Value;
		_pendingLongAtr = _lastAtr;
		_pendingLongExpiry = candle.CloseTime + TimeSpan.FromMinutes(Math.Max(0, ResetMinutes));
	}

	private void TryCreateShortPending(ICandleMessage candle)
	{
		if (_pendingShortLevel.HasValue || Position < 0 || _lastAtr <= 0m)
		return;

		var lastBullish = FindLastBullishOpen();
		if (!lastBullish.HasValue)
		return;

		var minDistance = _pipSize;
		if (lastBullish.Value >= candle.ClosePrice - minDistance)
		return;

		_pendingShortLevel = lastBullish.Value;
		_pendingShortAtr = _lastAtr;
		_pendingShortExpiry = candle.CloseTime + TimeSpan.FromMinutes(Math.Max(0, ResetMinutes));
	}

	private decimal? FindLastBearishOpen()
	{
		foreach (var snapshot in _history)
		{
			if (snapshot.Close < snapshot.Open && snapshot.Open - snapshot.Close > _dojiDiff2)
			return snapshot.Open;
		}

		return null;
	}

	private decimal? FindLastBullishOpen()
	{
		foreach (var snapshot in _history)
		{
			if (snapshot.Close > snapshot.Open && snapshot.Close - snapshot.Open > _dojiDiff2)
			return snapshot.Open;
		}

		return null;
	}

	private void SetupLongPosition(decimal entryPrice, decimal? atr)
	{
		_entryPrice = entryPrice;

		if (atr.HasValue && atr.Value > 0m)
		{
			_longStopPrice = entryPrice - atr.Value;
			_longTakePrice = entryPrice + atr.Value;
		}
		else
		{
			_longStopPrice = null;
			_longTakePrice = null;
		}

		_shortStopPrice = null;
		_shortTakePrice = null;
		_pendingShortLevel = null;
		_pendingShortAtr = null;
		_pendingShortExpiry = null;
	}

	private void SetupShortPosition(decimal entryPrice, decimal? atr)
	{
		_entryPrice = entryPrice;

		if (atr.HasValue && atr.Value > 0m)
		{
			_shortStopPrice = entryPrice + atr.Value;
			_shortTakePrice = entryPrice - atr.Value;
		}
		else
		{
			_shortStopPrice = null;
			_shortTakePrice = null;
		}

		_longStopPrice = null;
		_longTakePrice = null;
		_pendingLongLevel = null;
		_pendingLongAtr = null;
		_pendingLongExpiry = null;
	}

	private void ClearPositionState()
	{
		_entryPrice = null;
		_longStopPrice = null;
		_longTakePrice = null;
		_shortStopPrice = null;
		_shortTakePrice = null;
	}

	private void UpdateHistory(ICandleMessage candle)
	{
		_history.Insert(0, new CandleSnapshot(candle.OpenPrice, candle.ClosePrice));
		if (_history.Count > HistoryDepth)
		_history.RemoveAt(_history.Count - 1);
	}

	private readonly struct CandleSnapshot
	{
		public CandleSnapshot(decimal open, decimal close)
		{
			Open = open;
			Close = close;
		}

		public decimal Open { get; }
		public decimal Close { get; }
	}
}
