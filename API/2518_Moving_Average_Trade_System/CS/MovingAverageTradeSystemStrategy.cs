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
/// Moving Average Trade System originally written for MetaTrader.
/// Uses four simple moving averages on median price to detect medium-term trend reversals.
/// </summary>
public class MovingAverageTradeSystemStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfitSteps;
	private readonly StrategyParam<decimal> _stopLossSteps;
	private readonly StrategyParam<decimal> _trailingStopSteps;
	private readonly StrategyParam<decimal> _slopeThresholdSteps;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _mediumPeriod;
	private readonly StrategyParam<int> _signalPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _smaFast;
	private SimpleMovingAverage _smaMedium;
	private SimpleMovingAverage _smaSignal;
	private SimpleMovingAverage _smaSlow;

	private decimal? _previousSignal;

	private decimal? _longEntryPrice;
	private decimal? _longTakeProfit;
	private decimal? _longStopLoss;
	private decimal _longHigh;

	private decimal? _shortEntryPrice;
	private decimal? _shortTakeProfit;
	private decimal? _shortStopLoss;
	private decimal _shortLow;


	/// <summary>
	/// Desired take profit distance in price steps.
	/// </summary>
	public decimal TakeProfitSteps
	{
		get => _takeProfitSteps.Value;
		set => _takeProfitSteps.Value = value;
	}

	/// <summary>
	/// Desired stop loss distance in price steps.
	/// </summary>
	public decimal StopLossSteps
	{
		get => _stopLossSteps.Value;
		set => _stopLossSteps.Value = value;
	}

	/// <summary>
	/// Trailing stop offset in price steps.
	/// </summary>
	public decimal TrailingStopSteps
	{
		get => _trailingStopSteps.Value;
		set => _trailingStopSteps.Value = value;
	}

	/// <summary>
	/// Minimum separation between the signal SMA and the slow SMA (in price steps) to validate breakouts.
	/// </summary>
	public decimal SlopeThresholdSteps
	{
		get => _slopeThresholdSteps.Value;
		set => _slopeThresholdSteps.Value = value;
	}

	/// <summary>
	/// Fast SMA period (originally 5).
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Medium SMA period (originally 20).
	/// </summary>
	public int MediumPeriod
	{
		get => _mediumPeriod.Value;
		set => _mediumPeriod.Value = value;
	}

	/// <summary>
	/// Signal SMA period that must cross the slow SMA upward or downward (originally 40).
	/// </summary>
	public int SignalPeriod
	{
		get => _signalPeriod.Value;
		set => _signalPeriod.Value = value;
	}

	/// <summary>
	/// Slow SMA period defining the background trend (originally 60).
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Candle type for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MovingAverageTradeSystemStrategy"/> class.
	/// </summary>
	public MovingAverageTradeSystemStrategy()
	{

		_takeProfitSteps = Param(nameof(TakeProfitSteps), 50m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit (steps)", "Distance to take profit in price steps", "Risk Management")
			
			.SetOptimize(10m, 200m, 10m);

		_stopLossSteps = Param(nameof(StopLossSteps), 50m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss (steps)", "Distance to stop loss in price steps", "Risk Management")
			
			.SetOptimize(10m, 200m, 10m);

		_trailingStopSteps = Param(nameof(TrailingStopSteps), 11m)
			.SetNotNegative()
			.SetDisplay("Trailing Stop (steps)", "Trailing stop offset in price steps", "Risk Management")
			
			.SetOptimize(0m, 100m, 5m);

		_slopeThresholdSteps = Param(nameof(SlopeThresholdSteps), 0m)
			.SetNotNegative()
			.SetDisplay("Slope Threshold", "Minimum SMA40 vs SMA60 distance in steps", "Signals")
			
			.SetOptimize(0m, 10m, 1m);

		_fastPeriod = Param(nameof(FastPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Fast SMA", "Fast SMA length", "Signals")
			
			.SetOptimize(3, 20, 1);

		_mediumPeriod = Param(nameof(MediumPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Medium SMA", "Medium SMA length", "Signals")
			
			.SetOptimize(10, 60, 1);

		_signalPeriod = Param(nameof(SignalPeriod), 40)
			.SetGreaterThanZero()
			.SetDisplay("Signal SMA", "Crossing SMA length", "Signals")
			
			.SetOptimize(20, 80, 1);

		_slowPeriod = Param(nameof(SlowPeriod), 60)
			.SetGreaterThanZero()
			.SetDisplay("Slow SMA", "Slow SMA length", "Signals")
			
			.SetOptimize(30, 120, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for calculations", "Data");
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

		_previousSignal = null;

		ResetLongState();
		ResetShortState();
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		// Create the moving averages on median price to match the original indicator setup.
		_smaFast = new SMA { Length = FastPeriod };
		_smaMedium = new SMA { Length = MediumPeriod };
		_smaSignal = new SMA { Length = SignalPeriod };
		_smaSlow = new SMA { Length = SlowPeriod };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(_smaFast, _smaMedium, _smaSignal, _smaSlow, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _smaFast);
			DrawIndicator(area, _smaMedium);
			DrawIndicator(area, _smaSignal);
			DrawIndicator(area, _smaSlow);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaFast, decimal smaMedium, decimal smaSignal, decimal smaSlow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_smaFast.IsFormed || !_smaMedium.IsFormed || !_smaSignal.IsFormed || !_smaSlow.IsFormed)
		{
			_previousSignal = smaSignal;
			return;
		}

		var previousSignal = _previousSignal;
		_previousSignal = smaSignal;

		if (previousSignal is null)
			return;

		var priceStep = GetPriceStep();
		var slopeThreshold = SlopeThresholdSteps * priceStep;

		var bullishStructure = smaFast > smaMedium && smaMedium > smaSignal;
		var bearishStructure = smaFast < smaMedium && smaMedium < smaSignal;
		var bullishSlope = (smaSignal - smaSlow) >= slopeThreshold;
		var bearishSlope = (smaSlow - smaSignal) >= slopeThreshold;
		var bullishCross = previousSignal.Value <= smaSlow;
		var bearishCross = previousSignal.Value >= smaSlow;

		var buySignal = bullishStructure && bullishSlope && bullishCross;
		var sellSignal = bearishStructure && bearishSlope && bearishCross;

		if (buySignal && Position <= 0m)
		{
			// Flip the position by buying enough volume to cover shorts and add the desired long exposure.
			var volume = Volume + (Position < 0m ? Math.Abs(Position) : 0m);

			if (volume > 0m)
				BuyMarket();
		}
		else if (sellSignal && Position >= 0m)
		{
			// Flip the position by selling enough volume to cover longs and add the desired short exposure.
			var volume = Volume + (Position > 0m ? Position : 0m);

			if (volume > 0m)
				SellMarket();
		}
		else
		{
			// Manage open positions when no new entry is triggered this bar.
			if (Position > 0m)
			{
				if (smaSignal <= smaSlow)
				{
					SellMarket();
				}
				else
				{
					ManageLongPosition(candle, priceStep);
				}
			}
			else if (Position < 0m)
			{
				if (smaSignal >= smaSlow)
				{
					BuyMarket();
				}
				else
				{
					ManageShortPosition(candle, priceStep);
				}
			}
		}
	}

	private void ManageLongPosition(ICandleMessage candle, decimal priceStep)
	{
		if (!_longEntryPrice.HasValue)
			return;

		_longHigh = Math.Max(_longHigh, candle.HighPrice);

		if (_longTakeProfit.HasValue && candle.HighPrice >= _longTakeProfit.Value)
		{
			SellMarket();
			return;
		}

		if (_longStopLoss.HasValue && candle.LowPrice <= _longStopLoss.Value)
		{
			SellMarket();
			return;
		}

		if (TrailingStopSteps <= 0m)
			return;

		var trailingLevel = _longHigh - TrailingStopSteps * priceStep;
		if (candle.ClosePrice <= trailingLevel)
			SellMarket();
	}

	private void ManageShortPosition(ICandleMessage candle, decimal priceStep)
	{
		if (!_shortEntryPrice.HasValue)
			return;

		_shortLow = Math.Min(_shortLow, candle.LowPrice);

		if (_shortTakeProfit.HasValue && candle.LowPrice <= _shortTakeProfit.Value)
		{
			BuyMarket();
			return;
		}

		if (_shortStopLoss.HasValue && candle.HighPrice >= _shortStopLoss.Value)
		{
			BuyMarket();
			return;
		}

		if (TrailingStopSteps <= 0m)
			return;

		var trailingLevel = _shortLow + TrailingStopSteps * priceStep;
		if (candle.ClosePrice >= trailingLevel)
			BuyMarket();
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		var price = trade.Trade?.Price;
		if (price is null)
			return;

		if (Position > 0m)
		{
			// After switching to long reset the short tracking and configure profit/stop levels.
			ResetShortState();
			SetupLongState(price.Value);
		}
		else if (Position < 0m)
		{
			// After switching to short reset the long tracking and configure profit/stop levels.
			ResetLongState();
			SetupShortState(price.Value);
		}
		else
		{
			// Flat position clears both trackers.
			ResetLongState();
			ResetShortState();
		}
	}

	private void SetupLongState(decimal entryPrice)
	{
		var priceStep = GetPriceStep();

		_longEntryPrice = entryPrice;
		_longHigh = entryPrice;
		_longTakeProfit = TakeProfitSteps > 0m ? entryPrice + TakeProfitSteps * priceStep : null;
		_longStopLoss = StopLossSteps > 0m ? entryPrice - StopLossSteps * priceStep : null;
	}

	private void SetupShortState(decimal entryPrice)
	{
		var priceStep = GetPriceStep();

		_shortEntryPrice = entryPrice;
		_shortLow = entryPrice;
		_shortTakeProfit = TakeProfitSteps > 0m ? entryPrice - TakeProfitSteps * priceStep : null;
		_shortStopLoss = StopLossSteps > 0m ? entryPrice + StopLossSteps * priceStep : null;
	}

	private void ResetLongState()
	{
		_longEntryPrice = null;
		_longTakeProfit = null;
		_longStopLoss = null;
		_longHigh = 0m;
	}

	private void ResetShortState()
	{
		_shortEntryPrice = null;
		_shortTakeProfit = null;
		_shortStopLoss = null;
		_shortLow = 0m;
	}

	private decimal GetPriceStep()
	{
		var step = Security?.PriceStep;
		return step is null || step == 0m ? 1m : step.Value;
	}
}