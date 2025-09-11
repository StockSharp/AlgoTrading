using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Larry Connors RSI 3 strategy.
/// Combines a long-term trend filter with short-term RSI exhaustion.
/// </summary>
public class LarryConnorsRsi3Strategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _smaPeriod;
	private readonly StrategyParam<decimal> _dropTrigger;
	private readonly StrategyParam<decimal> _oversoldLevel;
	private readonly StrategyParam<decimal> _overboughtLevel;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _rsiPrev1;
	private decimal _rsiPrev2;

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// SMA period.
	/// </summary>
	public int SmaPeriod
	{
		get => _smaPeriod.Value;
		set => _smaPeriod.Value = value;
	}

	/// <summary>
	/// RSI trigger level for drop start.
	/// </summary>
	public decimal DropTrigger
	{
		get => _dropTrigger.Value;
		set => _dropTrigger.Value = value;
	}

	/// <summary>
	/// RSI oversold level.
	/// </summary>
	public decimal OversoldLevel
	{
		get => _oversoldLevel.Value;
		set => _oversoldLevel.Value = value;
	}

	/// <summary>
	/// RSI overbought level.
	/// </summary>
	public decimal OverboughtLevel
	{
		get => _overboughtLevel.Value;
		set => _overboughtLevel.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="LarryConnorsRsi3Strategy"/>.
	/// </summary>
	public LarryConnorsRsi3Strategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 2)
			.SetDisplay("RSI Period", "Period for RSI calculation", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(2, 5, 1);

		_smaPeriod = Param(nameof(SmaPeriod), 200)
			.SetDisplay("SMA Period", "Period for trend SMA", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(50, 300, 50);

		_dropTrigger = Param(nameof(DropTrigger), 60m)
			.SetDisplay("Drop Trigger", "RSI level required before drop", "Strategy")
			.SetCanOptimize(true)
			.SetOptimize(50m, 70m, 5m);

		_oversoldLevel = Param(nameof(OversoldLevel), 10m)
			.SetDisplay("Oversold Level", "RSI oversold threshold", "Strategy")
			.SetCanOptimize(true)
			.SetOptimize(5m, 20m, 1m);

		_overboughtLevel = Param(nameof(OverboughtLevel), 70m)
			.SetDisplay("Overbought Level", "RSI exit threshold", "Strategy")
			.SetCanOptimize(true)
			.SetOptimize(60m, 80m, 5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
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
		_rsiPrev1 = 0m;
		_rsiPrev2 = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var sma = new SimpleMovingAverage { Length = SmaPeriod };
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawIndicator(area, rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var hasHistory = _rsiPrev1 != 0m && _rsiPrev2 != 0m;
		var condition1 = candle.ClosePrice > smaValue;
		var condition2 = hasHistory && _rsiPrev2 > _rsiPrev1 && _rsiPrev1 > rsiValue && _rsiPrev2 > DropTrigger;
		var condition3 = rsiValue < OversoldLevel;

		if (condition1 && condition2 && condition3 && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
		}
		else if (rsiValue > OverboughtLevel && Position > 0)
		{
			SellMarket(Position);
		}

		_rsiPrev2 = _rsiPrev1;
		_rsiPrev1 = rsiValue;
	}
}
