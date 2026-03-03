using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// VWAP Breakout strategy.
/// Enters long when price breaks above VWAP, short when below.
/// </summary>
public class VWAPBreakoutStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _previousClosePrice;
	private decimal _previousVWAP;
	private int _cooldown;

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
	/// Initialize the VWAP Breakout strategy.
	/// </summary>
	public VWAPBreakoutStrategy()
	{
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
		_previousClosePrice = default;
		_previousVWAP = default;
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_previousClosePrice = 0;
		_previousVWAP = 0;
		_cooldown = 0;

		var vwap = new VolumeWeightedMovingAverage();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(vwap, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, vwap);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal vwapPrice)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_previousClosePrice == 0)
		{
			_previousClosePrice = candle.ClosePrice;
			_previousVWAP = vwapPrice;
			return;
		}

		if (_cooldown > 0)
		{
			_cooldown--;
			_previousClosePrice = candle.ClosePrice;
			_previousVWAP = vwapPrice;
			return;
		}

		var breakoutUp = _previousClosePrice <= _previousVWAP && candle.ClosePrice > vwapPrice;
		var breakoutDown = _previousClosePrice >= _previousVWAP && candle.ClosePrice < vwapPrice;

		if (Position == 0 && breakoutUp)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
		else if (Position == 0 && breakoutDown)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position > 0 && candle.ClosePrice < vwapPrice)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position < 0 && candle.ClosePrice > vwapPrice)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}

		_previousClosePrice = candle.ClosePrice;
		_previousVWAP = vwapPrice;
	}
}
