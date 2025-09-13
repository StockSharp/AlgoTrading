using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// PresentTrend RMI Synergy strategy combines an RSI momentum filter with an ATR-based trailing stop.
/// </summary>
public class PresentTrendRmiSynergyStrategy : Strategy
{
	private readonly StrategyParam<int> _rmiPeriod;
	private readonly StrategyParam<int> _superTrendLength;
	private readonly StrategyParam<decimal> _superTrendMultiplier;
private readonly StrategyParam<Sides?> _direction;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _stopPrice;

	/// <summary>
	/// Period for RMI calculation (default: 21)
	/// </summary>
	public int RmiPeriod
	{
		get => _rmiPeriod.Value;
		set => _rmiPeriod.Value = value;
	}

	/// <summary>
	/// Length for trend moving average and ATR (default: 5)
	/// </summary>
	public int SuperTrendLength
	{
		get => _superTrendLength.Value;
		set => _superTrendLength.Value = value;
	}

	/// <summary>
	/// ATR multiplier for trailing stop (default: 4.0)
	/// </summary>
	public decimal SuperTrendMultiplier
	{
		get => _superTrendMultiplier.Value;
		set => _superTrendMultiplier.Value = value;
	}

	/// <summary>
	/// Allowed trading direction
	/// </summary>
public Sides? Direction
{
get => _direction.Value;
set => _direction.Value = value;
}

	/// <summary>
	/// Type of candles used for strategy calculation
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize the PresentTrend RMI Synergy strategy
	/// </summary>
	public PresentTrendRmiSynergyStrategy()
	{
		_rmiPeriod = Param(nameof(RmiPeriod), 21)
			.SetDisplay("RMI Length", "Period for RMI calculation", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(10, 30, 5)
			.SetGreaterThanZero();

		_superTrendLength = Param(nameof(SuperTrendLength), 5)
			.SetDisplay("SuperTrend Length", "Length for trend MA and ATR", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(3, 14, 1)
			.SetGreaterThanZero();

		_superTrendMultiplier = Param(nameof(SuperTrendMultiplier), 4m)
			.SetDisplay("SuperTrend Multiplier", "ATR multiplier for trailing stop", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(2m, 6m, 0.5m)
			.SetGreaterThanZero();

_direction = Param(nameof(Direction), (Sides?)null)
.SetDisplay("Trade Direction", "Allowed trading direction", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "Data");
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
		_stopPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		StartProtection();

		var rsi = new RelativeStrengthIndex { Length = RmiPeriod };
		var atr = new AverageTrueRange { Length = SuperTrendLength };
		var sma = new SimpleMovingAverage { Length = SuperTrendLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, atr, sma, ProcessCandle)
			.Start();

		var priceArea = CreateChartArea();
		var rsiArea = CreateChartArea();
		if (priceArea != null)
		{
			DrawCandles(priceArea, subscription);
			DrawIndicator(priceArea, sma);
			DrawOwnTrades(priceArea);
		}

		if (rsiArea != null)
			DrawIndicator(rsiArea, rsi);
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue, decimal atrValue, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var trendDir = candle.ClosePrice > smaValue ? 1 : -1;
		var upperBand = smaValue + SuperTrendMultiplier * atrValue;
		var lowerBand = smaValue - SuperTrendMultiplier * atrValue;

		if (Position == 0)
		{
if ((Direction is null or Sides.Buy) && rsiValue > 60m && trendDir == 1)
			{
				BuyMarket(Volume);
				_stopPrice = lowerBand;
			}
else if ((Direction is null or Sides.Sell) && rsiValue < 40m && trendDir == -1)
			{
				SellMarket(Volume);
				_stopPrice = upperBand;
			}
		}
		else if (Position > 0)
		{
			if (trendDir == 1)
				_stopPrice = lowerBand;

			if (_stopPrice is decimal stop && candle.LowPrice <= stop)
			{
				SellMarket(Position);
				_stopPrice = null;
			}
		}
		else
		{
			if (trendDir == -1)
				_stopPrice = upperBand;

			if (_stopPrice is decimal stop && candle.HighPrice >= stop)
			{
				BuyMarket(Math.Abs(Position));
				_stopPrice = null;
			}
		}
	}
}

