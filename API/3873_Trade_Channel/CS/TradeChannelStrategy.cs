using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trade Channel strategy converted from the MQL TradeChannel expert advisor.
/// </summary>
public class TradeChannelStrategy : Strategy
{
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<int> _channelLength;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _trailingPoints;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _highest;
	private Lowest _lowest;
	private ATR _atr;

	private decimal _currentResistance;
	private decimal _previousResistance;
	private decimal _currentSupport;
	private decimal _previousSupport;

	private decimal _longEntryPrice;
	private decimal _shortEntryPrice;
	private decimal _longStopPrice;
	private decimal _shortStopPrice;

	private bool _channelInitialized;

	/// <summary>
	/// Position volume.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Number of candles used to build the price channel.
	/// </summary>
	public int ChannelLength
	{
		get => _channelLength.Value;
		set => _channelLength.Value = value;
	}

	/// <summary>
	/// ATR length for stop calculation.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in price steps.
	/// </summary>
	public decimal TrailingPoints
	{
		get => _trailingPoints.Value;
		set => _trailingPoints.Value = value;
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
	/// Initializes a new instance of the <see cref="TradeChannelStrategy"/> class.
	/// </summary>
	public TradeChannelStrategy()
	{
		_volume = Param(nameof(Volume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Volume", "Order volume", "Trading")
		.SetCanOptimize(true)
		.SetOptimize(0.1m, 2m, 0.1m);

		_channelLength = Param(nameof(ChannelLength), 20)
		.SetGreaterThanZero()
		.SetDisplay("Channel Length", "Number of candles for support/resistance", "Channel")
		.SetCanOptimize(true)
		.SetOptimize(10, 60, 5);

		_atrPeriod = Param(nameof(AtrPeriod), 4)
		.SetGreaterThanZero()
		.SetDisplay("ATR Period", "ATR length for stop calculations", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(2, 20, 2);

		_trailingPoints = Param(nameof(TrailingPoints), 30m)
		.SetNotNegative()
		.SetDisplay("Trailing (points)", "Trailing stop distance in price steps", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(0m, 100m, 10m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
		.SetDisplay("Candle Type", "Candles used for calculations", "Data");
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

		_currentResistance = 0m;
		_previousResistance = 0m;
		_currentSupport = 0m;
		_previousSupport = 0m;
		_longEntryPrice = 0m;
		_shortEntryPrice = 0m;
		_longStopPrice = 0m;
		_shortStopPrice = 0m;
		_channelInitialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_highest = new Highest { Length = ChannelLength, CandlePrice = CandlePrice.High };
		_lowest = new Lowest { Length = ChannelLength, CandlePrice = CandlePrice.Low };
		_atr = new ATR { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(_highest, _lowest, _atr, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _highest);
			DrawIndicator(area, _lowest);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue resistanceValue, IIndicatorValue supportValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!_highest.IsFormed || !_lowest.IsFormed || !_atr.IsFormed)
		{
			_channelInitialized = false;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var newResistance = resistanceValue.ToDecimal();
		var newSupport = supportValue.ToDecimal();
		var atr = atrValue.ToDecimal();

		if (!_channelInitialized)
		{
			InitializeChannelState(newResistance, newSupport);
			return;
		}

		_previousResistance = _currentResistance;
		_previousSupport = _currentSupport;
		_currentResistance = newResistance;
		_currentSupport = newSupport;

		var channelFlatTop = AreEqual(_currentResistance, _previousResistance);
		var channelFlatBottom = AreEqual(_currentSupport, _previousSupport);
		var pivot = (_currentResistance + _currentSupport + candle.ClosePrice) / 3m;

		if (Position == 0m)
		{
			TryEnterPositions(candle, atr, channelFlatTop, channelFlatBottom, pivot);
		}
		else if (Position > 0m)
		{
			ProcessLongPosition(candle, channelFlatTop);
		}
		else
		{
			ProcessShortPosition(candle, channelFlatBottom);
		}
	}

	private void InitializeChannelState(decimal resistance, decimal support)
	{
		_currentResistance = resistance;
		_previousResistance = resistance;
		_currentSupport = support;
		_previousSupport = support;
		_channelInitialized = true;
	}

	private void TryEnterPositions(ICandleMessage candle, decimal atr, bool channelFlatTop, bool channelFlatBottom, decimal pivot)
	{
		var volume = Volume;
		if (volume <= 0m)
		return;

		// Conditions replicate the MQL expert advisor behaviour.
		var shortSignal = channelFlatTop &&
		(candle.HighPrice >= _currentResistance ||
		(candle.ClosePrice < _currentResistance && candle.ClosePrice > pivot));

		if (shortSignal)
		{
			SellMarket(volume);
			_shortEntryPrice = candle.ClosePrice;
			_shortStopPrice = _currentResistance + atr;
			_longStopPrice = 0m;
			return;
		}

		var longSignal = channelFlatBottom &&
		(candle.LowPrice <= _currentSupport ||
		(candle.ClosePrice > _currentSupport && candle.ClosePrice < pivot));

		if (longSignal)
		{
			BuyMarket(volume);
			_longEntryPrice = candle.ClosePrice;
			_longStopPrice = _currentSupport - atr;
			_shortStopPrice = 0m;
		}
	}

	private void ProcessLongPosition(ICandleMessage candle, bool channelFlatTop)
	{
		var positionVolume = Math.Abs(Position);
		if (positionVolume <= 0m)
		return;

		if (channelFlatTop && candle.HighPrice >= _currentResistance)
		{
			SellMarket(positionVolume);
			ResetPositionState();
			return;
		}

		UpdateLongTrailingStop(candle);

		if (_longStopPrice > 0m && candle.LowPrice <= _longStopPrice)
		{
			SellMarket(positionVolume);
			ResetPositionState();
		}
	}

	private void ProcessShortPosition(ICandleMessage candle, bool channelFlatBottom)
	{
		var positionVolume = Math.Abs(Position);
		if (positionVolume <= 0m)
		return;

		if (channelFlatBottom && candle.LowPrice <= _currentSupport)
		{
			BuyMarket(positionVolume);
			ResetPositionState();
			return;
		}

		UpdateShortTrailingStop(candle);

		if (_shortStopPrice > 0m && candle.HighPrice >= _shortStopPrice)
		{
			BuyMarket(positionVolume);
			ResetPositionState();
		}
	}

	private void UpdateLongTrailingStop(ICandleMessage candle)
	{
		var trailingOffset = GetTrailingOffset();
		if (trailingOffset <= 0m || _longEntryPrice <= 0m)
		return;

		var profit = candle.ClosePrice - _longEntryPrice;
		if (profit <= trailingOffset)
		return;

		var newStop = candle.ClosePrice - trailingOffset;
		if (newStop > _longStopPrice)
		_longStopPrice = newStop;
	}

	private void UpdateShortTrailingStop(ICandleMessage candle)
	{
		var trailingOffset = GetTrailingOffset();
		if (trailingOffset <= 0m || _shortEntryPrice <= 0m)
		return;

		var profit = _shortEntryPrice - candle.ClosePrice;
		if (profit <= trailingOffset)
		return;

		var newStop = candle.ClosePrice + trailingOffset;
		if (_shortStopPrice <= 0m || newStop < _shortStopPrice)
		_shortStopPrice = newStop;
	}

	private decimal GetTrailingOffset()
	{
		var step = Security?.Step;
		if (step.HasValue && step.Value > 0m)
		return step.Value * TrailingPoints;

		return TrailingPoints;
	}

	private void ResetPositionState()
	{
		_longEntryPrice = 0m;
		_shortEntryPrice = 0m;
		_longStopPrice = 0m;
		_shortStopPrice = 0m;
	}

	private bool AreEqual(decimal first, decimal second)
	{
		var tolerance = Security?.Step ?? 0.0000001m;
		return Math.Abs(first - second) <= tolerance;
	}
}
