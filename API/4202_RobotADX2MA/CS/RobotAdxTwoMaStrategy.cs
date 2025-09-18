using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the "Robot_ADX+2MA" MetaTrader strategy combining exponential moving averages with ADX filters.
/// </summary>
public class RobotAdxTwoMaStrategy : Strategy
{
	private const int FastEmaPeriod = 5;
	private const int SlowEmaPeriod = 12;
	private const int AdxPeriod = 6;

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _differenceThreshold;

	private ExponentialMovingAverage? _fastEma;
	private ExponentialMovingAverage? _slowEma;
	private AverageDirectionalIndex? _adx;

	private decimal? _previousFastEma;
	private decimal? _previousSlowEma;
	private decimal? _currentFastEma;
	private decimal? _currentSlowEma;

	private decimal? _previousPlusDi;
	private decimal? _previousMinusDi;
	private decimal? _currentPlusDi;
	private decimal? _currentMinusDi;

	private DateTimeOffset? _emaUpdateTime;
	private DateTimeOffset? _adxUpdateTime;
	private DateTimeOffset? _lastProcessedTime;

	public RobotAdxTwoMaStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe processed by the strategy.", "General");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 4700)
			.SetGreaterOrEqualZero()
			.SetDisplay("Take Profit (points)", "Distance to the take profit measured in price steps.", "Risk");

		_stopLossPoints = Param(nameof(StopLossPoints), 2400)
			.SetGreaterOrEqualZero()
			.SetDisplay("Stop Loss (points)", "Distance to the stop loss measured in price steps.", "Risk");

		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Default volume for every market order.", "Trading");

		_differenceThreshold = Param(nameof(DifferenceThreshold), 10)
			.SetGreaterOrEqualZero()
			.SetDisplay("EMA Difference", "Minimum distance between the fast and slow EMA expressed in price steps.", "Indicator");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	public int DifferenceThreshold
	{
		get => _differenceThreshold.Value;
		set => _differenceThreshold.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_fastEma = null;
		_slowEma = null;
		_adx = null;

		_previousFastEma = null;
		_previousSlowEma = null;
		_currentFastEma = null;
		_currentSlowEma = null;

		_previousPlusDi = null;
		_previousMinusDi = null;
		_currentPlusDi = null;
		_currentMinusDi = null;

		_emaUpdateTime = null;
		_adxUpdateTime = null;
		_lastProcessedTime = null;

		Volume = TradeVolume;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume; // Align helper methods with the MetaTrader lot size.

		_fastEma = new ExponentialMovingAverage { Length = FastEmaPeriod };
		_slowEma = new ExponentialMovingAverage { Length = SlowEmaPeriod };
		_adx = new AverageDirectionalIndex { Length = AdxPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fastEma, _slowEma, ProcessMovingAverages)
			.BindEx(_adx, ProcessAdx)
			.Start();

		var takeProfitUnit = TakeProfitPoints > 0 ? new Unit(TakeProfitPoints, UnitTypes.Step) : null;
		var stopLossUnit = StopLossPoints > 0 ? new Unit(StopLossPoints, UnitTypes.Step) : null;

		if (takeProfitUnit != null || stopLossUnit != null)
		{
			StartProtection(takeProfit: takeProfitUnit, stopLoss: stopLossUnit, useMarketOrders: true);
		}

		var priceArea = CreateChartArea();
		if (priceArea != null)
		{
			DrawCandles(priceArea, subscription);
			if (_fastEma != null)
			{
				DrawIndicator(priceArea, _fastEma);
			}
			if (_slowEma != null)
			{
				DrawIndicator(priceArea, _slowEma);
			}
			DrawOwnTrades(priceArea);
		}

		if (_adx != null)
		{
			var adxArea = CreateChartArea();
			if (adxArea != null)
			{
				DrawIndicator(adxArea, _adx);
			}
		}
	}

	private void ProcessMovingAverages(ICandleMessage candle, decimal fastEmaValue, decimal slowEmaValue)
	{
		if (candle.State != CandleStates.Finished)
		{
			return; // Process only completed candles to mirror MetaTrader behaviour.
		}

		_previousFastEma = _currentFastEma;
		_previousSlowEma = _currentSlowEma;

		_currentFastEma = fastEmaValue;
		_currentSlowEma = slowEmaValue;

		_emaUpdateTime = candle.CloseTime;

		TryProcessSignal(candle);
	}

	private void ProcessAdx(ICandleMessage candle, IIndicatorValue adxValue)
	{
		if (candle.State != CandleStates.Finished)
		{
			return;
		}

		if (!adxValue.IsFinal || adxValue is not AverageDirectionalIndexValue adx)
		{
			return; // Wait until ADX provides a full data point.
		}

		_previousPlusDi = _currentPlusDi;
		_previousMinusDi = _currentMinusDi;

		_currentPlusDi = adx.Dx.Plus;
		_currentMinusDi = adx.Dx.Minus;

		_adxUpdateTime = candle.CloseTime;

		TryProcessSignal(candle);
	}

	private void TryProcessSignal(ICandleMessage candle)
	{
		if (_emaUpdateTime != candle.CloseTime || _adxUpdateTime != candle.CloseTime)
		{
			return; // Ensure EMA and ADX were updated for the same candle.
		}

		if (_lastProcessedTime == candle.CloseTime)
		{
			return; // Avoid double-processing when both bindings trigger sequentially.
		}

		if (_previousFastEma is null || _previousSlowEma is null)
		{
			return; // Need the EMA values from the previous candle (shift = 1 in MQL).
		}

		if (_previousPlusDi is null || _previousMinusDi is null || _currentPlusDi is null || _currentMinusDi is null)
		{
			return; // Need current and previous +DI/-DI to evaluate the filters.
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			return; // Wait until subscriptions are ready.
		}

		if (TradeVolume <= 0m)
		{
			return;
		}

		_lastProcessedTime = candle.CloseTime;

		var priceStep = Security?.PriceStep ?? 0m;
		var difference = Math.Abs(_previousFastEma.Value - _previousSlowEma.Value);
		var minimumDifference = priceStep > 0m
			? DifferenceThreshold * priceStep
			: DifferenceThreshold;

		if (difference <= minimumDifference)
		{
			return; // The EMAs are too close to justify an entry.
		}

		var canBuy = _previousFastEma.Value < _previousSlowEma.Value
			&& _previousPlusDi.Value < 5m
			&& _currentPlusDi.Value > 10m
			&& _currentPlusDi.Value > _currentMinusDi.Value;

		var canSell = _previousFastEma.Value > _previousSlowEma.Value
			&& _previousMinusDi.Value < 5m
			&& _currentMinusDi.Value > 10m
			&& _currentPlusDi.Value < _currentMinusDi.Value;

		if (Position != 0)
		{
			return; // The original EA opens a single position at a time.
		}

		if (canBuy)
		{
			BuyMarket(TradeVolume);
			return;
		}

		if (canSell)
		{
			SellMarket(TradeVolume);
		}
	}
}

