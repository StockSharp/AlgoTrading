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
/// Contrarian pattern strategy converted from the MetaTrader expert "open_close".
/// It evaluates relationships between consecutive candle opens and closes.
/// </summary>
public class OpenCloseStrategy : Strategy
{
	private readonly StrategyParam<decimal> _initialVolume;
	private readonly StrategyParam<decimal> _maximumRisk;
	private readonly StrategyParam<decimal> _decreaseFactor;
	private readonly StrategyParam<decimal> _minimumVolume;
	private readonly StrategyParam<DataType> _candleType;

	private bool _hasPreviousCandle;
	private decimal _previousOpen;
	private decimal _previousClose;

	private decimal _signedPosition;
	private Sides? _lastEntrySide;
	private decimal _lastEntryPrice;
	private int _consecutiveLosses;

	public OpenCloseStrategy()
	{
		_initialVolume = Param(nameof(InitialVolume), 0.1m)
			.SetDisplay("Initial Volume", "Fallback lot size used when risk sizing is unavailable.", "Risk");

		_maximumRisk = Param(nameof(MaximumRisk), 0.3m)
			.SetDisplay("Maximum Risk", "Fraction of account equity used for position sizing and drawdown exit.", "Risk");

		_decreaseFactor = Param(nameof(DecreaseFactor), 100m)
			.SetDisplay("Decrease Factor", "Lot reduction factor applied after consecutive losing trades.", "Risk");

		_minimumVolume = Param(nameof(MinimumVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Minimum Volume", "Lower bound for trade volume calculations.", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Time-frame used to evaluate the open/close pattern.", "Data");
	}

	/// <summary>
	/// Fallback volume used when risk-based sizing cannot be computed.
	/// </summary>
	public decimal InitialVolume
	{
		get => _initialVolume.Value;
		set => _initialVolume.Value = value;
	}

	/// <summary>
	/// Fraction of portfolio value that defines position size and drawdown threshold.
	/// </summary>
	public decimal MaximumRisk
	{
		get => _maximumRisk.Value;
		set => _maximumRisk.Value = value;
	}

	/// <summary>
	/// Reduction factor applied after multiple consecutive losing trades.
	/// </summary>
	public decimal DecreaseFactor
	{
		get => _decreaseFactor.Value;
		set => _decreaseFactor.Value = value;
	}
	/// <summary>
	/// Minimal volume allowed for any generated order.
	/// </summary>
	public decimal MinimumVolume
	{
		get => _minimumVolume.Value;
		set => _minimumVolume.Value = value;
	}


	/// <summary>
	/// Candle series used to evaluate the pattern.
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

		_hasPreviousCandle = false;
		_previousOpen = 0m;
		_previousClose = 0m;

		_signedPosition = 0m;
		_lastEntrySide = null;
		_lastEntryPrice = 0m;
		_consecutiveLosses = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade.Order == null || trade.Trade.Security != Security)
			return;

		var volume = trade.Trade.Volume;
		if (volume <= 0m)
			return;

		var delta = trade.Order.Side == Sides.Buy ? volume : -volume;
		var previousPosition = _signedPosition;
		_signedPosition += delta;

		if (previousPosition == 0m && _signedPosition != 0m)
		{
			// Store entry direction and price to evaluate the next exit result.
			_lastEntrySide = trade.Order.Side;
			_lastEntryPrice = trade.Trade.Price;
		}
		else if (previousPosition != 0m && _signedPosition == 0m)
		{
			if (_lastEntrySide != null && _lastEntryPrice != 0m)
			{
				var exitPrice = trade.Trade.Price;
				var profit = _lastEntrySide == Sides.Buy
					? exitPrice - _lastEntryPrice
					: _lastEntryPrice - exitPrice;

				if (profit > 0m)
					_consecutiveLosses = 0;
				else if (profit < 0m)
					_consecutiveLosses++;
			}

			_lastEntrySide = null;
			_lastEntryPrice = 0m;
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var open = candle.OpenPrice;
		var close = candle.ClosePrice;

		if (!_hasPreviousCandle)
		{
			// Wait for the very first completed candle to establish a reference.
			_previousOpen = open;
			_previousClose = close;
			_hasPreviousCandle = true;
			return;
		}

		if (Position == 0m)
		{
			TryEnter(open, close);
		}
		else
		{
			if (TryCloseForRisk(close))
			{
				_previousOpen = open;
				_previousClose = close;
				return;
			}

			TryExit(open, close);
		}

		_previousOpen = open;
		_previousClose = close;
	}

	private void TryEnter(decimal open, decimal close)
	{
		// Mirror the original MetaTrader buy setup: fade a bearish candle that opened above the previous open.
		if (open > _previousOpen && close < _previousClose)
		{
			var volume = CalculateOrderVolume();
			if (volume > 0m)
				BuyMarket(volume);
			return;
		}

		// Sell when the candle opens below the previous bar and closes higher, fading the bullish move.
		if (open < _previousOpen && close > _previousClose)
		{
			var volume = CalculateOrderVolume();
			if (volume > 0m)
				SellMarket(volume);
		}
	}

	private void TryExit(decimal open, decimal close)
	{
		if (Position > 0m)
		{
			// Long positions close on a new bearish continuation.
			if (open < _previousOpen && close < _previousClose)
				SellMarket(Position);
		}
		else if (Position < 0m)
		{
			// Short positions close on a bullish continuation.
			if (open > _previousOpen && close > _previousClose)
				BuyMarket(-Position);
		}
	}

	private bool TryCloseForRisk(decimal price)
	{
		if (MaximumRisk <= 0m || Position == 0m)
			return false;

		if (PositionPrice is not decimal entryPrice)
			return false;

		// Floating PnL expressed in currency units (positive for profit, negative for loss).
		var floating = Position * (price - entryPrice);
		if (floating >= 0m)
			return false;

		var accountValue = GetAccountValue();
		if (accountValue <= 0m)
			return false;

		var lossThreshold = accountValue * MaximumRisk;
		if (-floating < lossThreshold)
			return false;

		// Close the entire position once the floating loss exceeds the allowed threshold.
		ClosePosition();
		return true;
	}

	private decimal CalculateOrderVolume()
	{
		var volume = InitialVolume > 0m ? InitialVolume : MinimumVolume;

		var accountValue = GetAccountValue();
		if (accountValue > 0m && MaximumRisk > 0m)
		{
			var riskVolume = accountValue * MaximumRisk / 1000m;
			riskVolume = Math.Round(riskVolume, 5, MidpointRounding.AwayFromZero);

			if (riskVolume > 0m)
				volume = riskVolume;
		}

		if (DecreaseFactor > 0m && _consecutiveLosses > 1)
		{
			var reduction = volume * _consecutiveLosses / DecreaseFactor;
			var adjusted = volume - reduction;
			volume = Math.Round(adjusted, 1, MidpointRounding.AwayFromZero);
		}

		if (volume < MinimumVolume)
			volume = MinimumVolume;

		return AdjustVolume(volume);
	}

	private decimal AdjustVolume(decimal volume)
	{
		var security = Security;
		if (security == null)
			return volume > 0m ? volume : MinimumVolume;

		var step = security.VolumeStep ?? 0m;
		if (step > 0m)
		{
			var steps = Math.Max(1m, Math.Round(volume / step, MidpointRounding.AwayFromZero));
			volume = steps * step;
		}

		var minVolume = security.MinVolume ?? 0m;
		if (minVolume > 0m && volume < minVolume)
			volume = minVolume;

		var maxVolume = security.MaxVolume ?? 0m;
		if (maxVolume > 0m && volume > maxVolume)
			volume = maxVolume;

		return volume > 0m ? volume : MinimumVolume;
	}

	private decimal GetAccountValue()
	{
		var portfolio = Portfolio;
		if (portfolio == null)
			return 0m;

		if (portfolio.CurrentValue is decimal current && current > 0m)
			return current;

		if (portfolio.BeginValue is decimal begin && begin > 0m)
			return begin;

		return 0m;
	}
}

