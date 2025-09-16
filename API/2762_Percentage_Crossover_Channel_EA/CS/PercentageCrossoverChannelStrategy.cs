using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public enum PercentageChannelPriceMode
{
	Close,
	Open,
	High,
	Low,
	Median,
	Typical,
	Weighted,
	Average
}

/// <summary>
/// Percentage Crossover Channel strategy converted from MetaTrader 5.
/// </summary>
public class PercentageCrossoverChannelStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _percent;
	private readonly StrategyParam<PercentageChannelPriceMode> _priceMode;
	private readonly StrategyParam<bool> _tradeOnMiddleCross;
	private readonly StrategyParam<bool> _reverseSignals;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<decimal> _orderVolume;

	// Cached indicator values for the previous two finished candles.
	private decimal? _prevUpper;
	private decimal? _prevMiddle;
	private decimal? _prevLower;
	private decimal? _prevPrevUpper;
	private decimal? _prevPrevMiddle;
	private decimal? _prevPrevLower;

	// Stored price data for signal evaluation.
	private decimal? _prevClose;
	private decimal? _prevHigh;
	private decimal? _prevLow;
	private decimal? _prevPrevClose;
	private decimal? _prevPrevHigh;
	private decimal? _prevPrevLow;

	// Internal state of the channel middle line recursion.
	private decimal _lastMiddle;
	private bool _hasIndicatorState;

	// Protective levels that mimic MT5 stop loss and take profit requests.
	private decimal? _stopPrice;
	private decimal? _takePrice;
	private decimal _entryPrice;

	public PercentageCrossoverChannelStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for processing", "General");

		_percent = Param(nameof(Percent), 50m)
			.SetDisplay("Percent", "Channel width percent", "Channel")
			.SetCanOptimize(true)
			.SetGreaterThanZero();

		_priceMode = Param(nameof(PriceMode), PercentageChannelPriceMode.Close)
			.SetDisplay("Applied Price", "Price source for channel calculations", "Channel");

		_tradeOnMiddleCross = Param(nameof(TradeOnMiddleCross), false)
			.SetDisplay("Trade Middle Cross", "Use middle line crossovers instead of band touches", "Signals");

		_reverseSignals = Param(nameof(ReverseSignals), false)
			.SetDisplay("Reverse Signals", "Invert long and short logic", "Signals");

		_stopLossPoints = Param(nameof(StopLossPoints), 0)
			.SetDisplay("Stop Loss (points)", "Protective stop distance in points", "Risk")
			.SetGreaterOrEqualZero();

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 0)
			.SetDisplay("Take Profit (points)", "Target profit distance in points", "Risk")
			.SetGreaterOrEqualZero();

		_orderVolume = Param(nameof(OrderVolume), 1m)
			.SetDisplay("Order Volume", "Base volume for market entries", "Trading")
			.SetGreaterThanZero();
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public decimal Percent
	{
		get => _percent.Value;
		set => _percent.Value = value;
	}

	public PercentageChannelPriceMode PriceMode
	{
		get => _priceMode.Value;
		set => _priceMode.Value = value;
	}

	public bool TradeOnMiddleCross
	{
		get => _tradeOnMiddleCross.Value;
		set => _tradeOnMiddleCross.Value = value;
	}

	public bool ReverseSignals
	{
		get => _reverseSignals.Value;
		set => _reverseSignals.Value = value;
	}

	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
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

		_prevUpper = null;
		_prevMiddle = null;
		_prevLower = null;
		_prevPrevUpper = null;
		_prevPrevMiddle = null;
		_prevPrevLower = null;

		_prevClose = null;
		_prevHigh = null;
		_prevLow = null;
		_prevPrevClose = null;
		_prevPrevHigh = null;
		_prevPrevLow = null;

		_lastMiddle = 0m;
		_hasIndicatorState = false;

		ResetProtection();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = OrderVolume;

		// Subscribe to candle updates that will drive the high level logic.
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
		// Work only with completed candles to stay consistent with the MT5 implementation.
		if (candle.State != CandleStates.Finished)
			return;

		var exitTriggered = CheckProtection(candle);

		if (!exitTriggered)
			TryEnterPositions(candle);

		UpdateChannelState(candle);
	}

	private void TryEnterPositions(ICandleMessage candle)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Wait until the channel has valid values for two completed candles.
		if (!_prevLower.HasValue || !_prevPrevLower.HasValue)
			return;

		if (!_prevClose.HasValue || !_prevPrevClose.HasValue || !_prevHigh.HasValue || !_prevPrevHigh.HasValue || !_prevLow.HasValue || !_prevPrevLow.HasValue)
			return;

		var openLong = false;
		var openShort = false;

		if (TradeOnMiddleCross)
		{
			// Evaluate crossovers of the price and the middle channel line.
			var crossDown = _prevPrevClose.Value > _prevPrevMiddle.Value && _prevClose.Value < _prevMiddle.Value;
			var crossUp = _prevPrevClose.Value < _prevPrevMiddle.Value && _prevClose.Value > _prevMiddle.Value;

			if (!ReverseSignals)
			{
				if (crossDown)
					openLong = true;

				if (crossUp)
					openShort = true;
			}
			else
			{
				if (crossDown)
					openShort = true;

				if (crossUp)
					openLong = true;
			}
		}
		else
		{
			// Default mode trades touches of the outer channel boundaries.
			var touchLower = _prevPrevLow.Value > _prevPrevLower.Value && _prevLow.Value <= _prevLower.Value;
			var touchUpper = _prevPrevHigh.Value < _prevPrevUpper.Value && _prevHigh.Value >= _prevUpper.Value;

			if (!ReverseSignals)
			{
				if (touchLower)
					openLong = true;

				if (touchUpper)
					openShort = true;
			}
			else
			{
				if (touchLower)
					openShort = true;

				if (touchUpper)
					openLong = true;
			}
		}

		if (openLong)
		{
			EnterLong(candle);
		}
		else if (openShort)
		{
			EnterShort(candle);
		}
	}

	private void EnterLong(ICandleMessage candle)
	{
		// Combine base order volume with the size required to flatten shorts.
		var volume = OrderVolume + (Position < 0 ? Math.Abs(Position) : 0m);
		if (volume <= 0m)
			return;

		BuyMarket(volume);

		_entryPrice = candle.OpenPrice;
		_stopPrice = CalculateStopPrice(Sides.Buy, _entryPrice);
		_takePrice = CalculateTakePrice(Sides.Buy, _entryPrice);
	}

	private void EnterShort(ICandleMessage candle)
	{
		// Combine base order volume with the size required to flatten longs.
		var volume = OrderVolume + (Position > 0 ? Position : 0m);
		if (volume <= 0m)
			return;

		SellMarket(volume);

		_entryPrice = candle.OpenPrice;
		_stopPrice = CalculateStopPrice(Sides.Sell, _entryPrice);
		_takePrice = CalculateTakePrice(Sides.Sell, _entryPrice);
	}

	private bool CheckProtection(ICandleMessage candle)
	{
		// Emulate MT5 protective stop and take profit that were attached to market orders.
		if (Position > 0)
		{
			if (_stopPrice.HasValue && candle.LowPrice <= _stopPrice.Value)
			{
				SellMarket(Math.Abs(Position));
				ResetProtection();
				return true;
			}

			if (_takePrice.HasValue && candle.HighPrice >= _takePrice.Value)
			{
				SellMarket(Math.Abs(Position));
				ResetProtection();
				return true;
			}
		}
		else if (Position < 0)
		{
			if (_stopPrice.HasValue && candle.HighPrice >= _stopPrice.Value)
			{
				BuyMarket(Math.Abs(Position));
				ResetProtection();
				return true;
			}

			if (_takePrice.HasValue && candle.LowPrice <= _takePrice.Value)
			{
				BuyMarket(Math.Abs(Position));
				ResetProtection();
				return true;
			}
		}
		else
		{
			ResetProtection();
		}

		return false;
	}

	private void UpdateChannelState(ICandleMessage candle)
	{
		// Recreate the Percentage Crossover Channel middle line recursion.
		var percent = Percent <= 0m ? 0.001m : Percent;
		var plusFactor = 1m + percent / 100m;
		var minusFactor = 1m - percent / 100m;
		var price = GetAppliedPrice(candle);

		decimal currentMiddle;
		if (!_hasIndicatorState)
		{
			currentMiddle = price;
			_hasIndicatorState = true;
		}
		else
		{
			var lowerBound = price * minusFactor;
			var upperBound = price * plusFactor;
			var previousMiddle = _lastMiddle;

			currentMiddle = previousMiddle;

			if (lowerBound > previousMiddle)
				currentMiddle = lowerBound;
			else if (upperBound < previousMiddle)
				currentMiddle = upperBound;
		}

		var currentUpper = currentMiddle * plusFactor;
		var currentLower = currentMiddle * minusFactor;

		if (_prevUpper.HasValue)
		{
			_prevPrevUpper = _prevUpper;
			_prevPrevMiddle = _prevMiddle;
			_prevPrevLower = _prevLower;
			_prevPrevClose = _prevClose;
			_prevPrevHigh = _prevHigh;
			_prevPrevLow = _prevLow;
		}

		_prevUpper = currentUpper;
		_prevMiddle = currentMiddle;
		_prevLower = currentLower;
		_prevClose = candle.ClosePrice;
		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;
		_lastMiddle = currentMiddle;
	}

	private decimal GetAppliedPrice(ICandleMessage candle)
	{
		// Convert the selected price mode into a candle value.
		return PriceMode switch
		{
			PercentageChannelPriceMode.Open => candle.OpenPrice,
			PercentageChannelPriceMode.High => candle.HighPrice,
			PercentageChannelPriceMode.Low => candle.LowPrice,
			PercentageChannelPriceMode.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			PercentageChannelPriceMode.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			PercentageChannelPriceMode.Weighted => (candle.HighPrice + candle.LowPrice + (2m * candle.ClosePrice)) / 4m,
			PercentageChannelPriceMode.Average => (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m,
			_ => candle.ClosePrice,
		};
	}

	private decimal? CalculateStopPrice(Sides side, decimal entryPrice)
	{
		if (StopLossPoints <= 0)
			return null;

		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			return null;

		var offset = StopLossPoints * step;
		return side == Sides.Buy ? entryPrice - offset : entryPrice + offset;
	}

	private decimal? CalculateTakePrice(Sides side, decimal entryPrice)
	{
		if (TakeProfitPoints <= 0)
			return null;

		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			return null;

		var offset = TakeProfitPoints * step;
		return side == Sides.Buy ? entryPrice + offset : entryPrice - offset;
	}

	private void ResetProtection()
	{
		_stopPrice = null;
		_takePrice = null;
		_entryPrice = 0m;
	}
}
