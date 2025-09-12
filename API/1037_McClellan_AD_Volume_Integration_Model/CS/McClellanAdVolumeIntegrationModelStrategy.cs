using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// McClellan A-D Volume Integration Model strategy.
/// Uses weighted advance-decline line and EMA-based oscillator
/// to enter long positions on threshold cross and exit after X periods.
/// </summary>
public class McClellanAdVolumeIntegrationModelStrategy : Strategy
{
	private readonly StrategyParam<int> _emaShortLength;
	private readonly StrategyParam<int> _emaLongLength;
	private readonly StrategyParam<decimal> _oscThresholdLong;
	private readonly StrategyParam<int> _exitPeriods;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _emaShort;
	private ExponentialMovingAverage _emaLong;
	private int _entryBar;
	private int _barIndex;
	private decimal _previousOscillator;
	private bool _hasPrevOscillator;

	/// <summary>
	/// Short EMA period length.
	/// </summary>
	public int EmaShortLength
	{
		get => _emaShortLength.Value;
		set => _emaShortLength.Value = value;
	}

	/// <summary>
	/// Long EMA period length.
	/// </summary>
	public int EmaLongLength
	{
		get => _emaLongLength.Value;
		set => _emaLongLength.Value = value;
	}

	/// <summary>
	/// Oscillator threshold for long entries.
	/// </summary>
	public decimal OscThresholdLong
	{
		get => _oscThresholdLong.Value;
		set => _oscThresholdLong.Value = value;
	}

	/// <summary>
	/// Number of bars to exit after entry.
	/// </summary>
	public int ExitPeriods
	{
		get => _exitPeriods.Value;
		set => _exitPeriods.Value = value;
	}

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public McClellanAdVolumeIntegrationModelStrategy()
	{
		_emaShortLength = Param(nameof(EmaShortLength), 19)
			.SetGreaterThanZero()
			.SetDisplay("Short EMA Length", "EMA period for short term", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 30, 1);

		_emaLongLength = Param(nameof(EmaLongLength), 38)
			.SetGreaterThanZero()
			.SetDisplay("Long EMA Length", "EMA period for long term", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(20, 60, 1);

		_oscThresholdLong = Param(nameof(OscThresholdLong), -96m)
			.SetDisplay("Long Entry Threshold", "Oscillator level for long entry", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(-150m, -50m, 5m);

		_exitPeriods = Param(nameof(ExitPeriods), 13)
			.SetGreaterThanZero()
			.SetDisplay("Exit After Bars", "Bars to hold position", "Trading");

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

		_emaShort = null;
		_emaLong = null;
		_entryBar = -1;
		_barIndex = 0;
		_previousOscillator = 0m;
		_hasPrevOscillator = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_emaShort = new ExponentialMovingAverage { Length = EmaShortLength };
		_emaLong = new ExponentialMovingAverage { Length = EmaLongLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _emaShort);
			DrawIndicator(area, _emaLong);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var adLine = candle.ClosePrice - candle.OpenPrice;
		var volumeLine = candle.TotalVolume == 0 ? 1m : candle.TotalVolume;
		var weightedAdLine = adLine * volumeLine;

		var shortVal = _emaShort.Process(weightedAdLine, candle.OpenTime, true).ToDecimal();
		var longVal = _emaLong.Process(weightedAdLine, candle.OpenTime, true).ToDecimal();
		var oscillator = shortVal - longVal;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_previousOscillator = oscillator;
			_hasPrevOscillator = true;
			_barIndex++;
			return;
		}

		var longEntry = _hasPrevOscillator && _previousOscillator < OscThresholdLong && oscillator > OscThresholdLong;

		if (longEntry && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			_entryBar = _barIndex;
		}

		if (_entryBar >= 0 && Position > 0 && _barIndex - _entryBar >= ExitPeriods)
		{
			SellMarket(Math.Abs(Position));
			_entryBar = -1;
		}

		_previousOscillator = oscillator;
		_hasPrevOscillator = true;
		_barIndex++;
	}
}
