using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Gap Fill Reversal strategy.
/// Enters when a gap between candles is followed by a reversal candle.
/// Gap up + bearish candle = short, gap down + bullish candle = long.
/// Uses SMA for exit confirmation.
/// Uses cooldown to control trade frequency.
/// </summary>
public class GapFillReversalStrategy : Strategy
{
	private readonly StrategyParam<decimal> _minGapPercent;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private ICandleMessage _prevCandle;
	private int _cooldown;

	/// <summary>
	/// Minimum gap size as percentage.
	/// </summary>
	public decimal MinGapPercent
	{
		get => _minGapPercent.Value;
		set => _minGapPercent.Value = value;
	}

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
	public GapFillReversalStrategy()
	{
		_minGapPercent = Param(nameof(MinGapPercent), 0.02m)
			.SetRange(0.01m, 1m)
			.SetDisplay("Min Gap %", "Minimum gap size percentage", "Trading");

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

		var prevClose = _prevCandle.ClosePrice;

		// Gap detection
		var gapUp = candle.OpenPrice > prevClose;
		var gapDown = candle.OpenPrice < prevClose;

		decimal gapPercent = 0;
		if (gapUp)
			gapPercent = (candle.OpenPrice - prevClose) / prevClose * 100;
		else if (gapDown)
			gapPercent = (prevClose - candle.OpenPrice) / prevClose * 100;

		var isBearishCandle = candle.ClosePrice < candle.OpenPrice;
		var isBullishCandle = candle.ClosePrice > candle.OpenPrice;

		if (gapPercent >= MinGapPercent)
		{
			// Gap down + bullish reversal = long
			if (Position == 0 && gapDown && isBullishCandle)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
			// Gap up + bearish reversal = short
			else if (Position == 0 && gapUp && isBearishCandle)
			{
				SellMarket();
				_cooldown = CooldownBars;
			}
		}

		// Exit on SMA cross
		if (Position > 0 && candle.ClosePrice < smaValue)
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
