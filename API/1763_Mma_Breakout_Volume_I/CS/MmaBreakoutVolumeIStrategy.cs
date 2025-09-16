using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on smoothed moving average breakout with EMA exit.
/// </summary>
public class MmaBreakoutVolumeIStrategy : Strategy
{
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _exitPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevPrice;
	private decimal _prevSlow;

	/// <summary>
	/// Period for long SMMA.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Period for exit EMA.
	/// </summary>
	public int ExitPeriod
	{
		get => _exitPeriod.Value;
		set => _exitPeriod.Value = value;
	}

	/// <summary>
	/// Candle type to use.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize parameters.
	/// </summary>
	public MmaBreakoutVolumeIStrategy()
	{
		_slowPeriod = Param(nameof(SlowPeriod), 200)
			.SetDisplay("Slow SMMA Period", "Period for long smoothed moving average", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(100, 300, 50);

		_exitPeriod = Param(nameof(ExitPeriod), 5)
			.SetDisplay("Exit EMA Period", "Period for exit exponential moving average", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(3, 15, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_prevPrice = default;
		_prevSlow = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var slowSmma = new SmoothedMovingAverage { Length = SlowPeriod };
		var exitEma = new ExponentialMovingAverage { Length = ExitPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(slowSmma, exitEma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, slowSmma);
			DrawIndicator(area, exitEma);
			DrawOwnTrades(area);
		}

		this.StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal slowValue, decimal exitValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_prevPrice == 0m || _prevSlow == 0m)
		{
			_prevPrice = candle.ClosePrice;
			_prevSlow = slowValue;
			return;
		}

		var isCrossAbove = _prevPrice <= _prevSlow && candle.ClosePrice > slowValue;
		var isCrossBelow = _prevPrice >= _prevSlow && candle.ClosePrice < slowValue;
		var exitLong = Position > 0 && candle.ClosePrice < exitValue;
		var exitShort = Position < 0 && candle.ClosePrice > exitValue;

		if (isCrossAbove && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			LogInfo($"Buy: price crossed above SMMA({SlowPeriod}) at {candle.ClosePrice}");
		}
		else if (isCrossBelow && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			LogInfo($"Sell: price crossed below SMMA({SlowPeriod}) at {candle.ClosePrice}");
		}
		else if (exitLong)
		{
			SellMarket(Position);
			LogInfo($"Exit long: price fell below EMA({ExitPeriod}) at {candle.ClosePrice}");
		}
		else if (exitShort)
		{
			BuyMarket(Math.Abs(Position));
			LogInfo($"Exit short: price rose above EMA({ExitPeriod}) at {candle.ClosePrice}");
		}

		_prevPrice = candle.ClosePrice;
		_prevSlow = slowValue;
	}
}

