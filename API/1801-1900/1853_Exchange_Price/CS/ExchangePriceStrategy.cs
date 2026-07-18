using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on comparing current price with prices from short and long lookback periods.
/// </summary>
public class ExchangePriceStrategy : Strategy
{
	private readonly StrategyParam<int> _shortPeriod;
	private readonly StrategyParam<int> _longPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _minDiffPercent;
	private readonly StrategyParam<int> _cooldownBars;

	private readonly List<decimal> _prices = new();
	private decimal? _prevUpDiff;
	private decimal? _prevDownDiff;
	private int _cooldownRemaining;

	public int ShortPeriod { get => _shortPeriod.Value; set => _shortPeriod.Value = value; }
	public int LongPeriod { get => _longPeriod.Value; set => _longPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public decimal MinDiffPercent { get => _minDiffPercent.Value; set => _minDiffPercent.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }

	public ExchangePriceStrategy()
	{
		_shortPeriod = Param(nameof(ShortPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("Short Period", "Bars for short lookback", "General");

		_longPeriod = Param(nameof(LongPeriod), 48)
			.SetGreaterThanZero()
			.SetDisplay("Long Period", "Bars for long lookback", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_minDiffPercent = Param(nameof(MinDiffPercent), 0.0025m)
			.SetDisplay("Minimum Difference %", "Minimum normalized difference between short and long deltas", "Filters");

		_cooldownBars = Param(nameof(CooldownBars), 4)
			.SetDisplay("Cooldown Bars", "Completed candles to wait after a position change", "Trading");
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
		_prices.Clear();
		_prevUpDiff = null;
		_prevDownDiff = null;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

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

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		_prices.Add(candle.ClosePrice);
		if (_prices.Count > LongPeriod + 1)
			_prices.RemoveAt(0);

		if (_prices.Count <= LongPeriod || _prices.Count <= ShortPeriod)
			return;

		var priceShort = _prices[_prices.Count - 1 - ShortPeriod];
		var priceLong = _prices[_prices.Count - 1 - LongPeriod];
		var upDiff = candle.ClosePrice - priceShort;
		var downDiff = candle.ClosePrice - priceLong;
		var diffPercent = candle.ClosePrice != 0m ? Math.Abs(upDiff - downDiff) / candle.ClosePrice : 0m;

		if (_prevUpDiff is decimal prevUp && _prevDownDiff is decimal prevDown && _cooldownRemaining == 0)
		{
			var crossUp = prevUp <= prevDown && upDiff > downDiff && diffPercent >= MinDiffPercent;
			var crossDown = prevUp >= prevDown && upDiff < downDiff && diffPercent >= MinDiffPercent;

			if (crossUp && Position <= 0)
			{
				if (Position < 0)
					BuyMarket();

				BuyMarket();
				_cooldownRemaining = CooldownBars;
			}
			else if (crossDown && Position >= 0)
			{
				if (Position > 0)
					SellMarket();

				SellMarket();
				_cooldownRemaining = CooldownBars;
			}
		}

		_prevUpDiff = upDiff;
		_prevDownDiff = downDiff;
	}
}

