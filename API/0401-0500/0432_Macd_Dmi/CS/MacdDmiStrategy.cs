using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MACD + DMI strategy.
/// The MACD line is the difference of Ma1Length and Ma2Length EMAs. A long opens when it crosses above its signal line
/// while +DI is above -DI and ADX is above KeyLevel; a short opens on the mirrored setup. The reverse signal flips the
/// position, and percent stop-loss and take-profit protection limit each trade.
/// </summary>
public class MacdDmiStrategy : Strategy
{
	private readonly StrategyParam<int> _ma1Length;
	private readonly StrategyParam<int> _ma2Length;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<int> _dmiLength;
	private readonly StrategyParam<int> _adxSmoothing;
	private readonly StrategyParam<decimal> _keyLevel;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<DataType> _candleType;

	private SmoothedMovingAverage _adx;
	private decimal? _prevMacd;
	private decimal? _prevSignal;

	/// <summary>
	/// Fast EMA period of MACD.
	/// </summary>
	public int Ma1Length
	{
		get => _ma1Length.Value;
		set => _ma1Length.Value = value;
	}

	/// <summary>
	/// Slow EMA period of MACD.
	/// </summary>
	public int Ma2Length
	{
		get => _ma2Length.Value;
		set => _ma2Length.Value = value;
	}

	/// <summary>
	/// MACD signal line period.
	/// </summary>
	public int SignalLength
	{
		get => _signalLength.Value;
		set => _signalLength.Value = value;
	}

	/// <summary>
	/// Directional movement period.
	/// </summary>
	public int DmiLength
	{
		get => _dmiLength.Value;
		set => _dmiLength.Value = value;
	}

	/// <summary>
	/// ADX smoothing period.
	/// </summary>
	public int AdxSmoothing
	{
		get => _adxSmoothing.Value;
		set => _adxSmoothing.Value = value;
	}

	/// <summary>
	/// ADX level required for entries.
	/// </summary>
	public decimal KeyLevel
	{
		get => _keyLevel.Value;
		set => _keyLevel.Value = value;
	}

	/// <summary>
	/// Stop-loss percentage. 0 disables it.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Take-profit percentage. 0 disables it.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public MacdDmiStrategy()
	{
		_ma1Length = Param(nameof(Ma1Length), 10)
			.SetGreaterThanZero()
			.SetDisplay("MA1 Length", "Fast EMA period of MACD", "MACD");

		_ma2Length = Param(nameof(Ma2Length), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA2 Length", "Slow EMA period of MACD", "MACD");

		_signalLength = Param(nameof(SignalLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("Signal Length", "MACD signal line period", "MACD");

		_dmiLength = Param(nameof(DmiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("DMI Length", "Directional movement period", "DMI");

		_adxSmoothing = Param(nameof(AdxSmoothing), 14)
			.SetGreaterThanZero()
			.SetDisplay("ADX Smoothing", "ADX smoothing period", "DMI");

		_keyLevel = Param(nameof(KeyLevel), 20m)
			.SetDisplay("Key Level", "ADX level required for entries", "DMI");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop-loss percentage", "Risk");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 4m)
			.SetNotNegative()
			.SetDisplay("Take Profit %", "Take-profit percentage", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle type", "Candle type for strategy calculation", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_adx = null;
		_prevMacd = null;
		_prevSignal = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevMacd = null;
		_prevSignal = null;

		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = Ma1Length },
				LongMa = { Length = Ma2Length },
			},
			SignalMa = { Length = SignalLength },
		};
		var dmi = new DirectionalIndex { Length = DmiLength };
		// ADX is the DX of the DmiLength directional lines smoothed over AdxSmoothing bars.
		_adx = new SmoothedMovingAverage { Length = AdxSmoothing };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(macd, dmi, ProcessCandle)
			.Start();

		StartProtection(
			TakeProfitPercent > 0 ? new Unit(TakeProfitPercent, UnitTypes.Percent) : new Unit(),
			StopLossPercent > 0 ? new Unit(StopLossPercent, UnitTypes.Percent) : new Unit(),
			useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
			{
				DrawIndicator(oscillators, macd);
				DrawIndicator(oscillators, dmi);
			}
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue, IIndicatorValue dmiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!dmiValue.IsFormed)
			return;

		var dmi = (DirectionalIndexValue)dmiValue;
		if (dmi.Plus is not decimal diPlus || dmi.Minus is not decimal diMinus)
			return;

		var diSum = diPlus + diMinus;
		var dx = diSum == 0 ? 0m : 100m * Math.Abs(diPlus - diMinus) / diSum;
		var adx = _adx.Process(dx, candle.ServerTime, true).ToDecimal();

		if (!macdValue.IsFormed)
			return;

		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		if (macdTyped.Macd is not decimal macd || macdTyped.Signal is not decimal signal)
			return;

		var prevMacd = _prevMacd;
		var prevSignal = _prevSignal;
		_prevMacd = macd;
		_prevSignal = signal;

		if (!_adx.IsFormed || prevMacd is not decimal lastMacd || prevSignal is not decimal lastSignal)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var crossUp = lastMacd <= lastSignal && macd > signal;
		var crossDown = lastMacd >= lastSignal && macd < signal;

		if (crossUp && diPlus > diMinus && adx > KeyLevel && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (crossDown && diMinus > diPlus && adx > KeyLevel && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}
