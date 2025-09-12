using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trend-following strategy using EMA cross and ATR-based trailing stop.
/// </summary>
public class ImprovedEmaCdcTrailingStopStrategy : Strategy
{
	private readonly StrategyParam<int> _ema60Period;
	private readonly StrategyParam<int> _ema90Period;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<decimal> _profitTargetMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// EMA 60 period.
	/// </summary>
	public int Ema60Period { get => _ema60Period.Value; set => _ema60Period.Value = value; }

	/// <summary>
	/// EMA 90 period.
	/// </summary>
	public int Ema90Period { get => _ema90Period.Value; set => _ema90Period.Value = value; }

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }

	/// <summary>
	/// ATR multiplier for trailing stop.
	/// </summary>
	public decimal Multiplier { get => _multiplier.Value; set => _multiplier.Value = value; }

	/// <summary>
	/// ATR multiplier for profit target.
	/// </summary>
	public decimal ProfitTargetMultiplier { get => _profitTargetMultiplier.Value; set => _profitTargetMultiplier.Value = value; }

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of <see cref="ImprovedEmaCdcTrailingStopStrategy"/>.
	/// </summary>
	public ImprovedEmaCdcTrailingStopStrategy()
	{
		_ema60Period = Param(nameof(Ema60Period), 60)
			.SetGreaterThanZero()
			.SetDisplay("EMA 60 Period", "Length of the fast EMA", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(20, 100, 10);

		_ema90Period = Param(nameof(Ema90Period), 90)
			.SetGreaterThanZero()
			.SetDisplay("EMA 90 Period", "Length of the slow EMA", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(30, 120, 10);

		_atrPeriod = Param(nameof(AtrPeriod), 24)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "Period for ATR calculation", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(14, 50, 2);

		_multiplier = Param(nameof(Multiplier), 4m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "Multiplier for trailing stop", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 1m);

		_profitTargetMultiplier = Param(nameof(ProfitTargetMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Profit Target Multiplier", "ATR multiplier for take profit", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 1m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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

		var ema60 = new EMA { Length = Ema60Period };
		var ema90 = new EMA { Length = Ema90Period };
		var atr = new AverageTrueRange { Length = AtrPeriod };
		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			ShortPeriod = 12,
			LongPeriod = 26,
			SignalPeriod = 9
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(macd, ema60, ema90, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema60);
			DrawIndicator(area, ema90);
			DrawOwnTrades(area);

			var macdArea = CreateChartArea();
			DrawIndicator(macdArea, macd);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal macd, decimal signal, decimal histogram, decimal ema60, decimal ema90, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var longStop = candle.ClosePrice - Multiplier * atr;
		var shortStop = candle.ClosePrice + Multiplier * atr;
		var longProfitTarget = candle.ClosePrice + ProfitTargetMultiplier * atr;
		var shortProfitTarget = candle.ClosePrice - ProfitTargetMultiplier * atr;

		var longCondition = candle.ClosePrice > ema60 && ema60 > ema90 && macd > signal && candle.ClosePrice > longStop;
		var shortCondition = candle.ClosePrice < ema60 && ema60 < ema90 && macd < signal && candle.ClosePrice < shortStop;

		if (longCondition && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (shortCondition && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));

		if (Position > 0 && (candle.ClosePrice >= longProfitTarget || candle.ClosePrice < longStop))
			SellMarket(Math.Abs(Position));
		else if (Position < 0 && (candle.ClosePrice <= shortProfitTarget || candle.ClosePrice > shortStop))
			BuyMarket(Math.Abs(Position));
	}
}
