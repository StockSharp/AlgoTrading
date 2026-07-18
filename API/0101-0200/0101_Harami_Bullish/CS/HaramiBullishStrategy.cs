using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Harami Bullish strategy.
/// Enters long on bullish harami (bearish candle followed by smaller bullish candle inside it).
/// Enters short on bearish harami (bullish candle followed by smaller bearish candle inside it).
/// Uses SMA for exit confirmation.
/// Uses cooldown to control trade frequency.
/// </summary>
public class HaramiBullishStrategy : Strategy
{
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private ICandleMessage _prevCandle;
	private int _cooldown;

	/// <summary>
	/// MA period for exit.
	/// </summary>
	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
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
	public HaramiBullishStrategy()
	{
		_maLength = Param(nameof(MaLength), 20)
			.SetRange(10, 50)
			.SetDisplay("MA Length", "Period of SMA for exit", "Indicators");

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
		_prevCandle = null;
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevCandle = null;
		_cooldown = 0;

		var sma = new SimpleMovingAverage { Length = MaLength };

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

		if (_prevCandle == null)
		{
			_prevCandle = candle;
			return;
		}

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevCandle = candle;
			return;
		}

		// Bullish Harami: prev bearish, current bullish, current inside prev
		var bullishHarami =
			_prevCandle.ClosePrice < _prevCandle.OpenPrice &&
			candle.ClosePrice > candle.OpenPrice &&
			candle.HighPrice < _prevCandle.HighPrice &&
			candle.LowPrice > _prevCandle.LowPrice;

		// Bearish Harami: prev bullish, current bearish, current inside prev
		var bearishHarami =
			_prevCandle.ClosePrice > _prevCandle.OpenPrice &&
			candle.ClosePrice < candle.OpenPrice &&
			candle.HighPrice < _prevCandle.HighPrice &&
			candle.LowPrice > _prevCandle.LowPrice;

		if (Position == 0 && bullishHarami)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
		else if (Position == 0 && bearishHarami)
		{
			SellMarket();
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

		_prevCandle = candle;
	}
}
