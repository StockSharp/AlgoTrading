using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Template EA by Market strategy ported from MQL.
/// Uses MACD to detect bullish and bearish momentum and opens market orders accordingly.
/// </summary>
public class TemplateEAbyMarketStrategy : Strategy
{
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _maxOrders;
	private readonly StrategyParam<DataType> _candleType;

	private bool _isInitialized;
	private decimal _prevMacdMain;
	private decimal _prevMacdSignal;

	/// <summary>
	/// Fast EMA length for MACD calculation.
	/// </summary>
	public int MacdFastPeriod { get => _macdFastPeriod.Value; set => _macdFastPeriod.Value = value; }

	/// <summary>
	/// Slow EMA length for MACD calculation.
	/// </summary>
	public int MacdSlowPeriod { get => _macdSlowPeriod.Value; set => _macdSlowPeriod.Value = value; }

	/// <summary>
	/// Signal line length for MACD.
	/// </summary>
	public int MacdSignalPeriod { get => _macdSignalPeriod.Value; set => _macdSignalPeriod.Value = value; }

	/// <summary>
	/// Take profit distance in price points.
	/// </summary>
	public decimal TakeProfitPoints { get => _takeProfitPoints.Value; set => _takeProfitPoints.Value = value; }

	/// <summary>
	/// Stop loss distance in price points.
	/// </summary>
	public decimal StopLossPoints { get => _stopLossPoints.Value; set => _stopLossPoints.Value = value; }

	/// <summary>
	/// Order volume used for market entries.
	/// </summary>
	public decimal OrderVolume { get => _orderVolume.Value; set => _orderVolume.Value = value; }

	/// <summary>
	/// Maximum number of simultaneous orders expressed in units of <see cref="OrderVolume"/>.
	/// </summary>
	public int MaxOrders { get => _maxOrders.Value; set => _maxOrders.Value = value; }

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of <see cref="TemplateEAbyMarketStrategy"/>.
	/// </summary>
	public TemplateEAbyMarketStrategy()
	{
		_macdFastPeriod = Param(nameof(MacdFastPeriod), 12)
			.SetDisplay("MACD Fast EMA", "Fast EMA period for the MACD calculation", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(6, 18, 2);

		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 26)
			.SetDisplay("MACD Slow EMA", "Slow EMA period for the MACD calculation", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(20, 40, 2);

		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 9)
			.SetDisplay("MACD Signal", "Signal period for the MACD calculation", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 15, 1);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 50m)
			.SetDisplay("Take Profit", "Take profit distance in price points", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(10m, 150m, 10m);

		_stopLossPoints = Param(nameof(StopLossPoints), 100m)
			.SetDisplay("Stop Loss", "Stop loss distance in price points", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(20m, 200m, 20m);

		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetDisplay("Order Volume", "Volume used when sending market orders", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 1m, 0.1m);

		_maxOrders = Param(nameof(MaxOrders), 1)
			.SetDisplay("Max Orders", "Maximum number of concurrent orders (volume multiples)", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(1, 5, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Candle type used for MACD", "General");
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
		_isInitialized = false;
		_prevMacdMain = 0m;
		_prevMacdSignal = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var macd = new MovingAverageConvergenceDivergence
		{
			ShortPeriod = MacdFastPeriod,
			LongPeriod = MacdSlowPeriod,
			SignalPeriod = MacdSignalPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(macd, ProcessCandle)
			.Start();

		Volume = OrderVolume;

		StartProtection(
			takeProfit: TakeProfitPoints > 0m ? new Unit(TakeProfitPoints, UnitTypes.Price) : null,
			stopLoss: StopLossPoints > 0m ? new Unit(StopLossPoints, UnitTypes.Price) : null);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, macd);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal macdMain, decimal macdSignal, decimal macdHistogram)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		_ = macdHistogram; // Histogram component is not used in the original template.

		if (!_isInitialized)
		{
			_prevMacdMain = macdMain;
			_prevMacdSignal = macdSignal;
			_isInitialized = true;
			return;
		}

		var wasMacdAbove = _prevMacdMain > _prevMacdSignal;
		var isMacdAbove = macdMain > macdSignal;

		var crossUp = !wasMacdAbove && isMacdAbove;
		var crossDown = wasMacdAbove && !isMacdAbove;

		var maxExposure = MaxOrders * OrderVolume;

		if (crossUp && macdMain > 0m && macdSignal > 0m)
		{
			// The original template opens a long position when MACD crosses above the signal above zero.
			if (OrderVolume > 0m && Math.Abs(Position) < maxExposure)
				BuyMarket(OrderVolume);
		}
		else if (crossDown && macdMain < 0m && macdSignal < 0m)
		{
			// The original template opens a short position when MACD crosses below the signal below zero.
			if (OrderVolume > 0m && Math.Abs(Position) < maxExposure)
				SellMarket(OrderVolume);
		}

		_prevMacdMain = macdMain;
		_prevMacdSignal = macdSignal;
	}
}
