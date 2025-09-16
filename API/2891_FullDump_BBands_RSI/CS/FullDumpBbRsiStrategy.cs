using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// FullDump strategy combining Bollinger Bands and RSI signals with adaptive stops.
/// Replicates the MT5 expert logic with multi-step confirmation and break-even protection.
/// </summary>
public class FullDumpBbRsiStrategy : Strategy
{
	private readonly StrategyParam<int> _bandsPeriod;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _depth;
	private readonly StrategyParam<decimal> _indentInPoints;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<DataType> _candleType;

	private BollingerBands _bollingerBands;
	private RelativeStrengthIndex _rsi;

	private readonly Queue<decimal> _rsiValues = new();
	private readonly Queue<decimal> _upperBandValues = new();
	private readonly Queue<decimal> _lowerBandValues = new();
	private readonly Queue<decimal> _highValues = new();
	private readonly Queue<decimal> _lowValues = new();

	private decimal? _longStop;
	private decimal? _longTake;
	private decimal? _longEntry;
	private decimal? _shortStop;
	private decimal? _shortTake;
	private decimal? _shortEntry;

	/// <summary>
	/// Bollinger Bands period.
	/// </summary>
	public int BandsPeriod
	{
		get => _bandsPeriod.Value;
		set => _bandsPeriod.Value = value;
	}

	/// <summary>
	/// RSI averaging period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Number of recent candles to inspect.
	/// </summary>
	public int Depth
	{
		get => _depth.Value;
		set => _depth.Value = value;
	}

	/// <summary>
	/// Offset in price steps added to stops and targets.
	/// </summary>
	public decimal IndentInPoints
	{
		get => _indentInPoints.Value;
		set => _indentInPoints.Value = value;
	}

	/// <summary>
	/// Order volume in lots.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Candle type to subscribe.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="FullDumpBbRsiStrategy"/>.
	/// </summary>
	public FullDumpBbRsiStrategy()
	{
		_bandsPeriod = Param(nameof(BandsPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Period", "Length for Bollinger Bands", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 40, 1);

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "Averaging length for RSI", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(7, 21, 1);

		_depth = Param(nameof(Depth), 6)
			.SetGreaterThanZero()
			.SetDisplay("Depth", "Number of candles for checks", "Logic")
			.SetCanOptimize(true)
			.SetOptimize(3, 12, 1);

		_indentInPoints = Param(nameof(IndentInPoints), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Indent", "Additional offset in price steps", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(5m, 30m, 5m);

		_orderVolume = Param(nameof(OrderVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume in lots", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe", "General");
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

		_bollingerBands = new BollingerBands
		{
			Length = BandsPeriod,
			Width = 2m
		};

		_rsi = new RelativeStrengthIndex
		{
			Length = RsiPeriod
		};

		ResetTradeLevels();

		Volume = OrderVolume;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_bollingerBands, _rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal middleBand, decimal upperBand, decimal lowerBand, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateQueue(_rsiValues, rsiValue);
		UpdateQueue(_upperBandValues, upperBand);
		UpdateQueue(_lowerBandValues, lowerBand);
		UpdateQueue(_highValues, candle.HighPrice);
		UpdateQueue(_lowValues, candle.LowPrice);

		// Manage open trades before searching for a new setup.
		ManageOpenPositions(candle, upperBand, lowerBand);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var price = candle.ClosePrice;

		// Step 1: detect oversold/overbought RSI prints within the lookback window.
		var buyStep1 = HasValueBelow(_rsiValues, 30m);
		var sellStep1 = HasValueAbove(_rsiValues, 70m);

		// Step 2: check if the current close breaches any recent band value.
		var buyStep2 = HasPriceCrossedBand(price, _lowerBandValues, true);
		var sellStep2 = HasPriceCrossedBand(price, _upperBandValues, false);

		// Step 3: confirm price has rejoined the middle band from the proper side.
		var buyStep3 = price >= middleBand;
		var sellStep3 = price <= middleBand;

		if (buyStep1 && buyStep2 && buyStep3 && Position <= 0)
		{
			var lowest = GetExtreme(_lowValues, true);
			if (lowest != null)
			{
				var indent = GetIndentOffset();

				_longStop = lowest.Value - indent;
				_longTake = upperBand + indent;
				_longEntry = price;
				ResetShortLevels();

				BuyMarket(OrderVolume + Math.Abs(Position));
			}
		}
		else if (sellStep1 && sellStep2 && sellStep3 && Position >= 0)
		{
			var highest = GetExtreme(_highValues, false);
			if (highest != null)
			{
				var indent = GetIndentOffset();

				_shortStop = highest.Value + indent;
				_shortTake = lowerBand - indent;
				_shortEntry = price;
				ResetLongLevels();

				SellMarket(OrderVolume + Math.Abs(Position));
			}
		}
	}

	private void ManageOpenPositions(ICandleMessage candle, decimal upperBand, decimal lowerBand)
	{
		if (Position > 0)
		{
			if (_longStop != null && candle.LowPrice <= _longStop.Value)
			{
				SellMarket(Position);
				ResetLongLevels();
			}
			else if (_longTake != null && candle.HighPrice >= _longTake.Value)
			{
				SellMarket(Position);
				ResetLongLevels();
			}
			else if (_longEntry != null && (_longStop == null || _longStop.Value != _longEntry.Value) && candle.ClosePrice >= upperBand)
			{
				// Move the protective stop to break-even after the opposite band is touched.
				_longStop = _longEntry;
			}
		}
		else if (Position < 0)
		{
			if (_shortStop != null && candle.HighPrice >= _shortStop.Value)
			{
				BuyMarket(-Position);
				ResetShortLevels();
			}
			else if (_shortTake != null && candle.LowPrice <= _shortTake.Value)
			{
				BuyMarket(-Position);
				ResetShortLevels();
			}
			else if (_shortEntry != null && (_shortStop == null || _shortStop.Value != _shortEntry.Value) && candle.ClosePrice <= lowerBand)
			{
				// Symmetric break-even logic for short trades.
				_shortStop = _shortEntry;
			}
		}
		else
		{
			ResetTradeLevels();
		}
	}

	private void UpdateQueue(Queue<decimal> queue, decimal value)
	{
		queue.Enqueue(value);

		var depth = Depth;
		while (queue.Count > depth)
		{
			queue.Dequeue();
		}
	}

	private static bool HasValueBelow(IEnumerable<decimal> values, decimal threshold)
	{
		foreach (var value in values)
		{
			if (value < threshold)
				return true;
		}

		return false;
	}

	private static bool HasValueAbove(IEnumerable<decimal> values, decimal threshold)
	{
		foreach (var value in values)
		{
			if (value > threshold)
				return true;
		}

		return false;
	}

	private static bool HasPriceCrossedBand(decimal price, IEnumerable<decimal> bandValues, bool isLower)
	{
		foreach (var band in bandValues)
		{
			if (isLower)
			{
				if (price <= band)
					return true;
			}
			else
			{
				if (price >= band)
					return true;
			}
		}

		return false;
	}

	private static decimal? GetExtreme(IEnumerable<decimal> values, bool isLowest)
	{
		decimal? result = null;

		foreach (var value in values)
		{
			if (result == null)
			{
				result = value;
				continue;
			}

			if (isLowest)
			{
				if (value < result)
					result = value;
			}
			else if (value > result)
			{
				result = value;
			}
		}

		return result;
	}

	private decimal GetIndentOffset()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0)
			return 0m;

		return IndentInPoints * step;
	}

	private void ResetTradeLevels()
	{
		ResetLongLevels();
		ResetShortLevels();
	}

	private void ResetLongLevels()
	{
		_longStop = null;
		_longTake = null;
		_longEntry = null;
	}

	private void ResetShortLevels()
	{
		_shortStop = null;
		_shortTake = null;
		_shortEntry = null;
	}
}
