using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Parabolic SAR Reversal strategy.
/// Enters long when SAR switches from above to below price.
/// Enters short when SAR switches from below to above price.
/// Uses cooldown to control trade frequency.
/// </summary>
public class ParabolicSarReversalStrategy : Strategy
{
	private readonly StrategyParam<decimal> _acceleration;
	private readonly StrategyParam<decimal> _accelerationMax;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private bool? _prevSarAbove;
	private int _cooldown;

	/// <summary>
	/// Initial acceleration.
	/// </summary>
	public decimal Acceleration
	{
		get => _acceleration.Value;
		set => _acceleration.Value = value;
	}

	/// <summary>
	/// Max acceleration.
	/// </summary>
	public decimal AccelerationMax
	{
		get => _accelerationMax.Value;
		set => _accelerationMax.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Cooldown bars.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public ParabolicSarReversalStrategy()
	{
		_acceleration = Param(nameof(Acceleration), 0.02m)
			.SetRange(0.01m, 0.05m)
			.SetDisplay("Acceleration", "Initial acceleration factor", "SAR");

		_accelerationMax = Param(nameof(AccelerationMax), 0.2m)
			.SetRange(0.1m, 0.3m)
			.SetDisplay("Max Acceleration", "Maximum acceleration factor", "SAR");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_cooldownBars = Param(nameof(CooldownBars), 500)
			.SetRange(1, 1000)
			.SetDisplay("Cooldown Bars", "Bars to wait between trades", "General");
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
		_prevSarAbove = null;
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevSarAbove = null;
		_cooldown = 0;

		var sar = new ParabolicSar
		{
			Acceleration = Acceleration,
			AccelerationMax = AccelerationMax
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sar, ProcessCandle)
			.Start();

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

		var isSarAbove = sarValue > candle.ClosePrice;

		if (_prevSarAbove == null)
		{
			_prevSarAbove = isSarAbove;
			return;
		}

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevSarAbove = isSarAbove;
			return;
		}

		// SAR switched from above to below = bullish signal
		var sarSwitchedBelow = _prevSarAbove == true && !isSarAbove;
		// SAR switched from below to above = bearish signal
		var sarSwitchedAbove = _prevSarAbove == false && isSarAbove;

		if (Position == 0 && sarSwitchedBelow)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
		else if (Position == 0 && sarSwitchedAbove)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position > 0 && sarSwitchedAbove)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position < 0 && sarSwitchedBelow)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}

		_prevSarAbove = isSarAbove;
	}
}
