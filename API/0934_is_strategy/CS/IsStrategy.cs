using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simple strategy based on source values equal to 1 or 2.
/// </summary>
public class IsStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _reverse;
	private readonly StrategyParam<bool> _enableShort;
	private readonly StrategyParam<decimal> _profitPercent;
	private readonly StrategyParam<decimal> _lossPercent;

	private decimal _previousValue;

	/// <summary>
	/// Candle type for processing.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Reverse trading direction.
	/// </summary>
	public bool Reverse
	{
		get => _reverse.Value;
		set => _reverse.Value = value;
	}

	/// <summary>
	/// Enable short selling.
	/// </summary>
	public bool EnableShort
	{
		get => _enableShort.Value;
		set => _enableShort.Value = value;
	}

	/// <summary>
	/// Take profit percent.
	/// </summary>
	public decimal ProfitPercent
	{
		get => _profitPercent.Value;
		set => _profitPercent.Value = value;
	}

	/// <summary>
	/// Stop loss percent.
	/// </summary>
	public decimal LossPercent
	{
		get => _lossPercent.Value;
		set => _lossPercent.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public IsStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles used by the strategy", "General");

		_reverse = Param(nameof(Reverse), false)
			.SetDisplay("Reverse", "Reverse trading direction", "General");

		_enableShort = Param(nameof(EnableShort), true)
			.SetDisplay("Sell On", "Enable short selling", "General");

		_profitPercent = Param(nameof(ProfitPercent), 0.5m)
			.SetRange(0m, 30m)
			.SetCanOptimize(true)
			.SetDisplay("Profit %", "Take profit percent", "Risk");

		_lossPercent = Param(nameof(LossPercent), 0.5m)
			.SetRange(0m, 30m)
			.SetCanOptimize(true)
			.SetDisplay("Loss %", "Stop loss percent", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_previousValue = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection(
			takeProfit: new Unit(ProfitPercent, UnitTypes.Percent),
			stopLoss: new Unit(LossPercent, UnitTypes.Percent),
			isStopTrailing: false);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var hb5 = candle.ClosePrice;
		var ii = Reverse ? 2m : 1m;
		var i2 = Reverse ? 1m : 2m;
		var prev = _previousValue;

		if (hb5 == ii && prev != ii)
		{
			BuyMarket();
		}
		else if (hb5 == i2 && prev != i2)
		{
			if (Position > 0)
				SellMarket(Position);
		}

		if (hb5 == i2 && prev != i2 && EnableShort)
		{
			SellMarket();
		}
		else if (hb5 == ii && prev != ii && EnableShort)
		{
			if (Position < 0)
				BuyMarket(-Position);
		}

		_previousValue = hb5;
	}
}
