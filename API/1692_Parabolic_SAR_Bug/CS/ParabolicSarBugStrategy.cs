using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Parabolic SAR strategy with optional trailing stop and reverse mode.
/// </summary>
public class ParabolicSarBugStrategy : Strategy
{
	private readonly StrategyParam<decimal> _step;
	private readonly StrategyParam<decimal> _maxStep;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<bool> _useTrailingStop;
	private readonly StrategyParam<bool> _reverse;
	private readonly StrategyParam<bool> _closeOnSar;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevSar;
	private bool _prevPriceAbove;

	/// <summary>
	/// Parabolic SAR acceleration step.
	/// </summary>
	public decimal Step
	{
		get => _step.Value;
		set => _step.Value = value;
	}

	/// <summary>
	/// Maximum Parabolic SAR acceleration.
	/// </summary>
	public decimal MaxStep
	{
		get => _maxStep.Value;
		set => _maxStep.Value = value;
	}

	/// <summary>
	/// Stop loss percentage from entry price.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Take profit percentage from entry price.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Enable trailing stop for protective orders.
	/// </summary>
	public bool UseTrailingStop
	{
		get => _useTrailingStop.Value;
		set => _useTrailingStop.Value = value;
	}

	/// <summary>
	/// Reverse trading signals.
	/// </summary>
	public bool Reverse
	{
		get => _reverse.Value;
		set => _reverse.Value = value;
	}

	/// <summary>
	/// Close position when SAR flips to opposite side.
	/// </summary>
	public bool CloseOnSar
	{
		get => _closeOnSar.Value;
		set => _closeOnSar.Value = value;
	}

	/// <summary>
	/// Candle data type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="ParabolicSarBugStrategy"/>.
	/// </summary>
	public ParabolicSarBugStrategy()
	{
		_step = Param(nameof(Step), 0.02m)
			.SetDisplay("Step", "Acceleration factor for Parabolic SAR", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(0.01m, 0.05m, 0.01m);

		_maxStep = Param(nameof(MaxStep), 0.2m)
			.SetDisplay("Max Step", "Maximum acceleration factor for Parabolic SAR", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 0.5m, 0.1m);

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetDisplay("Stop Loss %", "Percent of entry price for stop loss", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 0.5m);

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 1m)
			.SetDisplay("Take Profit %", "Percent of entry price for take profit", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 5m, 0.5m);

		_useTrailingStop = Param(nameof(UseTrailingStop), false)
			.SetDisplay("Use Trailing Stop", "Enable trailing stop protection", "Risk");

		_reverse = Param(nameof(Reverse), false)
			.SetDisplay("Reverse", "Invert trading signals", "General");

		_closeOnSar = Param(nameof(CloseOnSar), true)
			.SetDisplay("Close On SAR", "Close position when SAR changes side", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		_prevSar = 0m;
		_prevPriceAbove = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var sar = new ParabolicSar
		{
			Acceleration = Step,
			AccelerationMax = MaxStep
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sar, ProcessCandle)
			.Start();

		StartProtection(
			takeProfit: new Unit(TakeProfitPercent, UnitTypes.Percent),
			stopLoss: new Unit(StopLossPercent, UnitTypes.Percent),
			isStopTrailing: UseTrailingStop
		);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sar);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal sarValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var priceAbove = candle.ClosePrice > sarValue;
		var crossed = _prevSar > 0m && priceAbove != _prevPriceAbove;

		if (crossed)
		{
			if (CloseOnSar && Position != 0)
				ClosePosition();

			var direction = priceAbove ? Sides.Buy : Sides.Sell;

			if (Reverse)
				direction = direction == Sides.Buy ? Sides.Sell : Sides.Buy;

			var volume = Volume + Math.Abs(Position);

			if (direction == Sides.Buy && Position <= 0)
				BuyMarket(volume);
			else if (direction == Sides.Sell && Position >= 0)
				SellMarket(volume);
		}

		_prevSar = sarValue;
		_prevPriceAbove = priceAbove;
	}
}
