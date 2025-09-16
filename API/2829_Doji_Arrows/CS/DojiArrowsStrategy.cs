using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Doji breakout strategy with optional fixed and trailing protection.
/// </summary>
public class DojiArrowsStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<decimal> _trailingStepPoints;
	private readonly StrategyParam<decimal> _dojiBodyPoints;
	private readonly StrategyParam<DataType> _candleType;

	private bool _hasPreviousCandle;
	private decimal _prevOpen;
	private decimal _prevClose;
	private decimal _prevHigh;
	private decimal _prevLow;

	private decimal? _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takePrice;

	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	public decimal TrailingStepPoints
	{
		get => _trailingStepPoints.Value;
		set => _trailingStepPoints.Value = value;
	}

	public decimal DojiBodyPoints
	{
		get => _dojiBodyPoints.Value;
		set => _dojiBodyPoints.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public DojiArrowsStrategy()
	{
		_stopLossPoints = Param(nameof(StopLossPoints), 30m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Stop Loss Points", "Stop loss distance in price steps.", "Risk")
			.SetCanOptimize();

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 90m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Take Profit Points", "Take profit distance in price steps.", "Risk")
			.SetCanOptimize();

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 15m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Trailing Stop Points", "Trailing distance in price steps.", "Risk")
			.SetCanOptimize();

		_trailingStepPoints = Param(nameof(TrailingStepPoints), 5m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Trailing Step Points", "Minimum profit before the trailing stop moves.", "Risk")
			.SetCanOptimize();

		_dojiBodyPoints = Param(nameof(DojiBodyPoints), 1m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Doji Body Points", "Maximum difference between open and close to treat the candle as a doji.", "Pattern")
			.SetCanOptimize();

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame used for signal generation.", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		SubscribeCandles(CandleType)
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		ManageActivePosition(candle);

		if (!_hasPreviousCandle)
		{
			CachePreviousCandle(candle);
			return;
		}

		var step = Security?.PriceStep ?? 1m;
		var tolerance = DojiBodyPoints <= 0m ? step : DojiBodyPoints * step;
		var bodySize = Math.Abs(_prevOpen - _prevClose);
		var isDoji = bodySize <= tolerance;

		var breakoutUp = isDoji && candle.ClosePrice > _prevHigh;
		var breakoutDown = isDoji && candle.ClosePrice < _prevLow;

		if (breakoutUp && Position <= 0)
		{
			ResetProtection();
			BuyMarket(Volume);
			InitializeProtection(candle.ClosePrice, true, step);
		}
		else if (breakoutDown && Position >= 0)
		{
			ResetProtection();
			SellMarket(Volume);
			InitializeProtection(candle.ClosePrice, false, step);
		}

		CachePreviousCandle(candle);
	}

	private void ManageActivePosition(ICandleMessage candle)
	{
		if (Position == 0)
			return;

		var step = Security?.PriceStep ?? 1m;
		var trailingDistance = TrailingStopPoints > 0m ? TrailingStopPoints * step : 0m;
		var trailingStep = TrailingStepPoints > 0m ? TrailingStepPoints * step : 0m;

		if (Position > 0)
		{
			if (trailingDistance > 0m && _entryPrice.HasValue)
			{
				var gain = candle.ClosePrice - _entryPrice.Value;

				if (gain > trailingDistance + trailingStep)
				{
					var newStop = candle.ClosePrice - trailingDistance;

					if (!_stopPrice.HasValue || newStop > _stopPrice.Value)
						_stopPrice = newStop;
				}
			}

			if (_stopPrice.HasValue && candle.LowPrice <= _stopPrice.Value)
			{
				SellMarket(Math.Abs(Position));
				ResetProtection();
				return;
			}

			if (_takePrice.HasValue && candle.HighPrice >= _takePrice.Value)
			{
				SellMarket(Math.Abs(Position));
				ResetProtection();
				return;
			}
		}
		else if (Position < 0)
		{
			if (trailingDistance > 0m && _entryPrice.HasValue)
			{
				var gain = _entryPrice.Value - candle.ClosePrice;

				if (gain > trailingDistance + trailingStep)
				{
					var newStop = candle.ClosePrice + trailingDistance;

					if (!_stopPrice.HasValue || newStop < _stopPrice.Value)
						_stopPrice = newStop;
				}
			}

			if (_stopPrice.HasValue && candle.HighPrice >= _stopPrice.Value)
			{
				BuyMarket(Math.Abs(Position));
				ResetProtection();
				return;
			}

			if (_takePrice.HasValue && candle.LowPrice <= _takePrice.Value)
			{
				BuyMarket(Math.Abs(Position));
				ResetProtection();
				return;
			}
		}
	}

	private void InitializeProtection(decimal price, bool isLong, decimal step)
	{
		_entryPrice = price;

		if (StopLossPoints > 0m)
		{
			var offset = StopLossPoints * step;
			_stopPrice = isLong ? price - offset : price + offset;
		}
		else
		{
			_stopPrice = null;
		}

		if (TakeProfitPoints > 0m)
		{
			var offset = TakeProfitPoints * step;
			_takePrice = isLong ? price + offset : price - offset;
		}
		else
		{
			_takePrice = null;
		}
	}

	private void ResetProtection()
	{
		_entryPrice = null;
		_stopPrice = null;
		_takePrice = null;
	}

	private void CachePreviousCandle(ICandleMessage candle)
	{
		_prevOpen = candle.OpenPrice;
		_prevClose = candle.ClosePrice;
		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;
		_hasPreviousCandle = true;
	}
}
