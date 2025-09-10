using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Big Runner Strategy - trades SMA crossover with optional stop loss and take profit.
/// </summary>
public class BigRunnerStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<bool> _useStopTake;
	private readonly StrategyParam<decimal> _takeProfitLongPercent;
	private readonly StrategyParam<decimal> _takeProfitShortPercent;
	private readonly StrategyParam<decimal> _stopLossLongPercent;
	private readonly StrategyParam<decimal> _stopLossShortPercent;
	private readonly StrategyParam<decimal> _percentOfPortfolio;
	private readonly StrategyParam<decimal> _leverage;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _takeProfitPrice;
	private bool _isLong;
	private decimal _prevClose;
	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _initialized;

	/// <summary>
	/// Fast SMA period.
	/// </summary>
	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }

	/// <summary>
	/// Slow SMA period.
	/// </summary>
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }

	/// <summary>
	/// Enable stop loss and take profit.
	/// </summary>
	public bool UseStopTake { get => _useStopTake.Value; set => _useStopTake.Value = value; }

	/// <summary>
	/// Take profit percent for long positions.
	/// </summary>
	public decimal TakeProfitLongPercent { get => _takeProfitLongPercent.Value; set => _takeProfitLongPercent.Value = value; }

	/// <summary>
	/// Take profit percent for short positions.
	/// </summary>
	public decimal TakeProfitShortPercent { get => _takeProfitShortPercent.Value; set => _takeProfitShortPercent.Value = value; }

	/// <summary>
	/// Stop loss percent for long positions.
	/// </summary>
	public decimal StopLossLongPercent { get => _stopLossLongPercent.Value; set => _stopLossLongPercent.Value = value; }

	/// <summary>
	/// Stop loss percent for short positions.
	/// </summary>
	public decimal StopLossShortPercent { get => _stopLossShortPercent.Value; set => _stopLossShortPercent.Value = value; }

	/// <summary>
	/// Percent of portfolio used per trade.
	/// </summary>
	public decimal PercentOfPortfolio { get => _percentOfPortfolio.Value; set => _percentOfPortfolio.Value = value; }

	/// <summary>
	/// Leverage multiplier.
	/// </summary>
	public decimal Leverage { get => _leverage.Value; set => _leverage.Value = value; }

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Constructor.
	/// </summary>
	public BigRunnerStrategy()
	{
		_fastLength = Param(nameof(FastLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Fast Length", "Fast SMA period", "SMA")
			.SetCanOptimize(true)
			.SetOptimize(3, 10, 1);

		_slowLength = Param(nameof(SlowLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Slow Length", "Slow SMA period", "SMA")
			.SetCanOptimize(true)
			.SetOptimize(15, 30, 1);

		_useStopTake = Param(nameof(UseStopTake), true)
			.SetDisplay("Use SL/TP", "Enable stop loss and take profit", "Risk");

		_takeProfitLongPercent = Param(nameof(TakeProfitLongPercent), 4m)
			.SetGreaterThanZero()
			.SetDisplay("TP Long %", "Take profit percent for long", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(2m, 8m, 1m);

		_takeProfitShortPercent = Param(nameof(TakeProfitShortPercent), 7m)
			.SetGreaterThanZero()
			.SetDisplay("TP Short %", "Take profit percent for short", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(4m, 10m, 1m);

		_stopLossLongPercent = Param(nameof(StopLossLongPercent), 2m)
			.SetGreaterThanZero()
			.SetDisplay("SL Long %", "Stop loss percent for long", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 1m);

		_stopLossShortPercent = Param(nameof(StopLossShortPercent), 2m)
			.SetGreaterThanZero()
			.SetDisplay("SL Short %", "Stop loss percent for short", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 1m);

		_percentOfPortfolio = Param(nameof(PercentOfPortfolio), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Portfolio %", "Percent of portfolio per trade", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(5m, 20m, 5m);

		_leverage = Param(nameof(Leverage), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Leverage", "Position leverage", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(1m, 3m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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
		_entryPrice = 0m;
		_stopPrice = 0m;
		_takeProfitPrice = 0m;
		_isLong = false;
		_initialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var fastMa = new SMA { Length = FastLength };
		var slowMa = new SMA { Length = SlowLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastMa, slowMa, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastMa);
			DrawIndicator(area, slowMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_initialized)
		{
			_prevClose = candle.ClosePrice;
			_prevFast = fastValue;
			_prevSlow = slowValue;
			_initialized = true;
			return;
		}

		var buySignal = _prevClose <= _prevFast && candle.ClosePrice > fastValue &&
			_prevFast <= _prevSlow && fastValue > slowValue;

		var sellSignal = _prevClose >= _prevFast && candle.ClosePrice < fastValue &&
			_prevFast >= _prevSlow && fastValue < slowValue;

		if (buySignal && Position <= 0)
			Enter(true, candle.ClosePrice);

		if (sellSignal && Position >= 0)
			Enter(false, candle.ClosePrice);

		if (UseStopTake && Position != 0)
			CheckStops(candle.ClosePrice);

		_prevClose = candle.ClosePrice;
		_prevFast = fastValue;
		_prevSlow = slowValue;
	}

	private void Enter(bool isLong, decimal price)
	{
		var volume = CalculateVolume(price);
		_entryPrice = price;
		_isLong = isLong;

		if (UseStopTake)
		{
			if (isLong)
			{
				var tp = TakeProfitLongPercent / 100m;
				var sl = StopLossLongPercent / 100m;
				_takeProfitPrice = _entryPrice * (1m + tp);
				_stopPrice = _entryPrice * (1m - sl);
			}
			else
			{
				var tp = TakeProfitShortPercent / 100m;
				var sl = StopLossShortPercent / 100m;
				_takeProfitPrice = _entryPrice * (1m - tp);
				_stopPrice = _entryPrice * (1m + sl);
			}
		}

		if (isLong)
			BuyMarket(volume + Math.Abs(Position));
		else
			SellMarket(volume + Math.Abs(Position));
	}

	private void CheckStops(decimal price)
	{
		if (_isLong && Position > 0)
		{
			if (price >= _takeProfitPrice || price <= _stopPrice)
				SellMarket(Math.Abs(Position));
		}
		else if (!_isLong && Position < 0)
		{
			if (price <= _takeProfitPrice || price >= _stopPrice)
				BuyMarket(Math.Abs(Position));
		}
	}

	private decimal CalculateVolume(decimal price)
	{
		var portfolioValue = Portfolio.CurrentValue ?? 0m;
		var portion = PercentOfPortfolio / 100m * Leverage;
		var size = portfolioValue * portion / price;
		return size > 0 ? size : Volume;
	}
}
