using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trend following strategy using Parabolic SAR and ADX filter.
/// Opens long when price is above SAR and ADX is below a threshold,
/// shorts when price is below SAR with low ADX.
/// Applies stop loss, take profit and break-even management.
/// </summary>
public class TrendCaptureStrategy : Strategy {
	private readonly StrategyParam<decimal> _sarStep;
	private readonly StrategyParam<decimal> _sarMax;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _adxLevel;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _breakEven;
	private readonly StrategyParam<DataType> _candleType;

	private ParabolicSar _sar = null!;
	private AverageDirectionalIndex _adx = null!;

	// Trade management
	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _takePrice;
	private bool _breakEvenActivated;
	private int _entryDirection;   // 1 for long, -1 for short
	private int _allowedDirection; // 0 - both, 1 - only sell allowed, -1 - only
								   // buy allowed

	/// <summary>
	/// Parabolic SAR step.
	/// </summary>
	public decimal SarStep {
		get => _sarStep.Value;
		set => _sarStep.Value = value;
	}

	/// <summary>
	/// Parabolic SAR maximum step.
	/// </summary>
	public decimal SarMax {
		get => _sarMax.Value;
		set => _sarMax.Value = value;
	}

	/// <summary>
	/// ADX period.
	/// </summary>
	public int AdxPeriod {
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// ADX level threshold.
	/// </summary>
	public decimal AdxLevel {
		get => _adxLevel.Value;
		set => _adxLevel.Value = value;
	}

	/// <summary>
	/// Stop loss in points.
	/// </summary>
	public decimal StopLoss {
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take profit in points.
	/// </summary>
	public decimal TakeProfit {
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Break-even trigger in points.
	/// </summary>
	public decimal BreakEven {
		get => _breakEven.Value;
		set => _breakEven.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType {
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize strategy parameters.
	/// </summary>
	public TrendCaptureStrategy() {
		_sarStep =
			Param(nameof(SarStep), 0.02m)
				.SetDisplay("SAR Step", "Acceleration factor", "Indicators");

		_sarMax =
			Param(nameof(SarMax), 0.2m)
				.SetDisplay("SAR Max", "Maximum acceleration", "Indicators");

		_adxPeriod =
			Param(nameof(AdxPeriod), 14)
				.SetDisplay("ADX Period", "Period for ADX", "Indicators");

		_adxLevel =
			Param(nameof(AdxLevel), 20m)
				.SetDisplay("ADX Level", "Maximum ADX level", "Trading");

		_stopLoss =
			Param(nameof(StopLoss), 1800m)
				.SetDisplay("Stop Loss", "Stop loss in points", "Trading");

		_takeProfit =
			Param(nameof(TakeProfit), 500m)
				.SetDisplay("Take Profit", "Take profit in points", "Trading");

		_breakEven =
			Param(nameof(BreakEven), 50m)
				.SetDisplay("Break Even",
							"Move stop to entry after profit in points",
							"Trading");

		_candleType =
			Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)>
	GetWorkingSecurities() {
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time) {
		base.OnStarted(time);

		_sar = new ParabolicSar { AccelerationStep = SarStep,
								  AccelerationMax = SarMax };
		_adx = new AverageDirectionalIndex { Length = AdxPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(_sar, _adx, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null) {
			DrawCandles(area, subscription);
			DrawIndicator(area, _sar);
			DrawIndicator(area, _adx);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue sarValue,
							   IIndicatorValue adxValue) {
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var sar = sarValue.ToDecimal();
		var adxTyped = (AverageDirectionalIndexValue)adxValue;
		if (adxTyped.MovingAverage is not decimal adx)
			return;

		var step = Security?.PriceStep ?? 1m;

		// Manage open position
		if (Position != 0) {
			if (!_breakEvenActivated) {
				if (_entryDirection > 0 &&
					candle.ClosePrice - _entryPrice >= BreakEven * step) {
					_stopPrice = _entryPrice;
					_breakEvenActivated = true;
				} else if (_entryDirection < 0 &&
						   _entryPrice - candle.ClosePrice >=
							   BreakEven * step) {
					_stopPrice = _entryPrice;
					_breakEvenActivated = true;
				}
			}

			if (_entryDirection > 0) {
				if (candle.LowPrice <= _stopPrice) {
					SellMarket(Position);
					UpdateAllowedDirection(candle.ClosePrice);
					return;
				}

				if (candle.HighPrice >= _takePrice) {
					SellMarket(Position);
					UpdateAllowedDirection(candle.ClosePrice);
					return;
				}
			} else if (_entryDirection < 0) {
				if (candle.HighPrice >= _stopPrice) {
					BuyMarket(Math.Abs(Position));
					UpdateAllowedDirection(candle.ClosePrice);
					return;
				}

				if (candle.LowPrice <= _takePrice) {
					BuyMarket(Math.Abs(Position));
					UpdateAllowedDirection(candle.ClosePrice);
					return;
				}
			}
		}

		// Entry logic
		var longSignal = candle.ClosePrice > sar && adx < AdxLevel;
		var shortSignal = candle.ClosePrice < sar && adx < AdxLevel;

		if (Position == 0) {
			if (longSignal && _allowedDirection != -1) {
				_entryDirection = 1;
				_entryPrice = candle.ClosePrice;
				_stopPrice = _entryPrice - StopLoss * step;
				_takePrice = _entryPrice + TakeProfit * step;
				_breakEvenActivated = false;
				BuyMarket(Volume);
			} else if (shortSignal && _allowedDirection != 1) {
				_entryDirection = -1;
				_entryPrice = candle.ClosePrice;
				_stopPrice = _entryPrice + StopLoss * step;
				_takePrice = _entryPrice - TakeProfit * step;
				_breakEvenActivated = false;
				SellMarket(Volume);
			}
		}
	}

	private void UpdateAllowedDirection(decimal exitPrice) {
		var profit = _entryDirection > 0 ? exitPrice - _entryPrice
										 : _entryPrice - exitPrice;
		_allowedDirection = profit > 0 ? (_entryDirection > 0 ? -1 : 1)
									   : (_entryDirection > 0 ? 1 : -1);
		_entryDirection = 0;
	}
}
