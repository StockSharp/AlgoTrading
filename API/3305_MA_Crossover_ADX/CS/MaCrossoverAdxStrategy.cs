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
/// Port of the MetaTrader "MA_Crossover_ADX" expert advisor.
/// Combines EMA slope, previous close confirmation, and ADX directional balance filters.
/// Applies optional protective stop loss and take profit distances expressed in instrument points.
/// </summary>
public class MaCrossoverAdxStrategy : Strategy
{
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _emaCurrent;
	private decimal? _emaPrevious;
	private decimal? _emaTwoAgo;
	private decimal? _previousClose;
	private decimal? _currentAdx;
	private decimal? _currentPlusDi;
	private decimal? _currentMinusDi;
	private DateTimeOffset? _emaUpdateTime;
	private DateTimeOffset? _adxUpdateTime;
	private DateTimeOffset? _lastProcessedTime;
	private decimal _pipSize;

	/// <summary>
	/// Period used for ADX calculation.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// Minimum ADX value required to confirm the trend strength.
	/// </summary>
	public decimal AdxThreshold
	{
		get => _adxThreshold.Value;
		set => _adxThreshold.Value = value;
	}

	/// <summary>
	/// Exponential moving average period.
	/// </summary>
	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	/// <summary>
	/// Stop loss distance in instrument points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance in instrument points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Trade volume for new entries.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MaCrossoverAdxStrategy"/> class.
	/// </summary>
	public MaCrossoverAdxStrategy()
	{
		_adxPeriod = Param(nameof(AdxPeriod), 33)
			.SetDisplay("ADX Period", "Number of bars used for ADX smoothing.", "Indicators")
			.SetCanOptimize(true);

		_adxThreshold = Param(nameof(AdxThreshold), 22m)
			.SetDisplay("ADX Threshold", "Minimum ADX main line confirming trend strength.", "Indicators")
			.SetCanOptimize(true);

		_emaPeriod = Param(nameof(EmaPeriod), 39)
			.SetDisplay("EMA Period", "Length of the confirming exponential moving average.", "Indicators")
			.SetCanOptimize(true);

		_stopLossPoints = Param(nameof(StopLossPoints), 400m)
			.SetDisplay("Stop Loss (points)", "Protective stop loss distance expressed in instrument points.", "Risk Management")
			.SetCanOptimize(true);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 900m)
			.SetDisplay("Take Profit (points)", "Target profit distance expressed in instrument points.", "Risk Management")
			.SetCanOptimize(true);

		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
			.SetDisplay("Trade Volume", "Order volume submitted on a fresh entry.", "General")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe powering the EMA and ADX filters.", "Data");
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

		// Clear cached indicator values when the strategy is reset.
		_emaCurrent = null;
		_emaPrevious = null;
		_emaTwoAgo = null;
		_previousClose = null;
		_currentAdx = null;
		_currentPlusDi = null;
		_currentMinusDi = null;
		_emaUpdateTime = null;
		_adxUpdateTime = null;
		_lastProcessedTime = null;
		_pipSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Resolve the minimum price increment for converting point based risk parameters.
		_pipSize = Security?.PriceStep ?? 0m;
		if (_pipSize <= 0m)
		{
			_pipSize = 1m;
		}

		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var adx = new AverageDirectionalIndex { Length = AdxPeriod };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(ema, OnEmaUpdated)
			.BindEx(adx, OnAdxUpdated)
			.Start();

		// Configure protective stop loss and take profit orders once at startup.
		StartProtection(
			takeProfit: TakeProfitPoints > 0m ? new Unit(TakeProfitPoints * _pipSize, UnitTypes.Absolute) : null,
			stopLoss: StopLossPoints > 0m ? new Unit(StopLossPoints * _pipSize, UnitTypes.Absolute) : null,
			useMarketOrders: true);
	}

	private void OnEmaUpdated(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
		{
			return;
		}

		// Shift EMA values to maintain the last three readings.
		_emaTwoAgo = _emaPrevious;
		_emaPrevious = _emaCurrent;
		_emaCurrent = emaValue;
		_emaUpdateTime = candle.CloseTime;

		TryProcessSignal(candle);

		// Store the close price of the completed candle for the next iteration.
		_previousClose = candle.ClosePrice;
	}

	private void OnAdxUpdated(ICandleMessage candle, IIndicatorValue adxValue)
	{
		if (candle.State != CandleStates.Finished)
		{
			return;
		}

		if (!adxValue.IsFinal || adxValue is not AverageDirectionalIndexValue typed)
		{
			return;
		}

		if (typed.MovingAverage is not decimal adxMain)
		{
			return;
		}

		var dx = typed.Dx;
		if (dx.Plus is not decimal plusDi || dx.Minus is not decimal minusDi)
		{
			return;
		}

		_currentAdx = adxMain;
		_currentPlusDi = plusDi;
		_currentMinusDi = minusDi;
		_adxUpdateTime = candle.CloseTime;

		TryProcessSignal(candle);
	}

	private void TryProcessSignal(ICandleMessage candle)
	{
		if (_emaUpdateTime != candle.CloseTime || _adxUpdateTime != candle.CloseTime)
		{
			return; // Wait until EMA and ADX are synchronized on the same candle.
		}

		if (_lastProcessedTime == candle.CloseTime)
		{
			return; // Avoid duplicate processing within the same candle.
		}

		if (_emaCurrent is not decimal emaCurrent ||
			_emaPrevious is not decimal emaPrevious ||
			_emaTwoAgo is not decimal emaTwoAgo ||
			_previousClose is not decimal previousClose ||
			_currentAdx is not decimal adxMain ||
			_currentPlusDi is not decimal plusDi ||
			_currentMinusDi is not decimal minusDi)
		{
			return; // Ensure all required values are ready.
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			return;
		}

		_lastProcessedTime = candle.CloseTime;

		var currentSlope = emaCurrent - emaPrevious;
		var previousSlope = emaPrevious - emaTwoAgo;
		var closeDeviation = previousClose - emaPrevious;
		var directionalBalance = plusDi - minusDi;

		var buySignal = currentSlope > 0m &&
			previousSlope > 0m &&
			closeDeviation > 0m &&
			adxMain > AdxThreshold &&
			directionalBalance > 0m;

		var sellSignal = currentSlope < 0m &&
			previousSlope < 0m &&
			closeDeviation < 0m &&
			adxMain > AdxThreshold &&
			directionalBalance < 0m;

		if (Position > 0)
		{
			if (sellSignal)
			{
				// Exit long positions when bearish conditions appear.
				SellMarket(Position);
			}

			return;
		}

		if (Position < 0)
		{
			if (buySignal)
			{
				// Exit short positions when bullish conditions appear.
				BuyMarket(Math.Abs(Position));
			}

			return;
		}

		if (TradeVolume <= 0m)
		{
			return; // Do not place new trades when volume is disabled.
		}

		if (buySignal)
		{
			// Enter long when EMA slope, previous close and ADX filters align.
			BuyMarket(TradeVolume);
		}
		else if (sellSignal)
		{
			// Enter short when all bearish conditions align.
			SellMarket(TradeVolume);
		}
	}
}

