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
/// Strategy based on sequential RSI normalization and smoothed StochRSI K/D crossover.
/// Buys when %K crosses above %D in oversold zone.
/// Sells when %K crosses below %D in overbought zone.
/// </summary>
public class StochasticRsiCrossStrategy : Strategy
{
	private readonly StrategyParam<int> _kPeriod;
	private readonly StrategyParam<int> _dPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _stochPeriod;
	private readonly StrategyParam<decimal> _stopLossPercent;

	private decimal _prevK;
	private decimal _prevD;
	private bool _hasPrevValues;
	private Order _pendingOrder;
	private RelativeStrengthIndex _rsi;
	private SimpleMovingAverage _kAverage;
	private SimpleMovingAverage _dAverage;
	private readonly Queue<decimal> _rsiWindow = new();

	/// <summary>
	/// K period.
	/// </summary>
	public int KPeriod
	{
		get => _kPeriod.Value;
		set => _kPeriod.Value = value;
	}

	/// <summary>
	/// D period.
	/// </summary>
	public int DPeriod
	{
		get => _dPeriod.Value;
		set => _dPeriod.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public int StochPeriod { get => _stochPeriod.Value; set => _stochPeriod.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="StochasticRsiCrossStrategy"/>.
	/// </summary>
	public StochasticRsiCrossStrategy()
	{
		_kPeriod = Param(nameof(KPeriod), 3).SetGreaterThanZero()
			.SetDisplay("K Period", "SMA period of raw StochRSI.", "Indicators")
			.SetOptimize(1, 5, 1);

		_dPeriod = Param(nameof(DPeriod), 3).SetGreaterThanZero()
			.SetDisplay("D Period", "SMA period of formed K values.", "Indicators")
			.SetOptimize(3, 5, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
		_rsiPeriod = Param(nameof(RsiPeriod), 14).SetGreaterThanZero()
			.SetDisplay("RSI Period", "Period of native close-price RSI.", "Indicators");
		_stochPeriod = Param(nameof(StochPeriod), 14).SetGreaterThanZero()
			.SetDisplay("Stochastic Period", "Rolling range of formed RSI values.", "Indicators");
		_stopLossPercent = Param(nameof(StopLossPercent), 2m).SetNotNegative()
			.SetDisplay("Stop Loss (%)", "Actual-fill percent stop; zero disables it.", "Protection");
		OrderRegistering += order => _pendingOrder = order;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (Security, DataType.Level1)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevK = default;
		_prevD = default;
		_hasPrevValues = default;
		_pendingOrder = null;
		_rsiWindow.Clear();
		_rsi = null;
		_kAverage = _dAverage = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		_kAverage = new SimpleMovingAverage { Length = KPeriod };
		_dAverage = new SimpleMovingAverage { Length = DPeriod };
		Indicators.Add(_kAverage);
		Indicators.Add(_dAverage);
		StartProtection(new Unit(), new Unit(StopLossPercent, UnitTypes.Percent), useMarketOrders: true, isLocalStop: true);
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Bind(ObserveProtectionQuote).Start();
		}

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _rsi);
			DrawIndicator(area, _kAverage);
			DrawIndicator(area, _dAverage);
			DrawOwnTrades(area);
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// Native protection runs before the callback, including between finished candles.
	}

	private static decimal Feed(SimpleMovingAverage average, decimal value, DateTime time)
		=> average.Process(new DecimalIndicatorValue(average, value, time) { IsFinal = true }).GetValue<decimal>();

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished || !_rsi.IsFormed)
			return;

		// Normalize formed RSI readings, not candle High/Low. Warm each stage sequentially.
		_rsiWindow.Enqueue(rsiValue);
		if (_rsiWindow.Count > StochPeriod)
			_rsiWindow.Dequeue();
		if (_rsiWindow.Count < StochPeriod)
			return;

		var low = _rsiWindow.Min();
		var high = _rsiWindow.Max();
		var raw = high == low ? 50m : 100m * (rsiValue - low) / (high - low);
		var k = Feed(_kAverage, raw, candle.OpenTime);
		if (!_kAverage.IsFormed)
			return;
		var d = Feed(_dAverage, k, candle.OpenTime);
		if (!_dAverage.IsFormed || !IsFormedAndOnlineAndAllowTrading())
			return;
		// Rolling decimal sums leave sub-tick residue at equal K/D plateaus. Quantize decisions,
		// not the values fed into the next SMA, so equality cannot manufacture or suppress crosses.
		k = Math.Round(k, 8);
		d = Math.Round(d, 8);

		if (!_hasPrevValues)
		{
			_hasPrevValues = true;
			_prevK = k;
			_prevD = d;
			return;
		}

		if (_pendingOrder is not null && _pendingOrder.State is not (OrderStates.Done or OrderStates.Failed))
		{
			_prevK = k;
			_prevD = d;
			return;
		}

		var crossedUp = _prevK <= _prevD && k > d;
		var crossedDown = _prevK >= _prevD && k < d;
		if (crossedUp && k < 20m && Position <= 0m)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
		}
		// %K crosses below %D in overbought zone (> 80) - sell
		else if (crossedDown && k > 80m && Position >= 0m)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
		}
		else if (Position > 0m && crossedDown)
			SellMarket(Position);
		else if (Position < 0m && crossedUp)
			BuyMarket(Math.Abs(Position));

		_prevK = k;
		_prevD = d;
	}
}
