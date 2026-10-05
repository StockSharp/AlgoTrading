using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA pullback speed strategy.
/// A dynamic EMA changes its length between 5 and MaxLength with the candle body relative to the largest recent body and
/// speeds up with price acceleration scaled by AccelMultiplier. Speed accumulates candle bodies while the dynamic EMA keeps its
/// direction and restarts when it turns. A long needs the close above the dynamic EMA after the low came back within
/// ReturnThreshold percent of it, a bullish reversal candle, the short EMA above the long EMA and speed of at least LongSpeedMin;
/// a short mirrors it with speed of at most ShortSpeedMax. The stop is AtrMultiplier ATR from entry, the take profit FixedTpPct percent.
/// </summary>
public class EmaPullbackSpeedStrategy : Strategy
{
	private const int _minLength = 5;
	private const int _lookback = 200;

	private readonly StrategyParam<int> _maxLength;
	private readonly StrategyParam<decimal> _accelMultiplier;
	private readonly StrategyParam<decimal> _returnThreshold;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<decimal> _fixedTpPct;
	private readonly StrategyParam<int> _shortEmaLength;
	private readonly StrategyParam<int> _longEmaLength;
	private readonly StrategyParam<decimal> _longSpeedMin;
	private readonly StrategyParam<decimal> _shortSpeedMax;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<decimal> _bodies = new();
	private readonly Queue<decimal> _deltas = new();
	private decimal? _dynEma;
	private decimal? _prevClose;
	private decimal? _prevOpen;
	private int _direction;
	private decimal _speed;
	private decimal _stopPrice;
	private decimal _takePrice;

	/// <summary>
	/// Maximum length of the dynamic EMA.
	/// </summary>
	public int MaxLength
	{
		get => _maxLength.Value;
		set => _maxLength.Value = value;
	}

	/// <summary>
	/// Weight of price acceleration in the dynamic EMA.
	/// </summary>
	public decimal AccelMultiplier
	{
		get => _accelMultiplier.Value;
		set => _accelMultiplier.Value = value;
	}

	/// <summary>
	/// Maximum distance in percent between the low (high) and the dynamic EMA for a pullback.
	/// </summary>
	public decimal ReturnThreshold
	{
		get => _returnThreshold.Value;
		set => _returnThreshold.Value = value;
	}

	/// <summary>
	/// ATR length.
	/// </summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <summary>
	/// Stop loss distance in ATR.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Take profit percentage.
	/// </summary>
	public decimal FixedTpPct
	{
		get => _fixedTpPct.Value;
		set => _fixedTpPct.Value = value;
	}

	/// <summary>
	/// Short EMA length.
	/// </summary>
	public int ShortEmaLength
	{
		get => _shortEmaLength.Value;
		set => _shortEmaLength.Value = value;
	}

	/// <summary>
	/// Long EMA length.
	/// </summary>
	public int LongEmaLength
	{
		get => _longEmaLength.Value;
		set => _longEmaLength.Value = value;
	}

	/// <summary>
	/// Minimum speed for a long.
	/// </summary>
	public decimal LongSpeedMin
	{
		get => _longSpeedMin.Value;
		set => _longSpeedMin.Value = value;
	}

	/// <summary>
	/// Maximum speed for a short.
	/// </summary>
	public decimal ShortSpeedMax
	{
		get => _shortSpeedMax.Value;
		set => _shortSpeedMax.Value = value;
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
	public EmaPullbackSpeedStrategy()
	{
		_maxLength = Param(nameof(MaxLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("Max Length", "Maximum length of the dynamic EMA", "Dynamic EMA");

		_accelMultiplier = Param(nameof(AccelMultiplier), 3m)
			.SetNotNegative()
			.SetDisplay("Accel Multiplier", "Weight of price acceleration", "Dynamic EMA");

		_returnThreshold = Param(nameof(ReturnThreshold), 5m)
			.SetNotNegative()
			.SetDisplay("Return Threshold %", "Maximum pullback distance from the dynamic EMA", "Dynamic EMA");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR length", "Risk");

		_atrMultiplier = Param(nameof(AtrMultiplier), 4m)
			.SetNotNegative()
			.SetDisplay("ATR Multiplier", "Stop loss distance in ATR", "Risk");

		_fixedTpPct = Param(nameof(FixedTpPct), 1.5m)
			.SetNotNegative()
			.SetDisplay("Take Profit %", "Take profit percentage", "Risk");

		_shortEmaLength = Param(nameof(ShortEmaLength), 21)
			.SetGreaterThanZero()
			.SetDisplay("Short EMA", "Short EMA length", "Trend");

		_longEmaLength = Param(nameof(LongEmaLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("Long EMA", "Long EMA length", "Trend");

		_longSpeedMin = Param(nameof(LongSpeedMin), 1000m)
			.SetDisplay("Long Speed Min", "Minimum speed for a long", "Speed");

		_shortSpeedMax = Param(nameof(ShortSpeedMax), -1000m)
			.SetDisplay("Short Speed Max", "Maximum speed for a short", "Speed");

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
		ResetState();
	}

	private void ResetState()
	{
		_bodies.Clear();
		_deltas.Clear();
		_dynEma = null;
		_prevClose = null;
		_prevOpen = null;
		_direction = 0;
		_speed = 0m;
		_stopPrice = 0m;
		_takePrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var shortEma = new ExponentialMovingAverage { Length = ShortEmaLength };
		var longEma = new ExponentialMovingAverage { Length = LongEmaLength };
		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(shortEma, longEma, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, shortEma);
			DrawIndicator(area, longEma);
			DrawOwnTrades(area);
		}
	}

	private static void Push(Queue<decimal> queue, decimal value)
	{
		queue.Enqueue(value);
		if (queue.Count > _lookback)
			queue.Dequeue();
	}

	private void ProcessCandle(ICandleMessage candle, decimal shortEma, decimal longEma, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var open = candle.OpenPrice;
		var close = candle.ClosePrice;
		var body = close - open;
		var prevClose = _prevClose;
		var prevOpen = _prevOpen;
		_prevClose = close;
		_prevOpen = open;

		Push(_bodies, Math.Abs(body));
		Push(_deltas, prevClose is decimal pc ? Math.Abs(close - pc) : 0m);

		var maxBody = _bodies.Max();
		var maxDelta = _deltas.Max();

		// Larger bullish bodies lengthen the EMA, bearish ones shorten it, and acceleration speeds it up.
		var norm = maxBody > 0m ? (body + maxBody) / (2m * maxBody) : 0.5m;
		var dynLength = _minLength + norm * (MaxLength - _minLength);
		var accel = maxDelta > 0m ? _deltas.Last() / maxDelta : 0m;
		var alpha = Math.Min(1m, 2m / (dynLength + 1m) * (1m + accel * AccelMultiplier));

		var prevDyn = _dynEma;
		var dyn = prevDyn is decimal pd ? alpha * close + (1m - alpha) * pd : close;
		_dynEma = dyn;

		if (prevDyn is decimal previous)
		{
			var direction = Math.Sign(dyn - previous);
			if (direction != 0 && direction != _direction)
			{
				_direction = direction;
				_speed = 0m;
			}
			_speed += body;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position > 0)
		{
			if ((AtrMultiplier > 0m && candle.LowPrice <= _stopPrice) || (FixedTpPct > 0m && candle.HighPrice >= _takePrice))
				SellMarket(Position);
			return;
		}

		if (Position < 0)
		{
			if ((AtrMultiplier > 0m && candle.HighPrice >= _stopPrice) || (FixedTpPct > 0m && candle.LowPrice <= _takePrice))
				BuyMarket(-Position);
			return;
		}

		if (prevOpen is not decimal po || prevClose is not decimal pclose || prevDyn is null || dyn <= 0m)
			return;

		var threshold = ReturnThreshold / 100m;
		var bullishReversal = close > open && pclose < po;
		var bearishReversal = close < open && pclose > po;
		var returnedLong = candle.LowPrice <= dyn * (1m + threshold);
		var returnedShort = candle.HighPrice >= dyn * (1m - threshold);

		if (close > dyn && bullishReversal && returnedLong && _speed > 0m && _speed >= LongSpeedMin && shortEma > longEma)
		{
			_stopPrice = close - atr * AtrMultiplier;
			_takePrice = close * (1m + FixedTpPct / 100m);
			BuyMarket(Volume);
		}
		else if (close < dyn && bearishReversal && returnedShort && _speed < 0m && _speed <= ShortSpeedMax && shortEma < longEma)
		{
			_stopPrice = close + atr * AtrMultiplier;
			_takePrice = close * (1m - FixedTpPct / 100m);
			SellMarket(Volume);
		}
	}
}
