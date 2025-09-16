namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy based on Arrows & Curves indicator.
/// </summary>
public class ArrowsCurvesStrategy : Strategy
{
	private readonly StrategyParam<int> _ssp;
	private readonly StrategyParam<decimal> _channel;
	private readonly StrategyParam<decimal> _stopChannel;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _buyOpen;
	private readonly StrategyParam<bool> _sellOpen;
	private readonly StrategyParam<bool> _buyClose;
	private readonly StrategyParam<bool> _sellClose;

	private Highest _highest = null!;
	private Lowest _lowest = null!;
	private bool _uptrend;
	private bool _oldUptrend;
	private bool _uptrend2;
	private bool _oldUptrend2;

	/// <summary>
	/// Initializes a new instance of <see cref="ArrowsCurvesStrategy"/>.
	/// </summary>
	public ArrowsCurvesStrategy()
	{
		_ssp = Param(nameof(SspPeriod), 20)
			.SetDisplay("SSP", "Period for channel", "Parameters")
			.SetCanOptimize(true);

		_channel = Param(nameof(Channel), 0m)
			.SetDisplay("Channel", "Channel expansion percent", "Parameters")
			.SetCanOptimize(true);

		_stopChannel = Param(nameof(StopChannel), 30m)
			.SetDisplay("Stop Channel", "Inner stop channel percent", "Parameters")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_buyOpen = Param(nameof(BuyPosOpen), true)
			.SetDisplay("Buy Open", "Allow opening long positions", "General");

		_sellOpen = Param(nameof(SellPosOpen), true)
			.SetDisplay("Sell Open", "Allow opening short positions", "General");
		_buyClose = Param(nameof(BuyPosClose), true)
			.SetDisplay("Buy Close", "Allow closing long positions", "General");

		_sellClose = Param(nameof(SellPosClose), true)
			.SetDisplay("Sell Close", "Allow closing short positions", "General");
	}

	public int SspPeriod
	{
		get => _ssp.Value;
		set => _ssp.Value = value;
	}

	public decimal Channel
	{
		get => _channel.Value;
		set => _channel.Value = value;
	}

	public decimal StopChannel
	{
		get => _stopChannel.Value;
		set => _stopChannel.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public bool BuyPosOpen
	{
		get => _buyOpen.Value;
		set => _buyOpen.Value = value;
	}

	public bool SellPosOpen
	{
		get => _sellOpen.Value;
		set => _sellOpen.Value = value;
	}

	public bool BuyPosClose
	{
		get => _buyClose.Value;
		set => _buyClose.Value = value;
	}

	public bool SellPosClose
	{
		get => _sellClose.Value;
		set => _sellClose.Value = value;
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

		_highest = null!;
		_lowest = null!;
		_uptrend = default;
		_oldUptrend = default;
		_uptrend2 = default;
		_oldUptrend2 = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_highest = new Highest { Length = SspPeriod };
		_lowest = new Lowest { Length = SspPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.WhenNew(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}
	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var highValue = _highest.Process(candle.HighPrice);
		var lowValue = _lowest.Process(candle.LowPrice);

		if (!highValue.IsFinal || !lowValue.IsFinal)
			return;

		var high = highValue.ToDecimal();
		var low = lowValue.ToDecimal();
		var diff = high - low;

		var smax = high + diff * Channel / 100m;
		var smin = low + diff * Channel / 100m;
		var smax2 = high - diff * (Channel + StopChannel) / 100m;
		var smin2 = low + diff * (Channel + StopChannel) / 100m;

		var close = candle.ClosePrice;

		if (close < smin && close < smax && _uptrend2)
			_uptrend = false;
		if (close > smax && close > smin && !_uptrend2)
			_uptrend = true;
		if ((close > smax2 || close > smin2) && !_uptrend)
			_uptrend2 = false;
		if ((close < smin2 || close < smax2) && _uptrend)
			_uptrend2 = true;

		var buy = false;
		var sell = false;
		var buyStop = false;
		var sellStop = false;

		if (close < smin && close < smax && !_uptrend2)
		{
			sell = true;
			_uptrend2 = true;
		}

		if (close > smax && close > smin && _uptrend2)
		{
			buy = true;
			_uptrend2 = false;
		}

		if (_uptrend != _oldUptrend)
		{
			if (_uptrend)
				buy = true;
			else
				sell = true;
		}

		if (_uptrend2 != _oldUptrend2)
		{
			if (_uptrend2)
				buyStop = true;
			else
				sellStop = true;
		}

		_oldUptrend = _uptrend;
		_oldUptrend2 = _uptrend2;

		if (sellStop && Position > 0 && BuyPosClose)
			ClosePosition();

		if (buyStop && Position < 0 && SellPosClose)
			ClosePosition();

		if (buy && BuyPosOpen && Position <= 0)
		{
			if (SellPosClose && Position < 0)
				ClosePosition();
			BuyMarket();
		}
		else if (sell && SellPosOpen && Position >= 0)
		{
			if (BuyPosClose && Position > 0)
				ClosePosition();
			SellMarket();
		}
	}
}
