using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Three White Soldiers strategy.
/// Enters long when three consecutive bullish candles with rising closes are detected.
/// Enters short when three consecutive bearish candles with falling closes are detected.
/// Uses SMA for exit confirmation.
/// Uses cooldown to control trade frequency.
/// </summary>
public class ThreeWhiteSoldiersStrategy : Strategy
{
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private ICandleMessage _candle1;
	private ICandleMessage _candle2;
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
	public ThreeWhiteSoldiersStrategy()
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
		_candle1 = null;
		_candle2 = null;
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_candle1 = null;
		_candle2 = null;
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

		// Shift candles
		var prev2 = _candle1;
		var prev1 = _candle2;
		_candle1 = _candle2;
		_candle2 = candle;

		if (prev2 == null || prev1 == null)
			return;

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		// Three White Soldiers: 3 consecutive bullish candles with rising closes
		var threeWhite =
			prev2.ClosePrice > prev2.OpenPrice &&
			prev1.ClosePrice > prev1.OpenPrice &&
			candle.ClosePrice > candle.OpenPrice &&
			prev1.ClosePrice > prev2.ClosePrice &&
			candle.ClosePrice > prev1.ClosePrice;

		// Three Black Crows: 3 consecutive bearish candles with falling closes
		var threeBlack =
			prev2.ClosePrice < prev2.OpenPrice &&
			prev1.ClosePrice < prev1.OpenPrice &&
			candle.ClosePrice < candle.OpenPrice &&
			prev1.ClosePrice < prev2.ClosePrice &&
			candle.ClosePrice < prev1.ClosePrice;

		if (Position == 0 && threeWhite)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
		else if (Position == 0 && threeBlack)
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
	}
}
