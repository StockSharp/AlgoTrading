using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Demonstrates position sizing based on risk and stop-loss percentage.
/// Places random entries using calculated quantity.
/// </summary>
public class CalculationPositionSizeBasedOnRiskStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _riskValue;
	private readonly StrategyParam<bool> _riskIsPercent;
	private readonly StrategyParam<int> _longPeriod;
	private readonly StrategyParam<int> _shortPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private int _barIndex;

	/// <summary>
	/// Stop-loss percentage from entry price.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Risk value either in percent or absolute currency.
	/// </summary>
	public decimal RiskValue
	{
		get => _riskValue.Value;
		set => _riskValue.Value = value;
	}

	/// <summary>
	/// Whether risk value is percentage of portfolio equity.
	/// </summary>
	public bool RiskIsPercent
	{
		get => _riskIsPercent.Value;
		set => _riskIsPercent.Value = value;
	}

	/// <summary>
	/// Period for random long entries.
	/// </summary>
	public int LongPeriod
	{
		get => _longPeriod.Value;
		set => _longPeriod.Value = value;
	}

	/// <summary>
	/// Period for random short entries.
	/// </summary>
	public int ShortPeriod
	{
		get => _shortPeriod.Value;
		set => _shortPeriod.Value = value;
	}

	/// <summary>
	/// Candle type used for timing.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="CalculationPositionSizeBasedOnRiskStrategy"/>.
	/// </summary>
	public CalculationPositionSizeBasedOnRiskStrategy()
	{
		_stopLossPercent = Param(nameof(StopLossPercent), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

		_riskValue = Param(nameof(RiskValue), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Risk Value", "Risk amount", "Risk");

		_riskIsPercent = Param(nameof(RiskIsPercent), true)
			.SetDisplay("Risk Is Percent", "Risk as % of equity", "Risk");

		_longPeriod = Param(nameof(LongPeriod), 333)
			.SetGreaterThanZero()
			.SetDisplay("Long Period", "Bars between long entries", "Random");

		_shortPeriod = Param(nameof(ShortPeriod), 444)
			.SetGreaterThanZero()
			.SetDisplay("Short Period", "Bars between short entries", "Random");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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
		_barIndex = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection(stopLoss: new Unit(StopLossPercent, UnitTypes.Percent));
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barIndex++;

		var qty = CalcPositionSize(candle.ClosePrice);

		if (_barIndex % LongPeriod == 0)
		{
			BuyMarket(qty);
		}
		else if (_barIndex % ShortPeriod == 0)
		{
			SellMarket(qty);
		}
	}

	private decimal CalcPositionSize(decimal entryPrice)
	{
		if (entryPrice <= 0)
			return 0m;

		var risk = RiskIsPercent ? Portfolio.CurrentValue * RiskValue / 100m : RiskValue;
		var riskPerUnit = entryPrice * StopLossPercent / 100m;

		return riskPerUnit > 0 ? risk / riskPerUnit : 0m;
	}
}
