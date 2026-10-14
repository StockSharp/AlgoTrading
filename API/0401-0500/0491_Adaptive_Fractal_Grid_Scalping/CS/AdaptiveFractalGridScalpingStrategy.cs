using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Adaptive Fractal Grid Scalping strategy.
/// Five-bar fractals mark the grid. While ATR is above VolatilityThreshold the strategy keeps a buy limit at the last fractal low minus
/// ATR * GridMultiplierLow when the close is above the SMA, or a sell limit at the last fractal high plus the same distance when it is
/// below. A filled long exits at the opposite grid level (fractal high plus ATR * GridMultiplierHigh) or by a trailing stop
/// ATR * TrailStopMultiplier behind the best price; shorts mirror this.
/// </summary>
public class AdaptiveFractalGridScalpingStrategy : Strategy
{
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<int> _smaLength;
	private readonly StrategyParam<decimal> _gridMultiplierHigh;
	private readonly StrategyParam<decimal> _gridMultiplierLow;
	private readonly StrategyParam<decimal> _trailStopMultiplier;
	private readonly StrategyParam<decimal> _volatilityThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _highs = [];
	private readonly List<decimal> _lows = [];
	private decimal? _fractalHigh;
	private decimal? _fractalLow;
	private Order _entryOrder;
	private decimal? _target;
	private decimal? _trailStop;

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <summary>
	/// Period of the trend SMA.
	/// </summary>
	public int SmaLength
	{
		get => _smaLength.Value;
		set => _smaLength.Value = value;
	}

	/// <summary>
	/// ATR multiplier of the opposite grid level used as the target.
	/// </summary>
	public decimal GridMultiplierHigh
	{
		get => _gridMultiplierHigh.Value;
		set => _gridMultiplierHigh.Value = value;
	}

	/// <summary>
	/// ATR multiplier of the entry grid level.
	/// </summary>
	public decimal GridMultiplierLow
	{
		get => _gridMultiplierLow.Value;
		set => _gridMultiplierLow.Value = value;
	}

	/// <summary>
	/// ATR multiplier of the trailing stop distance.
	/// </summary>
	public decimal TrailStopMultiplier
	{
		get => _trailStopMultiplier.Value;
		set => _trailStopMultiplier.Value = value;
	}

	/// <summary>
	/// ATR level above which the grid is active.
	/// </summary>
	public decimal VolatilityThreshold
	{
		get => _volatilityThreshold.Value;
		set => _volatilityThreshold.Value = value;
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
	public AdaptiveFractalGridScalpingStrategy()
	{
		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR period", "Indicators");

		_smaLength = Param(nameof(SmaLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("SMA Length", "Period of the trend SMA", "Indicators");

		_gridMultiplierHigh = Param(nameof(GridMultiplierHigh), 2.0m)
			.SetNotNegative()
			.SetDisplay("Grid Multiplier High", "ATR multiplier of the opposite grid level used as the target", "Grid");

		_gridMultiplierLow = Param(nameof(GridMultiplierLow), 0.5m)
			.SetNotNegative()
			.SetDisplay("Grid Multiplier Low", "ATR multiplier of the entry grid level", "Grid");

		_trailStopMultiplier = Param(nameof(TrailStopMultiplier), 0.5m)
			.SetNotNegative()
			.SetDisplay("Trail Stop Multiplier", "ATR multiplier of the trailing stop distance", "Risk");

		_volatilityThreshold = Param(nameof(VolatilityThreshold), 1.0m)
			.SetNotNegative()
			.SetDisplay("Volatility Threshold", "ATR level above which the grid is active", "Grid");

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

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var atr = new AverageTrueRange { Length = AtrLength };
		var sma = new SimpleMovingAverage { Length = SmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(atr, sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ResetState()
	{
		_highs.Clear();
		_lows.Clear();
		_fractalHigh = null;
		_fractalLow = null;
		_entryOrder = null;
		_target = null;
		_trailStop = null;
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue atrValue, IIndicatorValue smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateFractals(candle);

		if (!atrValue.IsFormed || !smaValue.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var atr = atrValue.ToDecimal();
		var sma = smaValue.ToDecimal();
		var close = candle.ClosePrice;

		if (Position != 0)
		{
			CancelEntry();
			ManagePosition(candle, atr);
			return;
		}

		_target = null;
		_trailStop = null;

		// A pending order that is still being registered is left alone until its state is known.
		if (_entryOrder != null && _entryOrder.State is OrderStates.None or OrderStates.Pending)
			return;

		CancelEntry();

		if (atr <= VolatilityThreshold)
			return;

		var offset = atr * GridMultiplierLow;

		if (close > sma && _fractalLow is decimal low)
			_entryOrder = BuyLimit(RoundPrice(low - offset), Volume);
		else if (close < sma && _fractalHigh is decimal high)
			_entryOrder = SellLimit(RoundPrice(high + offset), Volume);
	}

	private void ManagePosition(ICandleMessage candle, decimal atr)
	{
		var trailDistance = atr * TrailStopMultiplier;

		if (Position > 0)
		{
			_target ??= _fractalHigh is decimal fh ? fh + atr * GridMultiplierHigh : null;
			var newStop = candle.HighPrice - trailDistance;
			_trailStop = _trailStop is decimal s ? Math.Max(s, newStop) : newStop;

			if ((_target is decimal t && candle.HighPrice >= t) || candle.ClosePrice <= _trailStop)
				SellMarket(Position);
		}
		else
		{
			_target ??= _fractalLow is decimal fl ? fl - atr * GridMultiplierHigh : null;
			var newStop = candle.LowPrice + trailDistance;
			_trailStop = _trailStop is decimal s ? Math.Min(s, newStop) : newStop;

			if ((_target is decimal t && candle.LowPrice <= t) || candle.ClosePrice >= _trailStop)
				BuyMarket(-Position);
		}
	}

	private void UpdateFractals(ICandleMessage candle)
	{
		_highs.Add(candle.HighPrice);
		_lows.Add(candle.LowPrice);

		if (_highs.Count > 5)
		{
			_highs.RemoveAt(0);
			_lows.RemoveAt(0);
		}

		if (_highs.Count < 5)
			return;

		// The middle bar is a fractal when it beats the two bars on each side.
		var midHigh = _highs[2];
		if (midHigh > _highs[0] && midHigh > _highs[1] && midHigh > _highs[3] && midHigh > _highs[4])
			_fractalHigh = midHigh;

		var midLow = _lows[2];
		if (midLow < _lows[0] && midLow < _lows[1] && midLow < _lows[3] && midLow < _lows[4])
			_fractalLow = midLow;
	}

	private void CancelEntry()
	{
		if (_entryOrder != null && _entryOrder.State == OrderStates.Active)
			CancelOrder(_entryOrder);

		_entryOrder = null;
	}

	private decimal RoundPrice(decimal price)
	{
		var step = Security.PriceStep ?? 0m;
		return step > 0 ? Math.Round(price / step) * step : price;
	}
}
