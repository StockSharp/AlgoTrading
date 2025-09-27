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
/// Moving average cross strategy converted from the MetaTrader "Cross" expert advisor (MQL file 27596).
/// Opens a long position when the candle open crosses above the EMA and a short position on the opposite cross.
/// Applies fixed take profit and stop loss distances expressed in price steps.
/// </summary>
public class CrossStrategy : Strategy
{
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<DataType> _candleType;

	private bool _wasBuyConditionTrue;
	private bool _wasSellConditionTrue;
	private decimal _entryPrice;
	private decimal _entryVolume;
	private bool _isLongPosition;
	private decimal _previousPosition;

	/// <summary>
	/// Gets or sets the EMA length used for the trend filter.
	/// </summary>
	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	/// <summary>
	/// Gets or sets the take profit distance in price steps.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Gets or sets the stop loss distance in price steps.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Gets or sets the candle type processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CrossStrategy"/> class.
	/// </summary>
	public CrossStrategy()
	{
		_emaLength = Param(nameof(EmaLength), 200)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "Period of the moving average used for cross detection", "Trend Filter")
			.SetCanOptimize(true);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 200m)
			.SetNotNegative()
			.SetDisplay("Take Profit (steps)", "Take profit distance expressed in price steps", "Risk Management")
			.SetCanOptimize(true);

		_stopLossPoints = Param(nameof(StopLossPoints), 100m)
			.SetNotNegative()
			.SetDisplay("Stop Loss (steps)", "Stop loss distance expressed in price steps", "Risk Management")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles used by the strategy", "General");
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

		_wasBuyConditionTrue = false;
		_wasSellConditionTrue = false;
		_entryPrice = 0m;
		_entryVolume = 0m;
		_isLongPosition = false;
		_previousPosition = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var ema = new EMA { Length = EmaLength };
		var subscription = SubscribeCandles(CandleType);

		subscription.Bind(ema, (candle, emaValue) =>
		{
			// Process only completed candles to avoid noise from unfinished bars.
			if (candle.State != CandleStates.Finished)
				return;

			// Ensure the indicator already has enough history to be meaningful.
			if (!ema.IsFormed)
				return;

			// Skip trading until the strategy is fully initialized and allowed to trade.
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			ManageOpenPosition(candle);

			var candleOpen = candle.OpenPrice;

			// Detect first occurrence of the bullish condition after being false.
			var buyCondition = candleOpen > emaValue;
			var buyCross = buyCondition && !_wasBuyConditionTrue;
			_wasBuyConditionTrue = buyCondition;

			// Detect first occurrence of the bearish condition after being false.
			var sellCondition = candleOpen < emaValue;
			var sellCross = sellCondition && !_wasSellConditionTrue;
			_wasSellConditionTrue = sellCondition;

			if (buyCross && Position <= 0m)
			{
				// Reverse any existing short position and open a new long one.
				BuyMarket(Volume + Math.Abs(Position));
				return;
			}

			if (sellCross && Position >= 0m)
			{
				// Reverse any existing long position and open a new short one.
				SellMarket(Volume + Math.Abs(Position));
			}
		}).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ManageOpenPosition(ICandleMessage candle)
	{
		if (Position == 0m || _entryPrice <= 0m)
			return;

		var step = Security?.PriceStep ?? 1m;
		var high = candle.HighPrice;
		var low = candle.LowPrice;

		if (_isLongPosition && Position > 0m)
		{
			var takeProfitPrice = _entryPrice + TakeProfitPoints * step;
			if (TakeProfitPoints > 0m && high >= takeProfitPrice)
			{
				// Close the long position when the take profit level is touched.
				SellMarket(Position);
				return;
			}

			var stopPrice = _entryPrice - StopLossPoints * step;
			if (StopLossPoints > 0m && low <= stopPrice)
			{
				// Protect the position with a fixed stop loss level.
				SellMarket(Position);
			}
		}
		else if (!_isLongPosition && Position < 0m)
		{
			var takeProfitPrice = _entryPrice - TakeProfitPoints * step;
			if (TakeProfitPoints > 0m && low <= takeProfitPrice)
			{
				// Cover the short position when the target profit level is reached.
				BuyMarket(Math.Abs(Position));
				return;
			}

			var stopPrice = _entryPrice + StopLossPoints * step;
			if (StopLossPoints > 0m && high >= stopPrice)
			{
				// Close the short position because the stop loss level was breached.
				BuyMarket(Math.Abs(Position));
			}
		}
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade.Order.Security != Security)
			return;

		var currentPosition = Position;
		var price = trade.Trade.Price;
		var volume = trade.Trade.Volume;
		var side = trade.Order.Side;

		if (currentPosition == 0m)
		{
			// Position fully closed, reset tracking values.
			_entryPrice = 0m;
			_entryVolume = 0m;
			_isLongPosition = false;
		}
		else if (currentPosition > 0m)
		{
			if (_previousPosition <= 0m)
			{
				// New long position opened from a flat or short state.
				_entryPrice = price;
				_entryVolume = currentPosition;
			}
			else if (side == Sides.Buy)
			{
				// Scale in additional long volume and update the weighted average entry price.
				var totalVolume = _entryVolume + volume;
				_entryPrice = (_entryPrice * _entryVolume + price * volume) / totalVolume;
				_entryVolume = totalVolume;
			}

			_isLongPosition = true;
		}
		else if (currentPosition < 0m)
		{
			if (_previousPosition >= 0m)
			{
				// New short position opened from a flat or long state.
				_entryPrice = price;
				_entryVolume = Math.Abs(currentPosition);
			}
			else if (side == Sides.Sell)
			{
				// Scale in additional short volume and maintain the weighted average entry price.
				var totalVolume = _entryVolume + volume;
				_entryPrice = (_entryPrice * _entryVolume + price * volume) / totalVolume;
				_entryVolume = totalVolume;
			}

			_isLongPosition = false;
		}

		_previousPosition = currentPosition;
	}
}

