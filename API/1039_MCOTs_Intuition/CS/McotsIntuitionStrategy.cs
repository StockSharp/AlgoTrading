namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// MCOTs Intuition Strategy.
/// Uses RSI momentum and its standard deviation to detect entries.
/// </summary>
public class McotsIntuitionStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _stdDevMultiplier;
	private readonly StrategyParam<decimal> _exhaustionMultiplier;
	private readonly StrategyParam<int> _profitTargetTicks;
	private readonly StrategyParam<int> _stopLossTicks;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeStrengthIndex _rsi = null!;
	private StandardDeviation _momentumStdDev = null!;

	private decimal _prevRsi;
	private decimal _prevMomentum;
	private decimal _currentStdDev;
	private decimal _takeProfitPrice;
	private decimal _stopLossPrice;

	/// <summary>
	/// RSI calculation period.
	/// </summary>
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }

	/// <summary>
	/// Multiplier for standard deviation threshold.
	/// </summary>
	public decimal StdDevMultiplier { get => _stdDevMultiplier.Value; set => _stdDevMultiplier.Value = value; }

	/// <summary>
	/// Exhaustion multiplier for momentum comparison.
	/// </summary>
	public decimal ExhaustionMultiplier { get => _exhaustionMultiplier.Value; set => _exhaustionMultiplier.Value = value; }

	/// <summary>
	/// Profit target in ticks.
	/// </summary>
	public int ProfitTargetTicks { get => _profitTargetTicks.Value; set => _profitTargetTicks.Value = value; }

	/// <summary>
	/// Stop loss in ticks.
	/// </summary>
	public int StopLossTicks { get => _stopLossTicks.Value; set => _stopLossTicks.Value = value; }

	/// <summary>
	/// Type of candles to subscribe.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of <see cref="McotsIntuitionStrategy"/>.
	/// </summary>
	public McotsIntuitionStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetDisplay("RSI Period", "RSI calculation period", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(10, 20, 2);

		_stdDevMultiplier = Param(nameof(StdDevMultiplier), 1m)
			.SetDisplay("StdDev Multiplier", "Standard deviation multiplier", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 2m, 0.5m);

		_exhaustionMultiplier = Param(nameof(ExhaustionMultiplier), 1m)
			.SetDisplay("Exhaustion Multiplier", "Momentum exhaustion multiplier", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 1.5m, 0.1m);

		_profitTargetTicks = Param(nameof(ProfitTargetTicks), 40)
			.SetDisplay("Profit Target Ticks", "Profit target in ticks", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(20, 80, 10);

		_stopLossTicks = Param(nameof(StopLossTicks), 160)
			.SetDisplay("Stop Loss Ticks", "Stop loss in ticks", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(40, 200, 20);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "Parameters");
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

		_prevRsi = default;
		_prevMomentum = default;
		_currentStdDev = default;
		_takeProfitPrice = default;
		_stopLossPrice = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		_momentumStdDev = new StandardDeviation { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _rsi);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var momentum = rsiValue - _prevRsi;
		var stdValue = _momentumStdDev.Process(momentum, candle.ServerTime, true);
		_currentStdDev = stdValue.ToDecimal();

		if (!IsFormedAndOnlineAndAllowTrading() || !_momentumStdDev.IsFormed)
		{
			_prevRsi = rsiValue;
			_prevMomentum = momentum;
			return;
		}

		var priceStep = Security?.PriceStep ?? 1m;

		if (Position == 0)
		{
			if (momentum > _currentStdDev * StdDevMultiplier && momentum < _prevMomentum * ExhaustionMultiplier)
			{
				BuyMarket();
				_takeProfitPrice = candle.ClosePrice + ProfitTargetTicks * priceStep;
				_stopLossPrice = candle.ClosePrice - StopLossTicks * priceStep;
			}
			else if (momentum < -_currentStdDev * StdDevMultiplier && momentum > _prevMomentum * ExhaustionMultiplier)
			{
				SellMarket();
				_takeProfitPrice = candle.ClosePrice - ProfitTargetTicks * priceStep;
				_stopLossPrice = candle.ClosePrice + StopLossTicks * priceStep;
			}
		}
		else if (Position > 0)
		{
			if (candle.HighPrice >= _takeProfitPrice || candle.LowPrice <= _stopLossPrice)
				ClosePosition();
		}
		else if (Position < 0)
		{
			if (candle.LowPrice <= _takeProfitPrice || candle.HighPrice >= _stopLossPrice)
				ClosePosition();
		}

		_prevRsi = rsiValue;
		_prevMomentum = momentum;
	}
}
