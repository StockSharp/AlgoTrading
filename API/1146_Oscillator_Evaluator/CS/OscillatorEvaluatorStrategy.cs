using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that evaluates RSI and Stochastic oscillators.
/// Buys when both oscillators are oversold and sells when both are overbought.
/// </summary>
public class OscillatorEvaluatorStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _stochPeriod;
	private readonly StrategyParam<decimal> _overbought;
	private readonly StrategyParam<decimal> _oversold;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeStrengthIndex _rsi;
	private StochasticOscillator _stoch;

	/// <summary>
	/// RSI calculation length.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Stochastic oscillator length.
	/// </summary>
	public int StochPeriod
	{
		get => _stochPeriod.Value;
		set => _stochPeriod.Value = value;
	}

	/// <summary>
	/// Upper threshold for oscillators.
	/// </summary>
	public decimal Overbought
	{
		get => _overbought.Value;
		set => _overbought.Value = value;
	}

	/// <summary>
	/// Lower threshold for oscillators.
	/// </summary>
	public decimal Oversold
	{
		get => _oversold.Value;
		set => _oversold.Value = value;
	}

	/// <summary>
	/// Take profit percentage.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Stop loss percentage.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Candle type to use.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="OscillatorEvaluatorStrategy"/> class.
	/// </summary>
	public OscillatorEvaluatorStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI calculation length", "Parameters");

		_stochPeriod = Param(nameof(StochPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic Period", "Stochastic oscillator length", "Parameters");

		_overbought = Param(nameof(Overbought), 70m)
			.SetDisplay("Overbought", "Upper threshold for oscillators", "Signals");

		_oversold = Param(nameof(Oversold), 30m)
			.SetDisplay("Oversold", "Lower threshold for oscillators", "Signals");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 2m)
			.SetDisplay("Take Profit (%)", "Take profit percentage", "Protection");

		_stopLossPercent = Param(nameof(StopLossPercent), 1m)
			.SetDisplay("Stop Loss (%)", "Stop loss percentage", "Protection");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		_stoch = new StochasticOscillator { Length = StochPeriod, KPeriod = 3, DPeriod = 3 };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.BindEx(_rsi, _stoch, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _rsi);
			DrawIndicator(area, _stoch);
			DrawOwnTrades(area);
		}

		StartProtection(
			takeProfit: new Unit(TakeProfitPercent, UnitTypes.Percent),
			stopLoss: new Unit(StopLossPercent, UnitTypes.Percent)
		);

		base.OnStarted(time);
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue rsiValue, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!rsiValue.IsFinal || !stochValue.IsFinal)
		return;

		var rsi = rsiValue.ToDecimal();

		var stochTyped = (StochasticOscillatorValue)stochValue;
		if (stochTyped.K is not decimal stochK)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (rsi < Oversold && stochK < Oversold && Position <= 0)
		{
		CancelActiveOrders();
		BuyMarket(Volume + Math.Abs(Position));
		}
		else if (rsi > Overbought && stochK > Overbought && Position >= 0)
		{
		CancelActiveOrders();
		SellMarket(Volume + Math.Abs(Position));
		}

		if ((Position > 0 && rsi > 50 && stochK > 50) ||
		(Position < 0 && rsi < 50 && stochK < 50))
		{
		ClosePosition();
		}
	}
}
