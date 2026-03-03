using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Williams %R Hook Reversal strategy.
/// Enters long when Williams %R hooks up from oversold zone.
/// Enters short when Williams %R hooks down from overbought zone.
/// Exits when Williams %R reaches neutral zone.
/// Uses cooldown to control trade frequency.
/// </summary>
public class WilliamsRHookReversalStrategy : Strategy
{
	private readonly StrategyParam<int> _willRPeriod;
	private readonly StrategyParam<decimal> _oversoldLevel;
	private readonly StrategyParam<decimal> _overboughtLevel;
	private readonly StrategyParam<decimal> _exitLevel;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal? _prevWillR;
	private int _cooldown;

	/// <summary>
	/// Williams %R period.
	/// </summary>
	public int WillRPeriod
	{
		get => _willRPeriod.Value;
		set => _willRPeriod.Value = value;
	}

	/// <summary>
	/// Oversold level (typically -80).
	/// </summary>
	public decimal OversoldLevel
	{
		get => _oversoldLevel.Value;
		set => _oversoldLevel.Value = value;
	}

	/// <summary>
	/// Overbought level (typically -20).
	/// </summary>
	public decimal OverboughtLevel
	{
		get => _overboughtLevel.Value;
		set => _overboughtLevel.Value = value;
	}

	/// <summary>
	/// Exit level (neutral zone).
	/// </summary>
	public decimal ExitLevel
	{
		get => _exitLevel.Value;
		set => _exitLevel.Value = value;
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
	public WilliamsRHookReversalStrategy()
	{
		_willRPeriod = Param(nameof(WillRPeriod), 14)
			.SetRange(7, 21)
			.SetDisplay("Williams %R Period", "Period for Williams %R", "Williams %R");

		_oversoldLevel = Param(nameof(OversoldLevel), -80m)
			.SetRange(-90m, -70m)
			.SetDisplay("Oversold", "Oversold level", "Williams %R");

		_overboughtLevel = Param(nameof(OverboughtLevel), -20m)
			.SetRange(-30m, -10m)
			.SetDisplay("Overbought", "Overbought level", "Williams %R");

		_exitLevel = Param(nameof(ExitLevel), -50m)
			.SetRange(-60m, -40m)
			.SetDisplay("Exit Level", "Neutral exit zone", "Williams %R");

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
		_prevWillR = null;
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevWillR = null;
		_cooldown = 0;

		var williamsR = new WilliamsR { Length = WillRPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(williamsR, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, williamsR);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal willRValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_prevWillR == null)
		{
			_prevWillR = willRValue;
			return;
		}

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevWillR = willRValue;
			return;
		}

		// Hook up from oversold
		var oversoldHookUp = _prevWillR < OversoldLevel && willRValue > _prevWillR;
		// Hook down from overbought
		var overboughtHookDown = _prevWillR > OverboughtLevel && willRValue < _prevWillR;

		if (Position == 0 && oversoldHookUp)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
		else if (Position == 0 && overboughtHookDown)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position > 0 && willRValue < ExitLevel)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position < 0 && willRValue > ExitLevel)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}

		_prevWillR = willRValue;
	}
}
