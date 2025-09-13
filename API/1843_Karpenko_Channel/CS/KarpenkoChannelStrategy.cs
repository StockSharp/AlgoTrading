namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Karpenko Channel strategy.
/// Generates signals when the dynamic channel crosses the baseline.
/// Long when channel crosses below the base.
/// Short when channel crosses above the base.
/// Uses fixed stop-loss and take-profit.
/// </summary>
public class KarpenkoChannelStrategy : Strategy
{
	private readonly StrategyParam<int> _basicMa;
	private readonly StrategyParam<int> _history;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<bool> _allowBuyOpen;
	private readonly StrategyParam<bool> _allowSellOpen;
	private readonly StrategyParam<bool> _allowBuyClose;
	private readonly StrategyParam<bool> _allowSellClose;
	private readonly StrategyParam<DataType> _candleType;

	private SMA _baseMaSma;
	private SMA _rangeSma;

	private decimal _prevInd;
	private decimal _prevSign;

	/// <summary>
	/// Period for base moving average.
	/// </summary>
	public int BasicMa { get => _basicMa.Value; set => _basicMa.Value = value; }

	/// <summary>
	/// History length for range calculation.
	/// </summary>
	public int History { get => _history.Value; set => _history.Value = value; }

	/// <summary>
	/// Stop-loss distance in price units.
	/// </summary>
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }

	/// <summary>
	/// Take-profit distance in price units.
	/// </summary>
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }

	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool AllowBuyOpen { get => _allowBuyOpen.Value; set => _allowBuyOpen.Value = value; }

	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool AllowSellOpen { get => _allowSellOpen.Value; set => _allowSellOpen.Value = value; }

	/// <summary>
	/// Allow closing long positions.
	/// </summary>
	public bool AllowBuyClose { get => _allowBuyClose.Value; set => _allowBuyClose.Value = value; }

	/// <summary>
	/// Allow closing short positions.
	/// </summary>
	public bool AllowSellClose { get => _allowSellClose.Value; set => _allowSellClose.Value = value; }

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="KarpenkoChannelStrategy"/> class.
	/// </summary>
	public KarpenkoChannelStrategy()
	{
		_basicMa = Param(nameof(BasicMa), 144)
			.SetGreaterThanZero()
			.SetDisplay("Base MA", "Length of base moving average", "Parameters")
			.SetCanOptimize(true);

		_history = Param(nameof(History), 500)
			.SetGreaterThanZero()
			.SetDisplay("History", "Lookback length for range", "Parameters")
			.SetCanOptimize(true);

		_stopLoss = Param(nameof(StopLoss), 1000m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop-loss distance in price units", "Risk");

		_takeProfit = Param(nameof(TakeProfit), 2000m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take-profit distance in price units", "Risk");

		_allowBuyOpen = Param(nameof(AllowBuyOpen), true)
			.SetDisplay("Buy Open", "Enable long entries", "Signals");

		_allowSellOpen = Param(nameof(AllowSellOpen), true)
			.SetDisplay("Sell Open", "Enable short entries", "Signals");

		_allowBuyClose = Param(nameof(AllowBuyClose), true)
			.SetDisplay("Buy Close", "Enable closing longs", "Signals");

		_allowSellClose = Param(nameof(AllowSellClose), true)
			.SetDisplay("Sell Close", "Enable closing shorts", "Signals");

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
		_baseMaSma = null;
		_rangeSma = null;
		_prevInd = 0m;
		_prevSign = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_baseMaSma = new SMA { Length = BasicMa };
		_rangeSma = new SMA { Length = History };

		// Configure protection using fixed take-profit and stop-loss.
		StartProtection(new Unit(TakeProfit, UnitTypes.Price), new Unit(StopLoss, UnitTypes.Price));

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _baseMaSma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Use only completed candles.
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var time = candle.OpenTime;

		var baseValue = _baseMaSma.Process(candle.ClosePrice, time, true);
		var rangeValue = _rangeSma.Process(candle.HighPrice - candle.LowPrice, time, true);

		if (!_baseMaSma.IsFormed || !_rangeSma.IsFormed)
		{
			_prevInd = baseValue.ToDecimal();
			_prevSign = baseValue.ToDecimal();
			return;
		}

		var basePrice = baseValue.ToDecimal();
		var range = rangeValue.ToDecimal();

		var up = range;
		var dw = range;
		var upLevel = basePrice;
		while (candle.HighPrice > upLevel)
		{
			up *= 1.618m;
			upLevel = basePrice + up;
		}
		var dnLevel = basePrice;
		while (candle.LowPrice < dnLevel)
		{
			dw *= 1.618m;
			dnLevel = basePrice - dw;
		}

		var ind = basePrice == upLevel ? basePrice - dw : basePrice + up;
		var sign = basePrice;

		// Determine entry signals based on channel crossovers.
		var buyOpen = AllowBuyOpen && _prevInd > _prevSign && ind <= sign;
		var sellOpen = AllowSellOpen && _prevInd < _prevSign && ind >= sign;
		var buyClose = AllowBuyClose && _prevInd < _prevSign;
		var sellClose = AllowSellClose && _prevInd > _prevSign;

		// First close existing positions.
		if (sellClose && Position < 0)
			BuyMarket(Math.Abs(Position));

		if (buyClose && Position > 0)
			SellMarket(Math.Abs(Position));

		// Then open new positions.
		if (buyOpen && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (sellOpen && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}

		// Update previous values.
		_prevInd = ind;
		_prevSign = sign;
	}
}
