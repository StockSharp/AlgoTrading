using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Moving Average Shift WaveTrend strategy.
/// The oscillator is a Hull MA of the change of the distance between price and a configurable moving average.
/// A long opens when price is above the MA, the oscillator is positive and rising, price is above the long-term EMA,
/// ATR is above its average, the candle is inside the trading hours and no long wave is active yet. Shorts mirror it.
/// A position closes when the oscillator turns against it while price is on the other side of the MA, when a
/// percentage trailing stop is hit, or by the percentage stop loss and take profit.
/// </summary>
public class MovingAverageShiftWaveTrendStrategy : Strategy
{
	/// <summary>
	/// Moving average types.
	/// </summary>
	public enum MaTypes
	{
		/// <summary>Simple moving average.</summary>
		SMA,
		/// <summary>Exponential moving average.</summary>
		EMA,
		/// <summary>Smoothed moving average.</summary>
		SMMA,
		/// <summary>Weighted moving average.</summary>
		WMA,
		/// <summary>Hull moving average.</summary>
		HMA,
	}

	private readonly StrategyParam<MaTypes> _maType;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<int> _oscLength;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _trailPercent;
	private readonly StrategyParam<int> _longMaLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<DataType> _candleType;

	private IIndicator _ma;
	private ExponentialMovingAverage _longEma;
	private AverageTrueRange _atr;
	private HullMovingAverage _osc;
	private SimpleMovingAverage _atrAverage;
	private readonly Queue<decimal> _diffs = new();
	private decimal? _prevOsc;
	private bool _inLongWave;
	private bool _inShortWave;
	private decimal _trailExtreme;

	/// <summary>
	/// Moving average type.
	/// </summary>
	public MaTypes MaType
	{
		get => _maType.Value;
		set => _maType.Value = value;
	}

	/// <summary>
	/// Moving average length.
	/// </summary>
	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	/// <summary>
	/// Oscillator length.
	/// </summary>
	public int OscLength
	{
		get => _oscLength.Value;
		set => _oscLength.Value = value;
	}

	/// <summary>
	/// Take profit percentage.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Stop loss percentage.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Trailing stop percentage.
	/// </summary>
	public decimal TrailPercent
	{
		get => _trailPercent.Value;
		set => _trailPercent.Value = value;
	}

	/// <summary>
	/// Long-term trend EMA length.
	/// </summary>
	public int LongMaLength
	{
		get => _longMaLength.Value;
		set => _longMaLength.Value = value;
	}

	/// <summary>
	/// ATR length.
	/// </summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <summary>
	/// First trading hour (UTC).
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Hour when trading stops (UTC, exclusive).
	/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public MovingAverageShiftWaveTrendStrategy()
	{
		_maType = Param(nameof(MaType), MaTypes.SMA)
			.SetDisplay("MA Type", "Moving average type", "Indicators");

		_maLength = Param(nameof(MaLength), 40)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "Moving average length", "Indicators");

		_oscLength = Param(nameof(OscLength), 15)
			.SetGreaterThanZero()
			.SetDisplay("Oscillator Length", "Change period and Hull MA length of the oscillator", "Indicators");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 1.5m)
			.SetNotNegative()
			.SetDisplay("Take Profit %", "Take profit percentage", "Risk");

		_stopLossPercent = Param(nameof(StopLossPercent), 1m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

		_trailPercent = Param(nameof(TrailPercent), 1m)
			.SetNotNegative()
			.SetDisplay("Trail %", "Trailing stop percentage", "Risk");

		_longMaLength = Param(nameof(LongMaLength), 200)
			.SetGreaterThanZero()
			.SetDisplay("Long MA Length", "Long-term trend EMA length", "Filters");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR length and its average length", "Filters");

		_startHour = Param(nameof(StartHour), 9)
			.SetRange(0, 23)
			.SetDisplay("Start Hour", "First trading hour (UTC)", "Time");

		_endHour = Param(nameof(EndHour), 17)
			.SetRange(0, 24)
			.SetDisplay("End Hour", "Hour when trading stops (UTC, exclusive)", "Time");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		ResetState();
	}

	private void ResetState()
	{
		_diffs.Clear();
		_prevOsc = null;
		_inLongWave = false;
		_inShortWave = false;
		_trailExtreme = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		_ma = CreateMa(MaType, MaLength);
		_longEma = new ExponentialMovingAverage { Length = LongMaLength };
		_atr = new AverageTrueRange { Length = AtrLength };
		_osc = new HullMovingAverage { Length = OscLength };
		_atrAverage = new SimpleMovingAverage { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_ma, _longEma, _atr, ProcessCandle)
			.Start();

		StartProtection(
			new Unit(TakeProfitPercent, UnitTypes.Percent),
			new Unit(StopLossPercent, UnitTypes.Percent),
			useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ma);
			DrawIndicator(area, _longEma);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
				DrawIndicator(oscillators, _osc);
		}
	}

	private static IIndicator CreateMa(MaTypes type, int length)
	{
		return type switch
		{
			MaTypes.EMA => new ExponentialMovingAverage { Length = length },
			MaTypes.SMMA => new SmoothedMovingAverage { Length = length },
			MaTypes.WMA => new WeightedMovingAverage { Length = length },
			MaTypes.HMA => new HullMovingAverage { Length = length },
			_ => new SimpleMovingAverage { Length = length },
		};
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue, decimal longEmaValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_ma.IsFormed || !_atr.IsFormed)
			return;

		var close = candle.ClosePrice;
		var time = candle.OpenTime;

		var atrAverageValue = _atrAverage.Process(new DecimalIndicatorValue(_atrAverage, atrValue, time) { IsFinal = true });

		// The oscillator smooths how far the price-to-MA distance moved over OscLength candles.
		var diff = close - maValue;
		_diffs.Enqueue(diff);
		if (_diffs.Count <= OscLength)
			return;

		var change = diff - _diffs.Dequeue();
		var oscValue = _osc.Process(new DecimalIndicatorValue(_osc, change, time) { IsFinal = true });

		if (!_osc.IsFormed || oscValue.IsEmpty)
			return;

		var osc = oscValue.ToDecimal();

		var prevOsc = _prevOsc;
		_prevOsc = osc;

		if (prevOsc is not decimal prev)
			return;

		var rising = osc > prev;
		var falling = osc < prev;

		// A wave ends when the oscillator crosses back through zero.
		if (osc <= 0)
			_inLongWave = false;
		if (osc >= 0)
			_inShortWave = false;

		if (!_atrAverage.IsFormed || atrAverageValue.IsEmpty || !_longEma.IsFormed || !IsFormedAndOnlineAndAllowTrading())
			return;

		var atrAverage = atrAverageValue.ToDecimal();

		if (Position > 0)
		{
			_trailExtreme = Math.Max(_trailExtreme, close);

			var trailHit = TrailPercent > 0 && close <= _trailExtreme * (1 - TrailPercent / 100m);
			if ((falling && close < maValue) || trailHit)
			{
				SellMarket(Position);
				return;
			}
		}
		else if (Position < 0)
		{
			_trailExtreme = _trailExtreme == 0 ? close : Math.Min(_trailExtreme, close);

			var trailHit = TrailPercent > 0 && close >= _trailExtreme * (1 + TrailPercent / 100m);
			if ((rising && close > maValue) || trailHit)
			{
				BuyMarket(-Position);
				return;
			}
		}

		var hour = candle.OpenTime.Hour;
		if (hour < StartHour || hour >= EndHour)
			return;

		var isVolatile = atrValue > atrAverage;

		if (close > maValue && osc > 0 && rising && close > longEmaValue && isVolatile && !_inLongWave && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_inLongWave = true;
			_trailExtreme = close;
		}
		else if (close < maValue && osc < 0 && falling && close < longEmaValue && isVolatile && !_inShortWave && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_inShortWave = true;
			_trailExtreme = close;
		}
	}
}
