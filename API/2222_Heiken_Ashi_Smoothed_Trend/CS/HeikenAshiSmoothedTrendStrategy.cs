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
/// Heiken-Ashi strategy using EMA-smoothed candles.
/// Opens long positions when candle color turns bullish and short positions when it turns bearish.
/// </summary>
public class HeikenAshiSmoothedTrendStrategy : Strategy
{
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _openEma;
	private ExponentialMovingAverage _closeEma;
	private ExponentialMovingAverage _highEma;
	private ExponentialMovingAverage _lowEma;

	private decimal? _prevHaOpen;
	private decimal? _prevHaClose;
	private bool? _prevIsGreen;

	/// <summary>
	/// Length for EMA smoothing.
	/// </summary>
	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	/// <summary>
	/// Type of candles to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="HeikenAshiSmoothedTrendStrategy"/>.
	/// </summary>
	public HeikenAshiSmoothedTrendStrategy()
	{
		_emaLength = Param(nameof(EmaLength), 30)
			.SetDisplay("EMA Length", "Length for smoothing", "General")
			.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
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
		_openEma = null;
		_closeEma = null;
		_highEma = null;
		_lowEma = null;
		_prevHaOpen = null;
		_prevHaClose = null;
		_prevIsGreen = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_openEma = new ExponentialMovingAverage { Length = EmaLength };
		_closeEma = new ExponentialMovingAverage { Length = EmaLength };
		_highEma = new ExponentialMovingAverage { Length = EmaLength };
		_lowEma = new ExponentialMovingAverage { Length = EmaLength };

		Indicators.Add(_openEma);
		Indicators.Add(_closeEma);
		Indicators.Add(_highEma);
		Indicators.Add(_lowEma);

		// Use a dummy EMA for warmup/binding, do manual EMA processing inside
		var warmup = new ExponentialMovingAverage { Length = EmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(warmup, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal _warmupVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var t = candle.OpenTime;

		var oResult = _openEma.Process(new DecimalIndicatorValue(_openEma, candle.OpenPrice, t) { IsFinal = true });
		var cResult = _closeEma.Process(new DecimalIndicatorValue(_closeEma, candle.ClosePrice, t) { IsFinal = true });
		var hResult = _highEma.Process(new DecimalIndicatorValue(_highEma, candle.HighPrice, t) { IsFinal = true });
		var lResult = _lowEma.Process(new DecimalIndicatorValue(_lowEma, candle.LowPrice, t) { IsFinal = true });

		if (!oResult.IsFormed || !cResult.IsFormed || !hResult.IsFormed || !lResult.IsFormed)
			return;

		var openEma = oResult.GetValue<decimal>();
		var closeEma = cResult.GetValue<decimal>();
		var highEma = hResult.GetValue<decimal>();
		var lowEma = lResult.GetValue<decimal>();

		var haClose = (openEma + highEma + lowEma + closeEma) / 4m;
		var haOpen = _prevHaOpen is null ? (openEma + closeEma) / 2m : (_prevHaOpen.Value + _prevHaClose!.Value) / 2m;

		var isGreen = haClose >= haOpen;
		var buySignal = isGreen && _prevIsGreen == false;
		var sellSignal = !isGreen && _prevIsGreen == true;

		if (IsFormedAndOnlineAndAllowTrading())
		{
			if (buySignal && Position <= 0)
				BuyMarket();
			else if (sellSignal && Position >= 0)
				SellMarket();
		}

		_prevHaOpen = haOpen;
		_prevHaClose = haClose;
		_prevIsGreen = isGreen;
	}
}
