using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the MACD Candle indicator.
/// Compares MACD computed on opens vs closes.
/// </summary>
public class MacdCandleStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<DataType> _candleType;

	private MovingAverageConvergenceDivergenceSignal _macdOpen;
	private MovingAverageConvergenceDivergenceSignal _macdClose;
	private decimal? _previousColor;

	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public int SignalLength { get => _signalLength.Value; set => _signalLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MacdCandleStrategy()
	{
		_fastLength = Param(nameof(FastLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA period", "Indicator")
			.SetOptimize(8, 16, 2);

		_slowLength = Param(nameof(SlowLength), 26)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA period", "Indicator")
			.SetOptimize(20, 40, 2);

		_signalLength = Param(nameof(SignalLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("Signal", "Signal period", "Indicator")
			.SetOptimize(5, 15, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for indicators", "General");
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
		_previousColor = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_macdOpen = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = FastLength },
				LongMa = { Length = SlowLength },
			},
			SignalMa = { Length = SignalLength }
		};

		_macdClose = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = FastLength },
				LongMa = { Length = SlowLength },
			},
			SignalMa = { Length = SignalLength }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var openInput = new DecimalIndicatorValue(_macdOpen, candle.OpenPrice, candle.OpenTime) { IsFinal = true };
		var closeInput = new DecimalIndicatorValue(_macdClose, candle.ClosePrice, candle.OpenTime) { IsFinal = true };

		var openValue = _macdOpen.Process(openInput);
		var closeValue = _macdClose.Process(closeInput);

		if (!_macdOpen.IsFormed || !_macdClose.IsFormed)
			return;

		var openMacd = ((IMovingAverageConvergenceDivergenceSignalValue)openValue).Macd;
		var closeMacd = ((IMovingAverageConvergenceDivergenceSignalValue)closeValue).Macd;

		if (openMacd == null || closeMacd == null)
			return;

		var color = openMacd < closeMacd ? 2m : openMacd > closeMacd ? 0m : 1m;

		if (_previousColor is null)
		{
			_previousColor = color;
			return;
		}

		if (color == 2m && _previousColor < 2m)
		{
			if (Position < 0)
				BuyMarket();
			if (Position <= 0)
				BuyMarket();
		}
		else if (color == 0m && _previousColor > 0m)
		{
			if (Position > 0)
				SellMarket();
			if (Position >= 0)
				SellMarket();
		}

		_previousColor = color;
	}
}
