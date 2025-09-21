using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader expert ARD_ORDER_MANAGEMENT_EA-BETA_1.
/// Automates the Stochastic oscillator entries while ensuring existing positions are closed before reversing.
/// Includes optional trailing management that mirrors the manual order maintenance logic from the original EA.
/// </summary>
public class ArdOrderManagementStrategy : Strategy
{
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _modifyTakeProfitPips;
	private readonly StrategyParam<decimal> _modifyStopLossPips;
	private readonly StrategyParam<int> _stochasticPeriod;
	private readonly StrategyParam<int> _signalPeriod;
	private readonly StrategyParam<int> _slowingPeriod;
	private readonly StrategyParam<decimal> _buyThreshold;
	private readonly StrategyParam<decimal> _sellThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private StochasticOscillator _stochastic = null!;
	private decimal _pipSize;
	private decimal _longTrailingStop;
	private decimal _longTrailingTarget;
	private decimal _shortTrailingStop;
	private decimal _shortTrailingTarget;

	/// <summary>
	/// Trading volume used for new market entries.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Initial take-profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Initial stop-loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Distance in pips used when refreshing trailing take-profit targets.
	/// </summary>
	public decimal ModifyTakeProfitPips
	{
		get => _modifyTakeProfitPips.Value;
		set => _modifyTakeProfitPips.Value = value;
	}

	/// <summary>
	/// Distance in pips used when refreshing trailing stop levels.
	/// </summary>
	public decimal ModifyStopLossPips
	{
		get => _modifyStopLossPips.Value;
		set => _modifyStopLossPips.Value = value;
	}

	/// <summary>
	/// Lookback period of the Stochastic oscillator.
	/// </summary>
	public int StochasticPeriod
	{
		get => _stochasticPeriod.Value;
		set => _stochasticPeriod.Value = value;
	}

	/// <summary>
	/// Signal period (%%D smoothing) of the Stochastic oscillator.
	/// </summary>
	public int SignalPeriod
	{
		get => _signalPeriod.Value;
		set => _signalPeriod.Value = value;
	}

	/// <summary>
	/// Slowing parameter applied to the %%K line.
	/// </summary>
	public int SlowingPeriod
	{
		get => _slowingPeriod.Value;
		set => _slowingPeriod.Value = value;
	}

	/// <summary>
	/// Overbought threshold that triggers long entries after closing shorts.
	/// </summary>
	public decimal BuyThreshold
	{
		get => _buyThreshold.Value;
		set => _buyThreshold.Value = value;
	}

	/// <summary>
	/// Oversold threshold that triggers short entries after closing longs.
	/// </summary>
	public decimal SellThreshold
	{
		get => _sellThreshold.Value;
		set => _sellThreshold.Value = value;
	}

	/// <summary>
	/// Candle type that drives the indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes default parameters.
	/// </summary>
	public ArdOrderManagementStrategy()
	{
		_volume = Param(nameof(Volume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Trading volume used for market orders", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 5m, 0.1m);

		_takeProfitPips = Param(nameof(TakeProfitPips), 100m)
			.SetNotNegative()
			.SetDisplay("Take Profit (pips)", "Initial profit target distance", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(10m, 300m, 10m);

		_stopLossPips = Param(nameof(StopLossPips), 50m)
			.SetNotNegative()
			.SetDisplay("Stop Loss (pips)", "Initial protective stop distance", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(10m, 200m, 10m);

		_modifyTakeProfitPips = Param(nameof(ModifyTakeProfitPips), 100m)
			.SetNotNegative()
			.SetDisplay("Trailing Take Profit (pips)", "Distance maintained when refreshing profit targets", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0m, 300m, 10m);

		_modifyStopLossPips = Param(nameof(ModifyStopLossPips), 20m)
			.SetNotNegative()
			.SetDisplay("Trailing Stop (pips)", "Distance maintained when refreshing stop levels", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0m, 200m, 10m);

		_stochasticPeriod = Param(nameof(StochasticPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic Period", "Lookback period for %K calculation", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(3, 40, 1);

		_signalPeriod = Param(nameof(SignalPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("Signal Period", "Smoothing period for %D", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(1, 20, 1);

		_slowingPeriod = Param(nameof(SlowingPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("Slowing", "Smoothing applied to %K", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(1, 20, 1);

		_buyThreshold = Param(nameof(BuyThreshold), 80m)
			.SetDisplay("Buy Threshold", "Overbought level that triggers long entries", "Signals")
			.SetCanOptimize(true)
			.SetOptimize(55m, 95m, 5m);

		_sellThreshold = Param(nameof(SellThreshold), 20m)
			.SetDisplay("Sell Threshold", "Oversold level that triggers short entries", "Signals")
			.SetCanOptimize(true)
			.SetOptimize(5m, 45m, 5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Primary candle source for calculations", "General");
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

		_pipSize = CalculatePipSize();

		// Configure the Stochastic oscillator exactly once when the strategy starts.
		_stochastic = new StochasticOscillator
		{
			Length = StochasticPeriod,
			K = { Length = SlowingPeriod },
			D = { Length = SignalPeriod },
			Slowing = SlowingPeriod,
		};

		var subscription = SubscribeCandles(CandleType);

		// Bind the indicator to incoming candles and start the subscription.
		subscription
			.BindEx(_stochastic, ProcessCandle)
			.Start();

		var stopLoss = StopLossPips > 0m ? new Unit(StopLossPips * _pipSize, UnitTypes.Point) : null;
		var takeProfit = TakeProfitPips > 0m ? new Unit(TakeProfitPips * _pipSize, UnitTypes.Point) : null;

		// Start the built-in protection module to emulate the EA's fixed SL/TP placement.
		StartProtection(stopLoss: stopLoss, takeProfit: takeProfit);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _stochastic);
			DrawOwnTrades(area);
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
		{
			ResetLongState();
			ResetShortState();
			return;
		}

		if (Position > 0m && delta > 0m)
		{
			_longTrailingStop = ModifyStopLossPips > 0m ? PositionPrice - ModifyStopLossPips * _pipSize : 0m;
			_longTrailingTarget = ModifyTakeProfitPips > 0m ? PositionPrice + ModifyTakeProfitPips * _pipSize : 0m;
			ResetShortState();
		}
		else if (Position < 0m && delta < 0m)
		{
			_shortTrailingStop = ModifyStopLossPips > 0m ? PositionPrice + ModifyStopLossPips * _pipSize : 0m;
			_shortTrailingTarget = ModifyTakeProfitPips > 0m ? PositionPrice - ModifyTakeProfitPips * _pipSize : 0m;
			ResetLongState();
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochasticValue)
	{
		// Ignore incomplete candles to stay aligned with the original EA's close-based logic.
		if (candle.State != CandleStates.Finished)
			return;

		// Manage trailing logic before evaluating fresh signals.
		if (Position != 0m && UpdateTrailingProtection(candle))
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!stochasticValue.IsFinal)
			return;

		var value = (StochasticOscillatorValue)stochasticValue;
		if (value.K is not decimal kValue || value.D is not decimal dValue)
			return;

		LogInfo($"Stochastic %K={kValue:F2}, %D={dValue:F2}");

		if (kValue >= BuyThreshold && Position <= 0m)
		{
			LogInfo("Buy condition satisfied. Closing shorts (if any) and opening a long position.");
			EnterLong(candle.ClosePrice);
		}
		else if (kValue <= SellThreshold && Position >= 0m)
		{
			LogInfo("Sell condition satisfied. Closing longs (if any) and opening a short position.");
			EnterShort(candle.ClosePrice);
		}
	}

	private void EnterLong(decimal price)
	{
		var volume = Volume;
		if (Position < 0m)
		{
			volume += Math.Abs(Position);
			ResetShortState();
		}

		if (volume <= 0m)
		{
			LogWarning("Volume is non-positive. Long entry aborted.");
			return;
		}

		CancelActiveOrders();

		// Combine covering and new exposure in a single market order to mirror the EA behaviour.
		BuyMarket(volume);

			_longTrailingStop = ModifyStopLossPips > 0m ? price - ModifyStopLossPips * _pipSize : 0m;
			_longTrailingTarget = ModifyTakeProfitPips > 0m ? price + ModifyTakeProfitPips * _pipSize : 0m;
	}

	private void EnterShort(decimal price)
	{
		var volume = Volume;
		if (Position > 0m)
		{
			volume += Position;
			ResetLongState();
		}

		if (volume <= 0m)
		{
			LogWarning("Volume is non-positive. Short entry aborted.");
			return;
		}

		CancelActiveOrders();
		SellMarket(volume);

			_shortTrailingStop = ModifyStopLossPips > 0m ? price + ModifyStopLossPips * _pipSize : 0m;
			_shortTrailingTarget = ModifyTakeProfitPips > 0m ? price - ModifyTakeProfitPips * _pipSize : 0m;
	}

	private bool UpdateTrailingProtection(ICandleMessage candle)
	{
		var trailingStopDistance = ModifyStopLossPips > 0m ? ModifyStopLossPips * _pipSize : 0m;
		var trailingTargetDistance = ModifyTakeProfitPips > 0m ? ModifyTakeProfitPips * _pipSize : 0m;

		if (Position > 0m)
		{
			if (trailingStopDistance > 0m)
			{
				var candidateStop = candle.ClosePrice - trailingStopDistance;
				if (candidateStop > _longTrailingStop)
					_longTrailingStop = candidateStop;

				if (_longTrailingStop > 0m && candle.LowPrice <= _longTrailingStop)
				{
					LogInfo($"Long trailing stop hit at {_longTrailingStop:F5}. Closing position.");
					SellMarket(Position);
					ResetLongState();
					return true;
				}
			}

			if (trailingTargetDistance > 0m)
			{
				var candidateTarget = candle.ClosePrice + trailingTargetDistance;
				if (candidateTarget > _longTrailingTarget)
					_longTrailingTarget = candidateTarget;

				if (_longTrailingTarget > 0m && candle.HighPrice >= _longTrailingTarget)
				{
					LogInfo($"Long trailing target reached at {_longTrailingTarget:F5}. Locking profits.");
					SellMarket(Position);
					ResetLongState();
					return true;
				}
			}
		}
		else if (Position < 0m)
		{
			if (trailingStopDistance > 0m)
			{
				var candidateStop = candle.ClosePrice + trailingStopDistance;
				if (_shortTrailingStop == 0m || candidateStop < _shortTrailingStop)
					_shortTrailingStop = candidateStop;

				if (_shortTrailingStop > 0m && candle.HighPrice >= _shortTrailingStop)
				{
					LogInfo($"Short trailing stop hit at {_shortTrailingStop:F5}. Closing position.");
					BuyMarket(Math.Abs(Position));
					ResetShortState();
					return true;
				}
			}

			if (trailingTargetDistance > 0m)
			{
				var candidateTarget = candle.ClosePrice - trailingTargetDistance;
				if (_shortTrailingTarget == 0m || candidateTarget < _shortTrailingTarget)
					_shortTrailingTarget = candidateTarget;

				if (_shortTrailingTarget > 0m && candle.LowPrice <= _shortTrailingTarget)
				{
					LogInfo($"Short trailing target reached at {_shortTrailingTarget:F5}. Locking profits.");
					BuyMarket(Math.Abs(Position));
					ResetShortState();
					return true;
				}
			}
		}

		return false;
	}

	private void ResetLongState()
	{
		_longTrailingStop = 0m;
			_longTrailingTarget = 0m;
	}

	private void ResetShortState()
	{
		_shortTrailingStop = 0m;
			_shortTrailingTarget = 0m;
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 0m;

		if (step <= 0m)
			return 1m;

		if (step < 0.001m)
			return step * 10m;

		return step;
	}
}
