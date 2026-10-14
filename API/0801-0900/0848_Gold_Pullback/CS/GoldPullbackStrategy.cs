using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Gold pullback strategy.
/// The trend is up when the EmaFastLength EMA is above the EmaSlowLength EMA. A candle that touches the EmaPullbackLength EMA in an
/// uptrend, with the MACD (12, 26, 9) line above its signal, the TDI fast line (2-period SMA of RSI 13) above its signal line (7-period
/// SMA of the same RSI) and RSI above 50 goes long; the mirrored conditions go short. The stop is the signal candle low (high) minus
/// (plus) SlOffset, and the target lies at the same distance on the other side of the entry.
/// </summary>
public class GoldPullbackStrategy : Strategy
{
	private const int _rsiLength = 13;
	private const int _tdiFastLength = 2;
	private const int _tdiSignalLength = 7;

	private readonly StrategyParam<int> _emaFastLength;
	private readonly StrategyParam<int> _emaSlowLength;
	private readonly StrategyParam<int> _emaPullbackLength;
	private readonly StrategyParam<decimal> _slOffset;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _tdiFast;
	private SimpleMovingAverage _tdiSignal;
	private decimal _stopPrice;
	private decimal _takePrice;

	/// <summary>
	/// Fast trend EMA length.
	/// </summary>
	public int EmaFastLength
	{
		get => _emaFastLength.Value;
		set => _emaFastLength.Value = value;
	}

	/// <summary>
	/// Slow trend EMA length.
	/// </summary>
	public int EmaSlowLength
	{
		get => _emaSlowLength.Value;
		set => _emaSlowLength.Value = value;
	}

	/// <summary>
	/// Pullback EMA length.
	/// </summary>
	public int EmaPullbackLength
	{
		get => _emaPullbackLength.Value;
		set => _emaPullbackLength.Value = value;
	}

	/// <summary>
	/// Price offset added beyond the signal candle for the stop.
	/// </summary>
	public decimal SlOffset
	{
		get => _slOffset.Value;
		set => _slOffset.Value = value;
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
	public GoldPullbackStrategy()
	{
		_emaFastLength = Param(nameof(EmaFastLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("EMA Fast Length", "Fast trend EMA length", "Indicators");

		_emaSlowLength = Param(nameof(EmaSlowLength), 60)
			.SetGreaterThanZero()
			.SetDisplay("EMA Slow Length", "Slow trend EMA length", "Indicators");

		_emaPullbackLength = Param(nameof(EmaPullbackLength), 21)
			.SetGreaterThanZero()
			.SetDisplay("EMA Pullback Length", "Pullback EMA length", "Indicators");

		_slOffset = Param(nameof(SlOffset), 0.1m)
			.SetNotNegative()
			.SetDisplay("SL Offset", "Price offset added beyond the signal candle for the stop", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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
		_stopPrice = 0m;
		_takePrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_stopPrice = 0m;
		_takePrice = 0m;

		var emaFast = new ExponentialMovingAverage { Length = EmaFastLength };
		var emaSlow = new ExponentialMovingAverage { Length = EmaSlowLength };
		var emaPullback = new ExponentialMovingAverage { Length = EmaPullbackLength };
		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = 12 },
				LongMa = { Length = 26 },
			},
			SignalMa = { Length = 9 }
		};
		var rsi = new RelativeStrengthIndex { Length = _rsiLength };

		_tdiFast = new SimpleMovingAverage { Length = _tdiFastLength };
		_tdiSignal = new SimpleMovingAverage { Length = _tdiSignalLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(emaFast, emaSlow, emaPullback, macd, rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, emaFast);
			DrawIndicator(area, emaSlow);
			DrawIndicator(area, emaPullback);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue fastValue, IIndicatorValue slowValue, IIndicatorValue pullbackValue,
		IIndicatorValue macdValue, IIndicatorValue rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!rsiValue.IsFormed)
			return;

		var rsi = rsiValue.GetValue<decimal>();
		var tdiFast = _tdiFast.Process(new DecimalIndicatorValue(_tdiFast, rsi, candle.OpenTime) { IsFinal = true });
		var tdiSignal = _tdiSignal.Process(new DecimalIndicatorValue(_tdiSignal, rsi, candle.OpenTime) { IsFinal = true });

		if (ManagePosition(candle))
			return;

		if (!fastValue.IsFormed || !slowValue.IsFormed || !pullbackValue.IsFormed || !_tdiFast.IsFormed || !_tdiSignal.IsFormed)
			return;

		if (!macdValue.IsFormed || macdValue is not MovingAverageConvergenceDivergenceSignalValue { Macd: decimal macd, Signal: decimal signal })
			return;

		if (!IsFormedAndOnlineAndAllowTrading() || Position != 0)
			return;

		var emaFast = fastValue.GetValue<decimal>();
		var emaSlow = slowValue.GetValue<decimal>();
		var pullback = pullbackValue.GetValue<decimal>();
		var tdiMa = tdiFast.GetValue<decimal>();
		var tdiSig = tdiSignal.GetValue<decimal>();
		var touches = candle.LowPrice <= pullback && candle.HighPrice >= pullback;
		var close = candle.ClosePrice;

		if (touches && emaFast > emaSlow && macd > signal && tdiMa > tdiSig && rsi > 50m)
		{
			var stop = candle.LowPrice - SlOffset;
			if (stop >= close)
				return;

			BuyMarket(Volume);
			_stopPrice = stop;
			_takePrice = close + (close - stop);
		}
		else if (touches && emaFast < emaSlow && macd < signal && tdiMa < tdiSig && rsi < 50m)
		{
			var stop = candle.HighPrice + SlOffset;
			if (stop <= close)
				return;

			SellMarket(Volume);
			_stopPrice = stop;
			_takePrice = close - (stop - close);
		}
	}

	// Returns true when the position was closed on this candle.
	private bool ManagePosition(ICandleMessage candle)
	{
		if (Position > 0 && _stopPrice > 0)
		{
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takePrice)
			{
				SellMarket(Position);
				return true;
			}
		}
		else if (Position < 0 && _stopPrice > 0)
		{
			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takePrice)
			{
				BuyMarket(Math.Abs(Position));
				return true;
			}
		}

		return false;
	}
}
