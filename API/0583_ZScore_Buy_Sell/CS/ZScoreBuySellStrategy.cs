using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Z-Score mean reversion strategy with cooldown between signals.
/// </summary>
public class ZScoreBuySellStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rollingWindow;
	private readonly StrategyParam<decimal> _zThreshold;
	private readonly StrategyParam<int> _coolDown;

	private int _buyCooldownCounter;
	private int _sellCooldownCounter;

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Period for moving average and standard deviation.
	/// </summary>
	public int RollingWindow
	{
		get => _rollingWindow.Value;
		set => _rollingWindow.Value = value;
	}

	/// <summary>
	/// Absolute z-score level to trigger trades.
	/// </summary>
	public decimal ZThreshold
	{
		get => _zThreshold.Value;
		set => _zThreshold.Value = value;
	}

	/// <summary>
	/// Bars to wait after trade.
	/// </summary>
	public int CoolDown
	{
		get => _coolDown.Value;
		set => _coolDown.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ZScoreBuySellStrategy"/>.
	/// </summary>
	public ZScoreBuySellStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_rollingWindow = Param(nameof(RollingWindow), 80)
			.SetGreaterThanZero()
			.SetDisplay("Rolling Window", "Lookback period", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(20, 150, 10);

		_zThreshold = Param(nameof(ZThreshold), 2.8m)
			.SetGreaterThanZero()
			.SetDisplay("Z Threshold", "Z-score trigger level", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 0.5m);

		_coolDown = Param(nameof(CoolDown), 5)
			.SetGreaterThanZero()
			.SetDisplay("Cool Down", "Bars to wait after trade", "Parameters");
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
		_buyCooldownCounter = _sellCooldownCounter = CoolDown;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var sma = new SimpleMovingAverage { Length = RollingWindow };
		var stdDev = new StandardDeviation { Length = RollingWindow };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, stdDev, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue, decimal stdDevValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (stdDevValue == 0)
			return;

		var zScore = (candle.ClosePrice - smaValue) / stdDevValue;

		if (zScore > ZThreshold)
		{
			if (_sellCooldownCounter >= CoolDown)
			{
				if (Position >= 0)
					RegisterSell();
				_sellCooldownCounter = 0;
				_buyCooldownCounter = CoolDown;
			}
			else
			{
				_sellCooldownCounter++;
			}
		}
		else if (zScore < -ZThreshold)
		{
			if (_buyCooldownCounter >= CoolDown)
			{
				if (Position <= 0)
					RegisterBuy();
				_sellCooldownCounter = CoolDown;
				_buyCooldownCounter = 0;
			}
			else
			{
				_buyCooldownCounter++;
			}
		}
	}
}
