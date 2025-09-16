using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Parabolic SAR based strategy converted from the Expotest MQL expert advisor.
/// It enters trades in the SAR direction and doubles the next position size after a loss.
/// </summary>
public class ExpotestStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _sarStep;
	private readonly StrategyParam<decimal> _sarMaximum;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<decimal> _fixedVolume;

	private ParabolicSar _parabolicSar;

	private bool _hasActivePosition;
	private bool _isLongPosition;
	private decimal _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takePrice;
	private decimal _currentPositionVolume;

	private bool _hasLastTrade;
	private decimal _lastTradeVolume;
	private bool _lastTradeWasLoss;

	/// <summary>
	/// Candle type used for signal generation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Acceleration factor for the Parabolic SAR indicator.
	/// </summary>
	public decimal SarStep
	{
		get => _sarStep.Value;
		set => _sarStep.Value = value;
	}

	/// <summary>
	/// Maximum acceleration factor for the Parabolic SAR indicator.
	/// </summary>
	public decimal SarMaximum
	{
		get => _sarMaximum.Value;
		set => _sarMaximum.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in price steps.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in price steps.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Percentage of portfolio equity used for position sizing when fixed volume is zero.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Fixed trading volume. If zero the strategy calculates volume from the risk percentage.
	/// </summary>
	public decimal FixedVolume
	{
		get => _fixedVolume.Value;
		set => _fixedVolume.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ExpotestStrategy"/> class.
	/// </summary>
	public ExpotestStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use for signal generation", "General");

		_sarStep = Param(nameof(SarStep), 0.02m)
			.SetDisplay("SAR Step", "Acceleration factor for Parabolic SAR", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(0.01m, 0.05m, 0.01m);

		_sarMaximum = Param(nameof(SarMaximum), 0.2m)
			.SetDisplay("SAR Maximum", "Maximum acceleration factor for Parabolic SAR", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 0.4m, 0.05m);

		_stopLossPoints = Param(nameof(StopLossPoints), 150m)
			.SetDisplay("Stop Loss (points)", "Distance to the stop loss measured in price steps", "Risk Management")
			.SetNotNegative();

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 200m)
			.SetDisplay("Take Profit (points)", "Distance to the take profit measured in price steps", "Risk Management")
			.SetNotNegative();

		_riskPercent = Param(nameof(RiskPercent), 0.13m)
			.SetDisplay("Risk %", "Percent of equity to risk when calculating volume", "Risk Management")
			.SetNotNegative();

		_fixedVolume = Param(nameof(FixedVolume), 0m)
			.SetDisplay("Fixed Volume", "Fixed order volume. Set to 0 to use risk-based sizing", "Trading")
			.SetNotNegative();
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

		_parabolicSar = null;
		_hasActivePosition = false;
		_isLongPosition = false;
		_entryPrice = 0m;
		_stopPrice = null;
		_takePrice = null;
		_currentPositionVolume = 0m;
		_hasLastTrade = false;
		_lastTradeVolume = 0m;
		_lastTradeWasLoss = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_parabolicSar = new ParabolicSar
		{
			Acceleration = SarStep,
			AccelerationMax = SarMaximum
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_parabolicSar, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _parabolicSar);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal sarValue)
	{
		// Work only with finished candles to mirror the original tick-based logic safely.
		if (candle.State != CandleStates.Finished)
			return;

		// Wait until the Parabolic SAR is fully formed to avoid early random values.
		if (_parabolicSar == null || !_parabolicSar.IsFormed)
			return;

		// Ensure the strategy is ready for trading.
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Manage the currently open position before checking for new entries.
		if (_hasActivePosition)
		{
			ManageOpenPosition(candle);
		}

		// If the position is still active, skip new signals for this candle.
		if (_hasActivePosition || Position != 0)
			return;

		var price = candle.ClosePrice;
		var signal = 0;

		// Replicate the MQL logic: SAR below price -> buy, SAR above price -> sell.
		if (sarValue <= price)
			signal = 1;
		if (sarValue >= price)
			signal = -1;

		if (signal == 0)
			return;

		var volume = CalculateOrderVolume();
		if (volume <= 0m)
			return;

		var priceStep = Security?.PriceStep ?? 1m;
		if (priceStep <= 0m)
			priceStep = 1m;

		if (signal > 0 && Position <= 0)
		{
			BuyMarket(volume);
			PrepareProtectionLevels(price, priceStep, true, volume);
			LogInfo($"Entered long at {price} with volume {volume}. SAR={sarValue}.");
		}
		else if (signal < 0 && Position >= 0)
		{
			SellMarket(volume);
			PrepareProtectionLevels(price, priceStep, false, volume);
			LogInfo($"Entered short at {price} with volume {volume}. SAR={sarValue}.");
		}
	}

	private void PrepareProtectionLevels(decimal price, decimal priceStep, bool isLong, decimal volume)
	{
		// Store entry context so exit logic can evaluate stops and targets on upcoming candles.
		_hasActivePosition = true;
		_isLongPosition = isLong;
		_entryPrice = price;
		_currentPositionVolume = volume > 0m ? volume : Math.Abs(Position);
		if (_currentPositionVolume <= 0m)
			_currentPositionVolume = Volume > 0m ? Volume : 1m;

		var stopOffset = StopLossPoints > 0m ? StopLossPoints * priceStep : 0m;
		var takeOffset = TakeProfitPoints > 0m ? TakeProfitPoints * priceStep : 0m;

		_stopPrice = stopOffset > 0m
			? (isLong ? price - stopOffset : price + stopOffset)
			: null;

		_takePrice = takeOffset > 0m
			? (isLong ? price + takeOffset : price - takeOffset)
			: null;
	}

	private void ManageOpenPosition(ICandleMessage candle)
	{
		// Exit management checks whether the candle range touched take-profit or stop-loss levels.
		if (!_hasActivePosition)
			return;

		var exitVolume = Math.Abs(Position);
		if (exitVolume <= 0m)
			exitVolume = _currentPositionVolume;

		if (_isLongPosition)
		{
			if (_takePrice.HasValue && candle.HighPrice >= _takePrice.Value)
			{
				ClosePosition();
				RegisterClosedTrade(exitVolume, false);
				LogInfo($"Long take profit hit at {_takePrice.Value}. Entry {_entryPrice}.");
				return;
			}

			if (_stopPrice.HasValue && candle.LowPrice <= _stopPrice.Value)
			{
				ClosePosition();
				RegisterClosedTrade(exitVolume, true);
				LogInfo($"Long stop loss hit at {_stopPrice.Value}. Entry {_entryPrice}.");
				return;
			}
		}
		else
		{
			if (_takePrice.HasValue && candle.LowPrice <= _takePrice.Value)
			{
				ClosePosition();
				RegisterClosedTrade(exitVolume, false);
				LogInfo($"Short take profit hit at {_takePrice.Value}. Entry {_entryPrice}.");
				return;
			}

			if (_stopPrice.HasValue && candle.HighPrice >= _stopPrice.Value)
			{
				ClosePosition();
				RegisterClosedTrade(exitVolume, true);
				LogInfo($"Short stop loss hit at {_stopPrice.Value}. Entry {_entryPrice}.");
				return;
			}
		}
	}

	private void RegisterClosedTrade(decimal volume, bool wasLoss)
	{
		// Memorize trade statistics to reproduce the doubling logic from the MQL code.
		_hasActivePosition = false;
		_isLongPosition = false;
		_hasLastTrade = true;
		_lastTradeVolume = volume > 0m ? volume : _currentPositionVolume;
		_lastTradeWasLoss = wasLoss;
		_currentPositionVolume = 0m;
		_entryPrice = 0m;
		_stopPrice = null;
		_takePrice = null;
	}

	private decimal CalculateOrderVolume()
	{
		// Double the next position size after a loss, otherwise fall back to the base sizing method.
		if (_hasLastTrade && _lastTradeWasLoss && _lastTradeVolume > 0m)
			return _lastTradeVolume * 2m;

		var baseVolume = DetermineBaseVolume();
		return baseVolume > 0m ? baseVolume : 0m;
	}

	private decimal DetermineBaseVolume()
	{
		// Use fixed volume if it is provided.
		if (FixedVolume > 0m)
			return FixedVolume;

		var defaultVolume = Volume > 0m ? Volume : 1m;

		// Calculate risk-based position sizing when stop-loss information is available.
		if (RiskPercent <= 0m || StopLossPoints <= 0m || Portfolio == null)
			return defaultVolume;

		var equity = Portfolio.CurrentValue ?? 0m;
		if (equity <= 0m)
			return defaultVolume;

		var priceStep = Security?.PriceStep ?? 1m;
		if (priceStep <= 0m)
			priceStep = 1m;

		var stopDistance = StopLossPoints * priceStep;
		if (stopDistance <= 0m)
			return defaultVolume;

		var riskAmount = equity * RiskPercent / 100m;
		if (riskAmount <= 0m)
			return defaultVolume;

		var volume = riskAmount / stopDistance;
		return volume > 0m ? volume : defaultVolume;
	}
}
