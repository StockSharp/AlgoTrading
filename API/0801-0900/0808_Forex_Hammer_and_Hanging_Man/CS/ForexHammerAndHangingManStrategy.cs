using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Forex Hammer and Hanging Man strategy.
/// A candle qualifies when its range exceeds BodyLengthMultiplier times its body and its lower shadow is longer than ShadowRatio
/// times its upper shadow. A bullish one is a hammer and goes long, a bearish one is a hanging man and goes short, reversing an
/// opposite position. A position is closed after HoldPeriods candles.
/// </summary>
public class ForexHammerAndHangingManStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _bodyLengthMultiplier;
	private readonly StrategyParam<decimal> _shadowRatio;
	private readonly StrategyParam<int> _holdPeriods;

	private int _barsInPosition;

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// How many bodies the candle range must exceed.
	/// </summary>
	public decimal BodyLengthMultiplier
	{
		get => _bodyLengthMultiplier.Value;
		set => _bodyLengthMultiplier.Value = value;
	}

	/// <summary>
	/// How many upper shadows the lower shadow must exceed.
	/// </summary>
	public decimal ShadowRatio
	{
		get => _shadowRatio.Value;
		set => _shadowRatio.Value = value;
	}

	/// <summary>
	/// Candles a position is held.
	/// </summary>
	public int HoldPeriods
	{
		get => _holdPeriods.Value;
		set => _holdPeriods.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public ForexHammerAndHangingManStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_bodyLengthMultiplier = Param(nameof(BodyLengthMultiplier), 5m)
			.SetNotNegative()
			.SetDisplay("Body Multiplier", "How many bodies the candle range must exceed", "Pattern");

		_shadowRatio = Param(nameof(ShadowRatio), 1m)
			.SetNotNegative()
			.SetDisplay("Shadow Ratio", "How many upper shadows the lower shadow must exceed", "Pattern");

		_holdPeriods = Param(nameof(HoldPeriods), 26)
			.SetGreaterThanZero()
			.SetDisplay("Hold Periods", "Candles a position is held", "Trading");
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
		_barsInPosition = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_barsInPosition = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var body = Math.Abs(candle.ClosePrice - candle.OpenPrice);
		var range = candle.HighPrice - candle.LowPrice;
		var lowerShadow = Math.Min(candle.OpenPrice, candle.ClosePrice) - candle.LowPrice;
		var upperShadow = candle.HighPrice - Math.Max(candle.OpenPrice, candle.ClosePrice);

		var shape = range > 0 && range > BodyLengthMultiplier * body && lowerShadow > ShadowRatio * upperShadow;
		var hammer = shape && candle.ClosePrice > candle.OpenPrice;
		var hangingMan = shape && candle.ClosePrice < candle.OpenPrice;

		if (hammer && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_barsInPosition = 0;
			return;
		}

		if (hangingMan && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_barsInPosition = 0;
			return;
		}

		if (Position == 0)
			return;

		_barsInPosition++;

		if (_barsInPosition < HoldPeriods)
			return;

		if (Position > 0)
			SellMarket(Position);
		else
			BuyMarket(-Position);
	}
}
