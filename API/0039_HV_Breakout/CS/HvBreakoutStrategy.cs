using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades breakouts based on historical volatility.
/// It calculates price levels for breakouts using the historical volatility
/// and enters positions when price breaks above or below those levels.
/// </summary>
public class HvBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _hvPeriod;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _referencePrice;
	private bool _isReferenceSet;
	private int _cooldown;

	/// <summary>
	/// Period for Historical Volatility calculation.
	/// </summary>
	public int HvPeriod
	{
		get => _hvPeriod.Value;
		set => _hvPeriod.Value = value;
	}

	/// <summary>
	/// Period for Moving Average calculation for exit.
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
	/// Initialize the Historical Volatility Breakout strategy.
	/// </summary>
	public HvBreakoutStrategy()
	{
		_hvPeriod = Param(nameof(HvPeriod), 20)
			.SetDisplay("HV Period", "Period for Historical Volatility calculation", "Indicators")
			.SetOptimize(10, 30, 5);

		_maPeriod = Param(nameof(MAPeriod), 20)
			.SetDisplay("MA Period", "Period for Moving Average calculation for exit", "Indicators")
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
		_referencePrice = default;
		_isReferenceSet = default;
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_referencePrice = 0;
		_isReferenceSet = false;
		_cooldown = 0;

		var standardDeviation = new StandardDeviation { Length = HvPeriod };
		var sma = new SimpleMovingAverage { Length = MAPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(standardDeviation, sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal stdDevValue, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var hv = candle.ClosePrice > 0 ? stdDevValue / candle.ClosePrice : 0;

		if (!_isReferenceSet)
		{
			_referencePrice = candle.ClosePrice;
			_isReferenceSet = true;
			return;
		}

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		var upperBreakoutLevel = _referencePrice * (1 + hv);
		var lowerBreakoutLevel = _referencePrice * (1 - hv);

		if (Position == 0)
		{
			if (candle.ClosePrice > upperBreakoutLevel)
			{
				BuyMarket();
				_cooldown = CooldownBars;
				_referencePrice = candle.ClosePrice;
			}
			else if (candle.ClosePrice < lowerBreakoutLevel)
			{
				SellMarket();
				_cooldown = CooldownBars;
				_referencePrice = candle.ClosePrice;
			}
		}
		else if (Position > 0)
		{
			if (candle.ClosePrice < smaValue)
			{
				SellMarket();
				_cooldown = CooldownBars;
			}
		}
		else if (Position < 0)
		{
			if (candle.ClosePrice > smaValue)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
		}
	}
}
