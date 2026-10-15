using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Customizable BTC seasonality strategy.
/// Opens a long during the EntryHour UTC hour and closes it during the ExitHour UTC hour.
/// </summary>
public class CustomizableBtcSeasonalityStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _entryHour;
	private readonly StrategyParam<int> _exitHour;

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// UTC hour when the long is opened.
	/// </summary>
	public int EntryHour
	{
		get => _entryHour.Value;
		set => _entryHour.Value = value;
	}

	/// <summary>
	/// UTC hour when the long is closed.
	/// </summary>
	public int ExitHour
	{
		get => _exitHour.Value;
		set => _exitHour.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public CustomizableBtcSeasonalityStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_entryHour = Param(nameof(EntryHour), 21)
			.SetRange(0, 23)
			.SetDisplay("Entry Hour", "UTC hour when the long is opened", "Time");

		_exitHour = Param(nameof(ExitHour), 23)
			.SetRange(0, 23)
			.SetDisplay("Exit Hour", "UTC hour when the long is closed", "Time");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

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

		var hour = candle.OpenTime.ToUniversalTime().Hour;

		if (hour == ExitHour && Position > 0)
			SellMarket(Position);
		else if (hour == EntryHour && Position == 0)
			BuyMarket();
	}
}
