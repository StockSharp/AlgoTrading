using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Williams %R slowdown strategy.
/// Opens or closes positions when momentum stalls near specified levels.
/// </summary>
public class WprSlowdownStrategy : Strategy
{
	private readonly StrategyParam<int> _wprPeriod;
	private readonly StrategyParam<decimal> _levelMax;
	private readonly StrategyParam<decimal> _levelMin;
	private readonly StrategyParam<bool> _seekSlowdown;
	private readonly StrategyParam<bool> _buyPosOpen;
	private readonly StrategyParam<bool> _sellPosOpen;
	private readonly StrategyParam<bool> _buyPosClose;
	private readonly StrategyParam<bool> _sellPosClose;
	private readonly StrategyParam<DataType> _candleType;

	private WilliamsR _wpr;
	private decimal? _prevWpr;

	/// <summary>
	/// Williams %R period.
	/// </summary>
	public int WprPeriod
	{
		get => _wprPeriod.Value;
		set => _wprPeriod.Value = value;
	}

	/// <summary>
	/// Upper signal level (overbought threshold).
	/// </summary>
	public decimal LevelMax
	{
		get => _levelMax.Value;
		set => _levelMax.Value = value;
	}

	/// <summary>
	/// Lower signal level (oversold threshold).
	/// </summary>
	public decimal LevelMin
	{
		get => _levelMin.Value;
		set => _levelMin.Value = value;
	}

	/// <summary>
	/// Require slowdown between consecutive Williams %R values.
	/// </summary>
	public bool SeekSlowdown
	{
		get => _seekSlowdown.Value;
		set => _seekSlowdown.Value = value;
	}

	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool BuyPosOpen
	{
		get => _buyPosOpen.Value;
		set => _buyPosOpen.Value = value;
	}

	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool SellPosOpen
	{
		get => _sellPosOpen.Value;
		set => _sellPosOpen.Value = value;
	}

	/// <summary>
	/// Allow closing long positions on sell signals.
	/// </summary>
	public bool BuyPosClose
	{
		get => _buyPosClose.Value;
		set => _buyPosClose.Value = value;
	}

	/// <summary>
	/// Allow closing short positions on buy signals.
	/// </summary>
	public bool SellPosClose
	{
		get => _sellPosClose.Value;
		set => _sellPosClose.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="WprSlowdownStrategy"/> class.
	/// </summary>
	public WprSlowdownStrategy()
	{
		_wprPeriod = Param(nameof(WprPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("WPR Period", "Williams %R indicator period", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(6, 24, 1);

		_levelMax = Param(nameof(LevelMax), -20m)
			.SetDisplay("Level Max", "Upper signal level", "Indicator");

		_levelMin = Param(nameof(LevelMin), -80m)
			.SetDisplay("Level Min", "Lower signal level", "Indicator");

		_seekSlowdown = Param(nameof(SeekSlowdown), true)
			.SetDisplay("Seek Slowdown", "Require slowdown between values", "Indicator");

		_buyPosOpen = Param(nameof(BuyPosOpen), true)
			.SetDisplay("Open Long", "Allow opening long positions", "Trading");

		_sellPosOpen = Param(nameof(SellPosOpen), true)
			.SetDisplay("Open Short", "Allow opening short positions", "Trading");

		_buyPosClose = Param(nameof(BuyPosClose), true)
			.SetDisplay("Close Long", "Allow closing long positions", "Trading");

		_sellPosClose = Param(nameof(SellPosClose), true)
			.SetDisplay("Close Short", "Allow closing short positions", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(6).TimeFrame())
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
		_prevWpr = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_wpr = new WilliamsR { Length = WprPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_wpr, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _wpr);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal wpr)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!_wpr.IsFormed)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var slowdown = !_prevWpr.HasValue || Math.Abs(wpr - _prevWpr.Value) < 1m;

		var canBuy = wpr >= LevelMax && (!SeekSlowdown || slowdown);
		var canSell = wpr <= LevelMin && (!SeekSlowdown || slowdown);

		if (canBuy)
		{
			if (SellPosClose && Position < 0)
			BuyMarket(Math.Abs(Position));

			if (BuyPosOpen && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (canSell)
		{
			if (BuyPosClose && Position > 0)
			SellMarket(Math.Abs(Position));

			if (SellPosOpen && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		}

		_prevWpr = wpr;
	}
}

