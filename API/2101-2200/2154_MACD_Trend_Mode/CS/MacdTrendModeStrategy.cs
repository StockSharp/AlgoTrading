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
/// MACD strategy with selectable trend detection modes.
/// </summary>
public class MacdTrendModeStrategy : Strategy
{
	public enum TrendModes { Histogram, Cloud, Zero }

	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<TrendModes> _trendMode;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevHist, _prevPrevHist;
	private bool _hasPrevHist, _hasPrevPrevHist;
	private decimal _prevMacd, _prevSignal;
	private bool _hasPrevLines;

	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public int SignalLength { get => _signalLength.Value; set => _signalLength.Value = value; }
	public TrendModes TrendMode { get => _trendMode.Value; set => _trendMode.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MacdTrendModeStrategy()
	{
		_fastLength = Param(nameof(FastLength), 12).SetGreaterThanZero()
			.SetDisplay("Fast Length", "Fast EMA period", "MACD");
		_slowLength = Param(nameof(SlowLength), 26).SetGreaterThanZero()
			.SetDisplay("Slow Length", "Slow EMA period", "MACD");
		_signalLength = Param(nameof(SignalLength), 9).SetGreaterThanZero()
			.SetDisplay("Signal Length", "Signal line period", "MACD");
		_trendMode = Param(nameof(TrendMode), TrendModes.Cloud)
			.SetDisplay("Trend Mode", "Trend detection mode", "General");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevHist = default;
		_prevPrevHist = default;
		_hasPrevHist = false;
		_hasPrevPrevHist = false;
		_prevMacd = default;
		_prevSignal = default;
		_hasPrevLines = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd = { ShortMa = { Length = FastLength }, LongMa = { Length = SlowLength } },
			SignalMa = { Length = SignalLength }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(macd, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, macd);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!macdValue.IsFinal || !macdValue.IsFormed)
			return;

		var macdTyped = (IMovingAverageConvergenceDivergenceSignalValue)macdValue;
		if (macdTyped.Macd is not decimal macdVal || macdTyped.Signal is not decimal signalVal)
			return;

		var hist = macdVal - signalVal;
		var buySignal = false;
		var sellSignal = false;

		switch (TrendMode)
		{
			case TrendModes.Histogram:
				if (_hasPrevHist && _hasPrevPrevHist)
				{
					if (_prevHist < _prevPrevHist && hist > _prevHist) buySignal = true;
					if (_prevHist > _prevPrevHist && hist < _prevHist) sellSignal = true;
				}
				_prevPrevHist = _prevHist;
				_hasPrevPrevHist = _hasPrevHist;
				_prevHist = hist;
				_hasPrevHist = true;
				break;

			case TrendModes.Cloud:
				if (_hasPrevLines)
				{
					if (_prevMacd <= _prevSignal && macdVal > signalVal) buySignal = true;
					if (_prevMacd >= _prevSignal && macdVal < signalVal) sellSignal = true;
				}
				_prevMacd = macdVal;
				_prevSignal = signalVal;
				_hasPrevLines = true;
				break;

			case TrendModes.Zero:
				if (_hasPrevHist)
				{
					if (_prevHist <= 0m && hist > 0m) buySignal = true;
					if (_prevHist >= 0m && hist < 0m) sellSignal = true;
				}
				_prevHist = hist;
				_hasPrevHist = true;
				break;
		}

		if (buySignal && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		else if (sellSignal && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}
	}
}
