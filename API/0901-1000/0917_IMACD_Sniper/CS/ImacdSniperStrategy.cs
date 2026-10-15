using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// IMACD Sniper strategy.
/// A long opens when the MACD line crosses above its signal line with the close above the EMA, the gap between the lines above MacdDeltaMin,
/// both lines at least MacdZeroLimit away from zero, the candle volume above its RangeLength average and a strong bullish candle
/// (body at least half of its range). Shorts mirror these rules. An opposite MACD cross closes the position, and the take profit and
/// stop loss sit RangeMultiplierTp and RangeMultiplierSl average candle ranges away from the entry.
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

	private SimpleMovingAverage _rangeSma;
	private SimpleMovingAverage _volumeSma;
	private decimal? _prevMacd;
	private decimal? _prevSignal;
	private decimal? _takePrice;
	private decimal? _stopPrice;

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
	/// Minimum distance between MACD and signal lines for entry.
	/// </summary>
	public decimal MacdDeltaMin
	{
		get => _macdDeltaMin.Value;
		set => _macdDeltaMin.Value = value;
	}

	/// <summary>
	/// Minimum distance of both MACD lines from zero.
	/// </summary>
	public decimal MacdZeroLimit
	{
		get => _macdZeroLimit.Value;
		set => _macdZeroLimit.Value = value;
	}

	/// <summary>
	/// Candles for the average range and average volume.
	/// </summary>
	public int RangeLength
	{
		get => _rangeLength.Value;
		set => _rangeLength.Value = value;
	}

	/// <summary>
	/// Take-profit multiplier of the average range.
	/// </summary>
	public decimal RangeMultiplierTp
	{
		get => _rangeMultiplierTp.Value;
		set => _rangeMultiplierTp.Value = value;
	}

	/// <summary>
	/// Stop-loss multiplier of the average range.
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
			.SetOptimize(6, 24, 2);

		_slowLength = Param(nameof(SlowLength), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow Length", "MACD slow period", "MACD")
			.SetOptimize(20, 40, 2);

		_signalLength = Param(nameof(SignalLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal Length", "MACD signal smoothing", "MACD")
			.SetOptimize(5, 15, 1);

		_macdDeltaMin = Param(nameof(MacdDeltaMin), 0.03m)
			.SetNotNegative()
			.SetDisplay("Min MACD Delta", "Minimum distance between MACD and signal for entry", "Filters");

		_macdZeroLimit = Param(nameof(MacdZeroLimit), 0.05m)
			.SetNotNegative()
			.SetDisplay("MACD Zero Limit", "Minimum distance of both lines from zero for entry", "Filters");

		_rangeLength = Param(nameof(RangeLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("Range Length", "Candles for the average range and volume", "Risk")
			.SetOptimize(5, 30, 1);

		_rangeMultiplierTp = Param(nameof(RangeMultiplierTp), 4m)
			.SetNotNegative()
			.SetDisplay("Range Multiplier TP", "Take profit in average ranges", "Risk")
			.SetOptimize(1m, 6m, 1m);

		_rangeMultiplierSl = Param(nameof(RangeMultiplierSl), 1.5m)
			.SetNotNegative()
			.SetDisplay("Range Multiplier SL", "Stop loss in average ranges", "Risk")
			.SetOptimize(1m, 3m, 0.5m);

		_emaLength = Param(nameof(EmaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "Trend EMA period", "Trend")
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
		_rangeSma = null;
		_volumeSma = null;
		ResetState();
	}

	private void ResetState()
	{
		_prevMacd = null;
		_prevSignal = null;
		_takePrice = null;
		_stopPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		_rangeSma = new SimpleMovingAverage { Length = RangeLength };
		_volumeSma = new SimpleMovingAverage { Length = RangeLength };

		var ema = new ExponentialMovingAverage { Length = EmaLength };
		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd = { ShortMa = { Length = FastLength }, LongMa = { Length = SlowLength } },
			SignalMa = { Length = SignalLength }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(macd, ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);

			var macdArea = CreateChartArea();
			if (macdArea != null)
				DrawIndicator(macdArea, macd);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue, IIndicatorValue emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var range = candle.HighPrice - candle.LowPrice;
		var rangeValue = _rangeSma.Process(new DecimalIndicatorValue(_rangeSma, range, candle.OpenTime) { IsFinal = true });
		var volumeValue = _volumeSma.Process(new DecimalIndicatorValue(_volumeSma, candle.TotalVolume, candle.OpenTime) { IsFinal = true });

		if (!macdValue.IsFormed || macdValue is not MovingAverageConvergenceDivergenceSignalValue { Macd: decimal macd, Signal: decimal signal })
			return;

		var prevMacd = _prevMacd;
		var prevSignal = _prevSignal;
		_prevMacd = macd;
		_prevSignal = signal;

		if (CheckProtection(candle))
			return;

		if (prevMacd is not decimal pm || prevSignal is not decimal ps)
			return;

		if (!emaValue.IsFormed || !_rangeSma.IsFormed || !_volumeSma.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var ema = emaValue.GetValue<decimal>();
		var avgRange = rangeValue.GetValue<decimal>();
		var avgVolume = volumeValue.GetValue<decimal>();
		var close = candle.ClosePrice;

		var crossUp = pm <= ps && macd > signal;
		var crossDown = pm >= ps && macd < signal;

		var deltaOk = Math.Abs(macd - signal) > MacdDeltaMin;
		var farFromZero = Math.Abs(macd) > MacdZeroLimit && Math.Abs(signal) > MacdZeroLimit;
		var volumeOk = candle.TotalVolume > avgVolume;
		var body = Math.Abs(close - candle.OpenPrice);
		var strongBody = range > 0 && body * 2 >= range;

		var longEntry = crossUp && close > ema && deltaOk && farFromZero && volumeOk && strongBody && close > candle.OpenPrice;
		var shortEntry = crossDown && close < ema && deltaOk && farFromZero && volumeOk && strongBody && close < candle.OpenPrice;

		if (longEntry && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			SetLevels(close, avgRange, true);
		}
		else if (shortEntry && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			SetLevels(close, avgRange, false);
		}
		else if (Position > 0 && crossDown)
		{
			SellMarket(Position);
			ClearLevels();
		}
		else if (Position < 0 && crossUp)
		{
			BuyMarket(-Position);
			ClearLevels();
		}
	}

	private void SetLevels(decimal entry, decimal avgRange, bool isLong)
	{
		var take = avgRange * RangeMultiplierTp;
		var stop = avgRange * RangeMultiplierSl;
		var sign = isLong ? 1m : -1m;

		_takePrice = take > 0 ? entry + sign * take : null;
		_stopPrice = stop > 0 ? entry - sign * stop : null;
	}

	private void ClearLevels()
	{
		_takePrice = null;
		_stopPrice = null;
	}

	// Returns true when the take profit or stop loss closed the position on this candle.
	private bool CheckProtection(ICandleMessage candle)
	{
		if (Position == 0)
		{
			ClearLevels();
			return false;
		}

		if (Position > 0)
		{
			if ((_stopPrice is decimal sl && candle.LowPrice <= sl) || (_takePrice is decimal tp && candle.HighPrice >= tp))
			{
				SellMarket(Position);
				ClearLevels();
				return true;
			}
		}
		else
		{
			if ((_stopPrice is decimal sl && candle.HighPrice >= sl) || (_takePrice is decimal tp && candle.LowPrice <= tp))
			{
				BuyMarket(-Position);
				ClearLevels();
				return true;
			}
		}

		return false;
	}
}
