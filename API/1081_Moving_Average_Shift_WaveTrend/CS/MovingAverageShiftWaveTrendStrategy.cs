using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Moving Average Shift WaveTrend Strategy.
/// </summary>
public class MovingAverageShiftWaveTrendStrategy : Strategy
{
	private readonly StrategyParam<string> _maType;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<int> _oscLength;
	private readonly StrategyParam<decimal> _tpPercent;
	private readonly StrategyParam<decimal> _slPercent;
	private readonly StrategyParam<decimal> _trailPercent;
	private readonly StrategyParam<int> _longMaLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<DataType> _candleType;

	private RateOfChange _roc;
	private HullMovingAverage _osc;
	private bool _inWave;
	private decimal? _prevOsc;
	private decimal? _prevLongMa;
	private decimal? _highestSinceEntry;
	private decimal? _lowestSinceEntry;

	/// <summary>
	/// Moving average type.
	/// </summary>
	public string MaType { get => _maType.Value; set => _maType.Value = value; }

	/// <summary>
	/// Moving average length.
	/// </summary>
	public int MaLength { get => _maLength.Value; set => _maLength.Value = value; }

	/// <summary>
	/// Oscillator ROC length.
	/// </summary>
	public int OscLength { get => _oscLength.Value; set => _oscLength.Value = value; }

	/// <summary>
	/// Take profit percent.
	/// </summary>
	public decimal TakeProfitPercent { get => _tpPercent.Value; set => _tpPercent.Value = value; }

	/// <summary>
	/// Stop loss percent.
	/// </summary>
	public decimal StopLossPercent { get => _slPercent.Value; set => _slPercent.Value = value; }

	/// <summary>
	/// Trailing stop percent.
	/// </summary>
	public decimal TrailPercent { get => _trailPercent.Value; set => _trailPercent.Value = value; }

	/// <summary>
	/// Trend MA length.
	/// </summary>
	public int LongMaLength { get => _longMaLength.Value; set => _longMaLength.Value = value; }

	/// <summary>
	/// ATR length.
	/// </summary>
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }

	/// <summary>
	/// Start hour.
	/// </summary>
	public int StartHour { get => _startHour.Value; set => _startHour.Value = value; }

	/// <summary>
	/// End hour.
	/// </summary>
	public int EndHour { get => _endHour.Value; set => _endHour.Value = value; }

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initialize <see cref="MovingAverageShiftWaveTrendStrategy"/>.
	/// </summary>
	public MovingAverageShiftWaveTrendStrategy()
	{
		_maType = Param(nameof(MaType), "SMA").SetDisplay("MA Type", "Moving average type", "MA");
		_maLength = Param(nameof(MaLength), 40).SetDisplay("MA Length", "Length for MA", "MA");
		_oscLength = Param(nameof(OscLength), 15).SetDisplay("Osc Length", "ROC length for oscillator", "Oscillator");
		_tpPercent = Param(nameof(TakeProfitPercent), 1.5m).SetDisplay("Take Profit %", "Take profit percentage", "Risk");
		_slPercent = Param(nameof(StopLossPercent), 1m).SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");
		_trailPercent = Param(nameof(TrailPercent), 1m).SetDisplay("Trail Stop %", "Trailing stop percent", "Risk");
		_longMaLength = Param(nameof(LongMaLength), 200).SetDisplay("Trend MA Length", "Length for trend MA", "Trend Filter");
		_atrLength = Param(nameof(AtrLength), 14).SetDisplay("ATR Length", "Length for ATR", "Volatility Filter");
		_startHour = Param(nameof(StartHour), 9).SetDisplay("Start Hour", "Start hour", "Time Filter");
		_endHour = Param(nameof(EndHour), 17).SetDisplay("End Hour", "End hour", "Time Filter");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame()).SetDisplay("Candle Type", "Type of candles", "General");
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
		_roc = null;
		_osc = null;
		_inWave = false;
		_prevOsc = null;
		_prevLongMa = null;
		_highestSinceEntry = null;
		_lowestSinceEntry = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var ma = CreateMa(MaType, MaLength);
		ma.CandlePrice = CandlePrice.Median;

		var longMa = new ExponentialMovingAverage { Length = LongMaLength, CandlePrice = CandlePrice.Median };

		var atr = new AverageTrueRange { Length = AtrLength };
		var atrSma = new SimpleMovingAverage { Length = AtrLength };

		_roc = new RateOfChange { Length = OscLength };
		_osc = new HullMovingAverage { Length = 10 };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ma, longMa, atr, atrSma, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ma);
			DrawIndicator(area, longMa);
			DrawOwnTrades(area);
		}

		StartProtection(new Unit(TakeProfitPercent, UnitTypes.Percent), new Unit(StopLossPercent, UnitTypes.Percent));
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue, decimal longMaValue, decimal atrValue, decimal atrSmaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var median = (candle.HighPrice + candle.LowPrice) / 2m;

		var rocValue = _roc.Process(median - maValue, candle.OpenTime, true);
		if (!rocValue.IsFormed)
		{
			_prevLongMa = longMaValue;
			return;
		}

		var oscValue = _osc.Process(rocValue.ToDecimal(), candle.OpenTime, true);
		if (!oscValue.IsFormed)
		{
			_prevOsc = oscValue.ToDecimal();
			_prevLongMa = longMaValue;
			return;
		}

		var osc = oscValue.ToDecimal();

		var waveReset = _prevOsc is decimal prev && (osc > 0m && prev <= 0m || osc < 0m && prev >= 0m);
		if (waveReset)
			_inWave = false;

		var maGreen = median >= maValue;
		var maRed = median < maValue;
		var oscGreen = osc > 0m && _prevOsc is decimal po1 && osc > po1;
		var oscRed = osc < 0m && _prevOsc is decimal po2 && osc < po2;
		var longTrend = _prevLongMa is decimal pl && longMaValue > pl;
		var shortTrend = _prevLongMa is decimal sl && longMaValue < sl;
		var volOk = atrValue > atrSmaValue;
		var hour = candle.OpenTime.Hour;
		var hourOk = hour >= StartHour && hour < EndHour;

		if (maGreen && oscGreen && longTrend && !_inWave && volOk && hourOk && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_inWave = true;
			_highestSinceEntry = candle.HighPrice;
			_lowestSinceEntry = null;
		}
		else if (maRed && oscRed && shortTrend && !_inWave && volOk && hourOk && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_inWave = true;
			_lowestSinceEntry = candle.LowPrice;
			_highestSinceEntry = null;
		}

		if (Position > 0)
		{
			_highestSinceEntry = _highestSinceEntry is decimal h ? Math.Max(h, candle.HighPrice) : candle.HighPrice;

			var trailStop = _highestSinceEntry.Value * (1m - TrailPercent / 100m);
			if (candle.ClosePrice < trailStop)
			{
				ClosePosition();
				_inWave = false;
			}
			else if (oscRed && median < maValue)
			{
				ClosePosition();
				_inWave = false;
			}
		}
		else if (Position < 0)
		{
			_lowestSinceEntry = _lowestSinceEntry is decimal l ? Math.Min(l, candle.LowPrice) : candle.LowPrice;

			var trailStop = _lowestSinceEntry.Value * (1m + TrailPercent / 100m);
			if (candle.ClosePrice > trailStop)
			{
				ClosePosition();
				_inWave = false;
			}
			else if (oscGreen && median > maValue)
			{
				ClosePosition();
				_inWave = false;
			}
		}

		_prevOsc = osc;
		_prevLongMa = longMaValue;
	}

	private MovingAverage CreateMa(string type, int length)
	{
		return type switch
		{
			"EMA" => new ExponentialMovingAverage { Length = length },
			"SMMA (RMA)" => new SmoothedMovingAverage { Length = length },
			"WMA" => new WeightedMovingAverage { Length = length },
			"VWMA" => new VolumeWeightedMovingAverage { Length = length },
			_ => new SimpleMovingAverage { Length = length },
		};
	}
}
