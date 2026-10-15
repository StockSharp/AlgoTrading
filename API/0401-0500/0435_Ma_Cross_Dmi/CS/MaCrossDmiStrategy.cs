using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MA Cross + DMI strategy.
/// Goes long when the fast EMA crosses above the slow EMA while +DI is above -DI and ADX is above KeyLevel, and short on
/// the mirrored setup. Any opposite EMA crossover closes the position, or reverses it when the DMI confirms.
/// </summary>
public class MaCrossDmiStrategy : Strategy
{
	private readonly StrategyParam<int> _ma1Length;
	private readonly StrategyParam<int> _ma2Length;
	private readonly StrategyParam<int> _dmiLength;
	private readonly StrategyParam<int> _adxSmoothing;
	private readonly StrategyParam<decimal> _keyLevel;
	private readonly StrategyParam<DataType> _candleType;

	private SmoothedMovingAverage _adx;
	private decimal? _prevMa1;
	private decimal? _prevMa2;

	/// <summary>
	/// Fast EMA period.
	/// </summary>
	public int Ma1Length
	{
		get => _ma1Length.Value;
		set => _ma1Length.Value = value;
	}

	/// <summary>
	/// Slow EMA period.
	/// </summary>
	public int Ma2Length
	{
		get => _ma2Length.Value;
		set => _ma2Length.Value = value;
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
	public MaCrossDmiStrategy()
	{
		_ma1Length = Param(nameof(Ma1Length), 10)
			.SetGreaterThanZero()
			.SetDisplay("MA1 Length", "Fast EMA period", "Moving Average");

		_ma2Length = Param(nameof(Ma2Length), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA2 Length", "Slow EMA period", "Moving Average");

		_dmiLength = Param(nameof(DmiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("DMI Length", "Directional movement period", "DMI");

		_adxSmoothing = Param(nameof(AdxSmoothing), 14)
			.SetGreaterThanZero()
			.SetDisplay("ADX Smoothing", "ADX smoothing period", "DMI");

		_keyLevel = Param(nameof(KeyLevel), 20m)
			.SetDisplay("Key Level", "ADX level required for entries", "DMI");

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
		_prevMa1 = null;
		_prevMa2 = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevMa1 = null;
		_prevMa2 = null;

		var ma1 = new ExponentialMovingAverage { Length = Ma1Length };
		var ma2 = new ExponentialMovingAverage { Length = Ma2Length };
		var dmi = new DirectionalIndex { Length = DmiLength };
		// ADX is the DX of the DmiLength directional lines smoothed over AdxSmoothing bars.
		_adx = new SmoothedMovingAverage { Length = AdxSmoothing };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(ma1, ma2, dmi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ma1);
			DrawIndicator(area, ma2);
			DrawOwnTrades(area);

			var dmiArea = CreateChartArea();
			if (dmiArea != null)
				DrawIndicator(dmiArea, dmi);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue ma1Value, IIndicatorValue ma2Value, IIndicatorValue dmiValue)
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

		if (!ma1Value.IsFormed || !ma2Value.IsFormed)
			return;

		var ma1 = ma1Value.ToDecimal();
		var ma2 = ma2Value.ToDecimal();

		var prevMa1 = _prevMa1;
		var prevMa2 = _prevMa2;
		_prevMa1 = ma1;
		_prevMa2 = ma2;

		if (!_adx.IsFormed || prevMa1 is not decimal lastMa1 || prevMa2 is not decimal lastMa2)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var crossUp = lastMa1 <= lastMa2 && ma1 > ma2;
		var crossDown = lastMa1 >= lastMa2 && ma1 < ma2;

		if (crossUp && diPlus > diMinus && adx > KeyLevel && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (crossDown && diMinus > diPlus && adx > KeyLevel && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (Position > 0 && crossDown)
			SellMarket(Position);
		else if (Position < 0 && crossUp)
			BuyMarket(-Position);
	}
}
