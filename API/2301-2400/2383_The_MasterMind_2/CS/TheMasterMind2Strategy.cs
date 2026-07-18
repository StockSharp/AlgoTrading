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
/// Strategy based on Stochastic Oscillator and Williams %R.
/// Buys when Stochastic is very low and Williams %R confirms oversold.
/// Sells when Stochastic is very high and Williams %R confirms overbought.
/// Includes trailing stop and break-even logic.
/// </summary>
public class TheMasterMind2Strategy : Strategy
{
	private readonly StrategyParam<decimal> _lotSize;
	private readonly StrategyParam<int> _stochasticPeriod;
	private readonly StrategyParam<int> _stochasticK;
	private readonly StrategyParam<int> _stochasticD;
	private readonly StrategyParam<int> _wprPeriod;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<decimal> _trailingStepPoints;
	private readonly StrategyParam<decimal> _breakEvenPoints;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _takeProfitPrice;
	private decimal _prevSignal = decimal.MinValue;
	private decimal _prevWpr = decimal.MinValue;

	/// <summary>
	/// Trade volume in contracts.
	/// </summary>
	public decimal LotSize
	{
		get => _lotSize.Value;
		set => _lotSize.Value = value;
	}

	/// <summary>
	/// Period for Stochastic calculation.
	/// </summary>
	public int StochasticPeriod
	{
		get => _stochasticPeriod.Value;
		set => _stochasticPeriod.Value = value;
	}

	/// <summary>
	/// Smoothing for %K line.
	/// </summary>
	public int StochasticK
	{
		get => _stochasticK.Value;
		set => _stochasticK.Value = value;
	}

	/// <summary>
	/// Smoothing for %D line.
	/// </summary>
	public int StochasticD
	{
		get => _stochasticD.Value;
		set => _stochasticD.Value = value;
	}

	/// <summary>
	/// Period for Williams %R.
	/// </summary>
	public int WilliamsRPeriod
	{
		get => _wprPeriod.Value;
		set => _wprPeriod.Value = value;
	}

	/// <summary>
	/// Initial stop loss in price points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Initial take profit in price points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in points.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Trailing step in points.
	/// </summary>
	public decimal TrailingStepPoints
	{
		get => _trailingStepPoints.Value;
		set => _trailingStepPoints.Value = value;
	}

	/// <summary>
	/// Break-even activation distance in points.
	/// </summary>
	public decimal BreakEvenPoints
	{
		get => _breakEvenPoints.Value;
		set => _breakEvenPoints.Value = value;
	}

	/// <summary>
	/// Type of candles to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public TheMasterMind2Strategy()
	{
		_lotSize = Param(nameof(LotSize), 0.1m).SetDisplay("Lot Size", "Trade volume in contracts", "General");

		_stochasticPeriod = Param(nameof(StochasticPeriod), 50)
								.SetDisplay("Stochastic Period", "Period for Stochastic calculation", "Indicators")
								;

		_stochasticK = Param(nameof(StochasticK), 3)
						   .SetDisplay("Stochastic %K", "Smoothing of %K line", "Indicators")
						   ;

		_stochasticD = Param(nameof(StochasticD), 3)
						   .SetDisplay("Stochastic %D", "Smoothing of %D line", "Indicators")
						   ;

		_wprPeriod = Param(nameof(WilliamsRPeriod), 50)
						 .SetDisplay("Williams %R Period", "Period for Williams %R", "Indicators")
						 ;

		_stopLossPoints = Param(nameof(StopLossPoints), 500m)
							  .SetDisplay("Stop Loss", "Initial stop loss in price points", "Risk")
							  ;

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 500m)
								.SetDisplay("Take Profit", "Initial take profit in price points", "Risk")
								;

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 50m)
								  .SetDisplay("Trailing Stop", "Trailing stop distance in points", "Risk")
								  ;

		_trailingStepPoints = Param(nameof(TrailingStepPoints), 100m)
								  .SetDisplay("Trailing Step", "Minimum move to adjust trailing stop", "Risk")
								  ;

		_breakEvenPoints = Param(nameof(BreakEvenPoints), 150m)
							   .SetDisplay("Break Even", "Move stop to entry after profit", "Risk")
							   ;

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
						  .SetDisplay("Candle Type", "Type of candles", "General");
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
		ResetStops();
		_prevSignal = decimal.MinValue;
		_prevWpr = decimal.MinValue;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetStops();
		_prevSignal = decimal.MinValue;
		_prevWpr = decimal.MinValue;

		var stochastic = new StochasticOscillator();
		stochastic.K.Length = StochasticPeriod;
		stochastic.D.Length = StochasticD;

		var williams = new WilliamsR { Length = WilliamsRPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(stochastic, williams, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, stochastic);
			DrawIndicator(area, williams);
			DrawOwnTrades(area);
		}
	}

	private void ResetStops()
	{
		_entryPrice = 0m;
		_stopPrice = 0m;
		_takeProfitPrice = 0m;
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochValue, IIndicatorValue wprValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var stochTyped = (StochasticOscillatorValue)stochValue;
		if (stochTyped.D is not decimal signal)
			return;

		var wpr = wprValue.ToDecimal();
		var step = Security?.PriceStep ?? 1m;

		if (_prevSignal == decimal.MinValue)
		{
			_prevSignal = signal;
			_prevWpr = wpr;
			return;
		}

		// Manage existing position
		if (Position > 0)
		{
			// Activate break-even
			if (BreakEvenPoints > 0m && candle.ClosePrice - _entryPrice >= BreakEvenPoints * step &&
				(_stopPrice == 0m || _stopPrice < _entryPrice))
				_stopPrice = _entryPrice;

			// Update trailing stop
			if (TrailingStopPoints > 0m)
			{
				var target = candle.ClosePrice - TrailingStopPoints * step;
				if (_stopPrice == 0m || target - _stopPrice >= TrailingStepPoints * step)
					_stopPrice = target;
			}

			// Check stop or take profit
			if (candle.LowPrice <= _stopPrice || (_takeProfitPrice > 0m && candle.HighPrice >= _takeProfitPrice))
			{
				SellMarket();
				ResetStops();
			}
		}
		else if (Position < 0)
		{
			if (BreakEvenPoints > 0m && _entryPrice - candle.ClosePrice >= BreakEvenPoints * step &&
				(_stopPrice == 0m || _stopPrice > _entryPrice))
				_stopPrice = _entryPrice;

			if (TrailingStopPoints > 0m)
			{
				var target = candle.ClosePrice + TrailingStopPoints * step;
				if (_stopPrice == 0m || _stopPrice - target >= TrailingStepPoints * step)
					_stopPrice = target;
			}

			if (candle.HighPrice >= _stopPrice || (_takeProfitPrice > 0m && candle.LowPrice <= _takeProfitPrice))
			{
				BuyMarket();
				ResetStops();
			}
		}

		// Generate trade signals
		if (_prevSignal >= 20m && signal < 20m && _prevWpr >= -80m && wpr < -80m && Position <= 0)
		{
			BuyMarket();
			_entryPrice = candle.ClosePrice;
			_stopPrice = _entryPrice - StopLossPoints * step;
			_takeProfitPrice = _entryPrice + TakeProfitPoints * step;
		}
		else if (_prevSignal <= 80m && signal > 80m && _prevWpr <= -20m && wpr > -20m && Position >= 0)
		{
			SellMarket();
			_entryPrice = candle.ClosePrice;
			_stopPrice = _entryPrice + StopLossPoints * step;
			_takeProfitPrice = _entryPrice - TakeProfitPoints * step;
		}

		_prevSignal = signal;
		_prevWpr = wpr;
	}
}
