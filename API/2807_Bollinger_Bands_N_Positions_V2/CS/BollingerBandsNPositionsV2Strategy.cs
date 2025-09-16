using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bollinger Bands breakout strategy that can pyramid entries and applies pip-based risk management.
/// </summary>
public class BollingerBandsNPositionsV2Strategy : Strategy
{
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<int> _maxPositions;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _pipValue;
	private decimal _stopLossDistance;
	private decimal _takeProfitDistance;
	private decimal _trailingStopDistance;
	private decimal _trailingStepDistance;

	private decimal _longEntryPrice;
	private decimal _shortEntryPrice;
	private int _longEntryCount;
	private int _shortEntryCount;
	private decimal? _longStopPrice;
	private decimal? _longTakeProfitPrice;
	private decimal? _shortStopPrice;
	private decimal? _shortTakeProfitPrice;

	/// <summary>
	/// Bollinger Bands period.
	/// </summary>
	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	/// <summary>
	/// Bollinger Bands standard deviation multiplier.
	/// </summary>
	public decimal BollingerDeviation
	{
		get => _bollingerDeviation.Value;
		set => _bollingerDeviation.Value = value;
	}

	/// <summary>
	/// Maximum stacked entries per direction.
	/// </summary>
	public int MaxPositions
	{
		get => _maxPositions.Value;
		set => _maxPositions.Value = value;
	}

	/// <summary>
	/// Order volume per entry.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
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
	/// Take profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
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
	/// Additional profit in pips required before trailing stop is moved again.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Candle type processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="BollingerBandsNPositionsV2Strategy"/>.
	/// </summary>
	public BollingerBandsNPositionsV2Strategy()
	{
		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Period", "Period used for Bollinger Bands.", "Indicators")
			.SetCanOptimize(true);

		_bollingerDeviation = Param(nameof(BollingerDeviation), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Deviation", "Standard deviation multiplier for Bollinger Bands.", "Indicators")
			.SetCanOptimize(true);

		_maxPositions = Param(nameof(MaxPositions), 2)
			.SetGreaterThanZero()
			.SetDisplay("Max Positions", "Maximum number of stacked entries per direction.", "Trading");

		_volume = Param(nameof(Volume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume used for each entry.", "Trading");

		_stopLossPips = Param(nameof(StopLossPips), 30m)
			.SetNotNegative()
			.SetDisplay("Stop Loss (pips)", "Protective stop distance in pips.", "Risk")
			.SetCanOptimize(true);

		_takeProfitPips = Param(nameof(TakeProfitPips), 60m)
			.SetNotNegative()
			.SetDisplay("Take Profit (pips)", "Profit target distance in pips.", "Risk")
			.SetCanOptimize(true);

		_trailingStopPips = Param(nameof(TrailingStopPips), 5m)
			.SetNotNegative()
			.SetDisplay("Trailing Stop (pips)", "Trailing stop offset in pips.", "Risk")
			.SetCanOptimize(true);

		_trailingStepPips = Param(nameof(TrailingStepPips), 1m)
			.SetNotNegative()
			.SetDisplay("Trailing Step (pips)", "Extra profit in pips before trailing stop is adjusted.", "Risk")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for Bollinger analysis.", "General");
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
		ResetLongState();
		ResetShortState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (TrailingStopPips > 0m && TrailingStepPips <= 0m)
			throw new InvalidOperationException("Trailing step must be positive when trailing stop is enabled.");

		_pipValue = CalculatePipValue();
		UpdateRiskDistances();

		var bollinger = new BollingerBands
		{
			Length = BollingerPeriod,
			Width = BollingerDeviation
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(bollinger, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bollinger);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue indicatorValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		UpdateRiskDistances();

		var value = (BollingerBandsValue)indicatorValue;

		if (value.UpBand is not decimal upper || value.LowBand is not decimal lower)
			return;

		HandleRiskManagement(candle);

		if (candle.ClosePrice > upper)
		{
			TryEnterLong(candle);
			return;
		}

		if (candle.ClosePrice < lower)
		{
			TryEnterShort(candle);
		}
	}

	private void HandleRiskManagement(ICandleMessage candle)
	{
		if (_longEntryCount > 0 && Position > 0)
		{
			if (_longTakeProfitPrice is decimal takeProfit && candle.HighPrice >= takeProfit)
			{
				SellMarket(Position);
				ResetLongState();
				return;
			}

			if (_longStopPrice is decimal stopLoss && candle.LowPrice <= stopLoss)
			{
				SellMarket(Position);
				ResetLongState();
				return;
			}

			UpdateLongTrailing(candle);
		}
		else if (Position <= 0)
		{
			ResetLongState();
		}

		if (_shortEntryCount > 0 && Position < 0)
		{
			var positionVolume = Math.Abs(Position);

			if (_shortTakeProfitPrice is decimal takeProfit && candle.LowPrice <= takeProfit)
			{
				BuyMarket(positionVolume);
				ResetShortState();
				return;
			}

			if (_shortStopPrice is decimal stopLoss && candle.HighPrice >= stopLoss)
			{
				BuyMarket(positionVolume);
				ResetShortState();
				return;
			}

			UpdateShortTrailing(candle);
		}
		else if (Position >= 0)
		{
			ResetShortState();
		}
	}

	private void TryEnterLong(ICandleMessage candle)
	{
		if (_longEntryCount >= MaxPositions)
			return;

		if (Position < 0)
		{
			var closeVolume = Math.Abs(Position);
			if (closeVolume > 0)
			{
				BuyMarket(closeVolume);
				ResetShortState();
			}
		}

		var tradeVolume = Volume;
		if (tradeVolume <= 0)
			return;

		var existingVolume = _longEntryCount * tradeVolume;
		BuyMarket(tradeVolume);

		var entryPrice = candle.ClosePrice;
		var newVolume = existingVolume + tradeVolume;
		_longEntryPrice = existingVolume <= 0 ? entryPrice : ((_longEntryPrice * existingVolume) + entryPrice * tradeVolume) / newVolume;
		_longEntryCount++;
		_longStopPrice = StopLossPips > 0m ? _longEntryPrice - _stopLossDistance : null;
		_longTakeProfitPrice = TakeProfitPips > 0m ? _longEntryPrice + _takeProfitDistance : null;
	}

	private void TryEnterShort(ICandleMessage candle)
	{
		if (_shortEntryCount >= MaxPositions)
			return;

		if (Position > 0)
		{
			var closeVolume = Position;
			if (closeVolume > 0)
			{
				SellMarket(closeVolume);
				ResetLongState();
			}
		}

		var tradeVolume = Volume;
		if (tradeVolume <= 0)
			return;

		var existingVolume = _shortEntryCount * tradeVolume;
		SellMarket(tradeVolume);

		var entryPrice = candle.ClosePrice;
		var newVolume = existingVolume + tradeVolume;
		_shortEntryPrice = existingVolume <= 0 ? entryPrice : ((_shortEntryPrice * existingVolume) + entryPrice * tradeVolume) / newVolume;
		_shortEntryCount++;
		_shortStopPrice = StopLossPips > 0m ? _shortEntryPrice + _stopLossDistance : null;
		_shortTakeProfitPrice = TakeProfitPips > 0m ? _shortEntryPrice - _takeProfitDistance : null;
	}

	private void UpdateLongTrailing(ICandleMessage candle)
	{
		if (_trailingStopDistance <= 0m)
			return;

		var moveFromEntry = candle.ClosePrice - _longEntryPrice;
		if (moveFromEntry <= _trailingStopDistance + _trailingStepDistance)
			return;

		var newStop = candle.ClosePrice - _trailingStopDistance;

		if (_longStopPrice is not decimal currentStop || newStop > currentStop + _trailingStepDistance)
			_longStopPrice = newStop;
	}

	private void UpdateShortTrailing(ICandleMessage candle)
	{
		if (_trailingStopDistance <= 0m)
			return;

		var moveFromEntry = _shortEntryPrice - candle.ClosePrice;
		if (moveFromEntry <= _trailingStopDistance + _trailingStepDistance)
			return;

		var newStop = candle.ClosePrice + _trailingStopDistance;

		if (_shortStopPrice is not decimal currentStop || newStop < currentStop - _trailingStepDistance)
			_shortStopPrice = newStop;
	}

	private void ResetLongState()
	{
		_longEntryPrice = 0m;
		_longEntryCount = 0;
		_longStopPrice = null;
		_longTakeProfitPrice = null;
	}

	private void ResetShortState()
	{
		_shortEntryPrice = 0m;
		_shortEntryCount = 0;
		_shortStopPrice = null;
		_shortTakeProfitPrice = null;
	}

	private void UpdateRiskDistances()
	{
		_stopLossDistance = StopLossPips > 0m ? StopLossPips * _pipValue : 0m;
		_takeProfitDistance = TakeProfitPips > 0m ? TakeProfitPips * _pipValue : 0m;
		_trailingStopDistance = TrailingStopPips > 0m ? TrailingStopPips * _pipValue : 0m;
		_trailingStepDistance = TrailingStepPips > 0m ? TrailingStepPips * _pipValue : 0m;
	}

	private decimal CalculatePipValue()
	{
		var security = Security;
		if (security == null)
			return 1m;

		var step = security.PriceStep ?? security.MinPriceStep ?? 0m;
		if (step <= 0m)
			return 1m;

		var decimals = CountDecimals(step);
		if (decimals == 3 || decimals == 5)
			return step * 10m;

		return step;
	}

	private static int CountDecimals(decimal value)
	{
		value = Math.Abs(value);
		var decimals = 0;

		while (value != Math.Truncate(value) && decimals < 10)
		{
			value *= 10m;
			decimals++;
		}

		return decimals;
	}
}
