namespace StockSharp.Samples.Strategies;

using System;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// MACD + DMI Strategy
/// </summary>
public class MacdDmiStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleTypeParam;
	private readonly StrategyParam<int> _dmiLength;
	private readonly StrategyParam<int> _adxSmoothing;
	private readonly StrategyParam<int> _vstopLength;
	private readonly StrategyParam<decimal> _vstopMultiplier;

	private DirectionalIndex _dmi;
	private MovingAverageConvergenceDivergenceSignal _macd;
	private AverageTrueRange _atr;
	
	private decimal _vstop;
	private bool _uptrend = true;
	private decimal _max;
	private decimal _min;

	public MacdDmiStrategy()
	{
		_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(60).TimeFrame())
			.SetDisplay("Candle type", "Candle type for strategy calculation.", "General");

		_dmiLength = Param(nameof(DmiLength), 14)
			.SetDisplay("DMI Length", "DMI period", "DMI");

		_adxSmoothing = Param(nameof(AdxSmoothing), 14)
			.SetDisplay("ADX Smoothing", "ADX smoothing period", "DMI");

		_vstopLength = Param(nameof(VstopLength), 20)
			.SetDisplay("Vstop Length", "Volatility Stop period", "Vstop");

		_vstopMultiplier = Param(nameof(VstopMultiplier), 2.0m)
			.SetDisplay("Vstop Multiplier", "Volatility Stop multiplier", "Vstop");
	}

	public DataType CandleType
	{
		get => _candleTypeParam.Value;
		set => _candleTypeParam.Value = value;
	}

	public int DmiLength
	{
		get => _dmiLength.Value;
		set => _dmiLength.Value = value;
	}

	public int AdxSmoothing
	{
		get => _adxSmoothing.Value;
		set => _adxSmoothing.Value = value;
	}

	public int VstopLength
	{
		get => _vstopLength.Value;
		set => _vstopLength.Value = value;
	}

	public decimal VstopMultiplier
	{
		get => _vstopMultiplier.Value;
		set => _vstopMultiplier.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Initialize indicators
		_dmi = new DirectionalIndex
		{
			Length = DmiLength
		};

		_macd = new()
		{
			Macd =
			{
				ShortMa = { Length = 12 },
				LongMa = { Length = 26 },
			},
			SignalMa = { Length = 9 }
		};

		_atr = new AverageTrueRange
		{
			Length = VstopLength
		};

		// Subscribe to candles using high-level API
		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_dmi, _macd, _atr, OnProcess)
			.Start();

		// Setup chart
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void OnProcess(ICandleMessage candle, IIndicatorValue dmiValue, IIndicatorValue macdValue, IIndicatorValue atrValue)
	{
		// Process only finished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Wait for indicators to form
		if (!_dmi.IsFormed || !_macd.IsFormed || !_atr.IsFormed)
			return;

		// Get indicator values
		var dmiData = (DirectionalIndexValue)dmiValue;
		var posDm = dmiData.Plus;
		var negDm = dmiData.Minus;

		var macdData = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		var macdLine = macdData.Macd;
		var signalLine = macdData.Signal;

		// Get previous MACD values for crossover detection
		var prevMacdValue = _macd.GetValue<MovingAverageConvergenceDivergenceSignalValue>(1);
		var prevMacdLine = prevMacdValue.Macd;
		var prevSignalLine = prevMacdValue.Signal;

		// Calculate Volatility Stop
		CalculateVolatilityStop(candle, atrValue.ToDecimal());

		// Entry condition
		var macdCrossover = macdLine > signalLine && prevMacdLine <= prevSignalLine;
		var entryLong = macdCrossover && posDm > negDm;

		// Exit conditions
		var macdCrossunder = macdLine < signalLine && prevMacdLine >= prevSignalLine;
		var tales = macdCrossunder && posDm > negDm ? false : 
					macdCrossunder && posDm < negDm ? true : false;
		var crossUnderVstop = candle.ClosePrice < _vstop;
		var closeLong = tales || crossUnderVstop;

		// Execute trades
		if (entryLong && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (closeLong && Position > 0)
		{
			ClosePosition();
		}
	}

	private void CalculateVolatilityStop(ICandleMessage candle, decimal atrValue)
	{
		var src = candle.ClosePrice;
		var atrM = atrValue * VstopMultiplier;

		if (_max == 0)
		{
			_max = src;
			_min = src;
			_vstop = src;
			return;
		}

		_max = Math.Max(_max, src);
		_min = Math.Min(_min, src);

		var prevUptrend = _uptrend;
		
		if (_uptrend)
		{
			_vstop = Math.Max(_vstop, _max - atrM);
		}
		else
		{
			_vstop = Math.Min(_vstop, _min + atrM);
		}

		_uptrend = src - _vstop >= 0;

		if (_uptrend != prevUptrend)
		{
			_max = src;
			_min = src;
			_vstop = _uptrend ? _max - atrM : _min + atrM;
		}
	}
}