using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Modified On-Balance Volume strategy with divergence detection.
/// Enters long when OBV-M crosses above its signal line.
/// Enters short when OBV-M crosses below its signal line.
/// Logs regular and hidden divergences between price and OBV-M.
/// </summary>
public class ModifiedObvWithDivergenceDetectionStrategy : Strategy
{
	private readonly StrategyParam<MovingAverageTypeEnum> _maType;
	private readonly StrategyParam<int> _obvMaLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<DataType> _candleType;

	private readonly decimal[] _obvBuffer = new decimal[5];
	private readonly decimal[] _highBuffer = new decimal[5];
	private readonly decimal[] _lowBuffer = new decimal[5];
	private int _bufferCount;

	private decimal? _lastTopObv;
	private decimal? _lastTopPrice;
	private decimal? _lastBottomObv;
	private decimal? _lastBottomPrice;

	private bool _wasBelowSignal;
	private bool _isInitialized;

	/// <summary>
	/// Type of moving average used for OBV smoothing.
	/// </summary>
	public MovingAverageTypeEnum MaType
	{
		get => _maType.Value;
		set => _maType.Value = value;
	}

	/// <summary>
	/// OBV moving average length.
	/// </summary>
	public int ObvMaLength
	{
		get => _obvMaLength.Value;
		set => _obvMaLength.Value = value;
	}

	/// <summary>
	/// Signal moving average length.
	/// </summary>
	public int SignalLength
	{
		get => _signalLength.Value;
		set => _signalLength.Value = value;
	}

	/// <summary>
	/// Candle type for calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="ModifiedObvWithDivergenceDetectionStrategy"/>.
	/// </summary>
	public ModifiedObvWithDivergenceDetectionStrategy()
	{
		_maType = Param(nameof(MaType), MovingAverageTypeEnum.Exponential)
			.SetDisplay("MA Type", "Moving average type for OBV", "General");

		_obvMaLength = Param(nameof(ObvMaLength), 7)
			.SetGreaterThanZero()
			.SetDisplay("OBV MA Length", "Length of OBV moving average", "General");

		_signalLength = Param(nameof(SignalLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Signal Length", "Length of signal moving average", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for calculation", "General");
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

		Array.Clear(_obvBuffer, 0, _obvBuffer.Length);
		Array.Clear(_highBuffer, 0, _highBuffer.Length);
		Array.Clear(_lowBuffer, 0, _lowBuffer.Length);
		_bufferCount = 0;

		_lastTopObv = null;
		_lastTopPrice = null;
		_lastBottomObv = null;
		_lastBottomPrice = null;

		_wasBelowSignal = false;
		_isInitialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var obv = new OnBalanceVolume();

		IIndicator obvMa = MaType switch
		{
			MovingAverageTypeEnum.Simple => new SimpleMovingAverage(),
			MovingAverageTypeEnum.VolumeWeighted => new VolumeWeightedMovingAverage(),
			MovingAverageTypeEnum.Weighted => new WeightedMovingAverage(),
			_ => new ExponentialMovingAverage(),
		};
		obvMa.Length = ObvMaLength;

		IIndicator signalMa = MaType switch
		{
			MovingAverageTypeEnum.Simple => new SimpleMovingAverage(),
			MovingAverageTypeEnum.VolumeWeighted => new VolumeWeightedMovingAverage(),
			MovingAverageTypeEnum.Weighted => new WeightedMovingAverage(),
			_ => new ExponentialMovingAverage(),
		};
		signalMa.Length = SignalLength;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(obv, (candle, obvValue) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				var obvmValue = obvMa.Process(obvValue);
				var obvm = obvmValue.ToDecimal();
				var signal = signalMa.Process(obvmValue).ToDecimal();

				if (!_isInitialized)
				{
					if (!obvMa.IsFormed || !signalMa.IsFormed)
						return;

					_wasBelowSignal = obvm < signal;
					_isInitialized = true;
				}
				else
				{
					var isBelow = obvm < signal;

					if (_wasBelowSignal != isBelow)
					{
						if (!isBelow && Position <= 0)
							BuyMarket(Volume + Math.Abs(Position));
						else if (isBelow && Position >= 0)
							SellMarket(Volume + Math.Abs(Position));

						_wasBelowSignal = isBelow;
					}
				}

				UpdateBuffers(obvm, candle.HighPrice, candle.LowPrice);
				DetectDivergences(candle);
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, obvMa);
			DrawIndicator(area, signalMa);
			DrawOwnTrades(area);
		}
	}

	private void UpdateBuffers(decimal obv, decimal high, decimal low)
	{
		for (var i = _obvBuffer.Length - 1; i > 0; i--)
		{
			_obvBuffer[i] = _obvBuffer[i - 1];
			_highBuffer[i] = _highBuffer[i - 1];
			_lowBuffer[i] = _lowBuffer[i - 1];
		}

		_obvBuffer[0] = obv;
		_highBuffer[0] = high;
		_lowBuffer[0] = low;

		if (_bufferCount < 5)
			_bufferCount++;
	}

	private void DetectDivergences(ICandleMessage candle)
	{
		if (_bufferCount < 5)
			return;

		var topFractal = _obvBuffer[4] < _obvBuffer[2] && _obvBuffer[3] < _obvBuffer[2] && _obvBuffer[1] < _obvBuffer[2] && _obvBuffer[0] < _obvBuffer[2];
		var bottomFractal = _obvBuffer[4] > _obvBuffer[2] && _obvBuffer[3] > _obvBuffer[2] && _obvBuffer[1] > _obvBuffer[2] && _obvBuffer[0] > _obvBuffer[2];

		if (topFractal)
		{
			var curObv = _obvBuffer[2];
			var curPrice = _highBuffer[2];
			if (_lastTopObv is decimal prevObv && _lastTopPrice is decimal prevPrice)
			{
				if (curPrice > prevPrice && curObv < prevObv)
					LogInfo($"Regular bearish divergence at {candle.OpenTime:O}");
				if (curPrice < prevPrice && curObv > prevObv)
					LogInfo($"Hidden bearish divergence at {candle.OpenTime:O}");
			}
			_lastTopObv = curObv;
			_lastTopPrice = curPrice;
		}

		if (bottomFractal)
		{
			var curObv = _obvBuffer[2];
			var curPrice = _lowBuffer[2];
			if (_lastBottomObv is decimal prevObv2 && _lastBottomPrice is decimal prevPrice2)
			{
				if (curPrice < prevPrice2 && curObv > prevObv2)
					LogInfo($"Regular bullish divergence at {candle.OpenTime:O}");
				if (curPrice > prevPrice2 && curObv < prevObv2)
					LogInfo($"Hidden bullish divergence at {candle.OpenTime:O}");
			}
			_lastBottomObv = curObv;
			_lastBottomPrice = curPrice;
		}
	}

	/// <summary>
	/// Moving average types.
	/// </summary>
	public enum MovingAverageTypeEnum
	{
		/// <summary>
		/// Exponential moving average.
		/// </summary>
		Exponential,

		/// <summary>
		/// Simple moving average.
		/// </summary>
		Simple,

		/// <summary>
		/// Volume weighted moving average.
		/// </summary>
		VolumeWeighted,

		/// <summary>
		/// Weighted moving average.
		/// </summary>
		Weighted
	}
}