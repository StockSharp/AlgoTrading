using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simple strategy based on source values equal to 1 or 2.
/// Uses short-term SMA crossover to classify candle close as signal 1 (bullish) or 2 (bearish).
/// </summary>
public class IsStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _reverse;
	private readonly StrategyParam<bool> _enableShort;
	private readonly StrategyParam<decimal> _profitPercent;
	private readonly StrategyParam<decimal> _lossPercent;

	private decimal _previousValue;
	private SimpleMovingAverage _smaFast;
	private SimpleMovingAverage _smaSlow;

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
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles used by the strategy", "General");

		_reverse = Param(nameof(Reverse), false)
			.SetDisplay("Reverse", "Reverse trading direction", "General");

		_enableShort = Param(nameof(EnableShort), true)
			.SetDisplay("Sell On", "Enable short selling", "General");

		_profitPercent = Param(nameof(ProfitPercent), 1.5m)
			.SetRange(0m, 30m)

			.SetDisplay("Profit %", "Take profit percent", "Risk");

		_lossPercent = Param(nameof(LossPercent), 1.5m)
			.SetRange(0m, 30m)

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
		_smaFast = null;
		_smaSlow = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_smaFast = new SimpleMovingAverage { Length = 80 };
		_smaSlow = new SimpleMovingAverage { Length = 200 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_smaFast, _smaSlow, ProcessCandle)
			.Start();

		StartProtection(
			takeProfit: new Unit(ProfitPercent, UnitTypes.Percent),
			stopLoss: new Unit(LossPercent, UnitTypes.Percent),
			isStopTrailing: false);
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastVal, decimal slowVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Map price action to signal: 1 = bullish (fast > slow), 2 = bearish (fast < slow)
		var hb5 = fastVal > slowVal ? 1m : 2m;
		var ii = Reverse ? 2m : 1m;
		var i2 = Reverse ? 1m : 2m;
		var prev = _previousValue;

		if (hb5 == ii && prev != ii)
		{
			if (Position < 0 && EnableShort)
				BuyMarket(Position.Abs());

			BuyMarket();
		}
		else if (hb5 == i2 && prev != i2)
		{
			if (Position > 0)
				SellMarket(Position);

			if (EnableShort)
				SellMarket();
		}

		_previousValue = hb5;
	}
}
