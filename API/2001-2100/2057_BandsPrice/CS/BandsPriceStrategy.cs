using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Mean reversion strategy based on the Bands Price indicator.
/// Computes price position within Bollinger Bands as an oscillator.
/// Buys when leaving overbought zone, sells when leaving oversold zone.
/// </summary>
public class BandsPriceStrategy : Strategy
{
	private readonly StrategyParam<int> _bandsPeriod;
	private readonly StrategyParam<decimal> _bandsDeviation;
	private readonly StrategyParam<int> _smooth;
	private readonly StrategyParam<int> _upLevel;
	private readonly StrategyParam<int> _dnLevel;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _smoother;
	private readonly StrategyParam<bool> _buyOpen, _sellOpen, _buyClose, _sellClose;
	private Order _pending;
	private int _prevColor;
	private int _prevPrevColor;

	public int BandsPeriod { get => _bandsPeriod.Value; set => _bandsPeriod.Value = value; }
	public decimal BandsDeviation { get => _bandsDeviation.Value; set => _bandsDeviation.Value = value; }
	public int Smooth { get => _smooth.Value; set => _smooth.Value = value; }
	public int UpLevel { get => _upLevel.Value; set => _upLevel.Value = value; }
	public int DnLevel { get => _dnLevel.Value; set => _dnLevel.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public bool BuyOpen { get => _buyOpen.Value; set => _buyOpen.Value = value; }
	public bool SellOpen { get => _sellOpen.Value; set => _sellOpen.Value = value; }
	public bool BuyClose { get => _buyClose.Value; set => _buyClose.Value = value; }
	public bool SellClose { get => _sellClose.Value; set => _sellClose.Value = value; }
	public BandsPriceStrategy()
	{
		_buyOpen = Param(nameof(BuyOpen), true);
		_sellOpen = Param(nameof(SellOpen), true);
		_buyClose = Param(nameof(BuyClose), true);
		_sellClose = Param(nameof(SellClose), true);
		_bandsPeriod = Param(nameof(BandsPeriod), 100)
			.SetGreaterThanZero()
			.SetDisplay("Bands Period", "Bollinger Bands period", "Indicator");

		_bandsDeviation = Param(nameof(BandsDeviation), 2m)
			.SetDisplay("Bands Deviation", "Width of Bollinger Bands", "Indicator");

		_smooth = Param(nameof(Smooth), 5)
			.SetGreaterThanZero()
			.SetDisplay("Smoothing", "Length of smoothing EMA", "Indicator");

		_upLevel = Param(nameof(UpLevel), 25)
			.SetDisplay("Upper Level", "Threshold for overbought zone", "Indicator");

		_dnLevel = Param(nameof(DnLevel), -25)
			.SetDisplay("Lower Level", "Threshold for oversold zone", "Indicator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for analysis", "General");
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
		_smoother = default;
		_pending = null;
		_prevColor = -1;
		_prevPrevColor = -1;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevColor = -1;
		_prevPrevColor = -1;
		_pending = null;
		_smoother = new SimpleMovingAverage { Length = Smooth };

		Indicators.Add(_smoother);

		var bands = new BollingerBands { Length = BandsPeriod, Width = BandsDeviation };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(bands, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bbValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!bbValue.IsFormed)
			return;

		var bb = (BollingerBandsValue)bbValue;
		if (bb.UpBand is not decimal upper || bb.LowBand is not decimal lower)
			return;

		var width = upper - lower;
		if (width == 0)
			return;

		// Normalize price position within bands: -50 to +50
		var res = 100m * (candle.ClosePrice - lower) / width - 50m;

		var smoothResult = _smoother.Process(res, candle.OpenTime, true);
		if (!smoothResult.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var jres = smoothResult.ToDecimal();

		// Determine color zone
		int color;
		if (jres > UpLevel)
			color = 4; // overbought
		else if (jres > 0)
			color = 3; // above zero
		else if (jres < DnLevel)
			color = 0; // oversold
		else if (jres < 0)
			color = 1; // below zero
		else
			color = 2; // neutral

		// Use previous FINISHED color and current FINISHED color; no 1-bar lag.
		if (_prevColor != -1 && IsFormedAndOnlineAndAllowTrading() && (_pending is null || _pending.State is OrderStates.Done or OrderStates.Failed))
		{
			var longEntry = _prevColor == 4 && color != 4;
			var shortEntry = _prevColor == 0 && color > 0;
			if (Position > 0m && BuyClose && color <= 2)
				_pending = SellMarket(Position);
			else if (Position < 0m && SellClose && color >= 2)
				_pending = BuyMarket(-Position);
			else if (longEntry && Position <= 0m)
			{
				if (Position < 0m && SellClose) _pending = BuyMarket(-Position + (BuyOpen ? Volume : 0m));
				else if (Position == 0m && BuyOpen) _pending = BuyMarket(Volume);
			}
			else if (shortEntry && Position >= 0m)
			{
				if (Position > 0m && BuyClose) _pending = SellMarket(Position + (SellOpen ? Volume : 0m));
				else if (Position == 0m && SellOpen) _pending = SellMarket(Volume);
			}
		}

		_prevPrevColor = _prevColor;
		_prevColor = color;
	}
}
