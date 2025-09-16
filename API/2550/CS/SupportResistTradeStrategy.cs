using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Breakout strategy that trades when price moves beyond recent support/resistance with EMA trend filter and pip-based trailing stops.
/// </summary>
public class SupportResistTradeStrategy : Strategy
{
	private readonly StrategyParam<int> _lookback;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _orderVolume;

	private ExponentialMovingAverage _ema;
	private Highest _highest;
	private Lowest _lowest;

	private decimal? _prevSupport;
	private decimal? _prevResistance;

	private decimal? _longStop;
	private decimal? _shortStop;

	private TrendDirection _trend = TrendDirection.None;
	private decimal _pipSize;
	private bool _levelsInitialized;

	/// <summary>
	/// Number of candles used to build swing levels.
	/// </summary>
	public int Lookback
	{
		get => _lookback.Value;
		set => _lookback.Value = value;
	}

	/// <summary>
	/// Exponential moving average length for trend detection.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Default order volume.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="SupportResistTradeStrategy"/>.
	/// </summary>
	public SupportResistTradeStrategy()
	{
		_lookback = Param(nameof(Lookback), 55)
		.SetGreaterThanZero()
		.SetDisplay("Lookback", "Candles used for support and resistance", "Parameters")
		.SetCanOptimize(true);

		_maPeriod = Param(nameof(MaPeriod), 500)
		.SetGreaterThanZero()
		.SetDisplay("EMA Period", "Length of EMA trend filter", "Indicators")
		.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");

		_orderVolume = Param(nameof(OrderVolume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Order Volume", "Default order volume", "Trading");
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

		_ema = default;
		_highest = default;
		_lowest = default;

		_prevSupport = default;
		_prevResistance = default;
		_longStop = default;
		_shortStop = default;

		_trend = TrendDirection.None;
		_pipSize = 0m;
		_levelsInitialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = OrderVolume;

		// Prepare indicators for EMA trend and swing levels.
		_ema = new ExponentialMovingAverage { Length = MaPeriod };
		_highest = new Highest { Length = Lookback };
		_lowest = new Lowest { Length = Lookback };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_ema, _highest, _lowest, ProcessCandle)
		.Start();

		// Calculate pip size similar to MetaTrader adjustment for 3/5 digit quotes.
		_pipSize = Security?.PriceStep ?? 0.0001m;
		if (Security?.Decimals is 3 or 5)
		_pipSize *= 10m;

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema);
			DrawIndicator(area, _highest);
			DrawIndicator(area, _lowest);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue, decimal highestValue, decimal lowestValue)
	{
		// Use only completed candles for trading decisions.
		if (candle.State != CandleStates.Finished)
		return;

		if (!_ema.IsFormed || !_highest.IsFormed || !_lowest.IsFormed)
		return;

		var support = lowestValue;
		var resistance = highestValue;

		if (!_levelsInitialized)
		{
			_prevSupport = support;
			_prevResistance = resistance;
			_levelsInitialized = true;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevSupport = support;
			_prevResistance = resistance;
			return;
		}

		// Update trend direction using candle open price against EMA.
		if (candle.OpenPrice > emaValue)
		{
			_trend = TrendDirection.Bullish;
		}
		else if (candle.OpenPrice < emaValue)
		{
			_trend = TrendDirection.Bearish;
		}

		var exitPlaced = ManagePosition(candle);

		if (!exitPlaced && Position == 0)
		{
			if (_trend == TrendDirection.Bullish && _prevResistance.HasValue && candle.ClosePrice > _prevResistance.Value)
			{
				// Breakout above resistance in bullish trend opens long position.
				BuyMarket();
				_longStop = _prevSupport;
				_shortStop = null;
			}
			else if (_trend == TrendDirection.Bearish && _prevSupport.HasValue && candle.ClosePrice < _prevSupport.Value)
			{
				// Breakout below support in bearish trend opens short position.
				SellMarket();
				_shortStop = _prevResistance;
				_longStop = null;
			}
		}

		_prevSupport = support;
		_prevResistance = resistance;
	}

	private bool ManagePosition(ICandleMessage candle)
	{
		if (Position > 0)
		{
			if (_longStop.HasValue && candle.ClosePrice <= _longStop.Value)
			{
				// Close long when trailing stop level is breached.
				SellMarket(Position);
				_longStop = null;
				return true;
			}

			var entry = PositionPrice;
			var profitPerUnit = candle.ClosePrice - entry;

			if (profitPerUnit > 0m && _prevSupport.HasValue && candle.ClosePrice < _prevSupport.Value)
			{
				// Exit profitable long on drop below refreshed support.
				SellMarket(Position);
				_longStop = null;
				return true;
			}

			UpdateLongTrailing(candle.ClosePrice, entry);
		}
		else if (Position < 0)
		{
			if (_shortStop.HasValue && candle.ClosePrice >= _shortStop.Value)
			{
				// Close short when trailing stop level is breached.
				BuyMarket(Math.Abs(Position));
				_shortStop = null;
				return true;
			}

			var entry = PositionPrice;
			var profitPerUnit = entry - candle.ClosePrice;

			if (profitPerUnit > 0m && _prevResistance.HasValue && candle.ClosePrice > _prevResistance.Value)
			{
				// Exit profitable short on rally above refreshed resistance.
				BuyMarket(Math.Abs(Position));
				_shortStop = null;
				return true;
			}

			UpdateShortTrailing(candle.ClosePrice, entry);
		}
		else
		{
			_longStop = null;
			_shortStop = null;
		}

		return false;
	}

	private void UpdateLongTrailing(decimal closePrice, decimal entry)
	{
		if (_pipSize <= 0m)
		return;

		var firstTrigger = entry + 20m * _pipSize;
		var secondTrigger = entry + 40m * _pipSize;
		var thirdTrigger = entry + 60m * _pipSize;

		var firstStop = entry + 10m * _pipSize;
		var secondStop = entry + 20m * _pipSize;
		var thirdStop = entry + 30m * _pipSize;

		if (closePrice > thirdTrigger && (!_longStop.HasValue || _longStop.Value < thirdStop))
		{
			// Lock in additional profit after strong bullish move.
			_longStop = thirdStop;
		}
		else if (closePrice > secondTrigger && (!_longStop.HasValue || _longStop.Value < secondStop))
		{
			_longStop = secondStop;
		}
		else if (closePrice > firstTrigger && (!_longStop.HasValue || _longStop.Value < firstStop))
		{
			_longStop = firstStop;
		}
	}

	private void UpdateShortTrailing(decimal closePrice, decimal entry)
	{
		if (_pipSize <= 0m)
		return;

		var firstTrigger = entry - 20m * _pipSize;
		var secondTrigger = entry - 40m * _pipSize;
		var thirdTrigger = entry - 60m * _pipSize;

		var firstStop = entry - 10m * _pipSize;
		var secondStop = entry - 20m * _pipSize;
		var thirdStop = entry - 30m * _pipSize;

		if (closePrice < thirdTrigger && (!_shortStop.HasValue || _shortStop.Value > thirdStop))
		{
			// Lock in additional profit after strong bearish move.
			_shortStop = thirdStop;
		}
		else if (closePrice < secondTrigger && (!_shortStop.HasValue || _shortStop.Value > secondStop))
		{
			_shortStop = secondStop;
		}
		else if (closePrice < firstTrigger && (!_shortStop.HasValue || _shortStop.Value > firstStop))
		{
			_shortStop = firstStop;
		}
	}

	private enum TrendDirection
	{
		None,
		Bullish,
		Bearish,
	}
}
