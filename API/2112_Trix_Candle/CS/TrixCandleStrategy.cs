using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// TRIX Candle reversal strategy.
/// Opens long when bullish TRIX candle turns neutral or bearish and closes shorts.
/// Opens short when bearish TRIX candle turns neutral or bullish and closes longs.
/// </summary>
public class TrixCandleStrategy : Strategy
{
	private readonly StrategyParam<int> _trixPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _buyPosOpen;
	private readonly StrategyParam<bool> _sellPosOpen;
	private readonly StrategyParam<bool> _buyPosClose;
	private readonly StrategyParam<bool> _sellPosClose;

	private TripleExponentialMovingAverage _openTema = null!;
	private TripleExponentialMovingAverage _closeTema = null!;
	private int _prevColor;

	/// <summary>
	/// Period for TRIX smoothing.
	/// </summary>
	public int TrixPeriod { get => _trixPeriod.Value; set => _trixPeriod.Value = value; }

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool BuyPosOpen { get => _buyPosOpen.Value; set => _buyPosOpen.Value = value; }

	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool SellPosOpen { get => _sellPosOpen.Value; set => _sellPosOpen.Value = value; }

	/// <summary>
	/// Allow closing long positions.
	/// </summary>
	public bool BuyPosClose { get => _buyPosClose.Value; set => _buyPosClose.Value = value; }

	/// <summary>
	/// Allow closing short positions.
	/// </summary>
	public bool SellPosClose { get => _sellPosClose.Value; set => _sellPosClose.Value = value; }

	/// <summary>
	/// Initialize the strategy with default parameters.
	/// </summary>
	public TrixCandleStrategy()
	{
		_trixPeriod = Param(nameof(TrixPeriod), 14)
			.SetDisplay("TRIX Period", "Period for triple exponential smoothing", "Indicators")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for processing", "General");

		_buyPosOpen = Param(nameof(BuyPosOpen), true)
			.SetDisplay("Allow Buy Open", "Enable opening long positions", "Trading");

		_sellPosOpen = Param(nameof(SellPosOpen), true)
			.SetDisplay("Allow Sell Open", "Enable opening short positions", "Trading");

		_buyPosClose = Param(nameof(BuyPosClose), true)
			.SetDisplay("Allow Buy Close", "Enable closing long positions", "Trading");

		_sellPosClose = Param(nameof(SellPosClose), true)
			.SetDisplay("Allow Sell Close", "Enable closing short positions", "Trading");
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
		_prevColor = -1;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_openTema = new TripleExponentialMovingAverage { Length = TrixPeriod };
		_closeTema = new TripleExponentialMovingAverage { Length = TrixPeriod };
		_prevColor = -1;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection();

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

		var openValue = _openTema.Process(candle.OpenPrice).ToNullableDecimal();
		var closeValue = _closeTema.Process(candle.ClosePrice).ToNullableDecimal();

		if (openValue is null || closeValue is null)
			return;

		var color = 1;

		if (openValue.Value < closeValue.Value)
			color = 2;
		else if (openValue.Value > closeValue.Value)
			color = 0;

		if (_prevColor == -1)
		{
			_prevColor = color;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevColor = color;
			return;
		}

		var buyOpen = BuyPosOpen && _prevColor == 2 && color < 2;
		var sellOpen = SellPosOpen && _prevColor == 0 && color > 0;
		var buyClose = BuyPosClose && _prevColor == 0;
		var sellClose = SellPosClose && _prevColor == 2;

		if (sellClose && Position < 0)
			BuyMarket();

		if (buyClose && Position > 0)
			SellMarket();

		if (buyOpen && Position <= 0)
			BuyMarket();

		if (sellOpen && Position >= 0)
			SellMarket();

		_prevColor = color;
	}
}
