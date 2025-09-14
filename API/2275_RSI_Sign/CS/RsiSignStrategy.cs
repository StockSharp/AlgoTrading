using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;
using StockSharp.BusinessEntities;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSI based signal strategy using ATR for additional context.
/// Opens long when RSI crosses above the down level.
/// Opens short when RSI crosses below the up level.
/// </summary>
public class RsiSignStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _upLevel;
	private readonly StrategyParam<decimal> _downLevel;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _buyOpen;
	private readonly StrategyParam<bool> _sellOpen;
	private readonly StrategyParam<bool> _buyClose;
	private readonly StrategyParam<bool> _sellClose;

	private decimal? _previousRsi;

	/// <summary>
	/// Length of RSI indicator.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Length of ATR indicator.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Upper RSI level for sell signal.
	/// </summary>
	public decimal UpLevel
	{
		get => _upLevel.Value;
		set => _upLevel.Value = value;
	}

	/// <summary>
	/// Lower RSI level for buy signal.
	/// </summary>
	public decimal DownLevel
	{
		get => _downLevel.Value;
		set => _downLevel.Value = value;
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
	/// Allow opening long positions.
	/// </summary>
	public bool BuyOpen
	{
		get => _buyOpen.Value;
		set => _buyOpen.Value = value;
	}

	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool SellOpen
	{
		get => _sellOpen.Value;
		set => _sellOpen.Value = value;
	}

	/// <summary>
	/// Allow closing long positions.
	/// </summary>
	public bool BuyClose
	{
		get => _buyClose.Value;
		set => _buyClose.Value = value;
	}

	/// <summary>
	/// Allow closing short positions.
	/// </summary>
	public bool SellClose
	{
		get => _sellClose.Value;
		set => _sellClose.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public RsiSignStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "Length of RSI indicator", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(7, 28, 7);

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "Length of ATR indicator", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(7, 28, 7);

		_upLevel = Param(nameof(UpLevel), 70m)
			.SetDisplay("RSI Upper Level", "Sell when RSI falls below this value", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(60m, 80m, 5m);

		_downLevel = Param(nameof(DownLevel), 30m)
			.SetDisplay("RSI Lower Level", "Buy when RSI rises above this value", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(20m, 40m, 5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for indicator calculations", "General");

		_buyOpen = Param(nameof(BuyOpen), true)
			.SetDisplay("Allow Buy", "Enable opening long positions", "Trading");

		_sellOpen = Param(nameof(SellOpen), true)
			.SetDisplay("Allow Sell", "Enable opening short positions", "Trading");

		_buyClose = Param(nameof(BuyClose), true)
			.SetDisplay("Close Buy", "Allow closing long positions", "Trading");

		_sellClose = Param(nameof(SellClose), true)
			.SetDisplay("Close Sell", "Allow closing short positions", "Trading");
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
		// Reset previous RSI value
		_previousRsi = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Create indicators
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };

		// Subscribe to candles of specified type
		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(rsi, atr, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue, decimal atrValue)
	{
		// Use only finished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Keep previous RSI value for crossover detection
		var prevRsi = _previousRsi;
		_previousRsi = rsiValue;

		// Need at least two RSI values
		if (prevRsi is null)
			return;

		// Detect crossings of RSI relative to defined levels
		var upSignal = prevRsi <= DownLevel && rsiValue > DownLevel;
		var downSignal = prevRsi >= UpLevel && rsiValue < UpLevel;

		// Handle buy signal
		if (upSignal)
		{
			if (BuyOpen && Position <= 0)
				BuyMarket();

			if (SellClose && Position < 0)
				BuyMarket(-Position);
		}

		// Handle sell signal
		if (downSignal)
		{
			if (SellOpen && Position >= 0)
				SellMarket();

			if (BuyClose && Position > 0)
				SellMarket(Position);
		}
	}
}
