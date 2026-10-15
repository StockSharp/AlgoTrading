using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trades confirmed price/CCI pivot divergence in the CCI extreme zone.
/// A detected divergence stays valid for DivergencePeriod bars.
/// Exits on the favorable zero crossing or actual-fill percent protection.
/// </summary>
public class CciDivergenceStrategy : Strategy
{
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<int> _divergencePeriod;
	private readonly StrategyParam<decimal> _overboughtLevel;
	private readonly StrategyParam<decimal> _oversoldLevel;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLossPercent;

	private readonly List<(decimal High, decimal Low, decimal Cci)> _window = new();
	private (decimal Price, decimal Cci)? _lastLow;
	private (decimal Price, decimal Cci)? _lastHigh;
	private decimal? _previousCci;
	private Sides? _signal;
	private int _signalAge;
	private Order _pendingOrder;
	private CommodityChannelIndex _cci;

	public int CciPeriod { get => _cciPeriod.Value; set => _cciPeriod.Value = value; }
	public int DivergencePeriod { get => _divergencePeriod.Value; set => _divergencePeriod.Value = value; }
	public decimal OverboughtLevel { get => _overboughtLevel.Value; set => _overboughtLevel.Value = value; }
	public decimal OversoldLevel { get => _oversoldLevel.Value; set => _oversoldLevel.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }

	public CciDivergenceStrategy()
	{
		_cciPeriod = Param(nameof(CciPeriod), 20).SetGreaterThanZero()
			.SetDisplay("CCI Period", "Typical-price CCI length", "Indicators");
		_divergencePeriod = Param(nameof(DivergencePeriod), 5).SetRange(3, 31)
			.SetDisplay("Divergence Period", "Odd-width confirmed price pivot window and bars a detected divergence stays valid", "Pattern");
		_overboughtLevel = Param(nameof(OverboughtLevel), 100m)
			.SetDisplay("Overbought Level", "CCI at the later high must exceed this level", "Pattern");
		_oversoldLevel = Param(nameof(OversoldLevel), -100m)
			.SetDisplay("Oversold Level", "CCI at the later low must be below this level", "Pattern");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "CCI and price pivot timeframe", "General");
		_stopLossPercent = Param(nameof(StopLossPercent), 2m).SetNotNegative()
			.SetDisplay("Stop Loss (%)", "Actual-fill percent stop; zero disables it", "Protection");
		OrderRegistering += order => _pendingOrder = order;
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType), (Security, DataType.Level1)];

	protected override void OnReseted()
	{
		base.OnReseted();
		ClearState();
	}

	private void ClearState()
	{
		_window.Clear();
		_lastLow = _lastHigh = null;
		_previousCci = null;
		_signal = null;
		_signalAge = 0;
		_pendingOrder = null;
		_cci = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		if (DivergencePeriod % 2 != 1 || OverboughtLevel <= 0m || OversoldLevel >= 0m)
			throw new InvalidOperationException("DivergencePeriod must be odd, OverboughtLevel positive, and OversoldLevel negative.");
		ClearState();
		StartProtection(new Unit(), new Unit(StopLossPercent, UnitTypes.Percent), useMarketOrders: true, isLocalStop: true);
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Bind(ObserveProtectionQuote).Start();
		}
		_cci = new CommodityChannelIndex { Length = CciPeriod };
		var candles = SubscribeCandles(CandleType);
		candles.Bind(_cci, ProcessCandle).Start();
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, candles);
			DrawIndicator(area, _cci);
			DrawOwnTrades(area);
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// Native actual-fill protection evaluates executable quotes between signal candles.
	}

	private void ProcessCandle(ICandleMessage candle, decimal cciValue)
	{
		if (candle.State != CandleStates.Finished || !_cci.IsFormed)
			return;

		var crossedUp = _previousCci is decimal oldUp && oldUp <= 0m && cciValue > 0m;
		var crossedDown = _previousCci is decimal oldDown && oldDown >= 0m && cciValue < 0m;
		_previousCci = cciValue;

		if (_signal is not null && ++_signalAge > DivergencePeriod)
			_signal = null;

		_window.Add((candle.HighPrice, candle.LowPrice, cciValue));
		if (_window.Count == DivergencePeriod)
		{
			var middle = DivergencePeriod / 2;
			var pivot = _window[middle];
			if (_window.Where((_, index) => index != middle).All(bar => pivot.Low < bar.Low))
			{
				if (_lastLow is { } prior && pivot.Low < prior.Price &&
					pivot.Cci > prior.Cci && pivot.Cci < OversoldLevel)
				{
					_signal = Sides.Buy;
					_signalAge = 0;
				}
				_lastLow = (pivot.Low, pivot.Cci);
			}
			if (_window.Where((_, index) => index != middle).All(bar => pivot.High > bar.High))
			{
				if (_lastHigh is { } prior && pivot.High > prior.Price &&
					pivot.Cci < prior.Cci && pivot.Cci > OverboughtLevel)
				{
					_signal = Sides.Sell;
					_signalAge = 0;
				}
				_lastHigh = (pivot.High, pivot.Cci);
			}
			_window.RemoveAt(0);
		}

		if (!IsFormedAndOnlineAndAllowTrading() ||
			_pendingOrder is not null && _pendingOrder.State is not (OrderStates.Done or OrderStates.Failed))
			return;
		if (Position > 0m && crossedUp)
			SellMarket(Position);
		else if (Position < 0m && crossedDown)
			BuyMarket(Math.Abs(Position));
		else if (Position == 0m && _signal is Sides side)
		{
			if (side == Sides.Buy)
				BuyMarket(Volume);
			else
				SellMarket(Volume);
			_signal = null;
		}
	}
}
