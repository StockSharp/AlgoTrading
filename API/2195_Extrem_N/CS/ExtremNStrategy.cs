namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy trading reversals after alternating price extremes.
/// </summary>
public class ExtremNStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _buyPosOpen;
	private readonly StrategyParam<bool> _sellPosOpen;
	private readonly StrategyParam<bool> _buyPosClose;
	private readonly StrategyParam<bool> _sellPosClose;

	private decimal _prevUpper;
	private decimal _prevLower;
	private bool _upPrev;
	private bool _dnPrev;
	private bool _upPrev2;
	private bool _dnPrev2;
	private bool _isFirst;

	/// <summary>
	/// Donchian lookback period.
	/// </summary>
	public int Period
	{
		get => _period.Value;
		set => _period.Value = value;
	}

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Enable opening long positions.
	/// </summary>
	public bool BuyPosOpen
	{
		get => _buyPosOpen.Value;
		set => _buyPosOpen.Value = value;
	}

	/// <summary>
	/// Enable opening short positions.
	/// </summary>
	public bool SellPosOpen
	{
		get => _sellPosOpen.Value;
		set => _sellPosOpen.Value = value;
	}

	/// <summary>
	/// Enable closing long positions.
	/// </summary>
	public bool BuyPosClose
	{
		get => _buyPosClose.Value;
		set => _buyPosClose.Value = value;
	}

	/// <summary>
	/// Enable closing short positions.
	/// </summary>
	public bool SellPosClose
	{
		get => _sellPosClose.Value;
		set => _sellPosClose.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ExtremNStrategy"/>.
	/// </summary>
	public ExtremNStrategy()
	{
		_period = Param(nameof(Period), 9)
			.SetDisplay("Period", "Donchian lookback period", "General")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");

		_buyPosOpen = Param(nameof(BuyPosOpen), true)
			.SetDisplay("Buy Open", "Allow long entries", "Trading");

		_sellPosOpen = Param(nameof(SellPosOpen), true)
			.SetDisplay("Sell Open", "Allow short entries", "Trading");

		_buyPosClose = Param(nameof(BuyPosClose), true)
			.SetDisplay("Buy Close", "Allow closing longs", "Trading");

		_sellPosClose = Param(nameof(SellPosClose), true)
			.SetDisplay("Sell Close", "Allow closing shorts", "Trading");
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

		_prevUpper = default;
		_prevLower = default;
		_upPrev = default;
		_dnPrev = default;
		_upPrev2 = default;
		_dnPrev2 = default;
		_isFirst = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var donchian = new DonchianChannels
		{
			Length = Period
		};

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

		if (!_isFirst)
		{
			_prevUpper = upper;
			_prevLower = lower;
			_isFirst = true;
			return;
		}

		var up = candle.HighPrice > _prevUpper;
		var dn = candle.LowPrice < _prevLower;

		if (_upPrev2 && !_dnPrev2)
		{
			if (SellPosClose && Position < 0)
				BuyMarket(Math.Abs(Position));
			if (BuyPosOpen && dn && Position <= 0)
				BuyMarket();
		}
		else if (!_upPrev2 && _dnPrev2)
		{
			if (BuyPosClose && Position > 0)
				SellMarket(Position);
			if (SellPosOpen && up && Position >= 0)
				SellMarket();
		}

		_upPrev2 = _upPrev;
		_dnPrev2 = _dnPrev;
		_upPrev = up;
		_dnPrev = dn;
		_prevUpper = upper;
		_prevLower = lower;
	}
}
