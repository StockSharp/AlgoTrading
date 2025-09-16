using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Darvas Boxes breakout strategy.
/// Opens long position when price breaks above the upper box line.
/// Opens short position when price breaks below the lower box line.
/// Optional stop-loss and take-profit levels.
/// </summary>
public class DarvasBoxesSystemStrategy : Strategy
{
	private readonly StrategyParam<int> _boxPeriod;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<bool> _allowBuyEntry;
	private readonly StrategyParam<bool> _allowSellEntry;
	private readonly StrategyParam<bool> _allowBuyExit;
	private readonly StrategyParam<bool> _allowSellExit;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal _prevUpper;
	private decimal _prevLower;
	private decimal _prevClose;

	/// <summary>
	/// Period used to calculate the box boundaries.
	/// </summary>
	public int BoxPeriod
	{
		get => _boxPeriod.Value;
		set => _boxPeriod.Value = value;
	}

	/// <summary>
	/// Stop loss distance from the entry price.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take profit distance from the entry price.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool AllowBuyEntry
	{
		get => _allowBuyEntry.Value;
		set => _allowBuyEntry.Value = value;
	}

	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool AllowSellEntry
	{
		get => _allowSellEntry.Value;
		set => _allowSellEntry.Value = value;
	}

	/// <summary>
	/// Allow closing long positions on reverse signals or risk conditions.
	/// </summary>
	public bool AllowBuyExit
	{
		get => _allowBuyExit.Value;
		set => _allowBuyExit.Value = value;
	}

	/// <summary>
	/// Allow closing short positions on reverse signals or risk conditions.
	/// </summary>
	public bool AllowSellExit
	{
		get => _allowSellExit.Value;
		set => _allowSellExit.Value = value;
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
	/// Initialize parameters.
	/// </summary>
	public DarvasBoxesSystemStrategy()
	{
		_boxPeriod = Param(nameof(BoxPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Box Period", "Period for box calculation", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 40, 5);

		_stopLoss = Param(nameof(StopLoss), 1000m)
			.SetDisplay("Stop Loss", "Distance from entry price to stop loss", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(500m, 2000m, 500m);

		_takeProfit = Param(nameof(TakeProfit), 2000m)
			.SetDisplay("Take Profit", "Distance from entry price to take profit", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(1000m, 4000m, 500m);

		_allowBuyEntry = Param(nameof(AllowBuyEntry), true)
			.SetDisplay("Allow Buy Entry", "Permit opening long positions", "Trading Rules");

		_allowSellEntry = Param(nameof(AllowSellEntry), true)
			.SetDisplay("Allow Sell Entry", "Permit opening short positions", "Trading Rules");

		_allowBuyExit = Param(nameof(AllowBuyExit), true)
			.SetDisplay("Allow Buy Exit", "Permit closing long positions", "Trading Rules");

		_allowSellExit = Param(nameof(AllowSellExit), true)
			.SetDisplay("Allow Sell Exit", "Permit closing short positions", "Trading Rules");

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

		_entryPrice = 0m;
		_prevUpper = 0m;
		_prevLower = 0m;
		_prevClose = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var donchian = new DonchianChannels { Length = BoxPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(donchian, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, donchian);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var box = (DonchianChannelsValue)value;

		if (box.UpperBand is not decimal upper || box.LowerBand is not decimal lower)
			return;

		if (_prevUpper == 0m)
		{
			_prevUpper = upper;
			_prevLower = lower;
			_prevClose = candle.ClosePrice;
			return;
		}

		var isUpBreakout = candle.ClosePrice > _prevUpper && _prevClose <= _prevUpper;
		var isDownBreakout = candle.ClosePrice < _prevLower && _prevClose >= _prevLower;

		if (isUpBreakout)
		{
			if (AllowSellExit && Position < 0)
				BuyMarket(Math.Abs(Position));

			if (AllowBuyEntry && Position <= 0)
			{
				var volume = Volume + Math.Abs(Position);
				_entryPrice = candle.ClosePrice;
				BuyMarket(volume);
			}
		}
		else if (isDownBreakout)
		{
			if (AllowBuyExit && Position > 0)
				SellMarket(Position);

			if (AllowSellEntry && Position >= 0)
			{
				var volume = Volume + Math.Abs(Position);
				_entryPrice = candle.ClosePrice;
				SellMarket(volume);
			}
		}
		else if (Position > 0 && AllowBuyExit)
		{
			if (candle.ClosePrice <= _entryPrice - StopLoss || candle.ClosePrice >= _entryPrice + TakeProfit)
				SellMarket(Position);
		}
		else if (Position < 0 && AllowSellExit)
		{
			if (candle.ClosePrice >= _entryPrice + StopLoss || candle.ClosePrice <= _entryPrice - TakeProfit)
				BuyMarket(Math.Abs(Position));
		}

		_prevUpper = upper;
		_prevLower = lower;
		_prevClose = candle.ClosePrice;
	}
}
