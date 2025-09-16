using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Price Channel Stop.
/// The indicator builds stop levels from Donchian Channel and switches trend when price crosses them.
/// </summary>
public class PriceChannelStopStrategy : Strategy
{
	private readonly StrategyParam<int> _channelPeriod;
	private readonly StrategyParam<decimal> _risk;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _buyPosOpen;
	private readonly StrategyParam<bool> _sellPosOpen;
	private readonly StrategyParam<bool> _buyPosClose;
	private readonly StrategyParam<bool> _sellPosClose;

	private decimal _prevBsmax;
	private decimal _prevBsmin;
	private int _trend;
	private bool _isFirst = true;

	/// <summary>
	/// Period for channel calculation.
	/// </summary>
	public int ChannelPeriod
	{
		get => _channelPeriod.Value;
		set => _channelPeriod.Value = value;
	}

	/// <summary>
	/// Risk factor for stop calculation.
	/// </summary>
	public decimal Risk
	{
		get => _risk.Value;
		set => _risk.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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
	/// Allow closing long positions on opposite signal.
	/// </summary>
	public bool BuyPosClose
	{
		get => _buyPosClose.Value;
		set => _buyPosClose.Value = value;
	}

	/// <summary>
	/// Allow closing short positions on opposite signal.
	/// </summary>
	public bool SellPosClose
	{
		get => _sellPosClose.Value;
		set => _sellPosClose.Value = value;
	}

	/// <summary>
	/// Initialize the strategy.
	/// </summary>
	public PriceChannelStopStrategy()
	{
		_channelPeriod = Param(nameof(ChannelPeriod), 5)
			.SetDisplay("Channel Period", "Period for Price Channel calculation", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 1);

		_risk = Param(nameof(Risk), 0.10m)
			.SetDisplay("Risk", "Risk factor for stop levels", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(0.05m, 0.3m, 0.05m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_buyPosOpen = Param(nameof(BuyPosOpen), true)
			.SetDisplay("Buy Position Open", "Allow opening long positions", "Trading");

		_sellPosOpen = Param(nameof(SellPosOpen), true)
			.SetDisplay("Sell Position Open", "Allow opening short positions", "Trading");

		_buyPosClose = Param(nameof(BuyPosClose), true)
			.SetDisplay("Buy Position Close", "Allow closing long positions", "Trading");

		_sellPosClose = Param(nameof(SellPosClose), true)
			.SetDisplay("Sell Position Close", "Allow closing short positions", "Trading");
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
		_prevBsmax = default;
		_prevBsmin = default;
		_trend = 0;
		_isFirst = true;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var donchian = new DonchianChannels { Length = ChannelPeriod };

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

		var dc = (DonchianChannelsValue)value;
		if (dc.UpperBand is not decimal upper || dc.LowerBand is not decimal lower)
			return;

		var range = upper - lower;
		var dPrice = range * Risk;
		var bsmax = upper - dPrice;
		var bsmin = lower + dPrice;

		if (_isFirst)
		{
			_prevBsmax = bsmax;
			_prevBsmin = bsmin;
			_isFirst = false;
			return;
		}

		var trend = _trend;
		if (candle.ClosePrice > _prevBsmax)
			trend = 1;
		else if (candle.ClosePrice < _prevBsmin)
			trend = -1;

		if (trend > 0 && bsmin < _prevBsmin)
			bsmin = _prevBsmin;
		if (trend < 0 && bsmax > _prevBsmax)
			bsmax = _prevBsmax;

		var isBuySignal = _trend <= 0 && trend > 0;
		var isSellSignal = _trend >= 0 && trend < 0;

		if (isBuySignal)
		{
			if (SellPosClose && Position < 0)
				BuyMarket(Math.Abs(Position));

			if (BuyPosOpen && Position <= 0)
				BuyMarket(Volume);
		}
		else if (isSellSignal)
		{
			if (BuyPosClose && Position > 0)
				SellMarket(Position);

			if (SellPosOpen && Position >= 0)
				SellMarket(Volume);
		}

		_trend = trend;
		_prevBsmax = bsmax;
		_prevBsmin = bsmin;
	}
}
