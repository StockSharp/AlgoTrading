using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that opens a long position at a user-defined UTC hour and closes it at another.
/// </summary>
public class CustomizableBtcSeasonalityStrategy : Strategy
{
	private readonly StrategyParam<int> _entryHour;
	private readonly StrategyParam<int> _exitHour;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Entry hour in UTC.
	/// </summary>
	public int EntryHour
	{
		get => _entryHour.Value;
		set => _entryHour.Value = value;
	}

	/// <summary>
	/// Exit hour in UTC.
	/// </summary>
	public int ExitHour
	{
		get => _exitHour.Value;
		set => _exitHour.Value = value;
	}

	/// <summary>
	/// Candle type for strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CustomizableBtcSeasonalityStrategy"/>.
	/// </summary>
	public CustomizableBtcSeasonalityStrategy()
	{
		_entryHour = Param(nameof(EntryHour), 21)
			.SetRange(0, 23)
			.SetDisplay("Entry Hour", "Entry hour in UTC", "Strategy")
			.SetCanOptimize(true);

		_exitHour = Param(nameof(ExitHour), 23)
			.SetRange(0, 23)
			.SetDisplay("Exit Hour", "Exit hour in UTC", "Strategy")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for strategy", "Strategy");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

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

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var closeTime = candle.CloseTime.UtcDateTime;

		if (closeTime.Hour == EntryHour && closeTime.Minute == 0 && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			LogInfo($"Entered long at {closeTime}: Volume={volume}");
		}
		else if (closeTime.Hour == ExitHour && closeTime.Minute == 0 && Position > 0)
		{
			ClosePosition();
			LogInfo($"Closed long at {closeTime}");
		}
	}
}
