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
/// Port of the MetaTrader expert TrailingStopFrCn.
/// Applies fractal or candle trailing stops with optional fixed distance control.
/// </summary>
public class TrailingStopFrCnStrategy : Strategy
{
	private readonly StrategyParam<bool> _onlyProfit;
	private readonly StrategyParam<bool> _onlyWithoutLoss;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _minStopDistancePips;
	private readonly StrategyParam<TrailingSources> _trailingMode;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<CandleSnapshot> _candles = new();
	private readonly List<decimal> _fractalLows = new();
	private readonly List<decimal> _fractalHighs = new();

	private Order _stopOrder;
	private decimal? _currentStopPrice;

	private decimal _pipSize;
	private decimal _lastClosePrice;
	private decimal _bestBid;
	private decimal _bestAsk;
	private bool _hasBestBid;
	private bool _hasBestAsk;
	private decimal _lastPosition;

	/// <summary>
	/// Moves the stop-loss only after the trade becomes profitable.
	/// </summary>
	public bool OnlyProfit
	{
		get => _onlyProfit.Value;
		set => _onlyProfit.Value = value;
	}

	/// <summary>
	/// Stops trailing once the stop-loss protects the position from losses.
	/// </summary>
	public bool OnlyWithoutLoss
	{
		get => _onlyWithoutLoss.Value;
		set => _onlyWithoutLoss.Value = value;
	}

	/// <summary>
	/// Fixed trailing distance in pips. Set to zero to trail via fractals or candle extremes.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Minimal distance in pips between current price and stop-loss.
	/// </summary>
	public decimal MinStopDistancePips
	{
		get => _minStopDistancePips.Value;
		set => _minStopDistancePips.Value = value;
	}

	/// <summary>
	/// Trailing source when the fixed trailing distance is disabled.
	/// </summary>
	public TrailingSources TrailingMode
	{
		get => _trailingMode.Value;
		set => _trailingMode.Value = value;
	}

	/// <summary>
	/// Candle type used for fractal or swing calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public TrailingStopFrCnStrategy()
	{
		_onlyProfit = Param(nameof(OnlyProfit), true)
			.SetDisplay("Only In Profit", "Advance stop-loss only once the trade is profitable", "Risk");

		_onlyWithoutLoss = Param(nameof(OnlyWithoutLoss), false)
			.SetDisplay("Stop At Break-Even", "Do not trail further once stop guarantees no loss", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 0m)
			.SetDisplay("Trailing Stop (pips)", "Fixed trailing distance expressed in pips", "Trailing");

		_minStopDistancePips = Param(nameof(MinStopDistancePips), 0m)
			.SetDisplay("Min Stop Distance (pips)", "Broker enforced minimal stop distance", "Trailing");

		_trailingMode = Param(nameof(TrailingMode), TrailingSources.Candles)
			.SetDisplay("Trailing Mode", "Data source for adaptive trailing", "Trailing");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Candles used for trailing calculations", "Data");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_candles.Clear();
		_fractalLows.Clear();
		_fractalHighs.Clear();

		ResetStopOrder();

		_pipSize = 0m;
		_lastClosePrice = 0m;
		_bestBid = 0m;
		_bestAsk = 0m;
		_hasBestBid = false;
		_hasBestAsk = false;
		_lastPosition = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = GetPipSize();

		var candleSubscription = SubscribeCandles(CandleType);
		candleSubscription
			.Bind(ProcessCandle)
			.Start();

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, candleSubscription);
			DrawOwnTrades(area);
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		var previousPosition = _lastPosition;
		_lastPosition = Position;

		if (Position == 0m)
		{
			ResetStopOrder();
			return;
		}

		if (previousPosition == 0m || Math.Sign(previousPosition) != Math.Sign(Position))
		{
			ResetStopOrder();
		}

		TryUpdateTrailing();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_lastClosePrice = candle.ClosePrice;

		RegisterCandle(candle);
		TryUpdateTrailing();
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		if (message.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidValue))
		{
			var bid = (decimal)bidValue;
			if (bid > 0m)
			{
				_bestBid = bid;
				_hasBestBid = true;
			}
		}

		if (message.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askValue))
		{
			var ask = (decimal)askValue;
			if (ask > 0m)
			{
				_bestAsk = ask;
				_hasBestAsk = true;
			}
		}

		TryUpdateTrailing();
	}

	private void TryUpdateTrailing()
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var position = Position;
		var volume = Math.Abs(position);
		if (volume <= 0m)
		{
			ResetStopOrder();
			return;
		}

		var entryPrice = PositionPrice;
		if (entryPrice == null || entryPrice <= 0m)
			entryPrice = _lastClosePrice > 0m ? _lastClosePrice : null;

		if (position > 0m)
		{
			var price = _hasBestBid ? _bestBid : _lastClosePrice;
			if (price <= 0m)
				return;

			var candidate = CalculateLongStop(price);
			if (candidate is null)
				return;

			if (OnlyProfit && entryPrice is decimal ep && candidate.Value <= ep)
				return;

			if (OnlyWithoutLoss && _currentStopPrice is decimal existingStop && entryPrice is decimal ep2 && existingStop >= ep2 && existingStop != 0m)
				return;

			if (_currentStopPrice is decimal current && current >= candidate.Value)
				return;

			UpdateStopOrder(true, candidate.Value, volume);
		}
		else if (position < 0m)
		{
			var price = _hasBestAsk ? _bestAsk : _lastClosePrice;
			if (price <= 0m)
				return;

			var candidate = CalculateShortStop(price);
			if (candidate is null)
				return;

			if (OnlyProfit && entryPrice is decimal ep && candidate.Value >= ep)
				return;

			if (OnlyWithoutLoss && _currentStopPrice is decimal existingStop && entryPrice is decimal ep2 && existingStop >= ep2 && existingStop != 0m)
				return;

			if (_currentStopPrice is decimal current && current != 0m && current <= candidate.Value)
				return;

			UpdateStopOrder(false, candidate.Value, volume);
		}
	}

	private decimal? CalculateLongStop(decimal price)
	{
		var trailingPips = TrailingStopPips;
		if (trailingPips > 0m && _pipSize > 0m)
		{
			var distance = trailingPips * _pipSize;
			if (distance > 0m)
				return price - distance;
		}

		var minOffset = GetMinStopOffset();

		if (TrailingMode == TrailingSources.Fractals)
		{
			for (var i = _fractalLows.Count - 1; i >= 0; i--)
			{
				var level = _fractalLows[i];
				if (level > 0m && price - level > minOffset)
					return level;
			}
		}
		else
		{
			var maxLookback = Math.Min(99, _candles.Count - 1);
			for (var offset = 1; offset <= maxLookback; offset++)
			{
				var index = _candles.Count - 1 - offset;
				if (index < 0)
					break;

				var low = _candles[index].Low;
				if (low > 0m && price - low > minOffset)
					return low;
			}
		}

		return null;
	}

	private decimal? CalculateShortStop(decimal price)
	{
		var trailingPips = TrailingStopPips;
		if (trailingPips > 0m && _pipSize > 0m)
		{
			var distance = trailingPips * _pipSize;
			if (distance > 0m)
				return price + distance;
		}

		var minOffset = GetMinStopOffset();

		if (TrailingMode == TrailingSources.Fractals)
		{
			for (var i = _fractalHighs.Count - 1; i >= 0; i--)
			{
				var level = _fractalHighs[i];
				if (level > 0m && level - price > minOffset)
					return level;
			}
		}
		else
		{
			var maxLookback = Math.Min(99, _candles.Count - 1);
			for (var offset = 1; offset <= maxLookback; offset++)
			{
				var index = _candles.Count - 1 - offset;
				if (index < 0)
					break;

				var high = _candles[index].High;
				if (high > 0m && high - price > minOffset)
					return high;
			}
		}

		return null;
	}

	private decimal GetMinStopOffset()
	{
		if (_pipSize <= 0m)
			return 0m;

		var minPips = Math.Max(MinStopDistancePips, 0m);
		return minPips * _pipSize;
	}

	private void UpdateStopOrder(bool isLongPosition, decimal stopPrice, decimal volume)
	{
		if (volume <= 0m)
			return;

		if (_stopOrder != null && _stopOrder.State == OrderStates.Active && _stopOrder.Price == stopPrice && _stopOrder.Volume == volume)
			return;

		if (_stopOrder != null && _stopOrder.State == OrderStates.Active)
			CancelOrder(_stopOrder);

		_stopOrder = isLongPosition
			? SellStop(price: stopPrice, volume: volume)
			: BuyStop(price: stopPrice, volume: volume);

		_currentStopPrice = stopPrice;
	}

	private void ResetStopOrder()
	{
		if (_stopOrder != null && _stopOrder.State == OrderStates.Active)
			CancelOrder(_stopOrder);

		_stopOrder = null;
		_currentStopPrice = null;
	}

	private decimal GetPipSize()
	{
		var step = Security?.PriceStep ?? 0.0001m;
		var decimals = Security?.Decimals ?? 0;

		if (decimals >= 3)
			return step * 10m;

		return step > 0m ? step : 0.0001m;
	}

	private void RegisterCandle(ICandleMessage candle)
	{
		_candles.Add(new CandleSnapshot(candle));

		if (_candles.Count > 300)
			_candles.RemoveRange(0, _candles.Count - 300);

		if (_candles.Count < 5)
			return;

		var centerIndex = _candles.Count - 3;
		var c0 = _candles[centerIndex - 2];
		var c1 = _candles[centerIndex - 1];
		var c2 = _candles[centerIndex];
		var c3 = _candles[centerIndex + 1];
		var c4 = _candles[centerIndex + 2];

		if (c2.Low < c0.Low && c2.Low < c1.Low && c2.Low < c3.Low && c2.Low < c4.Low)
			RegisterFractalLow(c2.Low);

		if (c2.High > c0.High && c2.High > c1.High && c2.High > c3.High && c2.High > c4.High)
			RegisterFractalHigh(c2.High);
	}

	private void RegisterFractalLow(decimal price)
	{
		if (price <= 0m)
			return;

		_fractalLows.Add(price);
		if (_fractalLows.Count > 100)
			_fractalLows.RemoveRange(0, _fractalLows.Count - 100);
	}

	private void RegisterFractalHigh(decimal price)
	{
		if (price <= 0m)
			return;

		_fractalHighs.Add(price);
		if (_fractalHighs.Count > 100)
			_fractalHighs.RemoveRange(0, _fractalHighs.Count - 100);
	}

	private enum TrailingSources
	{
		Fractals,
		Candles,
	}

	private readonly struct CandleSnapshot
	{
		public CandleSnapshot(ICandleMessage candle)
		{
			Time = candle.OpenTime;
			High = candle.HighPrice;
			Low = candle.LowPrice;
			Close = candle.ClosePrice;
		}

		public DateTimeOffset Time { get; }
		public decimal High { get; }
		public decimal Low { get; }
		public decimal Close { get; }
	}
}
