using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA crossover with volume, stacked take profits and trailing stop strategy.
/// The fast EMA crossing the slow EMA opens a position in the crossing direction, reversing an opposite one, when the candle
/// volume exceeds its average times VolumeMultiplier. A third of the position is taken at Tp1Multiplier ATR and another third at
/// Tp2Multiplier ATR from the entry. After price has moved TrailTriggerMultiplier ATR in favour a trailing stop follows the best
/// price at TrailOffsetMultiplier ATR. The ATR is frozen at entry.
/// </summary>
public class EmaCrossoverVolumeStackedTpTrailingSlStrategy : Strategy
{
	// The README does not give the volume average length.
	private const int _volumeAverageLength = 20;
	private const decimal _partialShare = 0.33m;

	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<decimal> _volumeMultiplier;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _tp1Multiplier;
	private readonly StrategyParam<decimal> _tp2Multiplier;
	private readonly StrategyParam<decimal> _trailOffsetMultiplier;
	private readonly StrategyParam<decimal> _trailTriggerMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _volumeAverage;
	private decimal? _prevFast;
	private decimal? _prevSlow;
	private decimal _entryPrice;
	private decimal _entryAtr;
	private decimal _entryVolume;
	private decimal _bestPrice;
	private bool _tp1Done;
	private bool _tp2Done;
	private bool _trailActive;

	/// <summary>
	/// Fast EMA length.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Slow EMA length.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// Multiplier of the average volume the candle volume must exceed.
	/// </summary>
	public decimal VolumeMultiplier
	{
		get => _volumeMultiplier.Value;
		set => _volumeMultiplier.Value = value;
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
	/// First take profit in ATR.
	/// </summary>
	public decimal Tp1Multiplier
	{
		get => _tp1Multiplier.Value;
		set => _tp1Multiplier.Value = value;
	}

	/// <summary>
	/// Second take profit in ATR.
	/// </summary>
	public decimal Tp2Multiplier
	{
		get => _tp2Multiplier.Value;
		set => _tp2Multiplier.Value = value;
	}

	/// <summary>
	/// Trailing distance in ATR.
	/// </summary>
	public decimal TrailOffsetMultiplier
	{
		get => _trailOffsetMultiplier.Value;
		set => _trailOffsetMultiplier.Value = value;
	}

	/// <summary>
	/// Favourable move in ATR that activates the trailing stop.
	/// </summary>
	public decimal TrailTriggerMultiplier
	{
		get => _trailTriggerMultiplier.Value;
		set => _trailTriggerMultiplier.Value = value;
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
	public EmaCrossoverVolumeStackedTpTrailingSlStrategy()
	{
		_fastLength = Param(nameof(FastLength), 21)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA length", "Indicators");

		_slowLength = Param(nameof(SlowLength), 55)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA length", "Indicators");

		_volumeMultiplier = Param(nameof(VolumeMultiplier), 1.2m)
			.SetNotNegative()
			.SetDisplay("Volume Multiplier", "Multiplier of the average volume", "Filters");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR length", "Indicators");

		_tp1Multiplier = Param(nameof(Tp1Multiplier), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("TP1 ATR", "First take profit in ATR", "Risk");

		_tp2Multiplier = Param(nameof(Tp2Multiplier), 2.5m)
			.SetGreaterThanZero()
			.SetDisplay("TP2 ATR", "Second take profit in ATR", "Risk");

		_trailOffsetMultiplier = Param(nameof(TrailOffsetMultiplier), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Trail Offset ATR", "Trailing distance in ATR", "Risk");

		_trailTriggerMultiplier = Param(nameof(TrailTriggerMultiplier), 1.5m)
			.SetNotNegative()
			.SetDisplay("Trail Trigger ATR", "Favourable move in ATR that activates trailing", "Risk");

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
		ResetState();
	}

	private void ResetState()
	{
		_prevFast = null;
		_prevSlow = null;
		_entryPrice = 0m;
		_entryAtr = 0m;
		_entryVolume = 0m;
		_bestPrice = 0m;
		_tp1Done = false;
		_tp2Done = false;
		_trailActive = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var fast = new ExponentialMovingAverage { Length = FastLength };
		var slow = new ExponentialMovingAverage { Length = SlowLength };
		var atr = new AverageTrueRange { Length = AtrLength };
		_volumeAverage = new SimpleMovingAverage { Length = _volumeAverageLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fast, slow, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fast);
			DrawIndicator(area, slow);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var volumeAverageValue = _volumeAverage.Process(new DecimalIndicatorValue(_volumeAverage, candle.TotalVolume, candle.OpenTime) { IsFinal = true });

		var prevFast = _prevFast;
		var prevSlow = _prevSlow;
		_prevFast = fast;
		_prevSlow = slow;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// An exit order sent on this candle is not reflected in Position yet.
		if (Position != 0 && ManagePosition(candle))
			return;

		if (!volumeAverageValue.IsFormed || prevFast is not decimal pf || prevSlow is not decimal ps || atr <= 0m)
			return;

		var volumeOk = candle.TotalVolume > volumeAverageValue.GetValue<decimal>() * VolumeMultiplier;
		if (!volumeOk)
			return;

		if (pf <= ps && fast > slow && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			OpenPosition(candle.ClosePrice, atr);
		}
		else if (pf >= ps && fast < slow && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			OpenPosition(candle.ClosePrice, atr);
		}
	}

	private void OpenPosition(decimal price, decimal atr)
	{
		_entryPrice = price;
		_entryAtr = atr;
		_entryVolume = Volume;
		_bestPrice = price;
		_tp1Done = false;
		_tp2Done = false;
		_trailActive = false;
	}

	private decimal PartialVolume()
	{
		var step = Security?.VolumeStep ?? 0m;
		var part = _entryVolume * _partialShare;

		if (step > 0m)
			part = Math.Floor(part / step) * step;

		return Math.Min(part, Math.Abs(Position));
	}

	private bool ManagePosition(ICandleMessage candle)
	{
		var trailDistance = TrailOffsetMultiplier * _entryAtr;

		if (Position > 0)
		{
			if (_trailActive && candle.LowPrice <= _bestPrice - trailDistance)
			{
				SellMarket(Position);
				return true;
			}

			_bestPrice = Math.Max(_bestPrice, candle.HighPrice);

			if (!_trailActive && _bestPrice - _entryPrice >= TrailTriggerMultiplier * _entryAtr)
				_trailActive = true;

			if (!_tp1Done && candle.HighPrice >= _entryPrice + Tp1Multiplier * _entryAtr)
			{
				_tp1Done = true;
				var part = PartialVolume();
				if (part > 0m)
				{
					SellMarket(part);
					return true;
				}
			}
			else if (_tp1Done && !_tp2Done && candle.HighPrice >= _entryPrice + Tp2Multiplier * _entryAtr)
			{
				_tp2Done = true;
				var part = PartialVolume();
				if (part > 0m)
				{
					SellMarket(part);
					return true;
				}
			}
		}
		else
		{
			if (_trailActive && candle.HighPrice >= _bestPrice + trailDistance)
			{
				BuyMarket(-Position);
				return true;
			}

			_bestPrice = Math.Min(_bestPrice, candle.LowPrice);

			if (!_trailActive && _entryPrice - _bestPrice >= TrailTriggerMultiplier * _entryAtr)
				_trailActive = true;

			if (!_tp1Done && candle.LowPrice <= _entryPrice - Tp1Multiplier * _entryAtr)
			{
				_tp1Done = true;
				var part = PartialVolume();
				if (part > 0m)
				{
					BuyMarket(part);
					return true;
				}
			}
			else if (_tp1Done && !_tp2Done && candle.LowPrice <= _entryPrice - Tp2Multiplier * _entryAtr)
			{
				_tp2Done = true;
				var part = PartialVolume();
				if (part > 0m)
				{
					BuyMarket(part);
					return true;
				}
			}
		}

		return false;
	}
}
