using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that uses ATR for trailing stop management.
/// It enters positions using a simple moving average and manages exits with a dynamic
/// trailing stop calculated as a multiple of ATR.
/// </summary>
public class AtrTrailingStrategy : Strategy
{
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _entryPrice;
	private decimal _trailingStopLevel;
	private int _cooldown;

	/// <summary>
	/// Period for ATR calculation.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier for trailing stop calculation.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Period for Moving Average calculation for entry.
	/// </summary>
	public int MAPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Type of candles used for strategy calculation.
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
	/// Initialize the ATR Trailing strategy.
	/// </summary>
	public AtrTrailingStrategy()
	{
		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetDisplay("ATR Period", "Period for ATR calculation", "Indicators")
			.SetOptimize(7, 21, 7);

		_atrMultiplier = Param(nameof(AtrMultiplier), 3.0m)
			.SetDisplay("ATR Multiplier", "ATR multiplier for trailing stop", "Risk")
			.SetOptimize(2.0m, 4.0m, 0.5m);

		_maPeriod = Param(nameof(MAPeriod), 20)
			.SetDisplay("MA Period", "Period for Moving Average calculation for entry", "Indicators")
			.SetOptimize(10, 50, 5);

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
		_entryPrice = default;
		_trailingStopLevel = default;
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_entryPrice = 0;
		_trailingStopLevel = 0;
		_cooldown = 0;

		var atr = new AverageTrueRange { Length = AtrPeriod };
		var sma = new SimpleMovingAverage { Length = MAPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(atr, sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var trailingStopDistance = atrValue * AtrMultiplier;

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		if (Position == 0)
		{
			if (candle.ClosePrice > smaValue)
			{
				BuyMarket();
				_entryPrice = candle.ClosePrice;
				_trailingStopLevel = _entryPrice - trailingStopDistance;
				_cooldown = CooldownBars;
			}
			else if (candle.ClosePrice < smaValue)
			{
				SellMarket();
				_entryPrice = candle.ClosePrice;
				_trailingStopLevel = _entryPrice + trailingStopDistance;
				_cooldown = CooldownBars;
			}
		}
		else if (Position > 0)
		{
			var newTrailingStopLevel = candle.ClosePrice - trailingStopDistance;
			if (newTrailingStopLevel > _trailingStopLevel)
				_trailingStopLevel = newTrailingStopLevel;

			if (candle.LowPrice <= _trailingStopLevel)
			{
				SellMarket();
				_cooldown = CooldownBars;
			}
		}
		else if (Position < 0)
		{
			var newTrailingStopLevel = candle.ClosePrice + trailingStopDistance;
			if (newTrailingStopLevel < _trailingStopLevel || _trailingStopLevel == 0)
				_trailingStopLevel = newTrailingStopLevel;

			if (candle.HighPrice >= _trailingStopLevel)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
		}
	}
}
