using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Neon Momentum Waves strategy based on MACD histogram levels.
/// </summary>
public class NeonMomentumWavesStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<decimal> _entryLevel;
	private readonly StrategyParam<decimal> _longExitLevel;
	private readonly StrategyParam<decimal> _shortExitLevel;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevHist;

	/// <summary>
	/// MACD fast EMA length.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// MACD slow EMA length.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// MACD signal smoothing length.
	/// </summary>
	public int SignalLength
	{
		get => _signalLength.Value;
		set => _signalLength.Value = value;
	}

	/// <summary>
	/// Histogram entry level.
	/// </summary>
	public decimal EntryLevel
	{
		get => _entryLevel.Value;
		set => _entryLevel.Value = value;
	}

	/// <summary>
	/// Histogram level to exit long position.
	/// </summary>
	public decimal LongExitLevel
	{
		get => _longExitLevel.Value;
		set => _longExitLevel.Value = value;
	}

	/// <summary>
	/// Histogram level to exit short position.
	/// </summary>
	public decimal ShortExitLevel
	{
		get => _shortExitLevel.Value;
		set => _shortExitLevel.Value = value;
	}

	/// <summary>
	/// Candle type parameter.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="NeonMomentumWavesStrategy"/>.
	/// </summary>
	public NeonMomentumWavesStrategy()
	{
		_fastLength = Param(nameof(FastLength), 12)
			.SetDisplay("Fast Length", "MACD fast EMA length", "MACD")
			.SetCanOptimize(true);

		_slowLength = Param(nameof(SlowLength), 26)
			.SetDisplay("Slow Length", "MACD slow EMA length", "MACD")
			.SetCanOptimize(true);

		_signalLength = Param(nameof(SignalLength), 20)
			.SetDisplay("Signal Length", "MACD signal smoothing", "MACD")
			.SetCanOptimize(true);

		_entryLevel = Param(nameof(EntryLevel), 0m)
			.SetDisplay("Entry Level", "Histogram entry threshold", "Parameters");

		_longExitLevel = Param(nameof(LongExitLevel), 11m)
			.SetDisplay("Long Exit Level", "Histogram level to exit longs", "Parameters");

		_shortExitLevel = Param(nameof(ShortExitLevel), -9m)
			.SetDisplay("Short Exit Level", "Histogram level to exit shorts", "Parameters");

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
		_prevHist = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var macd = new MovingAverageConvergenceDivergenceSignal
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
			.BindEx(macd, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, macd);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var typed = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		if (typed.Macd is not decimal macdLine || typed.Signal is not decimal signal)
			return;

		var hist = macdLine - signal;

		if (_prevHist is null)
		{
			_prevHist = hist;
			return;
		}

		var prev = _prevHist.Value;
		var crossUp = prev <= EntryLevel && hist > EntryLevel;
		var crossDown = prev >= EntryLevel && hist < EntryLevel;
		var longExit = Position > 0 && prev <= LongExitLevel && hist > LongExitLevel;
		var shortExit = Position < 0 && prev >= ShortExitLevel && hist < ShortExitLevel;

		if (longExit)
			SellMarket(Position);

		if (shortExit)
			BuyMarket(Math.Abs(Position));

		if (crossUp && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));

		if (crossDown && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));

		_prevHist = hist;
	}
}
