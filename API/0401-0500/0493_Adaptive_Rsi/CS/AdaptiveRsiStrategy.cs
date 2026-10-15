namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Adaptive RSI Strategy - uses RSI to compute adaptive smoothing factor,
/// trades on turns (local min/max) of the adaptive RSI line.
/// </summary>
public class AdaptiveRsiStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal? _arsiPrev;
	private decimal? _arsiPrevPrev;
	private int _cooldownRemaining;

	public AdaptiveRsiStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle type", "Candle type for strategy calculation.", "General");

		_length = Param(nameof(Length), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "Parameters");

		_cooldownBars = Param(nameof(CooldownBars), 15)
			.SetDisplay("Cooldown Bars", "Bars between trades", "Risk");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
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

		_arsiPrev = null;
		_arsiPrevPrev = null;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var rsi = new RelativeStrengthIndex { Length = Length };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var alpha = 2m * Math.Abs(rsiValue / 100m - 0.5m);
		var src = candle.ClosePrice;

		var prev = _arsiPrev ?? src;
		var arsi = alpha * src + (1 - alpha) * prev;

		if (_arsiPrevPrev is not null)
		{
			if (_cooldownRemaining > 0)
			{
				_cooldownRemaining--;
				_arsiPrevPrev = _arsiPrev;
				_arsiPrev = arsi;
				return;
			}

			var longCondition = _arsiPrev <= _arsiPrevPrev && arsi > _arsiPrev;
			var shortCondition = _arsiPrev >= _arsiPrevPrev && arsi < _arsiPrev;

			if (longCondition && Position <= 0)
			{
				if (Position < 0)
					BuyMarket(Math.Abs(Position));
				BuyMarket(Volume);
				_cooldownRemaining = CooldownBars;
			}
			else if (shortCondition && Position >= 0)
			{
				if (Position > 0)
					SellMarket(Math.Abs(Position));
				SellMarket(Volume);
				_cooldownRemaining = CooldownBars;
			}
		}

		_arsiPrevPrev = _arsiPrev;
		_arsiPrev = arsi;
	}
}
