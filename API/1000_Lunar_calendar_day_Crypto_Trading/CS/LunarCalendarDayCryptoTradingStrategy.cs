using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades according to the lunar calendar.
/// Enters a long position on a specified lunar day and exits on another.
/// </summary>
public class LunarCalendarDayCryptoTradingStrategy : Strategy
{
	private static readonly TimeSpan SeoulOffset = TimeSpan.FromHours(9);

	private static readonly Dictionary<int, (DateTimeOffset Start, int[] Lengths)> LunarData = new()
	{
		{2020, (new DateTimeOffset(2020, 1, 25, 0, 0, 0, SeoulOffset), new[] {29, 30, 29, 30, 29, 30, 29, 30, 29, 30, 30, 29})},
		{2021, (new DateTimeOffset(2021, 2, 12, 0, 0, 0, SeoulOffset), new[] {30, 29, 30, 29, 30, 29, 30, 29, 30, 29, 30, 30})},
		{2022, (new DateTimeOffset(2022, 2, 1, 0, 0, 0, SeoulOffset), new[] {29, 30, 29, 30, 29, 30, 29, 30, 30, 29, 30, 29})},
		{2023, (new DateTimeOffset(2023, 1, 22, 0, 0, 0, SeoulOffset), new[] {30, 29, 30, 29, 30, 29, 30, 30, 29, 30, 29, 30})},
		{2024, (new DateTimeOffset(2024, 2, 10, 0, 0, 0, SeoulOffset), new[] {30, 29, 30, 29, 30, 29, 30, 29, 30, 29, 30, 30, 29})},
		{2025, (new DateTimeOffset(2025, 1, 29, 0, 0, 0, SeoulOffset), new[] {29, 30, 29, 30, 29, 30, 29, 30, 30, 29, 30, 29})},
		{2026, (new DateTimeOffset(2026, 2, 17, 0, 0, 0, SeoulOffset), new[] {30, 29, 30, 29, 30, 29, 30, 30, 29, 30, 29, 30})},
	};

	private readonly StrategyParam<int> _buyDay;
	private readonly StrategyParam<int> _sellDay;
	private readonly StrategyParam<DataType> _candleType;

	private DateTimeOffset _lastTradeDate;

	public int BuyDay { get => _buyDay.Value; set => _buyDay.Value = value; }
	public int SellDay { get => _sellDay.Value; set => _sellDay.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public LunarCalendarDayCryptoTradingStrategy()
	{
		_buyDay = Param(nameof(BuyDay), 12)
			.SetDisplay("Buy Day", "Lunar day to enter long", "Trading")
			.SetOptimize(1, 30, 1);

		_sellDay = Param(nameof(SellDay), 26)
			.SetDisplay("Sell Day", "Lunar day to exit", "Trading")
			.SetOptimize(1, 30, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for candles", "General");
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
		_lastTradeDate = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_lastTradeDate = default;

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

		var day = GetLunarDay(candle.OpenTime);
		if (day is null)
			return;

		// Only trade once per calendar day
		if (candle.OpenTime.Date == _lastTradeDate.Date)
			return;

		if (day == BuyDay && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
			_lastTradeDate = candle.OpenTime;
		}

		if (day == SellDay && Position > 0)
		{
			SellMarket();
			_lastTradeDate = candle.OpenTime;
		}
	}

	private static int? GetLunarDay(DateTimeOffset time)
	{
		if (!LunarData.TryGetValue(time.Year, out var data))
			return null;

		if (time < data.Start)
			return null;

		var days = (time.Date - data.Start.Date).Days;

		var offset = 0;
		foreach (var length in data.Lengths)
		{
			if (days < offset + length)
				return days - offset + 1;

			offset += length;
		}

		return null;
	}
}
