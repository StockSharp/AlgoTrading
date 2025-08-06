namespace StockSharp.Samples.Strategies;

using System;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// QQE Signals Strategy
/// </summary>
public class QqeSignalsStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleTypeParam;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _rsiSmoothing;
	private readonly StrategyParam<decimal> _qqeFactor;
	private readonly StrategyParam<decimal> _threshold;

	private RelativeStrengthIndex _rsi;
	private ExponentialMovingAverage _rsiMa;
	private ExponentialMovingAverage _atrRsi;
	private ExponentialMovingAverage _maAtrRsi;
	private ExponentialMovingAverage _dar;

	private decimal _longband;
	private decimal _shortband;
	private int _trend;
	private int _qqeXlong;
	private int _qqeXshort;

	public QqeSignalsStrategy()
	{
		_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle type", "Candle type for strategy calculation.", "General");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetDisplay("RSI Length", "RSI period", "QQE");

		_rsiSmoothing = Param(nameof(RsiSmoothing), 5)
			.SetDisplay("RSI Smoothing", "RSI smoothing period", "QQE");

		_qqeFactor = Param(nameof(QqeFactor), 4.238m)
			.SetDisplay("Fast QQE Factor", "QQE factor", "QQE");

		_threshold = Param(nameof(Threshold), 10m)
			.SetDisplay("Threshold", "Threshold value", "QQE");
	}

	public DataType CandleType
	{
		get => _candleTypeParam.Value;
		set => _candleTypeParam.Value = value;
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

	public decimal Threshold
	{
		get => _threshold.Value;
		set => _threshold.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Initialize indicators
		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		_rsiMa = new ExponentialMovingAverage { Length = RsiSmoothing };
		
		var wildersPeriod = RsiPeriod * 2 - 1;
		_atrRsi = new ExponentialMovingAverage { Length = 1 }; // For calculating absolute difference
		_maAtrRsi = new ExponentialMovingAverage { Length = wildersPeriod };
		_dar = new ExponentialMovingAverage { Length = wildersPeriod };

		// Subscribe to candles
		var subscription = this.SubscribeCandles(CandleType);
		subscription
			.WhenCandlesFinished()
			.Do(ProcessCandle)
			.Apply(this);

		subscription.Start();

		// Setup chart
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Skip if strategy is not ready
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Calculate RSI
		var rsiValue = _rsi.Process(candle);
		if (!_rsi.IsFormed)
			return;

		// Calculate smoothed RSI
		var rsiMaValue = _rsiMa.Process(rsiValue);
		if (!_rsiMa.IsFormed)
			return;

		var rsIndex = rsiMaValue.GetValue<decimal>();

		// Calculate ATR of RSI
		var prevRsiMa = _rsiMa.GetValue(1);
		var atrRsiValue = Math.Abs(prevRsiMa - rsIndex);
		
		// Calculate MA of ATR RSI
		var maAtrRsiValue = _maAtrRsi.Process(atrRsiValue);
		if (!_maAtrRsi.IsFormed)
			return;

		// Calculate DAR
		var darValue = _dar.Process(maAtrRsiValue);
		if (!_dar.IsFormed)
			return;

		var deltaFastAtrRsi = darValue.GetValue<decimal>() * QqeFactor;

		// Calculate bands
		var newshortband = rsIndex + deltaFastAtrRsi;
		var newlongband = rsIndex - deltaFastAtrRsi;

		// Update bands
		var prevLongband = _longband;
		var prevShortband = _shortband;
		var prevRsIndex = _rsiMa.GetValue(1);

		if (prevRsIndex > prevLongband && rsIndex > prevLongband)
		{
			_longband = Math.Max(prevLongband, newlongband);
		}
		else
		{
			_longband = newlongband;
		}

		if (prevRsIndex < prevShortband && rsIndex < prevShortband)
		{
			_shortband = Math.Min(prevShortband, newshortband);
		}
		else
		{
			_shortband = newshortband;
		}

		// Determine trend
		var prevTrend = _trend;
		
		if (rsIndex > _shortband && prevRsIndex <= prevShortband)
		{
			_trend = 1;
		}
		else if (rsIndex < _longband && prevRsIndex >= prevLongband)
		{
			_trend = -1;
		}

		// Calculate FastAtrRsiTL
		var fastAtrRsiTL = _trend == 1 ? _longband : _shortband;

		// Update QQE crosses
		if (fastAtrRsiTL < rsIndex)
		{
			_qqeXlong++;
			_qqeXshort = 0;
		}
		else
		{
			_qqeXshort++;
			_qqeXlong = 0;
		}

		// Generate signals
		var qqeLong = _qqeXlong == 1;
		var qqeShort = _qqeXshort == 1;

		// Execute trades
		if (qqeLong && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (qqeShort && Position > 0)
		{
			ClosePosition();
		}
	}
}