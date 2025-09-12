using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Triple EMA with QQE filter and trailing stop.
/// </summary>
public class TripleEmaQqeTrendFollowingStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _rsiSmoothing;
	private readonly StrategyParam<decimal> _qqeFactor;
	private readonly StrategyParam<int> _tema1Length;
	private readonly StrategyParam<int> _tema2Length;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _ema1Tema1;
	private ExponentialMovingAverage _ema2Tema1;
	private ExponentialMovingAverage _ema3Tema1;
	private ExponentialMovingAverage _ema1Tema2;
	private ExponentialMovingAverage _ema2Tema2;
	private ExponentialMovingAverage _ema3Tema2;

	private RelativeStrengthIndex _rsi;
	private ExponentialMovingAverage _rsiMa;
	private ExponentialMovingAverage _maAtrRsi;
	private ExponentialMovingAverage _dar;

	private decimal _longband;
	private decimal _shortband;
	private int _trend;
	private int _qqeXlong;
	private int _qqeXshort;

	private decimal _tema2Prev;
	private decimal _prevRsiMa;
	private decimal? _stopLoss;

	public TripleEmaQqeTrendFollowingStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetDisplay("RSI Length", "RSI period", "QQE");

		_rsiSmoothing = Param(nameof(RsiSmoothing), 5)
			.SetDisplay("RSI Smoothing", "RSI smoothing period", "QQE");

		_qqeFactor = Param(nameof(QqeFactor), 4.238m)
			.SetDisplay("QQE Factor", "QQE factor", "QQE");

		_tema1Length = Param(nameof(Tema1Length), 20)
			.SetDisplay("TEMA #1 Length", "Length of the first TEMA", "TEMA");

		_tema2Length = Param(nameof(Tema2Length), 40)
			.SetDisplay("TEMA #2 Length", "Length of the second TEMA", "TEMA");

		_stopLossPips = Param(nameof(StopLossPips), 120)
			.SetDisplay("Stop Loss (pips)", "Trailing stop in pips", "Risk");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	public int RsiSmoothing
	{
		get => _rsiSmoothing.Value;
		set => _rsiSmoothing.Value = value;
	}

	public decimal QqeFactor
	{
		get => _qqeFactor.Value;
		set => _qqeFactor.Value = value;
	}

	public int Tema1Length
	{
		get => _tema1Length.Value;
		set => _tema1Length.Value = value;
	}

	public int Tema2Length
	{
		get => _tema2Length.Value;
		set => _tema2Length.Value = value;
	}

	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
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

		_longband = default;
		_shortband = default;
		_trend = default;
		_qqeXlong = default;
		_qqeXshort = default;
		_tema2Prev = default;
		_prevRsiMa = default;
		_stopLoss = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ema1Tema1 = new() { Length = Tema1Length };
		_ema2Tema1 = new() { Length = Tema1Length };
		_ema3Tema1 = new() { Length = Tema1Length };

		_ema1Tema2 = new() { Length = Tema2Length };
		_ema2Tema2 = new() { Length = Tema2Length };
		_ema3Tema2 = new() { Length = Tema2Length };

		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		_rsiMa = new ExponentialMovingAverage { Length = RsiSmoothing };

		var wilders = RsiPeriod * 2 - 1;
		_maAtrRsi = new ExponentialMovingAverage { Length = wilders };
		_dar = new ExponentialMovingAverage { Length = wilders };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

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

		var price = candle.ClosePrice;
		var time = candle.OpenTime;

		var tema1 = CalcTema(_ema1Tema1, _ema2Tema1, _ema3Tema1, price, time);
		var tema2 = CalcTema(_ema1Tema2, _ema2Tema2, _ema3Tema2, price, time);

		var rsi = _rsi.Process(candle).ToDecimal();
		if (!_rsi.IsFormed)
		{
			_tema2Prev = tema2;
			_prevRsiMa = rsi;
			return;
		}

		var rsiMa = _rsiMa.Process(rsi, time, true).ToDecimal();
		if (!_rsiMa.IsFormed)
		{
			_tema2Prev = tema2;
			_prevRsiMa = rsiMa;
			return;
		}

		var atrRsi = Math.Abs(_prevRsiMa - rsiMa);
		var maAtrRsi = _maAtrRsi.Process(atrRsi, time, true).ToDecimal();
		if (!_maAtrRsi.IsFormed)
		{
			_tema2Prev = tema2;
			_prevRsiMa = rsiMa;
			return;
		}

		var dar = _dar.Process(maAtrRsi, time, true).ToDecimal();
		if (!_dar.IsFormed)
		{
			_tema2Prev = tema2;
			_prevRsiMa = rsiMa;
			return;
		}

		var delta = dar * QqeFactor;
		var newLong = rsiMa - delta;
		var newShort = rsiMa + delta;

		var prevLong = _longband;
		var prevShort = _shortband;
		var prevTrend = _trend;

		_longband = _prevRsiMa > prevLong && rsiMa > prevLong ? Math.Max(prevLong, newLong) : newLong;
		_shortband = _prevRsiMa < prevShort && rsiMa < prevShort ? Math.Min(prevShort, newShort) : newShort;

		if (rsiMa > _shortband && _prevRsiMa <= prevShort)
			_trend = 1;
		else if (rsiMa < _longband && _prevRsiMa >= prevLong)
			_trend = -1;
		else
			_trend = prevTrend;

		var fastAtrRsiTl = _trend == 1 ? _longband : _shortband;

		if (fastAtrRsiTl < rsiMa)
		{
			_qqeXlong++;
			_qqeXshort = 0;
		}
		else
		{
			_qqeXshort++;
			_qqeXlong = 0;
		}

		var qqeLong = _qqeXlong == 1;
		var qqeShort = _qqeXshort == 1;

		var step = Security.PriceStep ?? 1m;

		var longCond = price > tema1 && tema1 > tema2 && tema2 > _tema2Prev && qqeLong && time.DayOfWeek != DayOfWeek.Monday;
		var shortCond = price < tema1 && tema1 < tema2 && tema2 < _tema2Prev && qqeShort && time.DayOfWeek != DayOfWeek.Monday;

		if (longCond && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_stopLoss = price - StopLossPips * step;
		}
		else if (shortCond && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_stopLoss = price + StopLossPips * step;
		}
		else if (Position > 0)
		{
			_stopLoss = Math.Max(_stopLoss ?? price, price - StopLossPips * step);
			if (price < _stopLoss)
				SellMarket(Position);
		}
		else if (Position < 0)
		{
			_stopLoss = Math.Min(_stopLoss ?? price, price + StopLossPips * step);
			if (price > _stopLoss)
				BuyMarket(-Position);
		}

		_tema2Prev = tema2;
		_prevRsiMa = rsiMa;
	}

	private static decimal CalcTema(ExponentialMovingAverage ema1, ExponentialMovingAverage ema2, ExponentialMovingAverage ema3, decimal price, DateTimeOffset time)
	{
		var e1 = ema1.Process(price, time, true).ToDecimal();
		var e2 = ema2.Process(e1, time, true).ToDecimal();
		var e3 = ema3.Process(e2, time, true).ToDecimal();
		return 3m * (e1 - e2) + e3;
	}
}
