using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on MACD turning points and zero line breakdowns.
/// Four modes define how signals are generated.
/// </summary>
public class ColorRsiMacdStrategy : Strategy
{
	public enum AlgModes
	{
		Breakdown,
		MacdTwist,
		SignalTwist,
		MacdDisposition
	}

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _signalPeriod;
	private readonly StrategyParam<AlgModes> _mode;

	private decimal? _histPrev;
	private decimal? _macdPrev;
	private decimal? _macdPrev2;
	private decimal? _signalPrev;
	private decimal? _signalPrev2;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }
	public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }
	public int SignalPeriod { get => _signalPeriod.Value; set => _signalPeriod.Value = value; }
	public AlgModes Mode { get => _mode.Value; set => _mode.Value = value; }

	public ColorRsiMacdStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_fastPeriod = Param(nameof(FastPeriod), 12)
			.SetDisplay("Fast Period", "Fast EMA period", "MACD");

		_slowPeriod = Param(nameof(SlowPeriod), 26)
			.SetDisplay("Slow Period", "Slow EMA period", "MACD");

		_signalPeriod = Param(nameof(SignalPeriod), 9)
			.SetDisplay("Signal Period", "Signal line period", "MACD");

		_mode = Param(nameof(Mode), AlgModes.MacdDisposition)
			.SetDisplay("Mode", "Algorithm mode", "Logic");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_histPrev = null;
		_macdPrev = null;
		_macdPrev2 = null;
		_signalPrev = null;
		_signalPrev2 = null;

		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = FastPeriod },
				LongMa = { Length = SlowPeriod }
			},
			SignalMa = { Length = SignalPeriod }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(macd, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			var macdArea = CreateChartArea();
			if (macdArea != null)
				DrawIndicator(macdArea, macd);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (macdValue is not IMovingAverageConvergenceDivergenceSignalValue data)
			return;

		if (data.Macd is not decimal macdLine || data.Signal is not decimal signalLine)
			return;

		var hist = macdLine - signalLine;

		switch (Mode)
		{
			case AlgModes.Breakdown:
				if (_histPrev is decimal prevHist)
				{
					if (prevHist < 0m && hist >= 0m && Position <= 0)
						BuyMarket();
					else if (prevHist > 0m && hist <= 0m && Position >= 0)
						SellMarket();
				}
				_histPrev = hist;
				break;

			case AlgModes.MacdTwist:
				if (_macdPrev2 is decimal m2 && _macdPrev is decimal m1)
				{
					if (m1 < m2 && macdLine > m1 && Position <= 0)
						BuyMarket();
					else if (m1 > m2 && macdLine < m1 && Position >= 0)
						SellMarket();
				}
				_macdPrev2 = _macdPrev;
				_macdPrev = macdLine;
				break;

			case AlgModes.SignalTwist:
				if (_signalPrev2 is decimal s2 && _signalPrev is decimal s1)
				{
					if (s1 < s2 && signalLine > s1 && Position <= 0)
						BuyMarket();
					else if (s1 > s2 && signalLine < s1 && Position >= 0)
						SellMarket();
				}
				_signalPrev2 = _signalPrev;
				_signalPrev = signalLine;
				break;

			case AlgModes.MacdDisposition:
				if (_macdPrev is decimal mp && _signalPrev is decimal sp)
				{
					if (mp <= sp && macdLine > signalLine && Position <= 0)
						BuyMarket();
					else if (mp >= sp && macdLine < signalLine && Position >= 0)
						SellMarket();
				}
				_macdPrev = macdLine;
				_signalPrev = signalLine;
				break;
		}
	}
}
