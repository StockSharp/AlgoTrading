using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Martingale strategy based on dual MACD indicators.
/// </summary>
public class MartingaleMacdStrategy : Strategy {
	private readonly StrategyParam<decimal> _shape;
	private readonly StrategyParam<int> _doublingCount;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<int> _macd1Fast;
	private readonly StrategyParam<int> _macd1Slow;
	private readonly StrategyParam<int> _macd2Fast;
	private readonly StrategyParam<int> _macd2Slow;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _macd1Prev1;
	private decimal? _macd1Prev2;
	private decimal? _macd2Prev;
	private int _lossStreak;
	private decimal? _entryPrice;
	private Sides? _entrySide;

	/// <summary>
	/// Base balance divider used to calculate initial position size.
	/// </summary>
	public decimal Shape {
	get => _shape.Value;
	set => _shape.Value = value;
	}

	/// <summary>
	/// Maximum number of consecutive doublings.
	/// </summary>
	public int DoublingCount {
	get => _doublingCount.Value;
	set => _doublingCount.Value = value;
	}

	/// <summary>
	/// Stop loss in points.
	/// </summary>
	public int StopLossPoints {
	get => _stopLossPoints.Value;
	set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit in points.
	/// </summary>
	public int TakeProfitPoints {
	get => _takeProfitPoints.Value;
	set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Fast period for the first MACD indicator.
	/// </summary>
	public int Macd1Fast {
	get => _macd1Fast.Value;
	set => _macd1Fast.Value = value;
	}

	/// <summary>
	/// Slow period for the first MACD indicator.
	/// </summary>
	public int Macd1Slow {
	get => _macd1Slow.Value;
	set => _macd1Slow.Value = value;
	}

	/// <summary>
	/// Fast period for the second MACD indicator.
	/// </summary>
	public int Macd2Fast {
	get => _macd2Fast.Value;
	set => _macd2Fast.Value = value;
	}

	/// <summary>
	/// Slow period for the second MACD indicator.
	/// </summary>
	public int Macd2Slow {
	get => _macd2Slow.Value;
	set => _macd2Slow.Value = value;
	}

	/// <summary>
	/// Candle type used for analysis.
	/// </summary>
	public DataType CandleType {
	get => _candleType.Value;
	set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize parameters.
	/// </summary>
	public MartingaleMacdStrategy() {
	_shape = Param(nameof(Shape), 1000m)
			 .SetDisplay("Shape", "Balance divider for initial size",
				 "Trading")
			 .SetCanOptimize(true);
	_doublingCount =
		Param(nameof(DoublingCount), 1)
		.SetDisplay("Doubling Count",
				"Maximum consecutive volume doublings", "Risk")
		.SetCanOptimize(true);
	_stopLossPoints =
		Param(nameof(StopLossPoints), 500)
		.SetDisplay("Stop Loss", "Stop loss in points", "Risk")
		.SetCanOptimize(true);
	_takeProfitPoints =
		Param(nameof(TakeProfitPoints), 1500)
		.SetDisplay("Take Profit", "Take profit in points", "Risk")
		.SetCanOptimize(true);
	_macd1Fast = Param(nameof(Macd1Fast), 5)
			 .SetDisplay("MACD1 Fast", "Fast EMA for first MACD",
					 "Indicators")
			 .SetCanOptimize(true);
	_macd1Slow = Param(nameof(Macd1Slow), 20)
			 .SetDisplay("MACD1 Slow", "Slow EMA for first MACD",
					 "Indicators")
			 .SetCanOptimize(true);
	_macd2Fast = Param(nameof(Macd2Fast), 10)
			 .SetDisplay("MACD2 Fast", "Fast EMA for second MACD",
					 "Indicators")
			 .SetCanOptimize(true);
	_macd2Slow = Param(nameof(Macd2Slow), 15)
			 .SetDisplay("MACD2 Slow", "Slow EMA for second MACD",
					 "Indicators")
			 .SetCanOptimize(true);
	_candleType =
		Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe for candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)>
	GetWorkingSecurities() {
	return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted() {
	base.OnReseted();
	_macd1Prev1 = null;
	_macd1Prev2 = null;
	_macd2Prev = null;
	_lossStreak = 0;
	_entryPrice = null;
	_entrySide = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time) {
	base.OnStarted(time);

	var macd1 = new MovingAverageConvergenceDivergence {
		ShortPeriod = Macd1Fast, LongPeriod = Macd1Slow, SignalPeriod = 3
	};
	var macd2 = new MovingAverageConvergenceDivergence {
		ShortPeriod = Macd2Fast, LongPeriod = Macd2Slow, SignalPeriod = 3
	};

	var subscription = SubscribeCandles(CandleType);
	subscription.Bind(macd1, macd2, ProcessCandle).Start();

	var area = CreateChartArea();
	if (area != null) {
		DrawCandles(area, subscription);
		DrawIndicator(area, macd1);
		DrawIndicator(area, macd2);
		DrawOwnTrades(area);
	}

	var step = Security?.PriceStep ?? 1m;
	StartProtection(
		takeProfit: new Unit(TakeProfitPoints * step, UnitTypes.Absolute),
		stopLoss: new Unit(StopLossPoints * step, UnitTypes.Absolute));
	}

	private void ProcessCandle(ICandleMessage candle, decimal macd1,
				   decimal macd2) {
	if (candle.State != CandleStates.Finished)
		return;

	if (!IsFormedAndOnlineAndAllowTrading())
		return;

	if (_macd1Prev1.HasValue && _macd1Prev2.HasValue &&
		_macd2Prev.HasValue) {
		var t0 = macd1;
		var t1 = _macd1Prev1.Value;
		var t2 = _macd1Prev2.Value;
		var k0 = macd2;
		var k1 = _macd2Prev.Value;

		if (t0 > t1 && t1 < t2 && k1 > k0 && Position <= 0) {
		var volume = CalculateVolume();
		BuyMarket(volume);
		_entryPrice = candle.ClosePrice;
		_entrySide = Sides.Buy;
		} else if (t0 < t1 && t1 > t2 && k1 < k0 && Position >= 0) {
		var volume = CalculateVolume();
		SellMarket(volume);
		_entryPrice = candle.ClosePrice;
		_entrySide = Sides.Sell;
		}
	}

	_macd1Prev2 = _macd1Prev1;
	_macd1Prev1 = macd1;
	_macd2Prev = macd2;
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade) {
	base.OnNewMyTrade(trade);

	if (Position != 0 || _entryPrice == null || _entrySide == null)
		return;

	var exitPrice = trade.Trade.Price;
	var profit = _entrySide == Sides.Buy ? exitPrice - _entryPrice.Value
						 : _entryPrice.Value - exitPrice;

	if (profit < 0 && _lossStreak < DoublingCount)
		_lossStreak++;
	else
		_lossStreak = 0;

	_entryPrice = null;
	_entrySide = null;
	}

	private decimal CalculateVolume() {
	var minVolume = Security?.MinVolume ?? 1m;
	var balance = Portfolio?.CurrentValue ?? 0m;
	var lot = Math.Floor(balance / Shape) * minVolume;
	if (lot <= 0)
		lot = minVolume;

	var volume = lot * (decimal)Math.Pow(2, _lossStreak);
	var maxVolume = Security?.MaxVolume ?? volume;
	return Math.Min(volume, maxVolume);
	}
}
