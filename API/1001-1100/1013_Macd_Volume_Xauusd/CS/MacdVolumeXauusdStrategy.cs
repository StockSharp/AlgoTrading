using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MACD Volume XAUUSD strategy.
/// The volume oscillator is 100 * (EMA(volume, ShortLength) - EMA(volume, LongLength)) / EMA(volume, LongLength). The MACD line
/// crossing above zero with a positive oscillator and volume above the previous candle's volume goes long; crossing below zero
/// under the same volume conditions goes short, reversing an opposite position. The stop is StopLoss price steps from the entry
/// and the take profit is StopLoss * TakeProfitMultiplier steps. Leverage scales the order volume.
/// </summary>
public class MacdVolumeXauusdStrategy : Strategy
{
	private readonly StrategyParam<int> _shortLength;
	private readonly StrategyParam<int> _longLength;
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<decimal> _leverage;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfitMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _shortVolumeEma;
	private ExponentialMovingAverage _longVolumeEma;
	private decimal? _prevMacd;
	private decimal? _prevVolume;

	/// <summary>
	/// Short volume EMA length.
	/// </summary>
	public int ShortLength
	{
		get => _shortLength.Value;
		set => _shortLength.Value = value;
	}

	/// <summary>
	/// Long volume EMA length.
	/// </summary>
	public int LongLength
	{
		get => _longLength.Value;
		set => _longLength.Value = value;
	}

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
	/// MACD signal line length.
	/// </summary>
	public int SignalLength
	{
		get => _signalLength.Value;
		set => _signalLength.Value = value;
	}

	/// <summary>
	/// Multiplier of the order volume.
	/// </summary>
	public decimal Leverage
	{
		get => _leverage.Value;
		set => _leverage.Value = value;
	}

	/// <summary>
	/// Stop loss distance in price steps.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take profit distance as a multiple of the stop loss.
	/// </summary>
	public decimal TakeProfitMultiplier
	{
		get => _takeProfitMultiplier.Value;
		set => _takeProfitMultiplier.Value = value;
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
	public MacdVolumeXauusdStrategy()
	{
		_shortLength = Param(nameof(ShortLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Short Length", "Short volume EMA length", "Volume");

		_longLength = Param(nameof(LongLength), 8)
			.SetGreaterThanZero()
			.SetDisplay("Long Length", "Long volume EMA length", "Volume");

		_fastLength = Param(nameof(FastLength), 16)
			.SetGreaterThanZero()
			.SetDisplay("Fast Length", "MACD fast EMA length", "MACD");

		_slowLength = Param(nameof(SlowLength), 26)
			.SetGreaterThanZero()
			.SetDisplay("Slow Length", "MACD slow EMA length", "MACD");

		_signalLength = Param(nameof(SignalLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("Signal Length", "MACD signal line length", "MACD");

		_leverage = Param(nameof(Leverage), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Leverage", "Multiplier of the order volume", "Risk");

		_stopLoss = Param(nameof(StopLoss), 10100m)
			.SetNotNegative()
			.SetDisplay("Stop Loss", "Stop loss distance in price steps", "Risk");

		_takeProfitMultiplier = Param(nameof(TakeProfitMultiplier), 1.1m)
			.SetNotNegative()
			.SetDisplay("Take Profit Multiplier", "Take profit distance as a multiple of the stop loss", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
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
		_shortVolumeEma = null;
		_longVolumeEma = null;
		_prevMacd = null;
		_prevVolume = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevMacd = null;
		_prevVolume = null;

		_shortVolumeEma = new ExponentialMovingAverage { Length = ShortLength };
		_longVolumeEma = new ExponentialMovingAverage { Length = LongLength };

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

		var step = Security?.PriceStep ?? 1m;
		StartProtection(
			new Unit(StopLoss * TakeProfitMultiplier * step, UnitTypes.Absolute),
			new Unit(StopLoss * step, UnitTypes.Absolute),
			useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
				DrawIndicator(oscillators, macd);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var volume = candle.TotalVolume;
		var shortValue = _shortVolumeEma.Process(new DecimalIndicatorValue(_shortVolumeEma, volume, candle.OpenTime) { IsFinal = true });
		var longValue = _longVolumeEma.Process(new DecimalIndicatorValue(_longVolumeEma, volume, candle.OpenTime) { IsFinal = true });

		var prevVolume = _prevVolume;
		_prevVolume = volume;

		if (!macdValue.IsFormed || macdValue is not IMovingAverageConvergenceDivergenceSignalValue { Macd: decimal macdLine })
			return;

		var prevMacd = _prevMacd;
		_prevMacd = macdLine;

		if (!_shortVolumeEma.IsFormed || !_longVolumeEma.IsFormed || prevMacd is not decimal pm || prevVolume is not decimal pv)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var longEma = longValue.GetValue<decimal>();
		if (longEma == 0)
			return;

		var oscillator = 100m * (shortValue.GetValue<decimal>() - longEma) / longEma;
		var volumeOk = oscillator > 0 && volume > pv;
		var orderVolume = Volume * Leverage;

		if (volumeOk && pm <= 0 && macdLine > 0 && Position <= 0)
			BuyMarket(orderVolume + Math.Abs(Position));
		else if (volumeOk && pm >= 0 && macdLine < 0 && Position >= 0)
			SellMarket(orderVolume + Math.Abs(Position));
	}
}
