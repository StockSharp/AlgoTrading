using System;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public enum TrailingActivateMode
{
	EveryTick,
	NewBar,
}

/// <summary>
/// Converts the MetaTrader 5 "Trailing Activate" expert advisor into a StockSharp strategy.
/// The strategy manages existing positions by moving an internal trailing stop that mirrors the original logic.
/// </summary>
public class TrailingActivateStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<TrailingActivateMode> _trailingMode;
	private readonly StrategyParam<decimal> _trailingActivatePoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<decimal> _trailingStepPoints;

	private decimal? _longTrailingStop;
	private decimal? _shortTrailingStop;

	private decimal? _lastBid;
	private decimal? _lastAsk;

	private decimal _activateOffset;
	private decimal _stopOffset;
	private decimal _stepOffset;

	/// <summary>
	/// Initializes <see cref="TrailingActivateStrategy"/> with parameters matching the original expert advisor defaults.
	/// </summary>
	public TrailingActivateStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used when trailing on new bars", "General");

		_trailingMode = Param(nameof(TrailingMode), TrailingActivateMode.NewBar)
			.SetDisplay("Trailing Mode", "Choose between per-tick or per-bar trailing updates", "General");

		_trailingActivatePoints = Param(nameof(TrailingActivatePoints), 70m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Trailing Activate (points)", "Profit in points required before trailing starts", "Trailing");

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 250m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Trailing Stop (points)", "Distance between price and trailing stop in points", "Trailing");

		_trailingStepPoints = Param(nameof(TrailingStepPoints), 50m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Trailing Step (points)", "Minimum favorable move before shifting the trailing stop", "Trailing");
	}

	/// <summary>
	/// Candle type used when the trailing logic works on closed bars.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Defines whether the trailing stop reacts to every tick or to finished candles only.
	/// </summary>
	public TrailingActivateMode TrailingMode
	{
		get => _trailingMode.Value;
		set => _trailingMode.Value = value;
	}

	/// <summary>
	/// Profit threshold in points that must be reached before the trailing stop activates.
	/// </summary>
	public decimal TrailingActivatePoints
	{
		get => _trailingActivatePoints.Value;
		set => _trailingActivatePoints.Value = value;
	}

	/// <summary>
	/// Distance in points maintained between price and the trailing stop.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Minimum profit increment in points before the trailing stop is shifted.
	/// </summary>
	public decimal TrailingStepPoints
	{
		get => _trailingStepPoints.Value;
		set => _trailingStepPoints.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_longTrailingStop = null;
		_shortTrailingStop = null;
		_lastBid = null;
		_lastAsk = null;

		_activateOffset = 0m;
		_stopOffset = 0m;
		_stepOffset = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		UpdateOffsets();

		SubscribeCandles(CandleType)
			.Bind(ProcessCandle)
			.Start();

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (TrailingMode != TrailingActivateMode.NewBar)
			return;

		if (candle.State != CandleStates.Finished)
			return;

		UpdateOffsets();

		ApplyTrailingForLong(candle.ClosePrice, candle.LowPrice);
		ApplyTrailingForShort(candle.ClosePrice, candle.HighPrice);
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bid))
			_lastBid = (decimal)bid;

		if (level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var ask))
			_lastAsk = (decimal)ask;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (TrailingMode != TrailingActivateMode.EveryTick)
			return;

		UpdateOffsets();

		if (_lastBid is decimal currentBid)
			ApplyTrailingForLong(currentBid, currentBid);

		if (_lastAsk is decimal currentAsk)
			ApplyTrailingForShort(currentAsk, currentAsk);
	}

	private void ApplyTrailingForLong(decimal referencePrice, decimal testPrice)
	{
		if (Position <= 0)
		{
			_longTrailingStop = null;
			return;
		}

		if (_stopOffset <= 0m)
		{
			_longTrailingStop = null;
			return;
		}

		var entryPrice = Position.AveragePrice ?? referencePrice;

		var advance = referencePrice - entryPrice;
		var minimumMove = _stopOffset + _stepOffset;

		if (advance >= minimumMove && referencePrice - _stopOffset >= entryPrice + _activateOffset)
		{
			var proposedStop = referencePrice - _stopOffset;

			if (!_longTrailingStop.HasValue || proposedStop - _longTrailingStop.Value >= _stepOffset)
				_longTrailingStop = proposedStop;
		}

		if (_longTrailingStop is decimal stopPrice && testPrice <= stopPrice)
		{
			var volume = Math.Abs(Position);
			if (volume > 0m)
				SellMarket(volume);

			_longTrailingStop = null;
		}
	}

	private void ApplyTrailingForShort(decimal referencePrice, decimal testPrice)
	{
		if (Position >= 0)
		{
			_shortTrailingStop = null;
			return;
		}

		if (_stopOffset <= 0m)
		{
			_shortTrailingStop = null;
			return;
		}

		var entryPrice = Position.AveragePrice ?? referencePrice;

		var advance = entryPrice - referencePrice;
		var minimumMove = _stopOffset + _stepOffset;

		if (advance >= minimumMove && referencePrice + _stopOffset <= entryPrice - _activateOffset)
		{
			var proposedStop = referencePrice + _stopOffset;

			if (!_shortTrailingStop.HasValue || _shortTrailingStop.Value - proposedStop >= _stepOffset)
				_shortTrailingStop = proposedStop;
		}

		if (_shortTrailingStop is decimal stopPrice && testPrice >= stopPrice)
		{
			var volume = Math.Abs(Position);
			if (volume > 0m)
				BuyMarket(volume);

			_shortTrailingStop = null;
		}
	}

	private void UpdateOffsets()
	{
		var step = Security?.PriceStep ?? 1m;
		if (step <= 0m)
			step = 1m;

		_activateOffset = TrailingActivatePoints * step;
		_stopOffset = TrailingStopPoints * step;
		_stepOffset = TrailingStepPoints * step;
	}
}
