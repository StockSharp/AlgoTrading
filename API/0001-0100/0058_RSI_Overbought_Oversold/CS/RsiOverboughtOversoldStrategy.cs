using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSI Overbought/Oversold strategy.
/// Buys when RSI is oversold, sells when RSI is overbought.
/// </summary>
public class RsiOverboughtOversoldStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _overboughtLevel;
	private readonly StrategyParam<int> _oversoldLevel;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private int _cooldown;

	/// <summary>
	/// RSI calculation period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// RSI level considered overbought.
	/// </summary>
	public int OverboughtLevel
	{
		get => _overboughtLevel.Value;
		set => _overboughtLevel.Value = value;
	}

	/// <summary>
	/// RSI level considered oversold.
	/// </summary>
	public int OversoldLevel
	{
		get => _oversoldLevel.Value;
		set => _oversoldLevel.Value = value;
	}

	/// <summary>
	/// Type of candles to use.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Cooldown bars between trades.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="RsiOverboughtOversoldStrategy"/>.
	/// </summary>
	public RsiOverboughtOversoldStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetRange(5, 30)
			.SetDisplay("RSI Period", "Period for RSI calculation", "Indicators");

		_overboughtLevel = Param(nameof(OverboughtLevel), 70)
			.SetRange(60, 80)
			.SetDisplay("Overbought Level", "RSI overbought threshold", "Indicators");

		_oversoldLevel = Param(nameof(OversoldLevel), 30)
			.SetRange(20, 40)
			.SetDisplay("Oversold Level", "RSI oversold threshold", "Indicators");

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
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

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

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		if (Position == 0 && rsiValue <= OversoldLevel)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
		else if (Position == 0 && rsiValue >= OverboughtLevel)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position > 0 && rsiValue >= OverboughtLevel)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position < 0 && rsiValue <= OversoldLevel)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
	}
}
