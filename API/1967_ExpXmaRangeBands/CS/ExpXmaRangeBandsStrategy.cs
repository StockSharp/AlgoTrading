using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy converted from the MetaTrader example "Exp_XMA_Range_Bands".
/// Uses an EMA and ATR to build dynamic bands and trades when price re-enters the channel.
/// </summary>
public class ExpXmaRangeBandsStrategy : Strategy
{
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<int> _rangeLength;
	private readonly StrategyParam<decimal> _deviation;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevUpper;
	private decimal _prevLower;
	private decimal _prevClose;
	private decimal _entryPrice;
	private bool _isFirst = true;

	/// <summary>
	/// EMA period for the channel center.
	/// </summary>
	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	/// <summary>
	/// ATR period used for channel width.
	/// </summary>
	public int RangeLength
	{
		get => _rangeLength.Value;
		set => _rangeLength.Value = value;
	}

	/// <summary>
	/// ATR multiplier for band width.
	/// </summary>
	public decimal Deviation
	{
		get => _deviation.Value;
		set => _deviation.Value = value;
	}

	/// <summary>
	/// Stop loss in points.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take profit in points.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ExpXmaRangeBandsStrategy"/>.
	/// </summary>
	public ExpXmaRangeBandsStrategy()
	{
		_maLength = Param(nameof(MaLength), 100)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "EMA period for channel center", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(50, 200, 10);

		_rangeLength = Param(nameof(RangeLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR period for channel width", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(10, 60, 5);

		_deviation = Param(nameof(Deviation), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Deviation", "ATR multiplier for channel width", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 0.5m);

		_stopLoss = Param(nameof(StopLoss), 1000m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop loss in points", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(500m, 2000m, 500m);

		_takeProfit = Param(nameof(TakeProfit), 2000m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take profit in points", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(1000m, 4000m, 500m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
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
		_prevUpper = 0;
		_prevLower = 0;
		_prevClose = 0;
		_entryPrice = 0;
		_isFirst = true;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Create indicators
		var ema = new ExponentialMovingAverage { Length = MaLength };
		var atr = new AverageTrueRange { Length = RangeLength };

		// Subscribe to candles and bind indicators
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, atr, ProcessCandle)
			.Start();

		// Setup chart visualization if available
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawIndicator(area, atr);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue, decimal atrValue)
	{
		// Skip unfinished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Ensure strategy is ready to trade
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Calculate channel borders
		var upper = emaValue + atrValue * Deviation;
		var lower = emaValue - atrValue * Deviation;

		// Initialize previous values on first run
		if (_isFirst)
		{
			_prevUpper = upper;
			_prevLower = lower;
			_prevClose = candle.ClosePrice;
			_isFirst = false;
			return;
		}

		// Check signals based on previous candle crossing bands
		if (_prevClose > _prevUpper)
		{
			// Close existing short position
			if (Position < 0)
				BuyMarket(Math.Abs(Position));

			// Open long when price returns inside the channel
			if (candle.ClosePrice <= upper && Position <= 0)
			{
				var volume = Volume + Math.Abs(Position);
				BuyMarket(volume);
				_entryPrice = candle.ClosePrice;
			}
		}
		else if (_prevClose < _prevLower)
		{
			// Close existing long position
			if (Position > 0)
				SellMarket(Position);

			// Open short when price returns inside the channel
			if (candle.ClosePrice >= lower && Position >= 0)
			{
				var volume = Volume + Math.Abs(Position);
				SellMarket(volume);
				_entryPrice = candle.ClosePrice;
			}
		}

		// Check stop-loss and take-profit
		var step = Security.PriceStep ?? 1m;
		var sl = step * StopLoss;
		var tp = step * TakeProfit;

		if (Position > 0)
		{
			if (candle.ClosePrice <= _entryPrice - sl || candle.ClosePrice >= _entryPrice + tp)
			{
				SellMarket(Position);
				_entryPrice = 0;
			}
		}
		else if (Position < 0)
		{
			if (candle.ClosePrice >= _entryPrice + sl || candle.ClosePrice <= _entryPrice - tp)
			{
				BuyMarket(Math.Abs(Position));
				_entryPrice = 0;
			}
		}

		// Update previous values
		_prevUpper = upper;
		_prevLower = lower;
		_prevClose = candle.ClosePrice;
	}
}
