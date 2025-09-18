using System;
using System.Collections.Generic;
using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Reverse Day Fractal strategy trades breakouts of local highs and lows formed over the last two candles.
/// Opens long positions when the current bar posts a lower low and closes above the open.
/// Opens short positions when the current bar posts a higher high and closes below the open.
/// Uses optional take profit, stop loss, and trailing stop protections.
/// </summary>
public class ReverseDayFractalStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<decimal> _trailingStepPoints;
	private readonly StrategyParam<bool> _onlyOnePosition;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _previousLow1;
	private decimal? _previousLow2;
	private decimal? _previousHigh1;
	private decimal? _previousHigh2;
	private DateTimeOffset? _lastBuyBarTime;
	private DateTimeOffset? _lastSellBarTime;

	/// <summary>
	/// Trade volume used for new market entries.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in price steps.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in price steps.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in price steps.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop step expressed in price steps.
	/// </summary>
	public decimal TrailingStepPoints
	{
		get => _trailingStepPoints.Value;
		set => _trailingStepPoints.Value = value;
	}

	/// <summary>
	/// Determines whether strategy can hold only a single position at a time.
	/// </summary>
	public bool OnlyOnePosition
	{
		get => _onlyOnePosition.Value;
		set => _onlyOnePosition.Value = value;
	}

	/// <summary>
	/// Candle type used for signal calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy with default parameters.
	/// </summary>
	public ReverseDayFractalStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 0.01m)
			.SetDisplay("Trade Volume", "Volume used for each market order", "Trading");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 10m)
			.SetDisplay("Take Profit", "Take profit distance in price steps", "Risk");

		_stopLossPoints = Param(nameof(StopLossPoints), 20m)
			.SetDisplay("Stop Loss", "Stop loss distance in price steps", "Risk");

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 25m)
			.SetDisplay("Trailing Stop", "Trailing stop distance in price steps", "Risk");

		_trailingStepPoints = Param(nameof(TrailingStepPoints), 5m)
			.SetDisplay("Trailing Step", "Step for trailing stop adjustment", "Risk");

		_onlyOnePosition = Param(nameof(OnlyOnePosition), true)
			.SetDisplay("Only One Position", "Allow only a single position at a time", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles used for signals", "General");
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
		_previousLow1 = null;
		_previousLow2 = null;
		_previousHigh1 = null;
		_previousHigh2 = null;
		_lastBuyBarTime = null;
		_lastSellBarTime = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var takeProfitUnit = TakeProfitPoints > 0 ? new Unit(TakeProfitPoints, UnitTypes.Step) : null;
		var stopLossUnit = StopLossPoints > 0 ? new Unit(StopLossPoints, UnitTypes.Step) : null;
		var trailingStopUnit = TrailingStopPoints > 0 ? new Unit(TrailingStopPoints, UnitTypes.Step) : null;
		var trailingStepUnit = TrailingStepPoints > 0 ? new Unit(TrailingStepPoints, UnitTypes.Step) : null;

		StartProtection(
			takeProfit: takeProfitUnit,
			stopLoss: stopLossUnit,
			trailingStop: trailingStopUnit,
			trailingStep: trailingStepUnit);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (candle.OpenPrice is not decimal openPrice ||
			candle.ClosePrice is not decimal closePrice ||
			candle.LowPrice is not decimal lowPrice ||
			candle.HighPrice is not decimal highPrice)
		{
			return;
		}

		if (_previousLow1 is null || _previousLow2 is null || _previousHigh1 is null || _previousHigh2 is null)
		{
			UpdateHistory(lowPrice, highPrice);
			return;
		}

		var isBullishReversal = lowPrice < _previousLow1 && lowPrice < _previousLow2 && closePrice > openPrice;
		var isBearishReversal = highPrice > _previousHigh1 && highPrice > _previousHigh2 && closePrice < openPrice;

		var barTime = candle.OpenTime;

		if (isBullishReversal && barTime != _lastBuyBarTime)
		{
			TryOpenPosition(isLong: true);
			_lastBuyBarTime = barTime;
		}

		if (isBearishReversal && barTime != _lastSellBarTime)
		{
			TryOpenPosition(isLong: false);
			_lastSellBarTime = barTime;
		}

		UpdateHistory(lowPrice, highPrice);
	}

	private void TryOpenPosition(bool isLong)
	{
		if (OnlyOnePosition && Position != 0)
			return;

		var targetVolume = TradeVolume;

		if (isLong)
		{
			if (Position > 0)
				return;

			if (Position < 0)
				targetVolume += Math.Abs(Position);

			BuyMarket(targetVolume);
		}
		else
		{
			if (Position < 0)
				return;

			if (Position > 0)
				targetVolume += Math.Abs(Position);

			SellMarket(targetVolume);
		}
	}

	private void UpdateHistory(decimal lowPrice, decimal highPrice)
	{
		_previousLow2 = _previousLow1;
		_previousLow1 = lowPrice;
		_previousHigh2 = _previousHigh1;
		_previousHigh1 = highPrice;
	}
}
