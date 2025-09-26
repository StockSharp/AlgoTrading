using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Momentum exhaustion strategy converted from the original e-TurboFx MQL4 expert adviser.
/// </summary>
public class ETurboFxClassicStrategy : Strategy
{
	private readonly StrategyParam<int> _sequenceLength;
	private readonly StrategyParam<decimal> _takeProfitSteps;
	private readonly StrategyParam<decimal> _stopLossSteps;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<DataType> _candleType;

	private int _bearishSequence;
	private int _bullishSequence;
	private decimal _previousBearishBody;
	private decimal _previousBullishBody;

	/// <summary>
	/// Number of consecutive candles required to trigger a signal.
	/// </summary>
	public int SequenceLength
	{
		get => _sequenceLength.Value;
		set => _sequenceLength.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in price steps (ticks).
	/// A value of zero disables the take profit order.
	/// </summary>
	public decimal TakeProfitSteps
	{
		get => _takeProfitSteps.Value;
		set => _takeProfitSteps.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in price steps (ticks).
	/// A value of zero disables the protective stop.
	/// </summary>
	public decimal StopLossSteps
	{
		get => _stopLossSteps.Value;
		set => _stopLossSteps.Value = value;
	}

	/// <summary>
	/// Order volume sent with each market entry.
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
	/// Initializes a new instance of the <see cref="ETurboFxClassicStrategy" /> class.
	/// </summary>
	public ETurboFxClassicStrategy()
	{
		_sequenceLength = Param(nameof(SequenceLength), 3)
			.SetGreaterThanZero()
			.SetDisplay("Sequence Length", "Number of consecutive finished candles analysed for pattern detection", "Trading Rules")
			.SetCanOptimize(true)
			.SetOptimize(2, 6, 1);

		_takeProfitSteps = Param(nameof(TakeProfitSteps), 120m)
			.SetNotNegative()
			.SetDisplay("Take Profit (steps)", "Take profit distance in price steps (ticks)", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(60m, 180m, 20m);

		_stopLossSteps = Param(nameof(StopLossSteps), 70m)
			.SetNotNegative()
			.SetDisplay("Stop Loss (steps)", "Stop loss distance in price steps (ticks)", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(40m, 120m, 10m);

		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Order volume used for market entries", "Trading Rules")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 0.5m, 0.1m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame of the candles analysed by the strategy", "Market Data");
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
			// Configure protection block only once when the strategy starts.
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

	private Unit CreateStepUnit(decimal steps)
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
			// Ignore new signals while a position is active and rebuild the sequences afterwards.
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
			// Flat candles break both sequences because momentum stalled.
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
			// Body expanded compared to the previous bearish candle.
			_bearishSequence++;
		}
		else
		{
			// Restart the sequence when the body fails to expand.
			_bearishSequence = 1;
		}

		_previousBearishBody = bodySize;

		if (_bearishSequence >= SequenceLength)
		{
			// A string of expanding bearish candles hints a bullish reversal.
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
			// Body expanded compared to the previous bullish candle.
			_bullishSequence++;
		}
		else
		{
			// Restart the sequence when the body fails to expand.
			_bullishSequence = 1;
		}

		_previousBullishBody = bodySize;

		if (_bullishSequence >= SequenceLength)
		{
			// A string of expanding bullish candles hints a bearish reversal.
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
