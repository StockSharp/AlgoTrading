using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Volume Contraction Pattern (VCP) strategy.
/// Looks for narrowing volatility (ATR declining) and breakouts above/below MA bands.
/// </summary>
public class VcpStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _prevAtr;
	private int _contractionCount;
	private int _cooldown;

	/// <summary>
	/// MA Period.
	/// </summary>
	public int MAPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// ATR Period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier for breakout band.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Candle type for strategy calculation.
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
	/// Initialize the VCP strategy.
	/// </summary>
	public VcpStrategy()
	{
		_maPeriod = Param(nameof(MAPeriod), 20)
			.SetDisplay("MA Period", "Period for Moving Average calculation", "Indicators")
			.SetOptimize(10, 50, 10);

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetDisplay("ATR Period", "Period for ATR calculation", "Indicators")
			.SetOptimize(7, 21, 7);

		_atrMultiplier = Param(nameof(AtrMultiplier), 2.0m)
			.SetDisplay("ATR Multiplier", "ATR multiplier for breakout band", "Entry")
			.SetOptimize(1.0m, 3.0m, 0.5m);

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
		_prevAtr = default;
		_contractionCount = default;
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevAtr = 0;
		_contractionCount = 0;
		_cooldown = 0;

		var ma = new SimpleMovingAverage { Length = MAPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ma, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_prevAtr == 0)
		{
			_prevAtr = atrValue;
			return;
		}

		// Track contraction
		if (atrValue < _prevAtr)
			_contractionCount++;
		else
			_contractionCount = 0;

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevAtr = atrValue;
			return;
		}

		// After 3+ contracting bars, look for breakout
		var isContracted = _contractionCount >= 3;
		var upperBand = maValue + atrValue * AtrMultiplier;
		var lowerBand = maValue - atrValue * AtrMultiplier;

		if (Position == 0 && isContracted)
		{
			if (candle.ClosePrice > upperBand)
			{
				BuyMarket();
				_cooldown = CooldownBars;
				_contractionCount = 0;
			}
			else if (candle.ClosePrice < lowerBand)
			{
				SellMarket();
				_cooldown = CooldownBars;
				_contractionCount = 0;
			}
		}
		else if (Position > 0 && candle.ClosePrice < maValue)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position < 0 && candle.ClosePrice > maValue)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}

		_prevAtr = atrValue;
	}
}
