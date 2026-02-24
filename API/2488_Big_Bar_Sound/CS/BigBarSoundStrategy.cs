using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades when a candle exceeds a configurable size threshold.
/// Based on the BigBarSound MetaTrader EA concept - trades in the direction of
/// large candles with ATR-based stop-loss and take-profit.
/// </summary>
public class BigBarSoundStrategy : Strategy
{
	/// <summary>
	/// Defines how the candle size is calculated.
	/// </summary>
	public enum BigBarDifferenceModes
	{
		/// <summary>
		/// Measure the difference between close and open prices.
		/// </summary>
		OpenClose,

		/// <summary>
		/// Measure the distance between the high and low of the candle.
		/// </summary>
		HighLow,
	}

	private readonly StrategyParam<int> _barPoint;
	private readonly StrategyParam<BigBarDifferenceModes> _differenceMode;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrStopMultiplier;
	private readonly StrategyParam<decimal> _atrTpMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _stopPrice;
	private decimal _takeProfitPrice;
	private int _direction;

	/// <summary>
	/// Number of price steps required to trigger the alert.
	/// </summary>
	public int BarPoint
	{
		get => _barPoint.Value;
		set => _barPoint.Value = value;
	}

	/// <summary>
	/// Defines how the candle size is calculated.
	/// </summary>
	public BigBarDifferenceModes DifferenceMode
	{
		get => _differenceMode.Value;
		set => _differenceMode.Value = value;
	}

	/// <summary>
	/// ATR period for stop/take-profit calculations.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier for stop-loss distance.
	/// </summary>
	public decimal AtrStopMultiplier
	{
		get => _atrStopMultiplier.Value;
		set => _atrStopMultiplier.Value = value;
	}

	/// <summary>
	/// ATR multiplier for take-profit distance.
	/// </summary>
	public decimal AtrTpMultiplier
	{
		get => _atrTpMultiplier.Value;
		set => _atrTpMultiplier.Value = value;
	}

	/// <summary>
	/// Candle type used to monitor the market.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="BigBarSoundStrategy"/> class.
	/// </summary>
	public BigBarSoundStrategy()
	{
		_barPoint = Param(nameof(BarPoint), 100)
			.SetGreaterThanZero()
			.SetDisplay("Point Threshold", "Number of price steps required to trigger entry", "General")
			.SetOptimize(50, 500, 50);

		_differenceMode = Param(nameof(DifferenceMode), BigBarDifferenceModes.HighLow)
			.SetDisplay("Difference Mode", "How the candle size is calculated", "General");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "Period for ATR calculation", "Indicators")
			.SetOptimize(7, 28, 7);

		_atrStopMultiplier = Param(nameof(AtrStopMultiplier), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Stop Mult", "ATR multiplier for stop-loss", "Risk")
			.SetOptimize(1m, 3m, 0.5m);

		_atrTpMultiplier = Param(nameof(AtrTpMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("ATR TP Mult", "ATR multiplier for take-profit", "Risk")
			.SetOptimize(1m, 4m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to monitor", "Data");
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
		_stopPrice = 0m;
		_takeProfitPrice = 0m;
		_direction = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, atr);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Manage existing position
		if (Position > 0 && _direction > 0)
		{
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takeProfitPrice)
			{
				SellMarket(Position);
				_direction = 0;
				_stopPrice = 0m;
				_takeProfitPrice = 0m;
			}
		}
		else if (Position < 0 && _direction < 0)
		{
			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takeProfitPrice)
			{
				BuyMarket(Math.Abs(Position));
				_direction = 0;
				_stopPrice = 0m;
				_takeProfitPrice = 0m;
			}
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position != 0)
			return;

		if (atrValue <= 0m)
			return;

		// Calculate candle size
		var difference = DifferenceMode == BigBarDifferenceModes.OpenClose
			? Math.Abs(candle.ClosePrice - candle.OpenPrice)
			: candle.HighPrice - candle.LowPrice;

		var priceStep = Security?.PriceStep;
		var step = priceStep is null or <= 0m ? 1m : priceStep.Value;
		var threshold = step * BarPoint;

		if (difference < threshold)
			return;

		var isBullish = candle.ClosePrice > candle.OpenPrice;
		var stopDist = atrValue * AtrStopMultiplier;
		var tpDist = atrValue * AtrTpMultiplier;

		if (isBullish)
		{
			BuyMarket(Volume);
			_direction = 1;
			_stopPrice = candle.ClosePrice - stopDist;
			_takeProfitPrice = candle.ClosePrice + tpDist;
		}
		else
		{
			SellMarket(Volume);
			_direction = -1;
			_stopPrice = candle.ClosePrice + stopDist;
			_takeProfitPrice = candle.ClosePrice - tpDist;
		}
	}
}
