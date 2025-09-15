using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the Figurelli Series indicator.
/// Opens positions at a specified start time when the indicator is positive or negative.
/// Closes all positions at the stop time.
/// </summary>
public class FigurelliSeriesStrategy : Strategy
{
	private readonly StrategyParam<int> _startPeriod;
	private readonly StrategyParam<int> _step;
	private readonly StrategyParam<int> _total;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _startMinute;
	private readonly StrategyParam<int> _stopHour;
	private readonly StrategyParam<int> _stopMinute;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>Initial period for moving averages.</summary>
	public int StartPeriod { get => _startPeriod.Value; set => _startPeriod.Value = value; }
	/// <summary>Step between moving average periods.</summary>
	public int Step { get => _step.Value; set => _step.Value = value; }
	/// <summary>Number of moving averages.</summary>
	public int Total { get => _total.Value; set => _total.Value = value; }
	/// <summary>Hour to start trading.</summary>
	public int StartHour { get => _startHour.Value; set => _startHour.Value = value; }
	/// <summary>Minute to start trading.</summary>
	public int StartMinute { get => _startMinute.Value; set => _startMinute.Value = value; }
	/// <summary>Hour to stop trading.</summary>
	public int StopHour { get => _stopHour.Value; set => _stopHour.Value = value; }
	/// <summary>Minute to stop trading.</summary>
	public int StopMinute { get => _stopMinute.Value; set => _stopMinute.Value = value; }
	/// <summary>Candle type to use.</summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>Constructor.</summary>
	public FigurelliSeriesStrategy()
	{
		_startPeriod = Param(nameof(StartPeriod), 6)
			.SetGreaterThanZero()
			.SetDisplay("Start Period", "Initial period for moving averages", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(6, 18, 6);

		_step = Param(nameof(Step), 6)
			.SetGreaterThanZero()
			.SetDisplay("Step", "Step between moving average periods", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(6, 12, 2);

		_total = Param(nameof(Total), 36)
			.SetGreaterThanZero()
			.SetDisplay("Total", "Number of moving averages", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(12, 48, 12);

		_startHour = Param(nameof(StartHour), 8)
			.SetDisplay("Start Hour", "Hour to start trading", "Time")
			.SetCanOptimize(true)
			.SetOptimize(0, 23, 1);

		_startMinute = Param(nameof(StartMinute), 0)
			.SetDisplay("Start Minute", "Minute to start trading", "Time")
			.SetCanOptimize(true)
			.SetOptimize(0, 59, 1);

		_stopHour = Param(nameof(StopHour), 23)
			.SetDisplay("Stop Hour", "Hour to stop trading", "Time")
			.SetCanOptimize(true)
			.SetOptimize(0, 23, 1);

		_stopMinute = Param(nameof(StopMinute), 59)
			.SetDisplay("Stop Minute", "Minute to stop trading", "Time")
			.SetCanOptimize(true)
			.SetOptimize(0, 59, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for calculations", "General");
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

		var figurelli = new FigurelliSeriesIndicator
		{
			StartPeriod = StartPeriod,
			Step = Step,
			Total = Total
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(figurelli, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, figurelli);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var time = candle.OpenTime;

		// Close positions outside trading window
		if (time.Hour > StopHour ||
			(time.Hour == StopHour && time.Minute >= StopMinute) ||
			time.Hour < StartHour)
		{
			if (Position > 0)
				SellMarket(Position);
			else if (Position < 0)
				BuyMarket(-Position);
			return;
		}

		// Open positions at start time based on indicator sign
		if (time.Hour == StartHour && time.Minute == StartMinute)
		{
			if (value > 0 && Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
			else if (value < 0 && Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
		}
	}

	private class FigurelliSeriesIndicator : Indicator<decimal>
	{
		public int StartPeriod { get; set; }
		public int Step { get; set; }
		public int Total { get; set; }

		private readonly List<EMA> _averages = new();

		public override void Reset()
		{
			base.Reset();
			foreach (var ma in _averages)
				ma.Reset();
		}

		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var price = input.GetValue<decimal>();

			if (_averages.Count == 0)
			{
				for (var i = 0; i < Total; i++)
					_averages.Add(new EMA { Length = StartPeriod + Step * i });
			}

			var bids = 0;
			var asks = 0;

			foreach (var ma in _averages)
			{
				var maValue = ma.Process(input).GetValue<decimal>();
				if (!ma.IsFormed)
					continue;

				if (price > maValue)
					bids++;
				else if (price < maValue)
					asks++;
			}

			var result = bids - asks;
			return new DecimalIndicatorValue(this, result, input.Time);
		}
	}
}
