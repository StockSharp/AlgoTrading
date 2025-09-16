using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the Color LeMan Trend indicator.
/// </summary>
public class ColorLemanTrendStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _minPeriod;
	private readonly StrategyParam<int> _midPeriod;
	private readonly StrategyParam<int> _maxPeriod;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<bool> _allowBuy;
	private readonly StrategyParam<bool> _allowSell;
	private readonly StrategyParam<bool> _allowBuyClose;
	private readonly StrategyParam<bool> _allowSellClose;
	private readonly StrategyParam<decimal> _volume;

	private ExponentialMovingAverage _bullsEma;
	private ExponentialMovingAverage _bearsEma;

	private decimal? _prevBulls;
	private decimal? _prevBears;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int Min { get => _minPeriod.Value; set => _minPeriod.Value = value; }
	public int Midle { get => _midPeriod.Value; set => _midPeriod.Value = value; }
	public int Max { get => _maxPeriod.Value; set => _maxPeriod.Value = value; }
	public int PeriodEma { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }
	public decimal StopLossPoints { get => _stopLossPoints.Value; set => _stopLossPoints.Value = value; }
	public decimal TakeProfitPoints { get => _takeProfitPoints.Value; set => _takeProfitPoints.Value = value; }
	public bool AllowBuy { get => _allowBuy.Value; set => _allowBuy.Value = value; }
	public bool AllowSell { get => _allowSell.Value; set => _allowSell.Value = value; }
	public bool AllowBuyClose { get => _allowBuyClose.Value; set => _allowBuyClose.Value = value; }
	public bool AllowSellClose { get => _allowSellClose.Value; set => _allowSellClose.Value = value; }
	public decimal Volume { get => _volume.Value; set => _volume.Value = value; }

	/// <summary>
	/// Initializes a new instance of <see cref="ColorLemanTrendStrategy"/>.
	/// </summary>
	public ColorLemanTrendStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for indicator", "General");

		_minPeriod = Param(nameof(Min), 13)
			.SetGreaterThanZero()
			.SetDisplay("Min", "Shortest period", "Indicator")
			.SetCanOptimize(true);

		_midPeriod = Param(nameof(Midle), 21)
			.SetGreaterThanZero()
			.SetDisplay("Midle", "Middle period", "Indicator")
			.SetCanOptimize(true);

		_maxPeriod = Param(nameof(Max), 34)
			.SetGreaterThanZero()
			.SetDisplay("Max", "Longest period", "Indicator")
			.SetCanOptimize(true);

		_emaPeriod = Param(nameof(PeriodEma), 3)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "Smoothing length", "Indicator")
			.SetCanOptimize(true);

		_stopLossPoints = Param(nameof(StopLossPoints), 1000m)
			.SetDisplay("Stop Loss", "Stop loss in points", "Protection")
			.SetCanOptimize(true);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000m)
			.SetDisplay("Take Profit", "Take profit in points", "Protection")
			.SetCanOptimize(true);

		_allowBuy = Param(nameof(AllowBuy), true)
			.SetDisplay("Allow Buy", "Enable long entries", "Trading");

		_allowSell = Param(nameof(AllowSell), true)
			.SetDisplay("Allow Sell", "Enable short entries", "Trading");

		_allowBuyClose = Param(nameof(AllowBuyClose), true)
			.SetDisplay("Allow Buy Close", "Allow closing longs", "Trading");

		_allowSellClose = Param(nameof(AllowSellClose), true)
			.SetDisplay("Allow Sell Close", "Allow closing shorts", "Trading");

		_volume = Param(nameof(Volume), 1m)
			.SetDisplay("Volume", "Order volume", "Trading");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_bullsEma = new ExponentialMovingAverage { Length = PeriodEma };
		_bearsEma = new ExponentialMovingAverage { Length = PeriodEma };

		var highestMin = new Highest { Length = Min };
		var highestMid = new Highest { Length = Midle };
		var highestMax = new Highest { Length = Max };
		var lowestMin = new Lowest { Length = Min };
		var lowestMid = new Lowest { Length = Midle };
		var lowestMax = new Lowest { Length = Max };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(highestMin, highestMid, highestMax, lowestMin, lowestMid, lowestMax, ProcessCandle)
			.Start();

		StartProtection(
			new Unit(TakeProfitPoints, UnitTypes.Absolute),
			new Unit(StopLossPoints, UnitTypes.Absolute),
			false);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle,
		decimal highestMin, decimal highestMid, decimal highestMax,
		decimal lowestMin, decimal lowestMid, decimal lowestMax)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var hh = 3m * candle.HighPrice - (highestMin + highestMid + highestMax);
		var ll = (lowestMin + lowestMid + lowestMax) - 3m * candle.LowPrice;

		var bullsValue = _bullsEma.Process(hh);
		var bearsValue = _bearsEma.Process(ll);

		if (!bullsValue.IsFinal || !bearsValue.IsFinal)
			return;

		var bulls = bullsValue.ToDecimal();
		var bears = bearsValue.ToDecimal();

		bool buyOpen = false;
		bool sellOpen = false;
		bool buyClose = false;
		bool sellClose = false;

		if (_prevBulls is decimal prevUp && _prevBears is decimal prevDn)
		{
			if (prevUp > prevDn)
			{
				if (AllowBuy && bulls <= bears)
					buyOpen = true;
				if (AllowSellClose)
					sellClose = true;
			}

			if (prevUp < prevDn)
			{
				if (AllowSell && bulls >= bears)
					sellOpen = true;
				if (AllowBuyClose)
					buyClose = true;
			}
		}

		_prevBulls = bulls;
		_prevBears = bears;

		if (sellClose && Position < 0)
			BuyMarket(Volume);

		if (buyClose && Position > 0)
			SellMarket(Volume);

		if (buyOpen && Position <= 0)
			BuyMarket(Volume);

		if (sellOpen && Position >= 0)
			SellMarket(Volume);
	}
}