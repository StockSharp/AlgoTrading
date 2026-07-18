using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Elder's Impulse System.
/// Uses EMA direction and MACD histogram to determine impulse.
/// Green (bullish): EMA rising + MACD histogram rising -> buy
/// Red (bearish): EMA falling + MACD histogram falling -> sell
/// </summary>
public class ElderImpulseStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevEma;
	private decimal _prevHistogram;
	private bool _hasPrevValues;
	private int _prevImpulse; // 1=green, -1=red, 0=neutral
	private int _cooldown;

	/// <summary>
	/// EMA period.
	/// </summary>
	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
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
	/// Initializes a new instance of the <see cref="ElderImpulseStrategy"/>.
	/// </summary>
	public ElderImpulseStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 13)
			.SetDisplay("EMA Period", "Period for EMA calculation", "Indicators")
			.SetOptimize(8, 21, 3);

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
		_prevEma = default;
		_prevHistogram = default;
		_hasPrevValues = default;
		_prevImpulse = default;
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var macdSignal = new MovingAverageConvergenceDivergenceSignal();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(ema, macdSignal, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawIndicator(area, macdSignal);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue emaValue, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (emaValue.IsEmpty)
			return;

		var emaDec = emaValue.GetValue<decimal>();
		if (emaDec == 0)
			return;

		var macdTyped = (IMovingAverageConvergenceDivergenceSignalValue)macdValue;
		if (macdTyped.Macd is not decimal macd || macdTyped.Signal is not decimal signal)
			return;

		var histogram = macd - signal;

		if (!_hasPrevValues)
		{
			_hasPrevValues = true;
			_prevEma = emaDec;
			_prevHistogram = histogram;
			return;
		}

		var emaRising = emaDec > _prevEma;
		var histogramRising = histogram > _prevHistogram;

		// Determine current impulse
		int impulse;
		if (emaRising && histogramRising)
			impulse = 1;  // Green bar
		else if (!emaRising && !histogramRising && emaDec != _prevEma)
			impulse = -1; // Red bar
		else
			impulse = 0;  // Neutral (blue bar)

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevEma = emaDec;
			_prevHistogram = histogram;
			_prevImpulse = impulse;
			return;
		}

		// Trade only on impulse change
		if (impulse == 1 && _prevImpulse != 1 && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			_cooldown = 65;
		}
		else if (impulse == -1 && _prevImpulse != -1 && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			_cooldown = 65;
		}

		_prevEma = emaDec;
		_prevHistogram = histogram;
		_prevImpulse = impulse;
	}
}
