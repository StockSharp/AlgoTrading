using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bearish Engulfing strategy.
/// Enters short on bearish engulfing pattern above SMA.
/// Enters long on bullish engulfing pattern below SMA.
/// Exits via SMA crossover.
/// </summary>
public class EngulfingBearishStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private ICandleMessage _previousCandle;
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
	public EngulfingBearishStrategy()
	{
		_maPeriod = Param(nameof(MAPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Period for SMA", "Indicators");

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
		_previousCandle = null;
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_previousCandle = null;
		_cooldown = 0;

		var sma = new SimpleMovingAverage { Length = MAPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_cooldown > 0)
		{
			_cooldown--;
			_previousCandle = candle;
			return;
		}

		if (_previousCandle != null)
		{
			var isPrevBullish = _previousCandle.ClosePrice > _previousCandle.OpenPrice;
			var isPrevBearish = _previousCandle.ClosePrice < _previousCandle.OpenPrice;
			var isCurrBearish = candle.ClosePrice < candle.OpenPrice;
			var isCurrBullish = candle.ClosePrice > candle.OpenPrice;

			var bearishEngulfing = isPrevBullish && isCurrBearish &&
				candle.ClosePrice < _previousCandle.OpenPrice &&
				candle.OpenPrice > _previousCandle.ClosePrice;

			var bullishEngulfing = isPrevBearish && isCurrBullish &&
				candle.ClosePrice > _previousCandle.OpenPrice &&
				candle.OpenPrice < _previousCandle.ClosePrice;

			if (Position == 0 && bearishEngulfing && candle.ClosePrice > smaValue)
			{
				SellMarket();
				_cooldown = CooldownBars;
			}
			else if (Position == 0 && bullishEngulfing && candle.ClosePrice < smaValue)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
			else if (Position > 0 && candle.ClosePrice < smaValue)
			{
				SellMarket();
				_cooldown = CooldownBars;
			}
			else if (Position < 0 && candle.ClosePrice > smaValue)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
		}

		_previousCandle = candle;
	}
}
