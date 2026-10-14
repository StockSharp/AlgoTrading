using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Outside bar strategy.
/// A candle whose high is above the previous high and whose low is below the previous low is an outside bar, bullish when it closes
/// above its open and bearish otherwise. An entry is placed inside the bar, EntryPercentage of its range back from the extreme in the
/// signal direction. The stop sits StopLossOffset price steps beyond the opposite extreme and the target TpPercentage of the range
/// from the entry. At PartialRR times the risk PartialExitPercent of the position is closed and the stop moves to breakeven.
/// </summary>
public class OutsideBarStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _entryPercentage;
	private readonly StrategyParam<decimal> _tpPercentage;
	private readonly StrategyParam<decimal> _partialRr;
	private readonly StrategyParam<decimal> _partialExitPercent;
	private readonly StrategyParam<decimal> _stopLossOffset;

	private decimal? _prevHigh;
	private decimal? _prevLow;

	private int _pendingDirection;
	private decimal _pendingEntry;
	private decimal _pendingStop;
	private decimal _pendingTake;

	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _takePrice;
	private decimal _partialPrice;
	private bool _partialDone;

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Fraction of the bar range the entry sits back from the extreme in the signal direction.
	/// </summary>
	public decimal EntryPercentage
	{
		get => _entryPercentage.Value;
		set => _entryPercentage.Value = value;
	}

	/// <summary>
	/// Take profit distance as a fraction of the bar range.
	/// </summary>
	public decimal TpPercentage
	{
		get => _tpPercentage.Value;
		set => _tpPercentage.Value = value;
	}

	/// <summary>
	/// Reward to risk ratio of the partial exit.
	/// </summary>
	public decimal PartialRR
	{
		get => _partialRr.Value;
		set => _partialRr.Value = value;
	}

	/// <summary>
	/// Fraction of the position closed at the partial exit.
	/// </summary>
	public decimal PartialExitPercent
	{
		get => _partialExitPercent.Value;
		set => _partialExitPercent.Value = value;
	}

	/// <summary>
	/// Stop distance beyond the bar in price steps.
	/// </summary>
	public decimal StopLossOffset
	{
		get => _stopLossOffset.Value;
		set => _stopLossOffset.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public OutsideBarStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_entryPercentage = Param(nameof(EntryPercentage), 0.5m)
			.SetRange(0m, 1m)
			.SetDisplay("Entry %", "Fraction of the bar range the entry sits back from the extreme", "Trading");

		_tpPercentage = Param(nameof(TpPercentage), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit %", "Take profit distance as a fraction of the bar range", "Risk");

		_partialRr = Param(nameof(PartialRR), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Partial RR", "Reward to risk ratio of the partial exit", "Risk");

		_partialExitPercent = Param(nameof(PartialExitPercent), 0.5m)
			.SetRange(0m, 1m)
			.SetDisplay("Partial Exit %", "Fraction of the position closed at the partial exit", "Risk");

		_stopLossOffset = Param(nameof(StopLossOffset), 10m)
			.SetNotNegative()
			.SetDisplay("Stop Offset", "Stop distance beyond the bar in price steps", "Risk");
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
		_prevHigh = null;
		_prevLow = null;
		_pendingDirection = 0;
		_pendingEntry = 0m;
		_pendingStop = 0m;
		_pendingTake = 0m;
		_entryPrice = 0m;
		_stopPrice = 0m;
		_takePrice = 0m;
		_partialPrice = 0m;
		_partialDone = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

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

		var prevHigh = _prevHigh;
		var prevLow = _prevLow;
		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position != 0)
		{
			ManagePosition(candle);
			return;
		}

		if (_pendingDirection != 0 && TryFillPending(candle))
			return;

		if (prevHigh is not decimal ph || prevLow is not decimal pl)
			return;

		if (candle.HighPrice <= ph || candle.LowPrice >= pl)
			return;

		var range = candle.HighPrice - candle.LowPrice;
		var offset = StopLossOffset * (Security?.PriceStep ?? 1m);

		// A newer outside bar replaces any unfilled entry.
		if (candle.ClosePrice > candle.OpenPrice)
		{
			_pendingDirection = 1;
			_pendingEntry = candle.HighPrice - range * EntryPercentage;
			_pendingStop = candle.LowPrice - offset;
			_pendingTake = _pendingEntry + range * TpPercentage;
		}
		else
		{
			_pendingDirection = -1;
			_pendingEntry = candle.LowPrice + range * EntryPercentage;
			_pendingStop = candle.HighPrice + offset;
			_pendingTake = _pendingEntry - range * TpPercentage;
		}
	}

	private bool TryFillPending(ICandleMessage candle)
	{
		if (candle.LowPrice > _pendingEntry || candle.HighPrice < _pendingEntry)
			return false;

		var risk = Math.Abs(_pendingEntry - _pendingStop);

		_entryPrice = _pendingEntry;
		_stopPrice = _pendingStop;
		_takePrice = _pendingTake;
		_partialPrice = _pendingDirection > 0 ? _entryPrice + risk * PartialRR : _entryPrice - risk * PartialRR;
		_partialDone = false;

		if (_pendingDirection > 0)
			BuyMarket(Volume);
		else
			SellMarket(Volume);

		_pendingDirection = 0;
		return true;
	}

	private void ManagePosition(ICandleMessage candle)
	{
		if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takePrice)
			{
				SellMarket(Position);
				return;
			}

			if (!_partialDone && candle.HighPrice >= _partialPrice)
			{
				_partialDone = true;
				_stopPrice = _entryPrice;

				var part = Position * PartialExitPercent;
				if (part > 0)
					SellMarket(Math.Min(part, Position));
			}
		}
		else
		{
			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takePrice)
			{
				BuyMarket(-Position);
				return;
			}

			if (!_partialDone && candle.LowPrice <= _partialPrice)
			{
				_partialDone = true;
				_stopPrice = _entryPrice;

				var part = -Position * PartialExitPercent;
				if (part > 0)
					BuyMarket(Math.Min(part, -Position));
			}
		}
	}
}
