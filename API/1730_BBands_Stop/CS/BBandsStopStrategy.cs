using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bollinger Bands stop based trend following strategy.
/// </summary>
public class BBandsStopStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _deviation;
	private readonly StrategyParam<decimal> _moneyRisk;
	private readonly StrategyParam<bool> _buyPosOpen;
	private readonly StrategyParam<bool> _sellPosOpen;
	private readonly StrategyParam<bool> _buyPosClose;
	private readonly StrategyParam<bool> _sellPosClose;

	private int _trend;
	private decimal _smax1;
	private decimal _smin1;
	private decimal _bsmax1;
	private decimal _bsmin1;
	private bool _initialized;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int Length { get => _length.Value; set => _length.Value = value; }
	public decimal Deviation { get => _deviation.Value; set => _deviation.Value = value; }
	public decimal MoneyRisk { get => _moneyRisk.Value; set => _moneyRisk.Value = value; }
	public bool BuyPosOpen { get => _buyPosOpen.Value; set => _buyPosOpen.Value = value; }
	public bool SellPosOpen { get => _sellPosOpen.Value; set => _sellPosOpen.Value = value; }
	public bool BuyPosClose { get => _buyPosClose.Value; set => _buyPosClose.Value = value; }
	public bool SellPosClose { get => _sellPosClose.Value; set => _sellPosClose.Value = value; }

	public BBandsStopStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");
		_length = Param(nameof(Length), 20)
		.SetDisplay("Length", "Bollinger period", "Indicator");
		_deviation = Param(nameof(Deviation), 2m)
		.SetDisplay("Deviation", "Bollinger deviation", "Indicator");
		_moneyRisk = Param(nameof(MoneyRisk), 1m)
		.SetDisplay("Money Risk", "Offset factor", "Indicator");
		_buyPosOpen = Param(nameof(BuyPosOpen), true)
		.SetDisplay("Buy Open", "Allow to enter long", "Trading");
		_sellPosOpen = Param(nameof(SellPosOpen), true)
		.SetDisplay("Sell Open", "Allow to enter short", "Trading");
		_buyPosClose = Param(nameof(BuyPosClose), true)
		.SetDisplay("Buy Close", "Allow to exit long", "Trading");
		_sellPosClose = Param(nameof(SellPosClose), true)
		.SetDisplay("Sell Close", "Allow to exit short", "Trading");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var bb = new BollingerBands
		{
		Length = Length,
		Width = Deviation
		};

		SubscribeCandles(CandleType).Bind(bb, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal middle, decimal upper, decimal lower)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var mRisk = 0.5m * (MoneyRisk - 1m);
		var smax0 = upper;
		var smin0 = lower;

		if (!_initialized)
		{
		_initialized = true;
		_smax1 = smax0;
		_smin1 = smin0;
		var firstOffset = mRisk * (smax0 - smin0);
		_bsmax1 = smax0 + firstOffset;
		_bsmin1 = smin0 - firstOffset;
		return;
		}

		var prevTrend = _trend;

		if (candle.ClosePrice > _smax1)
		_trend = 1;
		else if (candle.ClosePrice < _smin1)
		_trend = -1;

		if (_trend > 0 && smin0 < _smin1)
		smin0 = _smin1;
		else if (_trend < 0 && smax0 > _smax1)
		smax0 = _smax1;

		var dsize = mRisk * (smax0 - smin0);
		var bsmax0 = smax0 + dsize;
		var bsmin0 = smin0 - dsize;

		if (_trend > 0 && bsmin0 < _bsmin1)
		bsmin0 = _bsmin1;
		else if (_trend < 0 && bsmax0 > _bsmax1)
		bsmax0 = _bsmax1;

		if (_trend > 0 && prevTrend <= 0)
		{
		if (SellPosClose && Position < 0)
		BuyMarket();
		if (BuyPosOpen && Position <= 0)
		BuyMarket();
		}
		else if (_trend < 0 && prevTrend >= 0)
		{
		if (BuyPosClose && Position > 0)
		SellMarket();
		if (SellPosOpen && Position >= 0)
		SellMarket();
		}

		_smax1 = smax0;
		_smin1 = smin0;
		_bsmax1 = bsmax0;
		_bsmin1 = bsmin0;
	}
}
