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

using StockSharp.Algo;

/// <summary>
/// Reimplementation of the MetaTrader expert advisor "OpenTiks" for StockSharp.
/// Detects four consecutive candles with strictly monotonic opens and highs to trigger entries,
/// then manages the position with optional stop-loss, trailing stop and progressive partial exits.
/// </summary>
public class OpenTiksStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<int> _maxOrders;
	private readonly StrategyParam<bool> _usePartialClose;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _priceStep;
	private decimal _volumeStep;
	private decimal _minVolumeLimit;
	private decimal _maxVolumeLimit;

	private decimal? _high1;
	private decimal? _high2;
	private decimal? _high3;

	private decimal? _open1;
	private decimal? _open2;
	private decimal? _open3;

	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal? _longTrailingStop;
	private decimal? _shortTrailingStop;

	private decimal _previousPosition;
	private decimal? _lastTradePrice;

	/// <summary>
	/// Initializes a new instance of the <see cref="OpenTiksStrategy"/> class.
	/// </summary>
	public OpenTiksStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Volume of each market entry in lots.", "Trading");

		_stopLossPoints = Param(nameof(StopLossPoints), 0m)
			.SetNotNegative()
			.SetDisplay("Stop Loss (points)", "Protective stop distance expressed in price points.", "Risk");

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 30m)
			.SetNotNegative()
			.SetDisplay("Trailing Stop (points)", "Trailing distance expressed in price points.", "Risk");

		_maxOrders = Param(nameof(MaxOrders), 1)
			.SetNotNegative()
			.SetDisplay("Max Orders", "Maximum number of simultaneously open entries. Zero disables the limit.", "Trading");

		_usePartialClose = Param(nameof(UsePartialClose), true)
			.SetDisplay("Use Partial Close", "Close half of the position whenever the trailing stop advances.", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe used for pattern detection.", "General");
	}

	/// <summary>
	/// Order volume used for every market entry.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set
		{
			_orderVolume.Value = value;
			Volume = value;
		}
	}

	/// <summary>
	/// Stop-loss distance expressed in price points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in price points.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Maximum number of simultaneously open entries.
	/// </summary>
	public int MaxOrders
	{
		get => _maxOrders.Value;
		set => _maxOrders.Value = value;
	}

	/// <summary>
	/// Enables progressive partial exits when true.
	/// </summary>
	public bool UsePartialClose
	{
		get => _usePartialClose.Value;
		set => _usePartialClose.Value = value;
	}

	/// <summary>
	/// Candle type requested from the market data feed.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_high1 = null;
		_high2 = null;
		_high3 = null;
		_open1 = null;
		_open2 = null;
		_open3 = null;
		_longEntryPrice = null;
		_shortEntryPrice = null;
		_longTrailingStop = null;
		_shortTrailingStop = null;
		_previousPosition = 0m;
		_lastTradePrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var security = Security;
		_priceStep = security?.PriceStep ?? 1m;
		if (_priceStep <= 0m)
			_priceStep = 1m;

		_volumeStep = security?.VolumeStep ?? 0m;
		_minVolumeLimit = security?.MinVolume ?? 0m;
		_maxVolumeLimit = security?.MaxVolume ?? 0m;

		Volume = NormalizeEntryVolume(OrderVolume);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		_lastTradePrice = trade.Trade?.Price ?? trade.Order.Price;
	}

	/// <inheritdoc />
	protected override void OnPositionReceived(Position position)
	{
		base.OnPositionReceived(position);

		if (Position > 0m)
		{
			if (_previousPosition <= 0m)
			{
			_longEntryPrice = _lastTradePrice;
			_longTrailingStop = null;
			_shortEntryPrice = null;
			_shortTrailingStop = null;
			}
			else if (delta > 0m && _lastTradePrice is decimal priceLong)
			{
			var previousVolume = Math.Max(0m, _previousPosition);
			var currentVolume = Math.Max(0m, Position);
			if (currentVolume > 0m)
			{
			var currentEntry = _longEntryPrice ?? priceLong;
			_longEntryPrice = (currentEntry * previousVolume + priceLong * delta) / currentVolume;
			}
			}

			if (Position <= 0m)
			{
			_longEntryPrice = null;
			_longTrailingStop = null;
			}
		}
		else if (Position < 0m)
		{
			if (_previousPosition >= 0m)
			{
			_shortEntryPrice = _lastTradePrice;
			_shortTrailingStop = null;
			_longEntryPrice = null;
			_longTrailingStop = null;
			}
			else if (delta < 0m && _lastTradePrice is decimal priceShort)
			{
			var previousVolume = Math.Max(0m, Math.Abs(_previousPosition));
			var currentVolume = Math.Max(0m, Math.Abs(Position));
			if (currentVolume > 0m)
			{
			var currentEntry = _shortEntryPrice ?? priceShort;
			_shortEntryPrice = (currentEntry * previousVolume + priceShort * Math.Abs(delta)) / currentVolume;
			}
			}

			if (Position >= 0m)
			{
			_shortEntryPrice = null;
			_shortTrailingStop = null;
			}
		}
		else
		{
			_longEntryPrice = null;
			_shortEntryPrice = null;
			_longTrailingStop = null;
			_shortTrailingStop = null;
		}

		_previousPosition = Position;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateTrailing(candle);

		var buySignal = false;
		var sellSignal = false;

		if (_high1 is decimal h1 && _high2 is decimal h2 && _high3 is decimal h3 &&
		_open1 is decimal o1 && _open2 is decimal o2 && _open3 is decimal o3)
		{
		var high = candle.HighPrice;
		var open = candle.OpenPrice;

		buySignal = high > h1 && h1 > h2 && h2 > h3 &&
		open > o1 && o1 > o2 && o2 > o3;

		sellSignal = high < h1 && h1 < h2 && h2 < h3 &&
		open < o1 && o1 < o2 && o2 < o3;
		}

		_high3 = _high2;
		_high2 = _high1;
		_high1 = candle.HighPrice;

		_open3 = _open2;
		_open2 = _open1;
		_open1 = candle.OpenPrice;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (buySignal)
			TryEnterLong();

		if (sellSignal)
			TryEnterShort();
	}

	private void TryEnterLong()
	{
		if (MaxOrders > 0 && EstimateOrdersCount(Position) >= MaxOrders)
			return;

		var volume = NormalizeEntryVolume(OrderVolume);
		if (volume <= 0m)
			return;

		BuyMarket(volume);
	}

	private void TryEnterShort()
	{
		if (MaxOrders > 0 && EstimateOrdersCount(Position) >= MaxOrders)
			return;

		var volume = NormalizeEntryVolume(OrderVolume);
		if (volume <= 0m)
			return;

		SellMarket(volume);
	}

	private int EstimateOrdersCount(decimal positionVolume)
	{
		var baseVolume = NormalizeEntryVolume(OrderVolume);
		if (baseVolume <= 0m)
			return positionVolume != 0m ? 1 : 0;

		var ratio = Math.Abs(positionVolume) / baseVolume;
		if (ratio <= 0m)
			return 0;

		return (int)Math.Ceiling(ratio);
	}

	private void UpdateTrailing(ICandleMessage candle)
	{
		var close = candle.ClosePrice;
		var low = candle.LowPrice;
		var high = candle.HighPrice;

		var stopDistance = StopLossPoints * _priceStep;
		var trailingDistance = TrailingStopPoints * _priceStep;

		if (Position > 0m && _longEntryPrice is decimal entryLong)
		{
			if (stopDistance > 0m && low <= entryLong - stopDistance)
			{
			SellMarket(Position);
			return;
			}

			if (trailingDistance > 0m && close - entryLong >= trailingDistance)
			{
			var desiredStop = close - trailingDistance;
			if (_longTrailingStop is not decimal currentStop || desiredStop > currentStop)
			{
			_longTrailingStop = desiredStop;
			TryReduceLongPosition();
			}

			if (_longTrailingStop is decimal trailingStop && low <= trailingStop)
			SellMarket(Position);
			}
		}
		else if (Position < 0m && _shortEntryPrice is decimal entryShort)
		{
			var positionVolume = Math.Abs(Position);

			if (stopDistance > 0m && high >= entryShort + stopDistance)
			{
			BuyMarket(positionVolume);
			return;
			}

			if (trailingDistance > 0m && entryShort - close >= trailingDistance)
			{
			var desiredStop = close + trailingDistance;
			if (_shortTrailingStop is not decimal currentStop || desiredStop < currentStop)
			{
			_shortTrailingStop = desiredStop;
			TryReduceShortPosition();
			}

			if (_shortTrailingStop is decimal trailingStop && high >= trailingStop)
			BuyMarket(positionVolume);
			}
		}
	}

	private void TryReduceLongPosition()
	{
		if (!UsePartialClose)
			return;

		if (Position <= 0m)
			return;

		var positionVolume = Position;
		var half = positionVolume / 2m;
		var normalizedHalf = NormalizeExitVolume(half, positionVolume);

		if (_minVolumeLimit > 0m && normalizedHalf < _minVolumeLimit)
		{
			SellMarket(positionVolume);
			return;
		}

		if (normalizedHalf > 0m)
		SellMarket(normalizedHalf);
	}

	private void TryReduceShortPosition()
	{
		if (!UsePartialClose)
			return;

		if (Position >= 0m)
			return;

		var positionVolume = Math.Abs(Position);
		var half = positionVolume / 2m;
		var normalizedHalf = NormalizeExitVolume(half, positionVolume);

		if (_minVolumeLimit > 0m && normalizedHalf < _minVolumeLimit)
		{
			BuyMarket(positionVolume);
			return;
		}

		if (normalizedHalf > 0m)
		BuyMarket(normalizedHalf);
	}

	private decimal NormalizeEntryVolume(decimal volume)
	{
		if (volume <= 0m)
		return 0m;

		if (_volumeStep > 0m)
		{
			var steps = Math.Round(volume / _volumeStep, MidpointRounding.AwayFromZero);
			if (steps <= 0m)
			steps = 1m;
			volume = steps * _volumeStep;
		}

		if (_minVolumeLimit > 0m && volume < _minVolumeLimit)
		volume = _minVolumeLimit;

		if (_maxVolumeLimit > 0m && volume > _maxVolumeLimit)
		volume = _maxVolumeLimit;

		return volume;
	}

	private decimal NormalizeExitVolume(decimal desired, decimal currentPosition)
	{
		if (desired <= 0m || currentPosition <= 0m)
		return 0m;

		var volume = desired;

		if (_volumeStep > 0m)
		{
			var steps = Math.Round(volume / _volumeStep, MidpointRounding.AwayFromZero);
			if (steps <= 0m)
			steps = 1m;
			volume = steps * _volumeStep;
		}

		if (volume > currentPosition)
		volume = currentPosition;

		return volume;
	}
}
