using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class ReminderMessageWithColorPickerStrategy : Strategy
{
	private readonly StrategyParam<string> _message;
	private readonly StrategyParam<int> _frequency;
	private readonly StrategyParam<DataType> _candleType;

	public string Message
	{
		get => _message.Value;
		set => _message.Value = value;
	}

	public int Frequency
	{
		get => _frequency.Value;
		set => _frequency.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public ReminderMessageWithColorPickerStrategy()
	{
		_message = Param(nameof(Message),
			"If you don't see an obvious trade, there probably isn't one.")
			.SetDisplay("Reminder to display");

		_frequency = Param(nameof(Frequency), 1)
			.SetDisplay("Only display every x bars", "Trading parameters")
			.SetCanOptimize(true)
			.SetOptimize(1, 10, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	private int _bars;

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		SubscribeCandles(CandleType)
			.Bind(OnCandle)
			.Start();
	}

	private void OnCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_bars++;

		if (_bars % Frequency == 0)
			LogInfo(Message);
	}
}
