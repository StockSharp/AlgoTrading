using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Parabolic SAR expert advisor with ADX confirmation and trailing stop.
/// </summary>
public class GenieStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _trailingStop;
	private readonly StrategyParam<decimal> _sarStep;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Take profit distance in price units.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in price units.
	/// </summary>
	public decimal TrailingStop
	{
		get => _trailingStop.Value;
		set => _trailingStop.Value = value;
	}

	/// <summary>
	/// Parabolic SAR acceleration factor.
	/// </summary>
	public decimal SarStep
	{
		get => _sarStep.Value;
		set => _sarStep.Value = value;
	}

	/// <summary>
	/// ADX calculation period.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// Type of candles used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="GenieStrategy"/>.
	/// </summary>
	public GenieStrategy()
	{
		_takeProfit = Param(nameof(TakeProfit), 500m)
			.SetDisplay("Take Profit", "Take profit distance", "Protection");

		_trailingStop = Param(nameof(TrailingStop), 200m)
			.SetDisplay("Trailing Stop", "Trailing stop distance", "Protection");

		_sarStep = Param(nameof(SarStep), 0.02m)
			.SetDisplay("SAR Step", "Acceleration factor", "Indicator");

		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetDisplay("ADX Period", "Period for ADX", "Indicator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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

		var adx = new AverageDirectionalIndex { Length = AdxPeriod };
		var sar = new ParabolicSar { AccelerationStep = SarStep, AccelerationMax = 0.2m };

		var subscription = SubscribeCandles(CandleType);

		var prevSar = 0m;
		var prevPdi = 0m;
		var prevMdi = 0m;
		ICandleMessage? prevCandle = null;
		var isFirst = true;

		subscription.BindEx(adx, sar, (candle, adxValue, sarValue) =>
		{
			if (candle.State != CandleStates.Finished)
				return;

			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			var adxTyped = (AverageDirectionalIndexValue)adxValue;

			if (adxTyped.MovingAverage is not decimal adxMain ||
				adxTyped.Dx.Plus is not decimal pdi ||
				adxTyped.Dx.Minus is not decimal mdi)
				return;

			var sarCurrent = sarValue.ToDecimal();

			if (isFirst)
			{
				prevSar = sarCurrent;
				prevPdi = pdi;
				prevMdi = mdi;
				prevCandle = candle;
				isFirst = false;
				return;
			}

			if (Position == 0)
			{
				var sellCondition = prevCandle != null &&
					prevSar < prevCandle.ClosePrice &&
					sarCurrent > candle.ClosePrice &&
					prevPdi > prevMdi &&
					pdi < mdi &&
					adxMain > pdi &&
					adxMain > mdi;

				var buyCondition = prevCandle != null &&
					prevSar > prevCandle.ClosePrice &&
					sarCurrent < candle.ClosePrice &&
					prevPdi < prevMdi &&
					pdi > mdi &&
					adxMain > pdi &&
					adxMain > mdi;

				if (sellCondition)
				{
					SellMarket(Volume + Math.Abs(Position));
				}
				else if (buyCondition)
				{
					BuyMarket(Volume + Math.Abs(Position));
				}
			}
			else
			{
				if (prevCandle != null)
				{
					if (Position > 0 && prevCandle.OpenPrice > prevCandle.ClosePrice)
					{
						SellMarket(Math.Abs(Position));
					}
					else if (Position < 0 && prevCandle.OpenPrice < prevCandle.ClosePrice)
					{
						BuyMarket(Math.Abs(Position));
					}
				}
			}

			prevSar = sarCurrent;
			prevPdi = pdi;
			prevMdi = mdi;
			prevCandle = candle;
		}).Start();

		StartProtection(
			takeProfit: new Unit(TakeProfit, UnitTypes.Absolute),
			stopLoss: new Unit(TrailingStop, UnitTypes.Absolute),
			isStopTrailing: true,
			useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, adx);
			DrawIndicator(area, sar);
			DrawOwnTrades(area);
		}
	}
}
