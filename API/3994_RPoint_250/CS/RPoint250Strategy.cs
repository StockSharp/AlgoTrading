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
/// Reverse-point breakout strategy converted from the MetaTrader 4 expert e_RPoint_250.
/// </summary>
public class RPoint250Strategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<int> _reversePoint;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _highest;
	private Lowest _lowest;

	private decimal _lastHighLevel;
	private decimal _lastLowLevel;
	private decimal _executedHighLevel;
	private decimal _executedLowLevel;
	private DateTimeOffset? _lastSignalTime;
	private decimal _priceStep;
	private decimal _trailingDistance;
	private decimal? _bestLongPrice;
	private decimal? _bestShortPrice;

	public RPoint250Strategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetDisplay("Order Volume", "Base volume for market entries.", "Trading")
			.SetCanOptimize(true);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 15m)
			.SetDisplay("Take Profit Points", "Take profit distance expressed in price points.", "Risk")
			.SetCanOptimize(true);

		_stopLossPoints = Param(nameof(StopLossPoints), 999m)
			.SetDisplay("Stop Loss Points", "Stop loss distance expressed in price points.", "Risk")
			.SetCanOptimize(true);

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 0m)
			.SetDisplay("Trailing Stop Points", "Optional trailing distance in price points.", "Risk")
			.SetCanOptimize(true);

		_reversePoint = Param(nameof(ReversePoint), 250)
			.SetDisplay("Reverse Point Length", "Number of candles scanned for the latest reversal levels.", "Signals")
			.SetGreaterThanZero()
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle aggregation used for calculations.", "General");
	}

	/// <summary>
	/// Market order volume used for both entries and reversals.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Take-profit distance in price points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in price points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Trailing-stop distance in price points.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Number of candles used to approximate the rPoint indicator.
	/// </summary>
	public int ReversePoint
	{
		get => _reversePoint.Value;
		set => _reversePoint.Value = value;
	}

	/// <summary>
	/// Candle type processed by the strategy.
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

		_highest = null;
		_lowest = null;
		_lastHighLevel = 0m;
		_lastLowLevel = 0m;
		_executedHighLevel = 0m;
		_executedLowLevel = 0m;
		_lastSignalTime = null;
		_priceStep = 0m;
		_trailingDistance = 0m;
		_bestLongPrice = null;
		_bestShortPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_highest = new Highest { Length = Math.Max(1, ReversePoint) };
		_lowest = new Lowest { Length = Math.Max(1, ReversePoint) };

		_priceStep = Security?.PriceStep ?? 0m;
		if (_priceStep <= 0m)
			_priceStep = 1m;

		var takeDistance = TakeProfitPoints > 0m ? _priceStep * TakeProfitPoints : 0m;
		var stopDistance = StopLossPoints > 0m ? _priceStep * StopLossPoints : 0m;
		_trailingDistance = TrailingStopPoints > 0m ? _priceStep * TrailingStopPoints : 0m;

		// Apply the same static protection as in the original MQL script.
		StartProtection(
			takeDistance > 0m ? new Unit(takeDistance, UnitTypes.Absolute) : default,
			stopDistance > 0m ? new Unit(stopDistance, UnitTypes.Absolute) : default);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_highest, _lowest, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _highest);
			DrawIndicator(area, _lowest);
			DrawOwnTrades(area);
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		// Reset trailing anchors whenever the net position changes.
		_bestLongPrice = null;
		_bestShortPrice = null;
	}

	private void ProcessCandle(ICandleMessage candle, decimal highestValue, decimal lowestValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_highest.IsFormed || !_lowest.IsFormed)
			return;

		// Capture the latest swing levels as soon as they appear.
		if (highestValue == candle.HighPrice && highestValue != _lastHighLevel)
			_lastHighLevel = highestValue;

		if (lowestValue == candle.LowPrice && lowestValue != _lastLowLevel)
			_lastLowLevel = lowestValue;

		if (Position > 0)
		{
			_bestLongPrice = _bestLongPrice is null || candle.HighPrice > _bestLongPrice
				? candle.HighPrice
				: _bestLongPrice;

			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Close the long position when price retraces by the trailing distance.
			if (_trailingDistance > 0m && _bestLongPrice is decimal bestLong && bestLong - candle.LowPrice >= _trailingDistance)
			{
				SellMarket(Position);
				_bestLongPrice = null;
				return;
			}

			// Reverse the position when a new high reversal point appears.
			if (_lastHighLevel != 0m && _lastHighLevel != _executedHighLevel)
			{
				SellMarket(Position);
				_bestLongPrice = null;
				return;
			}
		}
		else if (Position < 0)
		{
			_bestShortPrice = _bestShortPrice is null || candle.LowPrice < _bestShortPrice
				? candle.LowPrice
				: _bestShortPrice;

			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Close the short position when price rallies by the trailing distance.
			if (_trailingDistance > 0m && _bestShortPrice is decimal bestShort && candle.HighPrice - bestShort >= _trailingDistance)
			{
				BuyMarket(-Position);
				_bestShortPrice = null;
				return;
			}

			// Reverse the position when a new low reversal point appears.
			if (_lastLowLevel != 0m && _lastLowLevel != _executedLowLevel)
			{
				BuyMarket(-Position);
				_bestShortPrice = null;
				return;
			}
		}
		else
		{
			_bestLongPrice = null;
			_bestShortPrice = null;

			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			if (OrderVolume <= 0m)
				return;

			if (_lastSignalTime == candle.OpenTime)
				return;

			// Enter short when the reversal high changes.
			if (_lastHighLevel != 0m && _lastHighLevel != _executedHighLevel)
			{
				SellMarket(OrderVolume);
				_executedHighLevel = _lastHighLevel;
				_lastSignalTime = candle.OpenTime;
				_bestShortPrice = candle.ClosePrice;
				return;
			}

			// Enter long when the reversal low changes.
			if (_lastLowLevel != 0m && _lastLowLevel != _executedLowLevel)
			{
				BuyMarket(OrderVolume);
				_executedLowLevel = _lastLowLevel;
				_lastSignalTime = candle.OpenTime;
				_bestLongPrice = candle.ClosePrice;
			}
		}
	}
}

