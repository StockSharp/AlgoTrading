using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trades the 9:50 AM New York five-minute bar with fixed target and stop.
/// </summary>
public class The950BarStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _tickSize;
	private readonly StrategyParam<int> _targetTicks;
	private readonly StrategyParam<int> _stopTicks;

	private readonly TimeZoneInfo _nyTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
	private DateTime? _tradeDate;
	private decimal _targetPrice;
	private decimal _stopPrice;

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Instrument tick size.
	/// </summary>
	public decimal TickSize { get => _tickSize.Value; set => _tickSize.Value = value; }

	/// <summary>
	/// Profit target in ticks.
	/// </summary>
	public int TargetTicks { get => _targetTicks.Value; set => _targetTicks.Value = value; }

	/// <summary>
	/// Stop loss in ticks.
	/// </summary>
	public int StopTicks { get => _stopTicks.Value; set => _stopTicks.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="The950BarStrategy"/> class.
	/// </summary>
	public The950BarStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_tickSize = Param(nameof(TickSize), 0.25m)
			.SetDisplay("Tick Size", "Instrument tick size", "Parameters");

		_targetTicks = Param(nameof(TargetTicks), 150)
			.SetGreaterThanZero()
			.SetDisplay("Target Ticks", "Profit target in ticks", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(50, 300, 50);

		_stopTicks = Param(nameof(StopTicks), 200)
			.SetGreaterThanZero()
			.SetDisplay("Stop Ticks", "Stop loss in ticks", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(50, 300, 50);

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
		_tradeDate = null;
		_targetPrice = 0m;
		_stopPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		StartProtection();

		var sub = SubscribeCandles(CandleType);
		sub.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, sub);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var nyTime = TimeZoneInfo.ConvertTime(candle.OpenTime.UtcDateTime, _nyTimeZone);

		if (_tradeDate != nyTime.Date && nyTime.Hour == 9 && nyTime.Minute == 50)
		{
			_tradeDate = nyTime.Date;
			var isLong = candle.ClosePrice > candle.OpenPrice;
			if (isLong)
			{
				BuyMarket();
				_targetPrice = candle.ClosePrice + TickSize * TargetTicks;
				_stopPrice = candle.ClosePrice - TickSize * StopTicks;
			}
			else
			{
				SellMarket();
				_targetPrice = candle.ClosePrice - TickSize * TargetTicks;
				_stopPrice = candle.ClosePrice + TickSize * StopTicks;
			}
			return;
		}

		if (Position > 0)
		{
			if (candle.HighPrice >= _targetPrice || candle.LowPrice <= _stopPrice)
				SellMarket(Math.Abs(Position));
		}
		else if (Position < 0)
		{
			if (candle.LowPrice <= _targetPrice || candle.HighPrice >= _stopPrice)
				BuyMarket(Math.Abs(Position));
		}
	}
}
