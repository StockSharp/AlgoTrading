using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Channel breakout reversal strategy based on Donchian channel and ATR stops.
/// </summary>
public class TradeInChannelStrategy : Strategy
{
	private readonly StrategyParam<int> _channelPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<DataType> _candleType;

	private DonchianChannels _donchian = null!;
	private AverageTrueRange _atr = null!;

	private decimal? _previousUpper;
	private decimal? _previousLower;
	private decimal? _previousClose;

	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal? _longStop;
	private decimal? _shortStop;
	private decimal? _longBestPrice;
	private decimal? _shortBestPrice;
	private decimal? _longTrailingLevel;
	private decimal? _shortTrailingLevel;

	private decimal _priceStep = 1m;

	/// <summary>
	/// Donchian channel lookback.
	/// </summary>
	public int ChannelPeriod
	{
		get => _channelPeriod.Value;
		set => _channelPeriod.Value = value;
	}

	/// <summary>
	/// ATR calculation period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Order volume used for entries.
	/// </summary>
	public decimal Volume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in price steps.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Candle type for analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="TradeInChannelStrategy"/>.
	/// </summary>
	public TradeInChannelStrategy()
	{
		_channelPeriod = Param(nameof(ChannelPeriod), 20)
			.SetDisplay("Channel Period", "Donchian channel lookback", "Channel")
			.SetCanOptimize(true)
			.SetGreaterThanZero();

		_atrPeriod = Param(nameof(AtrPeriod), 4)
			.SetDisplay("ATR Period", "Average True Range length", "Volatility")
			.SetCanOptimize(true)
			.SetGreaterThanZero();

		_orderVolume = Param(nameof(Volume), 1m)
			.SetDisplay("Volume", "Order volume per trade", "Trading")
			.SetGreaterThanZero();

		_trailingStopPips = Param(nameof(TrailingStopPips), 30m)
			.SetDisplay("Trailing Stop (pips)", "Trailing stop distance in price steps", "Risk")
			.SetCanOptimize(true)
			.SetGreaterOrEqualZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for signals", "General");
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

		_donchian = null!;
		_atr = null!;

		_previousUpper = null;
		_previousLower = null;
		_previousClose = null;

		ResetLongState();
		ResetShortState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_priceStep = Security?.PriceStep is > 0m step ? step : 1m;

		_donchian = new DonchianChannels
		{
			Length = ChannelPeriod
		};

		_atr = new AverageTrueRange
		{
			Length = AtrPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_donchian, _atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _donchian);
			DrawIndicator(area, _atr);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue donchianValue, IIndicatorValue atrValue)
	{
		// Ignore unfinished candles to work only with confirmed data.
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var donchian = (DonchianChannelsValue)donchianValue;

		if (donchian.UpBand is not decimal upper || donchian.LowBand is not decimal lower)
		return;

		if (!atrValue.IsFinal)
		return;

		var atr = atrValue.ToDecimal();

		var previousUpper = _previousUpper;
		var previousLower = _previousLower;
		var previousClose = _previousClose;

		// Need at least one full bar history to evaluate pivots and channel stability.
		if (previousUpper is null || previousLower is null || previousClose is null)
		{
			_previousUpper = upper;
			_previousLower = lower;
			_previousClose = candle.ClosePrice;
			return;
		}

		var pivot = (upper + lower + previousClose.Value) / 3m;

		var closedLong = ManageLongPosition(candle, upper, previousUpper.Value);
		var closedShort = ManageShortPosition(candle, lower, previousLower.Value);

		if (Position == 0 && !closedLong && !closedShort)
		{
			EvaluateEntries(candle, upper, lower, previousUpper.Value, previousLower.Value, previousClose.Value, pivot, atr);
		}

		_previousUpper = upper;
		_previousLower = lower;
		_previousClose = candle.ClosePrice;
	}

	private bool ManageLongPosition(ICandleMessage candle, decimal upper, decimal previousUpper)
	{
		if (Position <= 0)
		return false;

		// Hard stop based on ATR.
		if (_longStop is decimal stop && candle.LowPrice <= stop)
		{
			SellMarket(Position);
			ResetLongState();
			return true;
		}

		// Exit when price breaks above a flat resistance level.
		if (upper == previousUpper && candle.HighPrice >= upper)
		{
			SellMarket(Position);
			ResetLongState();
			return true;
		}

		return ApplyLongTrailing(candle);
	}

	private bool ManageShortPosition(ICandleMessage candle, decimal lower, decimal previousLower)
	{
		if (Position >= 0)
		return false;

		// Hard stop based on ATR.
		if (_shortStop is decimal stop && candle.HighPrice >= stop)
		{
			BuyMarket(Math.Abs(Position));
			ResetShortState();
			return true;
		}

		// Exit when price breaks below a flat support level.
		if (lower == previousLower && candle.LowPrice <= lower)
		{
			BuyMarket(Math.Abs(Position));
			ResetShortState();
			return true;
		}

		return ApplyShortTrailing(candle);
	}

	private bool ApplyLongTrailing(ICandleMessage candle)
	{
		if (Position <= 0)
		return false;

		var offset = GetTrailingOffset();
		if (offset <= 0m || _longEntryPrice is not decimal entryPrice)
		{
			_longBestPrice = candle.HighPrice;
			return false;
		}

		_longBestPrice = _longBestPrice.HasValue
		? Math.Max(_longBestPrice.Value, candle.HighPrice)
		: candle.HighPrice;

		if (_longBestPrice is decimal best && best - entryPrice > offset)
		{
			var newLevel = best - offset;

			if (_longTrailingLevel is null || newLevel > _longTrailingLevel.Value)
			_longTrailingLevel = newLevel;

			if (_longTrailingLevel is decimal level && candle.LowPrice <= level)
			{
				SellMarket(Position);
				ResetLongState();
				return true;
			}
		}

		return false;
	}

	private bool ApplyShortTrailing(ICandleMessage candle)
	{
		if (Position >= 0)
		return false;

		var offset = GetTrailingOffset();
		if (offset <= 0m || _shortEntryPrice is not decimal entryPrice)
		{
			_shortBestPrice = candle.LowPrice;
			return false;
		}

		_shortBestPrice = _shortBestPrice.HasValue
		? Math.Min(_shortBestPrice.Value, candle.LowPrice)
		: candle.LowPrice;

		if (_shortBestPrice is decimal best && entryPrice - best > offset)
		{
			var newLevel = best + offset;

			if (_shortTrailingLevel is null || newLevel < _shortTrailingLevel.Value)
			_shortTrailingLevel = newLevel;

			if (_shortTrailingLevel is decimal level && candle.HighPrice >= level)
			{
				BuyMarket(Math.Abs(Position));
				ResetShortState();
				return true;
			}
		}

		return false;
	}

	private void EvaluateEntries(
		ICandleMessage candle,
	decimal upper,
	decimal lower,
	decimal previousUpper,
	decimal previousLower,
	decimal previousClose,
	decimal pivot,
	decimal atr)
	{
		var resistanceFlat = upper == previousUpper;
		var supportFlat = lower == previousLower;

		var shouldOpenShort = resistanceFlat &&
		(candle.HighPrice >= upper || (previousClose < upper && previousClose > pivot));

		var shouldOpenLong = supportFlat &&
		(candle.LowPrice <= lower || (previousClose > lower && previousClose < pivot));

		if (shouldOpenLong)
		{
			OpenLong(candle, lower, atr);
		}
		else if (shouldOpenShort)
		{
			OpenShort(candle, upper, atr);
		}
	}

	private void OpenLong(ICandleMessage candle, decimal support, decimal atr)
	{
		if (Volume <= 0m)
		return;

		BuyMarket(Volume);

		_longEntryPrice = candle.ClosePrice;
		_longBestPrice = candle.ClosePrice;
		_longTrailingLevel = null;
		_longStop = support - atr;

		ResetShortState();
	}

	private void OpenShort(ICandleMessage candle, decimal resistance, decimal atr)
	{
		if (Volume <= 0m)
		return;

		SellMarket(Volume);

		_shortEntryPrice = candle.ClosePrice;
		_shortBestPrice = candle.ClosePrice;
		_shortTrailingLevel = null;
		_shortStop = resistance + atr;

		ResetLongState();
	}

	private decimal GetTrailingOffset()
	{
		return TrailingStopPips * _priceStep;
	}

	private void ResetLongState()
	{
		_longEntryPrice = null;
		_longStop = null;
		_longBestPrice = null;
		_longTrailingLevel = null;
	}

	private void ResetShortState()
	{
		_shortEntryPrice = null;
		_shortStop = null;
		_shortBestPrice = null;
		_shortTrailingLevel = null;
	}
}
