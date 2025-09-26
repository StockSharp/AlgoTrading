using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Parabolic SAR flip strategy converted from the MetaTrader expert.
/// Reacts to the first dot that crosses the close and reverses the position.
/// </summary>
public class PsarBugStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<decimal> _sarAccelerationStep;
	private readonly StrategyParam<decimal> _sarAccelerationMax;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _previousSar;
	private decimal? _previousClose;

	/// <summary>
	/// Initializes a new instance of the <see cref="PsarBugStrategy"/> class.
	/// </summary>
	public PsarBugStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Order volume expressed in lots", "General")
			.SetCanOptimize(true)
			.SetOptimize(0.05m, 1m, 0.05m);

		_stopLossPoints = Param(nameof(StopLossPoints), 40)
			.SetDisplay("Stop Loss Points", "Stop-loss distance expressed in price steps", "Risk")
			.SetRange(0, 10000)
			.SetCanOptimize(true)
			.SetOptimize(0, 120, 10);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 70)
			.SetDisplay("Take Profit Points", "Take-profit distance expressed in price steps", "Risk")
			.SetRange(0, 10000)
			.SetCanOptimize(true)
			.SetOptimize(20, 200, 10);

		_sarAccelerationStep = Param(nameof(SarAccelerationStep), 0.02m)
			.SetDisplay("SAR Step", "Initial acceleration factor for Parabolic SAR", "Indicator")
			.SetRange(0.01m, 0.1m)
			.SetCanOptimize(true)
			.SetOptimize(0.01m, 0.05m, 0.01m);

		_sarAccelerationMax = Param(nameof(SarAccelerationMax), 0.2m)
			.SetDisplay("SAR Max", "Maximum acceleration factor for Parabolic SAR", "Indicator")
			.SetRange(0.1m, 0.5m)
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 0.4m, 0.05m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe used for calculations", "General");

		Volume = _tradeVolume.Value;
	}

	/// <summary>
	/// Base order volume in lots.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set
		{
			_tradeVolume.Value = value;
			Volume = value;
		}
	}

	/// <summary>
	/// Stop-loss distance expressed in price steps.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in price steps.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Initial acceleration factor for Parabolic SAR.
	/// </summary>
	public decimal SarAccelerationStep
	{
		get => _sarAccelerationStep.Value;
		set => _sarAccelerationStep.Value = value;
	}

	/// <summary>
	/// Maximum acceleration factor for Parabolic SAR.
	/// </summary>
	public decimal SarAccelerationMax
	{
		get => _sarAccelerationMax.Value;
		set => _sarAccelerationMax.Value = value;
	}

	/// <summary>
	/// Candle type used for data subscriptions.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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

		_previousSar = null;
		_previousClose = null;
		Volume = _tradeVolume.Value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = _tradeVolume.Value;

		var priceStep = GetPriceStep();
		var stopLoss = CreateProtectionUnit(StopLossPoints, priceStep);
		var takeProfit = CreateProtectionUnit(TakeProfitPoints, priceStep);

		StartProtection(stopLoss: stopLoss, takeProfit: takeProfit, useMarketOrders: true);

		var parabolicSar = new ParabolicSar
		{
			Acceleration = SarAccelerationStep,
			AccelerationMax = SarAccelerationMax
		};

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(parabolicSar, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, parabolicSar);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal sarValue)
	{
		// Work strictly with completed candles to mirror the MetaTrader implementation.
		if (candle.State != CandleStates.Finished)
			return;

		// Store the very first values and wait for the next candle to evaluate a flip.
		if (_previousSar is null || _previousClose is null)
		{
			_previousSar = sarValue;
			_previousClose = candle.ClosePrice;
			return;
		}

		// Determine whether the Parabolic SAR crossed the close between the last two candles.
		var buySignal = sarValue < candle.ClosePrice && _previousSar > _previousClose;
		var sellSignal = sarValue > candle.ClosePrice && _previousSar < _previousClose;

		_previousSar = sarValue;
		_previousClose = candle.ClosePrice;

		if (!buySignal && !sellSignal)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (buySignal && Position <= 0m)
		{
			var volume = Volume + Math.Abs(Position);
			if (volume > 0m)
			{
				// Reverse any short exposure and establish a long position when SAR flips below price.
				BuyMarket(volume);
			}
		}
		else if (sellSignal && Position >= 0m)
		{
			var volume = Volume + Math.Abs(Position);
			if (volume > 0m)
			{
				// Reverse any long exposure and establish a short position when SAR jumps above price.
				SellMarket(volume);
			}
		}
	}

	private static Unit CreateProtectionUnit(int points, decimal priceStep)
	{
		if (points <= 0 || priceStep <= 0m)
			return null;

		var offset = priceStep * points;
		return new Unit(offset, UnitTypes.Absolute);
	}
}
