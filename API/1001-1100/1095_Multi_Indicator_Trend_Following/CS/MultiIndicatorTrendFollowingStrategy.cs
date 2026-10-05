using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi indicator trend following strategy.
/// Goes long when the fast EMA crosses above the slow EMA with RSI above 50 and volume above VolumeMultiplier times its average,
/// and short on the mirrored crossover with RSI below 50, reversing an opposite position. Positions are closed by a stop loss and a
/// take profit placed StopLossAtrMultiplier and TakeProfitAtrMultiplier ATRs away from the entry price.
/// </summary>
public class MultiIndicatorTrendFollowingStrategy : Strategy
{
	private const decimal _rsiMiddle = 50m;

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastMaLength;
	private readonly StrategyParam<int> _slowMaLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _volumeMaLength;
	private readonly StrategyParam<decimal> _volumeMultiplier;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _stopLossAtrMultiplier;
	private readonly StrategyParam<decimal> _takeProfitAtrMultiplier;

	private SimpleMovingAverage _volumeMa;
	private decimal? _prevFast;
	private decimal? _prevSlow;
	private decimal? _stopPrice;
	private decimal? _takePrice;

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Fast EMA length.
	/// </summary>
	public int FastMaLength
	{
		get => _fastMaLength.Value;
		set => _fastMaLength.Value = value;
	}

	/// <summary>
	/// Slow EMA length.
	/// </summary>
	public int SlowMaLength
	{
		get => _slowMaLength.Value;
		set => _slowMaLength.Value = value;
	}

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// Volume moving average length.
	/// </summary>
	public int VolumeMaLength
	{
		get => _volumeMaLength.Value;
		set => _volumeMaLength.Value = value;
	}

	/// <summary>
	/// Volume must exceed its average times this multiplier.
	/// </summary>
	public decimal VolumeMultiplier
	{
		get => _volumeMultiplier.Value;
		set => _volumeMultiplier.Value = value;
	}

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiple for the stop loss distance.
	/// </summary>
	public decimal StopLossAtrMultiplier
	{
		get => _stopLossAtrMultiplier.Value;
		set => _stopLossAtrMultiplier.Value = value;
	}

	/// <summary>
	/// ATR multiple for the take profit distance.
	/// </summary>
	public decimal TakeProfitAtrMultiplier
	{
		get => _takeProfitAtrMultiplier.Value;
		set => _takeProfitAtrMultiplier.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public MultiIndicatorTrendFollowingStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_fastMaLength = Param(nameof(FastMaLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA length", "Indicators");

		_slowMaLength = Param(nameof(SlowMaLength), 30)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA length", "Indicators");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "Indicators");

		_volumeMaLength = Param(nameof(VolumeMaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Volume MA Length", "Volume moving average length", "Volume");

		_volumeMultiplier = Param(nameof(VolumeMultiplier), 1.5m)
			.SetNotNegative()
			.SetDisplay("Volume Multiplier", "Volume must exceed its average times this", "Volume");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period", "Risk");

		_stopLossAtrMultiplier = Param(nameof(StopLossAtrMultiplier), 2m)
			.SetNotNegative()
			.SetDisplay("Stop ATR Mult", "ATR multiple for the stop loss", "Risk");

		_takeProfitAtrMultiplier = Param(nameof(TakeProfitAtrMultiplier), 3m)
			.SetNotNegative()
			.SetDisplay("Take ATR Mult", "ATR multiple for the take profit", "Risk");
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

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var fast = new ExponentialMovingAverage { Length = FastMaLength };
		var slow = new ExponentialMovingAverage { Length = SlowMaLength };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var atr = new AverageTrueRange { Length = AtrPeriod };
		_volumeMa = new SimpleMovingAverage { Length = VolumeMaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(fast, slow, rsi, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fast);
			DrawIndicator(area, slow);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
				DrawIndicator(oscillators, rsi);
		}
	}

	private void ResetState()
	{
		_prevFast = null;
		_prevSlow = null;
		_stopPrice = null;
		_takePrice = null;
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue fastValue, IIndicatorValue slowValue, IIndicatorValue rsiValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var volumeAverage = _volumeMa.Process(candle.TotalVolume, candle.OpenTime, true);

		var exited = false;

		// The ATR levels are checked against the candle range, so exits happen before new signals.
		if (Position > 0 && _stopPrice is decimal longStop && _takePrice is decimal longTake)
		{
			if (candle.LowPrice <= longStop || candle.HighPrice >= longTake)
			{
				SellMarket(Position);
				_stopPrice = null;
				_takePrice = null;
				exited = true;
			}
		}
		else if (Position < 0 && _stopPrice is decimal shortStop && _takePrice is decimal shortTake)
		{
			if (candle.HighPrice >= shortStop || candle.LowPrice <= shortTake)
			{
				BuyMarket(-Position);
				_stopPrice = null;
				_takePrice = null;
				exited = true;
			}
		}

		if (!fastValue.IsFormed || !slowValue.IsFormed || !rsiValue.IsFormed || !atrValue.IsFormed || !_volumeMa.IsFormed)
			return;

		var fast = fastValue.GetValue<decimal>();
		var slow = slowValue.GetValue<decimal>();
		var prevFast = _prevFast;
		var prevSlow = _prevSlow;
		_prevFast = fast;
		_prevSlow = slow;

		if (exited || prevFast is not decimal pf || prevSlow is not decimal ps)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var rsi = rsiValue.GetValue<decimal>();
		var atr = atrValue.GetValue<decimal>();
		var highVolume = candle.TotalVolume > volumeAverage.GetValue<decimal>() * VolumeMultiplier;

		var crossUp = pf <= ps && fast > slow;
		var crossDown = pf >= ps && fast < slow;

		if (crossUp && rsi > _rsiMiddle && highVolume && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			SetLevels(candle.ClosePrice, atr, true);
		}
		else if (crossDown && rsi < _rsiMiddle && highVolume && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			SetLevels(candle.ClosePrice, atr, false);
		}
	}

	private void SetLevels(decimal entry, decimal atr, bool isLong)
	{
		var stop = atr * StopLossAtrMultiplier;
		var take = atr * TakeProfitAtrMultiplier;

		// A zero multiplier disables that level.
		_stopPrice = StopLossAtrMultiplier > 0 ? (isLong ? entry - stop : entry + stop) : (isLong ? decimal.MinValue : decimal.MaxValue);
		_takePrice = TakeProfitAtrMultiplier > 0 ? (isLong ? entry + take : entry - take) : (isLong ? decimal.MaxValue : decimal.MinValue);
	}
}
