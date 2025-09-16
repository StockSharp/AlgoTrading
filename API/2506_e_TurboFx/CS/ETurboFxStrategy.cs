using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Momentum reversal strategy that tracks consecutive candles with expanding bodies.
/// </summary>
public class ETurboFxStrategy : Strategy
{
	private readonly StrategyParam<int> _depthAnalysis;
	private readonly StrategyParam<decimal> _takeProfitSteps;
	private readonly StrategyParam<decimal> _stopLossSteps;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<DataType> _candleType;

	private int _bearishSequence;
	private int _bullishSequence;
	private decimal _previousBearishBody;
	private decimal _previousBullishBody;

	/// <summary>
	/// Number of recent candles analysed for momentum confirmation.
	/// </summary>
	public int DepthAnalysis
	{
		get => _depthAnalysis.Value;
		set => _depthAnalysis.Value = value;
	}

	/// <summary>
	/// Take profit distance measured in price steps (ticks).
	/// A value of zero disables the take profit order.
	/// </summary>
	public decimal TakeProfitSteps
	{
		get => _takeProfitSteps.Value;
		set => _takeProfitSteps.Value = value;
	}

	/// <summary>
	/// Stop loss distance measured in price steps (ticks).
	/// A value of zero disables the protective stop.
	/// </summary>
	public decimal StopLossSteps
	{
		get => _stopLossSteps.Value;
		set => _stopLossSteps.Value = value;
	}

	/// <summary>
	/// Volume used when sending market orders.
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
	/// Candle type analysed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ETurboFxStrategy" /> class.
	/// </summary>
	public ETurboFxStrategy()
	{
		_depthAnalysis = Param(nameof(DepthAnalysis), 3)
			.SetGreaterThanZero()
			.SetDisplay("Depth Analysis", "Number of finished candles used for pattern detection", "Trading Rules")
			.SetCanOptimize(true)
			.SetOptimize(2, 6, 1);

		_takeProfitSteps = Param(nameof(TakeProfitSteps), 120m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Take Profit (steps)", "Take profit distance in price steps (ticks)", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(60m, 180m, 20m);

		_stopLossSteps = Param(nameof(StopLossSteps), 70m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Stop Loss (steps)", "Stop loss distance in price steps (ticks)", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(40m, 120m, 10m);

		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Order volume used for entries", "Trading Rules")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 0.5m, 0.1m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe of the candles analysed by the strategy", "Market Data");
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
		ResetState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		ResetState();
		Volume = TradeVolume;

		var takeProfitUnit = CreateStepUnit(TakeProfitSteps);
		var stopLossUnit = CreateStepUnit(StopLossSteps);

		if (takeProfitUnit != null || stopLossUnit != null)
		{
			// Configure protective orders once the strategy starts.
			StartProtection(
				takeProfit: takeProfitUnit,
				stopLoss: stopLossUnit,
				isStopTrailing: false,
				useMarketOrders: true);
		}

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private Unit? CreateStepUnit(decimal steps)
	{
		if (steps <= 0)
			return null;

		return new Unit(steps, UnitTypes.Step);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position != 0)
		{
			// Do not look for new signals while a position is active.
			ResetState();
			return;
		}

		var bodySize = Math.Abs(candle.ClosePrice - candle.OpenPrice);

		if (candle.ClosePrice < candle.OpenPrice)
		{
			HandleBearishCandle(bodySize);
		}
		else if (candle.ClosePrice > candle.OpenPrice)
		{
			HandleBullishCandle(bodySize);
		}
		else
		{
			// Neutral candle breaks both sequences.
			ResetState();
		}
	}

	private void HandleBearishCandle(decimal bodySize)
	{
		ResetBullishSequence();

		if (bodySize <= 0)
		{
			ResetBearishSequence();
			return;
		}

		if (_bearishSequence == 0 || bodySize > _previousBearishBody)
		{
			// Body is larger than the previous bearish candle, extend the sequence.
			_bearishSequence++;
		}
		else
		{
			// Sequence restarts because body did not expand.
			_bearishSequence = 1;
		}

		_previousBearishBody = bodySize;

		if (_bearishSequence >= DepthAnalysis)
		{
			// Expanding bearish bodies suggest exhaustion that can trigger a long entry.
			BuyMarket(Volume);
			ResetBearishSequence();
		}
	}

	private void HandleBullishCandle(decimal bodySize)
	{
		ResetBearishSequence();

		if (bodySize <= 0)
		{
			ResetBullishSequence();
			return;
		}

		if (_bullishSequence == 0 || bodySize > _previousBullishBody)
		{
			// Body is larger than the previous bullish candle, extend the sequence.
			_bullishSequence++;
		}
		else
		{
			// Sequence restarts because body did not expand.
			_bullishSequence = 1;
		}

		_previousBullishBody = bodySize;

		if (_bullishSequence >= DepthAnalysis)
		{
			// Expanding bullish bodies suggest potential reversal to the downside.
			SellMarket(Volume);
			ResetBullishSequence();
		}
	}

	private void ResetBearishSequence()
	{
		_bearishSequence = 0;
		_previousBearishBody = 0m;
	}

	private void ResetBullishSequence()
	{
		_bullishSequence = 0;
		_previousBullishBody = 0m;
	}

	private void ResetState()
	{
		ResetBearishSequence();
		ResetBullishSequence();
	}
}
