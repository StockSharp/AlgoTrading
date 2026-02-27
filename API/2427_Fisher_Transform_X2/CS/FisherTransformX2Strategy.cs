using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Fisher Transform strategy using two Fisher indicators (trend + signal).
/// Trend Fisher defines direction; Signal Fisher generates entries.
/// Both use same timeframe but different lengths.
/// </summary>
public class FisherTransformX2Strategy : Strategy
{
	private readonly StrategyParam<int> _trendLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private EhlersFisherTransform _trendFisher;
	private EhlersFisherTransform _signalFisher;

	private decimal _prevTrend;
	private decimal _prevSignal;
	private decimal _prevPrevSignal;
	private int _trendDirection;
	private int _count;

	public int TrendLength { get => _trendLength.Value; set => _trendLength.Value = value; }
	public int SignalLength { get => _signalLength.Value; set => _signalLength.Value = value; }
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public FisherTransformX2Strategy()
	{
		_trendLength = Param(nameof(TrendLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Trend Length", "Fisher length for trend", "Parameters")
			.SetOptimize(10, 30, 2);

		_signalLength = Param(nameof(SignalLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Signal Length", "Fisher length for signal", "Parameters")
			.SetOptimize(5, 20, 1);

		_takeProfit = Param(nameof(TakeProfit), 2000m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take profit in price units", "Risk");

		_stopLoss = Param(nameof(StopLoss), 1000m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop loss in price units", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevTrend = 0m;
		_prevSignal = 0m;
		_prevPrevSignal = 0m;
		_trendDirection = 0;
		_count = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_trendFisher = new EhlersFisherTransform { Length = TrendLength };
		_signalFisher = new EhlersFisherTransform { Length = SignalLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_signalFisher, ProcessCandle)
			.Start();

		StartProtection(
			new Unit(TakeProfit, UnitTypes.Absolute),
			new Unit(StopLoss, UnitTypes.Absolute));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _signalFisher);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue signalResult)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Process trend Fisher manually with the candle
		var trendResult = _trendFisher.Process(candle);
		if (!_trendFisher.IsFormed || !_signalFisher.IsFormed)
			return;

		var signalVal = ((IEhlersFisherTransformValue)signalResult).MainLine ?? 0m;
		var trendVal = ((IEhlersFisherTransformValue)trendResult).MainLine ?? 0m;

		_count++;
		if (_count < 3)
		{
			_prevPrevSignal = _prevSignal;
			_prevSignal = signalVal;
			_prevTrend = trendVal;
			return;
		}

		// Update trend direction
		if (trendVal > _prevTrend)
			_trendDirection = 1;
		else if (trendVal < _prevTrend)
			_trendDirection = -1;

		// Signal crossover
		var signalCrossUp = signalVal > _prevSignal && _prevSignal <= _prevPrevSignal;
		var signalCrossDown = signalVal < _prevSignal && _prevSignal >= _prevPrevSignal;

		if (_trendDirection > 0 && signalCrossUp && Position <= 0)
			BuyMarket();
		else if (_trendDirection < 0 && signalCrossDown && Position >= 0)
			SellMarket();
		else if (_trendDirection < 0 && Position > 0)
			SellMarket();
		else if (_trendDirection > 0 && Position < 0)
			BuyMarket();

		_prevTrend = trendVal;
		_prevPrevSignal = _prevSignal;
		_prevSignal = signalVal;
	}
}
