using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bollinger Band Touch with SMI and MACD Angle Strategy.
/// Buys when price touches the lower Bollinger Band and both SMI and MACD angles point upward.
/// Closes the position when price reaches the upper Bollinger Band.
/// </summary>
public class BollingerBandTouchSmiMacdAngleStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _bollingerLength;
	private readonly StrategyParam<decimal> _bollingerMultiplier;
	private readonly StrategyParam<int> _smiLength;
	private readonly StrategyParam<int> _smiSignalLength;
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<decimal> _smiAngleThreshold;
	private readonly StrategyParam<decimal> _macdAngleThreshold;

	private BollingerBands _bollinger;
	private StochasticOscillator _stochastic;
	private SimpleMovingAverage _smiSma;
	private MovingAverageConvergenceDivergenceSignal _macd;

	private decimal? _prevSmi;
	private decimal? _prevMacd;

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Bollinger Bands period.
	/// </summary>
	public int BollingerLength { get => _bollingerLength.Value; set => _bollingerLength.Value = value; }

	/// <summary>
	/// Bollinger Bands standard deviation multiplier.
	/// </summary>
	public decimal BollingerMultiplier { get => _bollingerMultiplier.Value; set => _bollingerMultiplier.Value = value; }

	/// <summary>
	/// Stochastic length for SMI calculation.
	/// </summary>
	public int SmiLength { get => _smiLength.Value; set => _smiLength.Value = value; }

	/// <summary>
	/// Smoothing length for SMI.
	/// </summary>
	public int SmiSignalLength { get => _smiSignalLength.Value; set => _smiSignalLength.Value = value; }

	/// <summary>
	/// MACD fast EMA length.
	/// </summary>
	public int MacdFast { get => _macdFast.Value; set => _macdFast.Value = value; }

	/// <summary>
	/// MACD slow EMA length.
	/// </summary>
	public int MacdSlow { get => _macdSlow.Value; set => _macdSlow.Value = value; }

	/// <summary>
	/// MACD signal EMA length.
	/// </summary>
	public int MacdSignal { get => _macdSignal.Value; set => _macdSignal.Value = value; }

	/// <summary>
	/// Maximum allowed SMI angle.
	/// </summary>
	public decimal SmiAngleThreshold { get => _smiAngleThreshold.Value; set => _smiAngleThreshold.Value = value; }

	/// <summary>
	/// Maximum allowed MACD angle.
	/// </summary>
	public decimal MacdAngleThreshold { get => _macdAngleThreshold.Value; set => _macdAngleThreshold.Value = value; }

	/// <summary>
	/// Constructor.
	/// </summary>
	public BollingerBandTouchSmiMacdAngleStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_bollingerLength = Param(nameof(BollingerLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("BB Length", "Bollinger Bands period", "Bollinger Bands")
			.SetCanOptimize(true)
			.SetOptimize(10, 40, 5);

		_bollingerMultiplier = Param(nameof(BollingerMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("BB Multiplier", "Standard deviation multiplier", "Bollinger Bands")
			.SetCanOptimize(true)
			.SetOptimize(1m, 3m, 0.5m);

		_smiLength = Param(nameof(SmiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("SMI Length", "Stochastic length", "SMI");

		_smiSignalLength = Param(nameof(SmiSignalLength), 3)
			.SetGreaterThanZero()
			.SetDisplay("SMI Signal Length", "SMI smoothing length", "SMI");

		_macdFast = Param(nameof(MacdFast), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast Length", "Fast EMA length", "MACD");

		_macdSlow = Param(nameof(MacdSlow), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow Length", "Slow EMA length", "MACD");

		_macdSignal = Param(nameof(MacdSignal), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal Length", "Signal EMA length", "MACD");

		_smiAngleThreshold = Param(nameof(SmiAngleThreshold), 60m)
			.SetDisplay("SMI Angle Threshold", "Maximum allowed SMI angle", "Angles");

		_macdAngleThreshold = Param(nameof(MacdAngleThreshold), 50m)
			.SetDisplay("MACD Angle Threshold", "Maximum allowed MACD angle", "Angles");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_bollinger = new BollingerBands
		{
			Length = BollingerLength,
			Width = BollingerMultiplier
		};

		_stochastic = new StochasticOscillator
		{
			K = { Length = SmiLength },
			D = { Length = 1 }
		};

		_smiSma = new SimpleMovingAverage { Length = SmiSignalLength };

		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFast },
				LongMa = { Length = MacdSlow },
			},
			SignalMa = { Length = MacdSignal }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_bollinger, _stochastic, _macd, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _bollinger);
			DrawIndicator(area, _macd);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bbValue, IIndicatorValue stochValue, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var bb = (BollingerBandsValue)bbValue;
		if (bb.UpBand is not decimal upper || bb.LowBand is not decimal lower)
			return;

		var stoch = (StochasticOscillatorValue)stochValue;
		if (stoch.K is not decimal k)
			return;

		var smiVal = _smiSma.Process(new DecimalIndicatorValue(_smiSma, k, candle.ServerTime));
		if (!smiVal.IsFormed)
		{
			_prevSmi = smiVal.ToDecimal();
			return;
		}

		var smi = smiVal.ToDecimal();
		if (_prevSmi is null)
		{
			_prevSmi = smi;
			return;
		}

		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		if (macdTyped.Macd is not decimal macd)
			return;

		if (_prevMacd is null)
		{
			_prevMacd = macd;
			return;
		}

		var smiAngle = (decimal)(Math.Atan((double)(smi - _prevSmi.Value)) * 180.0 / Math.PI);
		var macdAngle = (decimal)(Math.Atan((double)(macd - _prevMacd.Value)) * 180.0 / Math.PI);

		var priceTouchesLower = candle.ClosePrice <= lower;
		var priceTouchesUpper = candle.ClosePrice >= upper;

		var smiAngledUp = smiAngle > 0m && smiAngle <= SmiAngleThreshold;
		var macdAngledUp = macdAngle > 0m && macdAngle <= MacdAngleThreshold;

		if (priceTouchesLower && smiAngledUp && macdAngledUp && Position <= 0)
			RegisterBuy();

		if (priceTouchesUpper && Position > 0)
			RegisterSell();

		_prevSmi = smi;
		_prevMacd = macd;
	}
}

