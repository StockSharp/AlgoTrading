namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Internal bar strength mean reversion strategy.
/// </summary>
public class InternalBarStrengthIbsStrategy : Strategy
{
	private readonly StrategyParam<decimal> _upperThreshold;
	private readonly StrategyParam<decimal> _lowerThreshold;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DateTimeOffset> _startTime;
	private readonly StrategyParam<DateTimeOffset> _endTime;

	/// <summary>
	/// IBS value to exit position.
	/// </summary>
	public decimal UpperThreshold { get => _upperThreshold.Value; set => _upperThreshold.Value = value; }

	/// <summary>
	/// IBS value to enter long.
	/// </summary>
	public decimal LowerThreshold { get => _lowerThreshold.Value; set => _lowerThreshold.Value = value; }

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Start time for signals.
	/// </summary>
	public DateTimeOffset StartTime { get => _startTime.Value; set => _startTime.Value = value; }

	/// <summary>
	/// End time for signals.
	/// </summary>
	public DateTimeOffset EndTime { get => _endTime.Value; set => _endTime.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="InternalBarStrengthIbsStrategy"/>.
	/// </summary>
	public InternalBarStrengthIbsStrategy()
	{
		_upperThreshold = Param(nameof(UpperThreshold), 0.8m)
			.SetRange(0m, 1m)
			.SetDisplay("Upper Threshold", "IBS value to exit position", "Parameters")
			.SetCanOptimize(true);

		_lowerThreshold = Param(nameof(LowerThreshold), 0.2m)
			.SetRange(0m, 1m)
			.SetDisplay("Lower Threshold", "IBS value to enter long", "Parameters")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_startTime = Param(nameof(StartTime), new DateTimeOffset(2014, 1, 1, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("Start Time", "Start time for strategy", "Time");

		_endTime = Param(nameof(EndTime), new DateTimeOffset(2099, 1, 1, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("End Time", "End time for strategy", "Time");
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

		StartProtection();

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

		var time = candle.OpenTime;

		var range = candle.HighPrice - candle.LowPrice;
		if (range == 0)
			return;

		var ibs = (candle.ClosePrice - candle.LowPrice) / range;

		var inWindow = time >= StartTime && time <= EndTime;

		if (inWindow && ibs < LowerThreshold && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			LogInfo($"Long entry at {candle.ClosePrice}, IBS={ibs:F2}");
		}

		if (Position > 0 && ibs >= UpperThreshold)
		{
			SellMarket(Math.Abs(Position));
			LogInfo($"Exit long at {candle.ClosePrice}, IBS={ibs:F2}");
		}
	}
}
