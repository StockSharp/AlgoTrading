using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSI Hook Reversal strategy.
/// Enters long when RSI hooks up from oversold zone.
/// Enters short when RSI hooks down from overbought zone.
/// Exits when RSI reaches neutral zone.
/// Uses cooldown to control trade frequency.
/// </summary>
public class RsiHookReversalStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _oversoldLevel;
	private readonly StrategyParam<int> _overboughtLevel;
	private readonly StrategyParam<int> _exitLevel;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _prevRsi;
	private int _cooldown;

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Oversold level.
	/// </summary>
	public int OversoldLevel
	{
		get => _oversoldLevel.Value;
		set => _oversoldLevel.Value = value;
	}

	/// <summary>
	/// Overbought level.
	/// </summary>
	public int OverboughtLevel
	{
		get => _overboughtLevel.Value;
		set => _overboughtLevel.Value = value;
	}

	/// <summary>
	/// Exit level (neutral zone).
	/// </summary>
	public int ExitLevel
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
	public RsiHookReversalStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetRange(7, 21)
			.SetDisplay("RSI Period", "Period for RSI", "RSI");

		_oversoldLevel = Param(nameof(OversoldLevel), 30)
			.SetRange(20, 40)
			.SetDisplay("Oversold", "Oversold level", "RSI");

		_overboughtLevel = Param(nameof(OverboughtLevel), 70)
			.SetRange(60, 80)
			.SetDisplay("Overbought", "Overbought level", "RSI");

		_exitLevel = Param(nameof(ExitLevel), 50)
			.SetRange(45, 55)
			.SetDisplay("Exit Level", "Neutral exit zone", "RSI");

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
		_prevRsi = default;
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevRsi = 0;
		_cooldown = 0;

		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_prevRsi == 0)
		{
			_prevRsi = rsiValue;
			return;
		}

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevRsi = rsiValue;
			return;
		}

		// RSI hook up from oversold
		var oversoldHookUp = _prevRsi < OversoldLevel && rsiValue > _prevRsi;
		// RSI hook down from overbought
		var overboughtHookDown = _prevRsi > OverboughtLevel && rsiValue < _prevRsi;

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
		else if (Position > 0 && rsiValue < ExitLevel)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position < 0 && rsiValue > ExitLevel)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}

		_prevRsi = rsiValue;
	}
}
