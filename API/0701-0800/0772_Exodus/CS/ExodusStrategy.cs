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
/// Volume-weighted directional momentum filtered by ADX.
/// Reverses on an opposite signal, exits on a momentum zero cross,
/// and protects filled positions by frozen ATR-based native distances.
/// </summary>
public class ExodusStrategy : Strategy
{
	private readonly StrategyParam<int> _vwmoMomentum;
	private readonly StrategyParam<int> _vwmoVolume;
	private readonly StrategyParam<int> _vwmoSmooth;
	private readonly StrategyParam<decimal> _vwmoThreshold;
	private readonly StrategyParam<int> _adxLength;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<decimal> _tpMultiplier;
	private readonly StrategyParam<DataType> _candleType;
	private readonly List<(decimal close, decimal volume)> _history = new();
	private readonly Queue<decimal> _oscillator = new();
	private decimal? _previousVwmo;
	private decimal? _previousClose;
	private decimal _atr;
	private int _atrSamples;
	private Order _pendingOrder;
	private Unit _stopDistance;
	private Unit _targetDistance;
	private bool _protectionStarted;
	private AverageDirectionalIndex _adx;

	public int VwmoMomentum { get => _vwmoMomentum.Value; set => _vwmoMomentum.Value = value; }
	public int VwmoVolume { get => _vwmoVolume.Value; set => _vwmoVolume.Value = value; }
	public int VwmoSmooth { get => _vwmoSmooth.Value; set => _vwmoSmooth.Value = value; }
	public decimal VwmoThreshold { get => _vwmoThreshold.Value; set => _vwmoThreshold.Value = value; }
	public int AdxLength { get => _adxLength.Value; set => _adxLength.Value = value; }
	public decimal AdxThreshold { get => _adxThreshold.Value; set => _adxThreshold.Value = value; }
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }
	public decimal TpMultiplier { get => _tpMultiplier.Value; set => _tpMultiplier.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ExodusStrategy()
	{
		_vwmoMomentum = Param(nameof(VwmoMomentum), 10).SetGreaterThanZero().SetDisplay("VWMO Momentum", "Momentum difference lag, in candles.", "VWMO");
		_vwmoVolume = Param(nameof(VwmoVolume), 20).SetGreaterThanZero().SetDisplay("VWMO Volume", "Window for volume-weighted momentum.", "VWMO");
		_vwmoSmooth = Param(nameof(VwmoSmooth), 3).SetGreaterThanZero().SetDisplay("VWMO Smooth", "SMA period for normalized oscillator.", "VWMO");
		_vwmoThreshold = Param(nameof(VwmoThreshold), 25m).SetNotNegative().SetDisplay("VWMO Threshold", "Absolute normalized oscillator entry threshold.", "VWMO");
		_adxLength = Param(nameof(AdxLength), 14).SetGreaterThanZero().SetDisplay("ADX Length", "Average Directional Index period.", "Strength");
		_adxThreshold = Param(nameof(AdxThreshold), 25m).SetNotNegative().SetDisplay("ADX Threshold", "Minimum trend strength for entry.", "Strength");
		_atrLength = Param(nameof(AtrLength), 14).SetGreaterThanZero().SetDisplay("ATR Length", "Wilder ATR period for frozen protection.", "Protection");
		_atrMultiplier = Param(nameof(AtrMultiplier), 2m).SetGreaterThanZero().SetDisplay("ATR Multiplier", "Stop-loss distance as an ATR multiple.", "Protection");
		_tpMultiplier = Param(nameof(TpMultiplier), 3m).SetGreaterThanZero().SetDisplay("TP Multiplier", "Take-profit distance as an additional stop multiple.", "Protection");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame()).SetDisplay("Candle Type", "Source candles.", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType), (Security, DataType.Level1)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_history.Clear();
		_oscillator.Clear();
		_previousVwmo = null;
		_previousClose = null;
		_atr = 0m;
		_atrSamples = 0;
		_pendingOrder = null;
		_protectionStarted = false;
		_stopDistance = null;
		_targetDistance = null;
		_adx = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_adx = new AverageDirectionalIndex { Length = AdxLength };
		_stopDistance = new Unit(0m, UnitTypes.Absolute);
		_targetDistance = new Unit(0m, UnitTypes.Absolute);
		OrderRegistering += order => _pendingOrder = order;

		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Bind(ObserveProtectionQuote).Start();
		}

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _adx);
			DrawOwnTrades(area);
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// Native protection is evaluated on the quote stream between signal candles.
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var adxResult = (AverageDirectionalIndexValue)_adx.Process(candle);
		var trueRange = _previousClose is decimal previous
			? Math.Max(candle.HighPrice - candle.LowPrice,
				Math.Max(Math.Abs(candle.HighPrice - previous), Math.Abs(candle.LowPrice - previous)))
			: candle.HighPrice - candle.LowPrice;
		_atrSamples++;
		var atrCount = Math.Min(_atrSamples, AtrLength);
		_atr = (_atr * (atrCount - 1) + trueRange) / atrCount;
		_previousClose = candle.ClosePrice;

		_history.Add((candle.ClosePrice, candle.TotalVolume));
		var needed = VwmoMomentum + VwmoVolume;
		if (_history.Count > needed) _history.RemoveAt(0);
		if (_history.Count < needed || !adxResult.IsFormed || adxResult.MovingAverage is not decimal strength)
			return;

		var numerator = 0m;
		var denominator = 0m;
		for (var i = _history.Count - VwmoVolume; i < _history.Count; i++)
		{
			var weightedDelta = (_history[i].close - _history[i - VwmoMomentum].close) * _history[i].volume;
			numerator += weightedDelta;
			denominator += Math.Abs(weightedDelta);
		}
		_oscillator.Enqueue(denominator > 0m ? numerator * 100m / denominator : 0m);
		while (_oscillator.Count > VwmoSmooth) _oscillator.Dequeue();
		if (_oscillator.Count < VwmoSmooth)
			return;

		var vwmo = _oscillator.Average();
		var pendingActive = _pendingOrder is not null && _pendingOrder.State is not (OrderStates.Done or OrderStates.Failed);
		if (!pendingActive)
		{
			var strong = strength > AdxThreshold;
			var signalLong = strong && vwmo > VwmoThreshold;
			var signalShort = strong && vwmo < -VwmoThreshold;
			var zeroExit = Position > 0m && _previousVwmo is decimal longMomentum && longMomentum > 0m && vwmo <= 0m
				|| Position < 0m && _previousVwmo is decimal shortMomentum && shortMomentum < 0m && vwmo >= 0m;

			if (signalLong && Position <= 0m)
			{
				ConfigureProtection(_atr);
				BuyMarket(Volume + Math.Abs(Position));
			}
			else if (signalShort && Position >= 0m)
			{
				ConfigureProtection(_atr);
				SellMarket(Volume + Math.Abs(Position));
			}
			else if (zeroExit && Position != 0m)
			{
				if (Position > 0m) SellMarket(Position);
				else BuyMarket(Math.Abs(Position));
			}
		}
		_previousVwmo = vwmo;
	}

	private void ConfigureProtection(decimal atr)
	{
		if (atr <= 0m)
			throw new InvalidOperationException("ATR must be positive before placing a protected trade.");
		_stopDistance.Value = atr * AtrMultiplier;
		_targetDistance.Value = _stopDistance.Value * TpMultiplier;
		if (!_protectionStarted)
		{
			StartProtection(_targetDistance, _stopDistance, useMarketOrders: true, isLocalStop: true);
			_protectionStarted = true;
		}
	}
}
