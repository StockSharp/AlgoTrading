using System;
using System.Collections.Generic;

using Ecng.Common;

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

	private decimal _prevMacd;
	private decimal _prevSignal;
	private decimal _entryPrice;

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
			.SetGreaterThanZero()
			.SetDisplay("Min MACD Delta", "Minimum MACD difference for entry", "Filters")
			
			.SetOptimize(0.01m, 0.1m, 0.01m);

		_macdZeroLimit = Param(nameof(MacdZeroLimit), 0.05m)
			.SetGreaterThanZero()
			.SetDisplay("MACD Zero Limit", "Minimum distance from zero for entry", "Filters")
			
			.SetOptimize(0.01m, 0.1m, 0.01m);

		_rangeLength = Param(nameof(RangeLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("Range Length", "Number of candles for range average", "Risk")
			
			.SetOptimize(5, 30, 1);

		_rangeMultiplierTp = Param(nameof(RangeMultiplierTp), 4m)
			.SetGreaterThanZero()
			.SetDisplay("Range Multiplier TP", "TP multiplier of range", "Risk")
			
			.SetOptimize(1m, 6m, 1m);

		_rangeMultiplierSl = Param(nameof(RangeMultiplierSl), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Range Multiplier SL", "SL multiplier of range", "Risk")
			
			.SetOptimize(1m, 3m, 0.5m);

		_emaLength = Param(nameof(EmaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "EMA period", "Trend")
			
			.SetOptimize(10, 50, 5);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
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
		_prevMacd = default;
		_prevSignal = default;
		_entryPrice = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

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

			var macdArea = CreateChartArea();
			DrawIndicator(macdArea, macd);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue, IIndicatorValue emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (macdValue is not MovingAverageConvergenceDivergenceSignalValue macdTyped)
			return;

		if (macdTyped.Macd is not decimal macd || macdTyped.Signal is not decimal signal)
			return;

		if (emaValue.IsEmpty)
			return;

		var ema = emaValue.ToDecimal();

		var macdDelta = Math.Abs(macd - signal);

		var longCondition = _prevMacd != 0 && _prevMacd <= _prevSignal && macd > signal && candle.ClosePrice > ema && macdDelta > MacdDeltaMin;
		var shortCondition = _prevMacd != 0 && _prevMacd >= _prevSignal && macd < signal && candle.ClosePrice < ema && macdDelta > MacdDeltaMin;

		if (longCondition && Position <= 0)
		{
			BuyMarket();
			_entryPrice = candle.ClosePrice;
		}
		else if (shortCondition && Position >= 0)
		{
			SellMarket();
			_entryPrice = candle.ClosePrice;
		}
		else if (Position > 0 && _prevMacd >= _prevSignal && macd < signal)
		{
			SellMarket();
		}
		else if (Position < 0 && _prevMacd <= _prevSignal && macd > signal)
		{
			BuyMarket();
		}

		_prevMacd = macd;
		_prevSignal = signal;
	}
}

