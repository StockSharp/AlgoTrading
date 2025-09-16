using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Chaikin oscillator breakout strategy with flat market filter and trailing stop.
/// </summary>
public class OneTwoThreeStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<decimal> _flatLevel;
	private readonly StrategyParam<decimal> _openLevel;
	private readonly StrategyParam<int> _barsCount;
	private readonly StrategyParam<decimal> _flatPercent;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _fastEma = null!;
	private ExponentialMovingAverage _slowEma = null!;
	private readonly Queue<decimal> _chaikinValues = new();
	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _takePrice;
	private decimal _trailingStopOffset;
	private decimal _trailingStepOffset;

	/// <summary>
	/// Trading volume used for market orders.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Trailing step distance expressed in pips.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Fast EMA length applied to the accumulation/distribution line.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Slow EMA length applied to the accumulation/distribution line.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// Absolute Chaikin value considered as a flat market.
	/// </summary>
	public decimal FlatLevel
	{
		get => _flatLevel.Value;
		set => _flatLevel.Value = value;
	}

	/// <summary>
	/// Chaikin threshold required to open new trades.
	/// </summary>
	public decimal OpenLevel
	{
		get => _openLevel.Value;
		set => _openLevel.Value = value;
	}

	/// <summary>
	/// Number of Chaikin values used in the flat market filter.
	/// </summary>
	public int BarsCount
	{
		get => _barsCount.Value;
		set => _barsCount.Value = value;
	}

	/// <summary>
	/// Maximum percentage of flat Chaikin values allowed before blocking trades.
	/// </summary>
	public decimal FlatPercent
	{
		get => _flatPercent.Value;
		set => _flatPercent.Value = value;
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="OneTwoThreeStrategy"/> class.
	/// </summary>
	public OneTwoThreeStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume", "Trading");

		_stopLossPips = Param(nameof(StopLossPips), 45m)
			.SetDisplay("Stop Loss", "Stop loss in pips", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 45m)
			.SetDisplay("Take Profit", "Take profit in pips", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 5m)
			.SetDisplay("Trailing Stop", "Trailing stop in pips", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
			.SetDisplay("Trailing Step", "Trailing step in pips", "Risk");

		_fastLength = Param(nameof(FastLength), 3)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA period", "Chaikin");

		_slowLength = Param(nameof(SlowLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA period", "Chaikin");

		_flatLevel = Param(nameof(FlatLevel), 40m)
			.SetDisplay("Flat Level", "Absolute Chaikin value treated as flat", "Filter");

		_openLevel = Param(nameof(OpenLevel), 110m)
			.SetDisplay("Open Level", "Chaikin breakout threshold", "Filter");

		_barsCount = Param(nameof(BarsCount), 20)
			.SetGreaterThanZero()
			.SetDisplay("Flat Bars", "Number of bars for flat detection", "Filter");

		_flatPercent = Param(nameof(FlatPercent), 55m)
			.SetDisplay("Flat Percent", "Max percent of flat bars", "Filter");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for candles", "Data");
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

		// Reset state variables when strategy is reset.
		_chaikinValues.Clear();
		_entryPrice = 0m;
		_stopPrice = 0m;
		_takePrice = 0m;
		_trailingStopOffset = 0m;
		_trailingStepOffset = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Prepare Chaikin oscillator components.
		_fastEma = new ExponentialMovingAverage { Length = FastLength };
		_slowEma = new ExponentialMovingAverage { Length = SlowLength };
		var ad = new AccumulationDistributionLine();

		// Subscribe to candles and bind accumulation/distribution values.
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ad, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ad);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal adValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Update Chaikin oscillator using EMA difference on accumulation/distribution values.
		var fastValue = _fastEma.Process(new DecimalIndicatorValue(_fastEma, adValue, candle.Time));
		var slowValue = _slowEma.Process(new DecimalIndicatorValue(_slowEma, adValue, candle.Time));

		if (!fastValue.IsFinal || !slowValue.IsFinal)
		{
			// Even if indicators are not ready yet, manage existing positions for exits.
			ManagePosition(candle);
			return;
		}

		var chaikin = fastValue.ToDecimal() - slowValue.ToDecimal();
		UpdateChaikinHistory(chaikin);

		if (ManagePosition(candle))
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_chaikinValues.Count < BarsCount)
			return;

		if (IsFlatMarket())
			return;

		TryEnterPosition(candle, chaikin);
	}

	private void UpdateChaikinHistory(decimal chaikin)
	{
		// Maintain a rolling window of Chaikin values for the flat filter.
		_chaikinValues.Enqueue(chaikin);
		while (_chaikinValues.Count > BarsCount)
			_chaikinValues.Dequeue();
	}

	private bool IsFlatMarket()
	{
		var flatCount = 0;
		var threshold = FlatLevel;
		foreach (var value in _chaikinValues)
		{
			if (Math.Abs(value) <= threshold)
				flatCount++;
		}

		var percent = (decimal)flatCount * 100m / BarsCount;
		return percent <= FlatPercent;
	}

	private void TryEnterPosition(ICandleMessage candle, decimal chaikin)
	{
		var step = GetPriceStep();
		var stopOffset = StopLossPips * step;
		var takeOffset = TakeProfitPips * step;
		_trailingStopOffset = TrailingStopPips > 0m ? TrailingStopPips * step : 0m;
		_trailingStepOffset = TrailingStepPips > 0m ? TrailingStepPips * step : 0m;

		var longSignal = chaikin >= OpenLevel && Position <= 0;
		var shortSignal = chaikin <= -OpenLevel && Position >= 0;

		if (longSignal)
		{
			CancelActiveOrders();
			var volume = OrderVolume + (Position < 0 ? -Position : 0m);
			if (volume <= 0m)
				return;

			BuyMarket(volume);
			_entryPrice = candle.ClosePrice;
			_stopPrice = stopOffset > 0m ? _entryPrice - stopOffset : 0m;
			_takePrice = takeOffset > 0m ? _entryPrice + takeOffset : 0m;
		}
		else if (shortSignal)
		{
			CancelActiveOrders();
			var volume = OrderVolume + (Position > 0 ? Position : 0m);
			if (volume <= 0m)
				return;

			SellMarket(volume);
			_entryPrice = candle.ClosePrice;
			_stopPrice = stopOffset > 0m ? _entryPrice + stopOffset : 0m;
			_takePrice = takeOffset > 0m ? _entryPrice - takeOffset : 0m;
		}
	}

	private bool ManagePosition(ICandleMessage candle)
	{
		if (Position > 0)
		{
			ApplyTrailingForLong(candle);

			if (_stopPrice > 0m && candle.LowPrice <= _stopPrice)
			{
				SellMarket(Position);
				ResetPositionState();
				return true;
			}

			if (_takePrice > 0m && candle.HighPrice >= _takePrice)
			{
				SellMarket(Position);
				ResetPositionState();
				return true;
			}
		}
		else if (Position < 0)
		{
			ApplyTrailingForShort(candle);

			var shortVolume = -Position;
			if (_stopPrice > 0m && candle.HighPrice >= _stopPrice)
			{
				BuyMarket(shortVolume);
				ResetPositionState();
				return true;
			}

			if (_takePrice > 0m && candle.LowPrice <= _takePrice)
			{
				BuyMarket(shortVolume);
				ResetPositionState();
				return true;
			}
		}

		return false;
	}

	private void ApplyTrailingForLong(ICandleMessage candle)
	{
		if (_trailingStopOffset <= 0m)
			return;

		var move = candle.ClosePrice - _entryPrice;
		var minDistance = _trailingStopOffset + _trailingStepOffset;
		if (move <= minDistance)
			return;

		var trigger = candle.ClosePrice - minDistance;
		if (_stopPrice == 0m || _stopPrice < trigger)
			_stopPrice = candle.ClosePrice - _trailingStopOffset;
	}

	private void ApplyTrailingForShort(ICandleMessage candle)
	{
		if (_trailingStopOffset <= 0m)
			return;

		var move = _entryPrice - candle.ClosePrice;
		var minDistance = _trailingStopOffset + _trailingStepOffset;
		if (move <= minDistance)
			return;

		var trigger = candle.ClosePrice + minDistance;
		if (_stopPrice == 0m || _stopPrice > trigger)
			_stopPrice = candle.ClosePrice + _trailingStopOffset;
	}

	private void ResetPositionState()
	{
		// Clear trade management levels once the position is closed.
		_entryPrice = 0m;
		_stopPrice = 0m;
		_takePrice = 0m;
		_trailingStopOffset = 0m;
		_trailingStepOffset = 0m;
	}

	private decimal GetPriceStep()
	{
		var step = Security?.PriceStep;
		return step ?? 1m;
	}
}
