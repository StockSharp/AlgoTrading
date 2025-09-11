using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Moving Average strategy with logistic or percent-based exits.
/// </summary>
public class MaWithLogisticStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<MaTypeEnum> _maType;
	private readonly StrategyParam<ExitTypeEnum> _exitType;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _logisticSlope;
	private readonly StrategyParam<decimal> _logisticMidpoint;
	private readonly StrategyParam<decimal> _takeProfitProbability;
	private readonly StrategyParam<decimal> _stopLossProbability;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Fast moving average length.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Slow moving average length.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// Moving average type.
	/// </summary>
	public MaTypeEnum MaType
	{
		get => _maType.Value;
		set => _maType.Value = value;
	}

	/// <summary>
	/// Exit mode.
	/// </summary>
	public ExitTypeEnum ExitType
	{
		get => _exitType.Value;
		set => _exitType.Value = value;
	}

	/// <summary>
	/// Take profit percent.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Stop loss percent.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Logistic slope.
	/// </summary>
	public decimal LogisticSlope
	{
		get => _logisticSlope.Value;
		set => _logisticSlope.Value = value;
	}

	/// <summary>
	/// Logistic midpoint.
	/// </summary>
	public decimal LogisticMidpoint
	{
		get => _logisticMidpoint.Value;
		set => _logisticMidpoint.Value = value;
	}

	/// <summary>
	/// Take profit probability threshold.
	/// </summary>
	public decimal TakeProfitProbability
	{
		get => _takeProfitProbability.Value;
		set => _takeProfitProbability.Value = value;
	}

	/// <summary>
	/// Stop loss probability threshold.
	/// </summary>
	public decimal StopLossProbability
	{
		get => _stopLossProbability.Value;
		set => _stopLossProbability.Value = value;
	}

	/// <summary>
	/// Type of candles for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MaWithLogisticStrategy"/>.
	/// </summary>
	public MaWithLogisticStrategy()
	{
		_fastLength =
			Param(nameof(FastLength), 9).SetDisplay("Fast MA", "Fast MA period", "Indicators").SetGreaterThanZero();
		_slowLength =
			Param(nameof(SlowLength), 21).SetDisplay("Slow MA", "Slow MA period", "Indicators").SetGreaterThanZero();
		_maType = Param(nameof(MaType), MaTypeEnum.EMA).SetDisplay("MA Type", "Moving average type", "Indicators");
		_exitType = Param(nameof(ExitType), ExitTypeEnum.Percent).SetDisplay("Exit Type", "Exit method", "General");
		_takeProfitPercent = Param(nameof(TakeProfitPercent), 20m)
								 .SetDisplay("TP %", "Take profit percent", "Percent")
								 .SetGreaterThanZero();
		_stopLossPercent =
			Param(nameof(StopLossPercent), 5m).SetDisplay("SL %", "Stop loss percent", "Percent").SetGreaterThanZero();
		_logisticSlope = Param(nameof(LogisticSlope), 10m)
							 .SetDisplay("Logistic k", "Logistic slope k", "Logistic")
							 .SetGreaterThanZero();
		_logisticMidpoint =
			Param(nameof(LogisticMidpoint), 0m).SetDisplay("Logistic mid", "Logistic mid (profit %)", "Logistic");
		_takeProfitProbability =
			Param(nameof(TakeProfitProbability), 0.8m).SetDisplay("TP Prob", "TP probability threshold", "Logistic");
		_stopLossProbability =
			Param(nameof(StopLossProbability), 0.2m).SetDisplay("SL Prob", "SL probability threshold", "Logistic");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
						  .SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var fastMa = CreateMa(MaType, FastLength);
		var slowMa = CreateMa(MaType, SlowLength);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(fastMa, slowMa, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastMa);
			DrawIndicator(area, slowMa);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;
		var longCond = close > fast && fast > slow;
		var shortCond = close < fast && fast < slow;

		if (longCond && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (shortCond && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));

		if (Position > 0)
		{
			if (ExitType == ExitTypeEnum.Percent)
			{
				var tpPrice = PositionAvgPrice * (1m + TakeProfitPercent / 100m);
				var slPrice = PositionAvgPrice * (1m - StopLossPercent / 100m);

				if (close >= tpPrice || close <= slPrice)
					SellMarket(Math.Abs(Position));
			}
			else if (ExitType == ExitTypeEnum.Logistic)
			{
				var profitPct = (close - PositionAvgPrice) / PositionAvgPrice;
				var prob = 1m / (1m + (decimal)Math.Exp((double)(-LogisticSlope * (profitPct - LogisticMidpoint))));
				if (prob >= TakeProfitProbability || prob <= StopLossProbability)
					SellMarket(Math.Abs(Position));
			}
		}
		else if (Position < 0)
		{
			if (ExitType == ExitTypeEnum.Percent)
			{
				var tpPrice = PositionAvgPrice * (1m - TakeProfitPercent / 100m);
				var slPrice = PositionAvgPrice * (1m + StopLossPercent / 100m);

				if (close <= tpPrice || close >= slPrice)
					BuyMarket(Math.Abs(Position));
			}
			else if (ExitType == ExitTypeEnum.Logistic)
			{
				var profitPct = (PositionAvgPrice - close) / PositionAvgPrice;
				var prob = 1m / (1m + (decimal)Math.Exp((double)(-LogisticSlope * (profitPct - LogisticMidpoint))));
				if (prob >= TakeProfitProbability || prob <= StopLossProbability)
					BuyMarket(Math.Abs(Position));
			}
		}
	}

	private MovingAverage CreateMa(MaTypeEnum type, int length)
	{
		return type switch {
			MaTypeEnum.EMA => new ExponentialMovingAverage { Length = length },
			MaTypeEnum.WMA => new WeightedMovingAverage { Length = length },
			_ => new SimpleMovingAverage { Length = length },
		};
	}

	/// <summary>
	/// Moving average types.
	/// </summary>
	public enum MaTypeEnum
	{
		EMA,
		SMA,
		WMA
	}

	/// <summary>
	/// Exit modes.
	/// </summary>
	public enum ExitTypeEnum
	{
		None,
		Percent,
		Logistic
	}
}
