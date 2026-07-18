namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// EMA Moving Away Strategy.
/// Buys when price moves too far below EMA (mean reversion).
/// Sells when price moves too far above EMA.
/// Exits when price returns to EMA.
/// </summary>
public class EmaMovingAwayStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleTypeParam;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<decimal> _movingAwayPercent;
	private readonly StrategyParam<int> _cooldownBars;

	private ExponentialMovingAverage _ema;
	private int _cooldownRemaining;

	public EmaMovingAwayStrategy()
	{
		_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle type", "Candle type for strategy calculation.", "General");

		_emaLength = Param(nameof(EmaLength), 55)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "EMA period", "Moving Average");

		_movingAwayPercent = Param(nameof(MovingAwayPercent), 1.5m)
			.SetDisplay("Moving away (%)", "Required percentage that price moves away from EMA", "Strategy");

		_cooldownBars = Param(nameof(CooldownBars), 10)
			.SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk");
	}

	public DataType CandleType
	{
		get => _candleTypeParam.Value;
		set => _candleTypeParam.Value = value;
	}

	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	public decimal MovingAwayPercent
	{
		get => _movingAwayPercent.Value;
		set => _movingAwayPercent.Value = value;
	}

	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_ema = null;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_ema = new ExponentialMovingAverage { Length = EmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_ema, OnProcess)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema);
			DrawOwnTrades(area);
		}
	}

	private void OnProcess(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_ema.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			return;
		}

		var close = candle.ClosePrice;

		// Calculate entry zones
		var longEntryLevel = emaValue * (1 - MovingAwayPercent / 100);
		var shortEntryLevel = emaValue * (1 + MovingAwayPercent / 100);

		// Exit conditions - price returns to EMA
		if (Position > 0 && close >= emaValue)
		{
			SellMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
			return;
		}
		else if (Position < 0 && close <= emaValue)
		{
			BuyMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
			return;
		}

		// Entry: price far below EMA - buy (mean reversion)
		if (close <= longEntryLevel && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Entry: price far above EMA - sell (mean reversion)
		else if (close >= shortEntryLevel && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
	}
}
