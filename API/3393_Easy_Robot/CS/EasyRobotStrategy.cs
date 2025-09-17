using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Momentum strategy that buys after a bullish hourly candle and sells after a bearish one.
/// Protective orders are based on ATR multiples and optionally trail with MetaTrader style pip distances.
/// </summary>
public class EasyRobotStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeFactor;
	private readonly StrategyParam<decimal> _stopFactor;
	private readonly StrategyParam<bool> _useTrailingStop;
	private readonly StrategyParam<decimal> _trailingStartPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private AverageTrueRange _atr = null!;
	private decimal _pipSize;
	private decimal _priceStep;
	private decimal _minStopDistance;
	private decimal? _entryPrice;
	private decimal? _longStopPrice;
	private decimal? _shortStopPrice;

	/// <summary>
	/// ATR take-profit multiplier.
	/// </summary>
	public decimal TakeFactor
	{
		get => _takeFactor.Value;
		set => _takeFactor.Value = value;
	}

	/// <summary>
	/// ATR stop-loss multiplier.
	/// </summary>
	public decimal StopFactor
	{
		get => _stopFactor.Value;
		set => _stopFactor.Value = value;
	}

	/// <summary>
	/// Enable trailing stop management.
	/// </summary>
	public bool UseTrailingStop
	{
		get => _useTrailingStop.Value;
		set => _useTrailingStop.Value = value;
	}

	/// <summary>
	/// Profit in MetaTrader pips required to activate trailing.
	/// </summary>
	public decimal TrailingStartPips
	{
		get => _trailingStartPips.Value;
		set => _trailingStartPips.Value = value;
	}

	/// <summary>
	/// MetaTrader pip distance between trailing stop updates.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
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
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="EasyRobotStrategy"/>.
	/// </summary>
	public EasyRobotStrategy()
	{
		_takeFactor = Param(nameof(TakeFactor), 4.2m)
			.SetDisplay("Take factor", "ATR multiplier used for take-profit", "Risk management");

		_stopFactor = Param(nameof(StopFactor), 4.9m)
			.SetDisplay("Stop factor", "ATR multiplier used for stop-loss", "Risk management");

		_useTrailingStop = Param(nameof(UseTrailingStop), true)
			.SetDisplay("Use trailing", "Enable MetaTrader style trailing stop", "Risk management");

		_trailingStartPips = Param(nameof(TrailingStartPips), 40m)
			.SetDisplay("Trailing start (pips)", "Required profit before trailing activates", "Risk management");

		_trailingStepPips = Param(nameof(TrailingStepPips), 19m)
			.SetDisplay("Trailing step (pips)", "Distance between trailing stop moves", "Risk management");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR period", "Period for ATR calculation", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle type", "Time frame used for signals", "General");
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

		_entryPrice = null;
		_longStopPrice = null;
		_shortStopPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_priceStep = Security?.PriceStep ?? 0m;
		_pipSize = GetPipSize();
		_minStopDistance = CalculateMinStopDistance();

		_atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_atr, ProcessCandle)
			.Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _atr);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue)
	{
		// Work with finished hourly candles only.
		if (candle.State != CandleStates.Finished)
			return;

		UpdateTrailing(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_atr.IsFormed || atrValue <= 0m)
			return;

		if (Position != 0m)
			return;

		var direction = Math.Sign(candle.ClosePrice - candle.OpenPrice);
		if (direction > 0)
			TryOpenLong(candle, atrValue);
		else if (direction < 0)
			TryOpenShort(candle, atrValue);
	}

	private void TryOpenLong(ICandleMessage candle, decimal atrValue)
	{
		var ask = GetCurrentAskPrice(candle);
		if (ask <= 0m)
			return;

		var stopDistance = NormalizeProtectiveDistance(atrValue * StopFactor);
		var takeDistance = NormalizeProtectiveDistance(atrValue * TakeFactor);

		if (stopDistance <= 0m || takeDistance <= 0m)
			return;

		var stopSteps = ConvertPriceToSteps(stopDistance);
		var takeSteps = ConvertPriceToSteps(takeDistance);

		BuyMarket(Volume + Math.Abs(Position));

		var resultingPosition = Position + Volume;
		if (stopSteps > 0m)
			SetStopLoss(stopSteps, ask, resultingPosition);
		if (takeSteps > 0m)
			SetTakeProfit(takeSteps, ask, resultingPosition);

		_entryPrice = ask;
		_longStopPrice = ask - stopDistance;
		_shortStopPrice = null;
	}

	private void TryOpenShort(ICandleMessage candle, decimal atrValue)
	{
		var bid = GetCurrentBidPrice(candle);
		if (bid <= 0m)
			return;

		var stopDistance = NormalizeProtectiveDistance(atrValue * StopFactor);
		var takeDistance = NormalizeProtectiveDistance(atrValue * TakeFactor);

		if (stopDistance <= 0m || takeDistance <= 0m)
			return;

		var stopSteps = ConvertPriceToSteps(stopDistance);
		var takeSteps = ConvertPriceToSteps(takeDistance);

		SellMarket(Volume + Math.Max(Position, 0m));

		var resultingPosition = Position - Volume;
		if (stopSteps > 0m)
			SetStopLoss(stopSteps, bid, resultingPosition);
		if (takeSteps > 0m)
			SetTakeProfit(takeSteps, bid, resultingPosition);

		_entryPrice = bid;
		_shortStopPrice = bid + stopDistance;
		_longStopPrice = null;
	}

	private void UpdateTrailing(ICandleMessage candle)
	{
		if (!UseTrailingStop || Position == 0m)
			return;

		var pipSize = _pipSize;
		if (pipSize <= 0m)
			return;

		var trailingStart = TrailingStartPips * pipSize;
		var trailingStep = TrailingStepPips * pipSize;
		var minDistance = _minStopDistance;

		if (Position > 0m)
		{
			var bid = GetCurrentBidPrice(candle);
			if (bid <= 0m || trailingStart <= 0m || trailingStep <= 0m)
				return;

			var entry = _entryPrice ?? PositionPrice;
			if (entry <= 0m || bid - entry < trailingStart)
				return;

			var candidate = bid - trailingStep;
			if (candidate > bid - minDistance)
				candidate = bid - minDistance;

			if (candidate <= 0m)
				return;

			if (_longStopPrice is decimal current && candidate <= current + (_priceStep > 0m ? _priceStep / 2m : 0m))
				return;

			var distance = bid - candidate;
			if (distance <= 0m)
				return;

			var steps = ConvertPriceToSteps(distance);
			if (steps <= 0m)
				return;

			SetStopLoss(steps, bid, Position);
			_longStopPrice = candidate;
		}
		else
		{
			var ask = GetCurrentAskPrice(candle);
			if (ask <= 0m || trailingStart <= 0m || trailingStep <= 0m)
				return;

			var entry = _entryPrice ?? PositionPrice;
			if (entry <= 0m || entry - ask < trailingStart)
				return;

			var candidate = ask + trailingStep;
			if (candidate < ask + minDistance)
				candidate = ask + minDistance;

			if (candidate <= 0m)
				return;

			if (_shortStopPrice is decimal current && candidate >= current - (_priceStep > 0m ? _priceStep / 2m : 0m))
				return;

			var distance = candidate - ask;
			if (distance <= 0m)
				return;

			var steps = ConvertPriceToSteps(distance);
			if (steps <= 0m)
				return;

			SetStopLoss(steps, ask, Position);
			_shortStopPrice = candidate;
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
		{
			_entryPrice = null;
			_longStopPrice = null;
			_shortStopPrice = null;
		}
	}

	private decimal NormalizeProtectiveDistance(decimal distance)
	{
		if (distance <= 0m)
			return 0m;

		return distance < _minStopDistance ? _minStopDistance : distance;
	}

	private decimal ConvertPriceToSteps(decimal distance)
	{
		if (distance <= 0m)
			return 0m;

		var step = _priceStep;
		if (step <= 0m)
			return distance;

		return distance / step;
	}

	private decimal GetCurrentAskPrice(ICandleMessage candle)
	{
	var ask = Security?.BestAsk?.Price ?? 0m;
	if (ask <= 0m)
	ask = Security?.LastPrice ?? 0m;
	if (ask <= 0m)
	ask = candle.ClosePrice;
	return ask;
	}

	private decimal GetCurrentBidPrice(ICandleMessage candle)
	{
	var bid = Security?.BestBid?.Price ?? 0m;
	if (bid <= 0m)
	bid = Security?.LastPrice ?? 0m;
	if (bid <= 0m)
	bid = candle.ClosePrice;
	return bid;
	}

	private decimal GetPipSize()
	{
	var step = Security?.PriceStep ?? 0.0001m;

	if (Security?.Decimals is int decimals && (decimals == 3 || decimals == 5))
	return step * 10m;

	return step;
	}

	private decimal CalculateMinStopDistance()
	{
	var pip = _pipSize;
	var step = _priceStep;
	if (pip > 0m)
	return pip;
	if (step > 0m)
	return step;
	return 0m;
	}
}
