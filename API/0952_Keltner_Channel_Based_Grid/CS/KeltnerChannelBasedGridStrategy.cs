using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Keltner Channel based grid rebalancing strategy.
/// </summary>
public class KeltnerChannelBasedGridStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _gridCoefficient;
	private readonly StrategyParam<int> _numGrids;
	private readonly StrategyParam<bool> _useExponential;
	private readonly StrategyParam<DataType> _candleType;

	public int Length { get => _length.Value; set => _length.Value = value; }
	public decimal GridCoefficient { get => _gridCoefficient.Value; set => _gridCoefficient.Value = value; }
	public int NumGrids { get => _numGrids.Value; set => _numGrids.Value = value; }
	public bool UseExponential { get => _useExponential.Value; set => _useExponential.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public KeltnerChannelBasedGridStrategy()
	{
		_length = Param(nameof(Length), 10)
		.SetGreaterThanZero()
		.SetDisplay("Length", "MA and ATR period", "Keltner")
		.SetCanOptimize(true)
		.SetOptimize(5, 20, 5);

		_gridCoefficient = Param(nameof(GridCoefficient), 1.33m)
		.SetGreaterThanZero()
		.SetDisplay("Grid coeff", "Multiplier for channel width", "Keltner")
		.SetCanOptimize(true)
		.SetOptimize(1m, 2m, 0.1m);

		_numGrids = Param(nameof(NumGrids), 12)
		.SetGreaterThanZero()
		.SetDisplay("Grids", "Number of grid levels", "Strategy")
		.SetCanOptimize(true)
		.SetOptimize(5, 20, 1);

		_useExponential = Param(nameof(UseExponential), true)
		.SetDisplay("Use EMA", "Use EMA instead of SMA", "Keltner");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle type", "Type of candles", "General");
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

		IIndicator ma = UseExponential ? new ExponentialMovingAverage { Length = Length } : new SimpleMovingAverage { Length = Length };
		var atr = new AverageTrueRange { Length = Length };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ma, atr, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ma);
			DrawIndicator(area, atr);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal ma, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (atrValue == 0)
		return;

		var bandWidth = atrValue * GridCoefficient;
		var kcRate = (candle.ClosePrice - ma) / bandWidth;
		var maxAmount = Portfolio?.CurrentValue / candle.ClosePrice ?? 0m;
		var targetPosition = kcRate * maxAmount * -1m;

		if (targetPosition > maxAmount)
		targetPosition = maxAmount;
		else if (targetPosition < -maxAmount)
		targetPosition = -maxAmount;

		var diff = Math.Abs(targetPosition - Position);
		if (diff >= maxAmount / NumGrids)
		{
			if (targetPosition > Position)
			BuyMarket(targetPosition - Position);
			else if (targetPosition < Position)
			SellMarket(Position - targetPosition);
		}
	}
}
