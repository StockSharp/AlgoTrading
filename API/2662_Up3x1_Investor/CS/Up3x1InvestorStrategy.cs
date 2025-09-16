using System;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Range breakout strategy based on the Up3x1 Investor expert advisor.
/// </summary>
public class Up3x1InvestorStrategy : Strategy
{
	private readonly StrategyParam<decimal> _rangeThresholdPips;
	private readonly StrategyParam<decimal> _bodyThresholdPips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<DataType> _candleType;

	private ICandleMessage? _previousCandle;
	private decimal? _entryPrice;
	private decimal _highestPrice;
	private decimal _lowestPrice;
	private decimal? _trailingStopPrice;

	public decimal RangeThresholdPips { get => _rangeThresholdPips.Value; set => _rangeThresholdPips.Value = value; }
	public decimal BodyThresholdPips { get => _bodyThresholdPips.Value; set => _bodyThresholdPips.Value = value; }
	public decimal StopLossPips { get => _stopLossPips.Value; set => _stopLossPips.Value = value; }
	public decimal TakeProfitPips { get => _takeProfitPips.Value; set => _takeProfitPips.Value = value; }
	public decimal TrailingStopPips { get => _trailingStopPips.Value; set => _trailingStopPips.Value = value; }
	public decimal TrailingStepPips { get => _trailingStepPips.Value; set => _trailingStepPips.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public Up3x1InvestorStrategy()
	{
		_rangeThresholdPips = Param(nameof(RangeThresholdPips), 50m)
			.SetDisplay("Range Threshold (pips)", "Minimum previous candle range in pips", "Signals");

		_bodyThresholdPips = Param(nameof(BodyThresholdPips), 20m)
			.SetDisplay("Body Threshold (pips)", "Minimum previous candle body in pips", "Signals");

		_stopLossPips = Param(nameof(StopLossPips), 50m)
			.SetDisplay("Stop Loss (pips)", "Distance of protective stop in pips", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 20m)
			.SetDisplay("Take Profit (pips)", "Distance of profit target in pips", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 30m)
			.SetDisplay("Trailing Stop (pips)", "Distance kept behind price when trailing", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
			.SetDisplay("Trailing Step (pips)", "Increment required to move trailing stop", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe for signals", "General");
	}

	/// <inheritdoc />
	public override System.Collections.Generic.IEnumerable<(StockSharp.BusinessEntities.Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_previousCandle = null;
		ResetPositionTracking();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Subscribe to the configured timeframe and process finished candles.
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Work only with fully formed candles to keep logic aligned with the original EA.
		if (candle.State != CandleStates.Finished)
			return;

		// If there is an open position without cached information, initialize tracking from broker data.
		if (Position != 0 && _entryPrice == null && PositionPrice != 0)
			InitializePositionTracking(PositionPrice);
		else if (Position == 0 && _entryPrice != null)
			ResetPositionTracking();

		var pipSize = GetPipSize();
		var stopLossDistance = StopLossPips > 0 ? StopLossPips * pipSize : 0m;
		var takeProfitDistance = TakeProfitPips > 0 ? TakeProfitPips * pipSize : 0m;
		var trailingStopDistance = TrailingStopPips > 0 ? TrailingStopPips * pipSize : 0m;
		var trailingStepDistance = TrailingStepPips > 0 ? TrailingStepPips * pipSize : 0m;

		// Manage existing trades before searching for a new signal.
		if (Position != 0 && _entryPrice != null)
		{
			if (ManageOpenPosition(candle, stopLossDistance, takeProfitDistance, trailingStopDistance, trailingStepDistance))
			{
				_previousCandle = candle;
				return;
			}
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_previousCandle = candle;
			return;
		}

		if (Position != 0)
		{
			_previousCandle = candle;
			return;
		}

		var referenceCandle = _previousCandle ?? candle;
		var range = referenceCandle.HighPrice - referenceCandle.LowPrice;
		var body = Math.Abs(referenceCandle.ClosePrice - referenceCandle.OpenPrice);
		var rangeThreshold = RangeThresholdPips * pipSize;
		var bodyThreshold = BodyThresholdPips * pipSize;

		// Bullish setup: strong bullish candle with large range and body.
		if (range > rangeThreshold && body > bodyThreshold && referenceCandle.ClosePrice > referenceCandle.OpenPrice)
		{
			BuyMarket(Volume);
			InitializePositionTracking(candle.ClosePrice);
		}
		// Bearish setup: strong bearish candle with large range and body.
		else if (range > rangeThreshold && body > bodyThreshold && referenceCandle.ClosePrice < referenceCandle.OpenPrice)
		{
			SellMarket(Volume);
			InitializePositionTracking(candle.ClosePrice);
		}

		_previousCandle = candle;
	}

	private bool ManageOpenPosition(ICandleMessage candle, decimal stopLossDistance, decimal takeProfitDistance, decimal trailingStopDistance, decimal trailingStepDistance)
	{
		if (_entryPrice == null)
			return false;

		if (Position > 0)
		{
			// Update the highest price reached by the long position.
			_highestPrice = Math.Max(_highestPrice, candle.HighPrice);

			// Check stop loss.
			if (stopLossDistance > 0m && candle.LowPrice <= _entryPrice.Value - stopLossDistance)
			{
				SellMarket(Position);
				ResetPositionTracking();
				return true;
			}

			// Check take profit.
			if (takeProfitDistance > 0m && candle.HighPrice >= _entryPrice.Value + takeProfitDistance)
			{
				SellMarket(Position);
				ResetPositionTracking();
				return true;
			}

			// Update trailing stop level when the move is large enough.
			if (trailingStopDistance > 0m && _highestPrice - _entryPrice.Value >= trailingStopDistance + trailingStepDistance)
			{
				var candidate = _highestPrice - trailingStopDistance;
				if (_trailingStopPrice == null || candidate - _trailingStopPrice.Value >= trailingStepDistance)
				_trailingStopPrice = candidate;
			}

			// Exit if price returned to the trailing stop.
			if (_trailingStopPrice != null && candle.LowPrice <= _trailingStopPrice.Value)
			{
				SellMarket(Position);
				ResetPositionTracking();
				return true;
			}
		}
		else if (Position < 0)
		{
			// Update the lowest price reached by the short position.
			_lowestPrice = Math.Min(_lowestPrice, candle.LowPrice);

			// Check stop loss for short trades.
			if (stopLossDistance > 0m && candle.HighPrice >= _entryPrice.Value + stopLossDistance)
			{
				BuyMarket(Math.Abs(Position));
				ResetPositionTracking();
				return true;
			}

			// Check take profit for short trades.
			if (takeProfitDistance > 0m && candle.LowPrice <= _entryPrice.Value - takeProfitDistance)
			{
				BuyMarket(Math.Abs(Position));
				ResetPositionTracking();
				return true;
			}

			// Update trailing stop for the short side.
			if (trailingStopDistance > 0m && _entryPrice.Value - _lowestPrice >= trailingStopDistance + trailingStepDistance)
			{
				var candidate = _lowestPrice + trailingStopDistance;
				if (_trailingStopPrice == null || _trailingStopPrice.Value - candidate >= trailingStepDistance)
				_trailingStopPrice = candidate;
			}

			// Exit once the trailing stop is touched.
			if (_trailingStopPrice != null && candle.HighPrice >= _trailingStopPrice.Value)
			{
				BuyMarket(Math.Abs(Position));
				ResetPositionTracking();
				return true;
			}
		}

		return false;
	}

	private void InitializePositionTracking(decimal entryPrice)
	{
		// Store entry information to evaluate stops and trailing logic.
		_entryPrice = entryPrice;
		_highestPrice = entryPrice;
		_lowestPrice = entryPrice;
		_trailingStopPrice = null;
	}

	private void ResetPositionTracking()
	{
		_entryPrice = null;
		_highestPrice = 0m;
		_lowestPrice = 0m;
		_trailingStopPrice = null;
	}

	private decimal GetPipSize()
	{
		var step = Security?.PriceStep;
		if (step == null || step == 0m)
			return 1m;

		return step.Value;
	}
}
