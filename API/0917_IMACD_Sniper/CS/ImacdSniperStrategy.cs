using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// IMACD Sniper strategy combining MACD crossovers, EMA trend filter,
/// volume confirmation and dynamic take-profit/stop-loss.
/// </summary>
public class ImacdSniperStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<decimal> _macdDeltaMin;
	private readonly StrategyParam<decimal> _macdZeroLimit;
	private readonly StrategyParam<int> _rangeLength;
	private readonly StrategyParam<decimal> _rangeMultiplierTp;
	private readonly StrategyParam<decimal> _rangeMultiplierSl;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<DataType> _candleType;

	private readonly SimpleMovingAverage _volumeMa = new() { Length = 20 };
	private SimpleMovingAverage _rangeMa;

	private decimal _prevMacd;
	private decimal _prevSignal;
	private bool _isFirst = true;
	private decimal _targetPrice;
	private decimal _stopPrice;

	/// <summary>
	/// MACD fast length.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// MACD slow length.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// MACD signal length.
	/// </summary>
	public int SignalLength
	{
		get => _signalLength.Value;
		set => _signalLength.Value = value;
	}

	/// <summary>
	/// Minimum MACD delta for entry.
	/// </summary>
	public decimal MacdDeltaMin
	{
		get => _macdDeltaMin.Value;
		set => _macdDeltaMin.Value = value;
	}

	/// <summary>
	/// Minimum distance from zero line.
	/// </summary>
	public decimal MacdZeroLimit
	{
		get => _macdZeroLimit.Value;
		set => _macdZeroLimit.Value = value;
	}

	/// <summary>
	/// Length for average range calculation.
	/// </summary>
	public int RangeLength
	{
		get => _rangeLength.Value;
		set => _rangeLength.Value = value;
	}

	/// <summary>
	/// Take-profit multiplier of the range.
	/// </summary>
	public decimal RangeMultiplierTp
	{
		get => _rangeMultiplierTp.Value;
		set => _rangeMultiplierTp.Value = value;
	}

	/// <summary>
	/// Stop-loss multiplier of the range.
	/// </summary>
	public decimal RangeMultiplierSl
	{
		get => _rangeMultiplierSl.Value;
		set => _rangeMultiplierSl.Value = value;
	}

	/// <summary>
	/// EMA length.
	/// </summary>
	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
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
	public ImacdSniperStrategy()
	{
		_fastLength = Param(nameof(FastLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast Length", "MACD fast period", "MACD")
			.SetCanOptimize(true)
			.SetOptimize(6, 24, 2);

		_slowLength = Param(nameof(SlowLength), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow Length", "MACD slow period", "MACD")
			.SetCanOptimize(true)
			.SetOptimize(20, 40, 2);

		_signalLength = Param(nameof(SignalLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal Length", "MACD signal smoothing", "MACD")
			.SetCanOptimize(true)
			.SetOptimize(5, 15, 1);

		_macdDeltaMin = Param(nameof(MacdDeltaMin), 0.03m)
			.SetGreaterThanZero()
			.SetDisplay("Min MACD Delta", "Minimum MACD difference for entry", "Filters")
			.SetCanOptimize(true)
			.SetOptimize(0.01m, 0.1m, 0.01m);

		_macdZeroLimit = Param(nameof(MacdZeroLimit), 0.05m)
			.SetGreaterThanZero()
			.SetDisplay("MACD Zero Limit", "Minimum distance from zero for entry", "Filters")
			.SetCanOptimize(true)
			.SetOptimize(0.01m, 0.1m, 0.01m);

		_rangeLength = Param(nameof(RangeLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("Range Length", "Number of candles for range average", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 1);

		_rangeMultiplierTp = Param(nameof(RangeMultiplierTp), 4m)
			.SetGreaterThanZero()
			.SetDisplay("Range Multiplier TP", "TP multiplier of range", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(1m, 6m, 1m);

		_rangeMultiplierSl = Param(nameof(RangeMultiplierSl), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Range Multiplier SL", "SL multiplier of range", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(1m, 3m, 0.5m);

		_emaLength = Param(nameof(EmaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "EMA period", "Trend")
			.SetCanOptimize(true)
			.SetOptimize(10, 50, 5);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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
		_volumeMa.Reset();
		_rangeMa?.Reset();
		_prevMacd = default;
		_prevSignal = default;
		_isFirst = true;
		_targetPrice = default;
		_stopPrice = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_rangeMa = new SimpleMovingAverage { Length = RangeLength };

		var ema = new ExponentialMovingAverage { Length = EmaLength };
		var macd = new MovingAverageConvergenceDivergence
		{
			ShortPeriod = FastLength,
			LongPeriod = SlowLength,
			SignalPeriod = SignalLength
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(macd, ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);

			var macdArea = CreateChartArea();
			DrawIndicator(macdArea, macd);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal macd, decimal signal, decimal histogram, decimal ema)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var volumeValue = _volumeMa.Process(new DecimalIndicatorValue(_volumeMa, candle.TotalVolume, candle.OpenTime));
		if (!volumeValue.IsFinal)
		{
			_prevMacd = macd;
			_prevSignal = signal;
			return;
		}
		var volumeAvg = volumeValue.GetValue<decimal>();

		var range = candle.HighPrice - candle.LowPrice;
		var rangeValue = _rangeMa.Process(new DecimalIndicatorValue(_rangeMa, range, candle.OpenTime));
		if (!rangeValue.IsFinal)
		{
			_prevMacd = macd;
			_prevSignal = signal;
			return;
		}
		var avgRange = rangeValue.GetValue<decimal>();

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevMacd = macd;
			_prevSignal = signal;
			return;
		}

		var macdDelta = Math.Abs(macd - signal);
		var macdFarFromZero = Math.Abs(macd) > MacdZeroLimit && Math.Abs(signal) > MacdZeroLimit;

		var body = Math.Abs(candle.ClosePrice - candle.OpenPrice);
		var isStrongBullish = candle.ClosePrice > candle.OpenPrice && body > 0.6m * range;
		var isStrongBearish = candle.ClosePrice < candle.OpenPrice && body > 0.6m * range;

		var longCondition = _prevMacd <= _prevSignal && macd > signal && candle.ClosePrice > ema && macdDelta > MacdDeltaMin && macdFarFromZero && candle.TotalVolume > volumeAvg && isStrongBullish;
		var shortCondition = _prevMacd >= _prevSignal && macd < signal && candle.ClosePrice < ema && macdDelta > MacdDeltaMin && macdFarFromZero && candle.TotalVolume > volumeAvg && isStrongBearish;

		if (longCondition && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_targetPrice = candle.ClosePrice + avgRange * RangeMultiplierTp;
			_stopPrice = candle.ClosePrice - avgRange * RangeMultiplierSl;
		}
		else if (shortCondition && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_targetPrice = candle.ClosePrice - avgRange * RangeMultiplierTp;
			_stopPrice = candle.ClosePrice + avgRange * RangeMultiplierSl;
		}

		var closeLong = Position > 0 && _prevMacd >= _prevSignal && macd < signal;
		var closeShort = Position < 0 && _prevMacd <= _prevSignal && macd > signal;

		if (closeLong)
			SellMarket(Position);

		if (closeShort)
			BuyMarket(-Position);

		if (Position > 0)
		{
			if (candle.ClosePrice >= _targetPrice || candle.ClosePrice <= _stopPrice)
				SellMarket(Position);
		}
		else if (Position < 0)
		{
			if (candle.ClosePrice <= _targetPrice || candle.ClosePrice >= _stopPrice)
				BuyMarket(-Position);
		}

		_prevMacd = macd;
		_prevSignal = signal;
	}
}

