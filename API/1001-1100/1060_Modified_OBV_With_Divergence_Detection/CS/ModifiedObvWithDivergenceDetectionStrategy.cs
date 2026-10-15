using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Modified OBV with divergence detection.
/// On-Balance Volume is smoothed by a selectable moving average (OBV-M) and a signal line is an EMA of OBV-M.
/// A cross of OBV-M above the signal goes long and a cross below goes short, reversing the opposite position.
/// Regular and hidden divergences between price and OBV-M are found with five-bar fractals and only logged.
/// </summary>
public class ModifiedObvWithDivergenceDetectionStrategy : Strategy
{
	/// <summary>
	/// Moving average types for OBV smoothing.
	/// </summary>
	public enum MaTypes
	{
		/// <summary>Simple moving average.</summary>
		Simple,
		/// <summary>Exponential moving average.</summary>
		Exponential,
		/// <summary>Weighted moving average.</summary>
		Weighted,
		/// <summary>Smoothed moving average.</summary>
		Smoothed,
	}

	private readonly StrategyParam<MaTypes> _maType;
	private readonly StrategyParam<int> _obvMaLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<DataType> _candleType;

	private IIndicator _obvMa;
	private ExponentialMovingAverage _signalMa;
	private decimal? _prevObvm;
	private decimal? _prevSignal;

	private readonly decimal[] _oscWindow = new decimal[5];
	private readonly decimal[] _highWindow = new decimal[5];
	private readonly decimal[] _lowWindow = new decimal[5];
	private int _windowCount;
	private decimal? _lastTopOsc;
	private decimal? _lastTopPrice;
	private decimal? _lastBottomOsc;
	private decimal? _lastBottomPrice;

	/// <summary>
	/// Moving average type used to smooth OBV.
	/// </summary>
	public MaTypes MaType
	{
		get => _maType.Value;
		set => _maType.Value = value;
	}

	/// <summary>
	/// Length of the OBV smoothing average.
	/// </summary>
	public int ObvMaLength
	{
		get => _obvMaLength.Value;
		set => _obvMaLength.Value = value;
	}

	/// <summary>
	/// Length of the signal line EMA.
	/// </summary>
	public int SignalLength
	{
		get => _signalLength.Value;
		set => _signalLength.Value = value;
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
	public ModifiedObvWithDivergenceDetectionStrategy()
	{
		_maType = Param(nameof(MaType), MaTypes.Exponential)
			.SetDisplay("MA Type", "Moving average type used to smooth OBV", "Indicators");

		_obvMaLength = Param(nameof(ObvMaLength), 7)
			.SetGreaterThanZero()
			.SetDisplay("OBV MA Length", "Length of the OBV smoothing average", "Indicators");

		_signalLength = Param(nameof(SignalLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Signal Length", "Length of the signal line EMA", "Indicators");

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
		_prevObvm = null;
		_prevSignal = null;
		_windowCount = 0;
		Array.Clear(_oscWindow);
		Array.Clear(_highWindow);
		Array.Clear(_lowWindow);
		_lastTopOsc = null;
		_lastTopPrice = null;
		_lastBottomOsc = null;
		_lastBottomPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var obv = new OnBalanceVolume();
		_obvMa = CreateMa(MaType, ObvMaLength);
		_signalMa = new ExponentialMovingAverage { Length = SignalLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(obv, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
			{
				DrawIndicator(oscillators, _obvMa);
				DrawIndicator(oscillators, _signalMa);
			}
		}
	}

	private static IIndicator CreateMa(MaTypes type, int length)
	{
		return type switch
		{
			MaTypes.Simple => new SimpleMovingAverage { Length = length },
			MaTypes.Weighted => new WeightedMovingAverage { Length = length },
			MaTypes.Smoothed => new SmoothedMovingAverage { Length = length },
			_ => new ExponentialMovingAverage { Length = length },
		};
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue obvValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var obvmValue = _obvMa.Process(obvValue);
		if (!_obvMa.IsFormed)
			return;

		var obvm = obvmValue.ToDecimal();
		var signalValue = _signalMa.Process(obvmValue);

		DetectDivergence(candle, obvm);

		if (!_signalMa.IsFormed)
			return;

		var signal = signalValue.ToDecimal();
		var prevObvm = _prevObvm;
		var prevSignal = _prevSignal;
		_prevObvm = obvm;
		_prevSignal = signal;

		if (prevObvm is not decimal po || prevSignal is not decimal ps)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var crossUp = po <= ps && obvm > signal;
		var crossDown = po >= ps && obvm < signal;

		if (crossUp && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (crossDown && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}

	private void DetectDivergence(ICandleMessage candle, decimal obvm)
	{
		for (var i = 0; i < 4; i++)
		{
			_oscWindow[i] = _oscWindow[i + 1];
			_highWindow[i] = _highWindow[i + 1];
			_lowWindow[i] = _lowWindow[i + 1];
		}

		_oscWindow[4] = obvm;
		_highWindow[4] = candle.HighPrice;
		_lowWindow[4] = candle.LowPrice;

		if (_windowCount < 5)
		{
			_windowCount++;
			if (_windowCount < 5)
				return;
		}

		// The fractal is confirmed two bars after its middle bar.
		var mid = _oscWindow[2];

		if (mid > _oscWindow[0] && mid > _oscWindow[1] && mid > _oscWindow[3] && mid > _oscWindow[4])
		{
			var price = _highWindow[2];

			if (_lastTopOsc is decimal prevOsc && _lastTopPrice is decimal prevPrice)
			{
				if (price > prevPrice && mid < prevOsc)
					LogInfo($"Regular bearish divergence at {candle.OpenTime}.");
				else if (price < prevPrice && mid > prevOsc)
					LogInfo($"Hidden bearish divergence at {candle.OpenTime}.");
			}

			_lastTopOsc = mid;
			_lastTopPrice = price;
		}

		if (mid < _oscWindow[0] && mid < _oscWindow[1] && mid < _oscWindow[3] && mid < _oscWindow[4])
		{
			var price = _lowWindow[2];

			if (_lastBottomOsc is decimal prevOsc && _lastBottomPrice is decimal prevPrice)
			{
				if (price < prevPrice && mid > prevOsc)
					LogInfo($"Regular bullish divergence at {candle.OpenTime}.");
				else if (price > prevPrice && mid < prevOsc)
					LogInfo($"Hidden bullish divergence at {candle.OpenTime}.");
			}

			_lastBottomOsc = mid;
			_lastBottomPrice = price;
		}
	}
}
