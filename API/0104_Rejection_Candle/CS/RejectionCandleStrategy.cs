using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Rejection Candle (Pin Bar) strategy.
/// Enters long on bullish rejection (lower low + bullish close + long lower wick).
/// Enters short on bearish rejection (higher high + bearish close + long upper wick).
/// Uses SMA for exit confirmation.
/// Uses cooldown to control trade frequency.
/// </summary>
public class RejectionCandleStrategy : Strategy
{
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<decimal> _wickRatio;
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
	/// Wick to body ratio threshold.
	/// </summary>
	public decimal WickRatio
	{
		get => _wickRatio.Value;
		set => _wickRatio.Value = value;
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
	public RejectionCandleStrategy()
	{
		_maLength = Param(nameof(MaLength), 20)
			.SetRange(10, 50)
			.SetDisplay("MA Length", "Period of SMA for exit", "Indicators");

		_wickRatio = Param(nameof(WickRatio), 1.5m)
			.SetRange(1m, 3m)
			.SetDisplay("Wick Ratio", "Min wick to body ratio for rejection", "Pattern");

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

		var bodySize = Math.Abs(candle.ClosePrice - candle.OpenPrice);
		if (bodySize == 0) bodySize = 0.01m; // avoid div by zero

		var upperWick = candle.HighPrice - Math.Max(candle.OpenPrice, candle.ClosePrice);
		var lowerWick = Math.Min(candle.OpenPrice, candle.ClosePrice) - candle.LowPrice;

		var isBullish = candle.ClosePrice > candle.OpenPrice;
		var isBearish = candle.ClosePrice < candle.OpenPrice;

		// Bullish rejection: made lower low, bullish close, long lower wick
		var bullishRejection =
			candle.LowPrice < _prevCandle.LowPrice &&
			isBullish &&
			lowerWick > bodySize * WickRatio;

		// Bearish rejection: made higher high, bearish close, long upper wick
		var bearishRejection =
			candle.HighPrice > _prevCandle.HighPrice &&
			isBearish &&
			upperWick > bodySize * WickRatio;

		if (Position == 0 && bullishRejection)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
		else if (Position == 0 && bearishRejection)
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
