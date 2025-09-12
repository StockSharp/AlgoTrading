using System;
using System.Collections.Generic;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Rate of Change indicator with bubble detection and dynamic position sizing.
/// </summary>
public class RateOfChangeStrategy : Strategy
{
	private readonly StrategyParam<int> _rocLength;
	private readonly StrategyParam<decimal> _bubbleThreshold;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _fixedRatioValue;
	private readonly StrategyParam<decimal> _increasingOrderAmount;
	private readonly StrategyParam<DateTimeOffset> _startDate;
	private readonly StrategyParam<DateTimeOffset> _endDate;
	private readonly StrategyParam<DataType> _candleType;

	private bool _inBubble;
	private int _bubbleCounter;
	private decimal _cashOrder;
	private decimal _capitalRef;
	private decimal _prevRoc;
	private bool _prevCrossUp;
	private bool _prevCrossDown;
	private decimal _prevPosition;
	private Sides? _lastTrade;
	/// <summary>
	/// ROC calculation length.
	/// </summary>
	public int RocLength
	{
		get => _rocLength.Value;
		set => _rocLength.Value = value;
	}

	/// <summary>
	/// ROC value that marks bubble conditions.
	/// </summary>
	public decimal BubbleThreshold
	{
		get => _bubbleThreshold.Value;
		set => _bubbleThreshold.Value = value;
	}

	/// <summary>
	/// Stop loss in percent.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Fixed ratio level in currency units.
	/// </summary>
	public decimal FixedRatioValue
	{
		get => _fixedRatioValue.Value;
		set => _fixedRatioValue.Value = value;
	}

	/// <summary>
	/// Amount added to order size when equity increases by fixed ratio.
	/// </summary>
	public decimal IncreasingOrderAmount
	{
		get => _increasingOrderAmount.Value;
		set => _increasingOrderAmount.Value = value;
	}

	/// <summary>
	/// Backtest start date.
	/// </summary>
	public DateTimeOffset StartDate
	{
		get => _startDate.Value;
		set => _startDate.Value = value;
	}

	/// <summary>
	/// Backtest end date.
	/// </summary>
	public DateTimeOffset EndDate
	{
		get => _endDate.Value;
		set => _endDate.Value = value;
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
	/// Initializes a new instance of the <see cref="RateOfChangeStrategy"/>.
	/// </summary>
	public RateOfChangeStrategy()
	{
		_rocLength = Param(nameof(RocLength), 365)
			.SetRange(50, 500)
			.SetDisplay("ROC Length", "Rate of Change lookback length", "Parameters")
			.SetCanOptimize(true);

		_bubbleThreshold = Param(nameof(BubbleThreshold), 180m)
			.SetRange(50m, 300m)
			.SetDisplay("Bubble Threshold", "ROC value defining bubble state", "Parameters")
			.SetCanOptimize(true);

		_stopLossPercent = Param(nameof(StopLossPercent), 6m)
			.SetRange(1m, 20m)
			.SetDisplay("Stop Loss %", "Percentage stop loss", "Risk Management")
			.SetCanOptimize(true);

		_fixedRatioValue = Param(nameof(FixedRatioValue), 400m)
			.SetRange(100m, 1000m)
			.SetDisplay("Fixed Ratio", "Equity step to adjust order size", "Money Management")
			.SetCanOptimize(true);

		_increasingOrderAmount = Param(nameof(IncreasingOrderAmount), 200m)
			.SetRange(50m, 500m)
			.SetDisplay("Order Increase", "Amount added per fixed ratio step", "Money Management")
			.SetCanOptimize(true);

		_startDate = Param(nameof(StartDate), new DateTimeOffset(new DateTime(2017, 1, 1)))
			.SetDisplay("Start Date", "Backtest start", "Backtesting");

		_endDate = Param(nameof(EndDate), new DateTimeOffset(new DateTime(2024, 7, 1)))
			.SetDisplay("End Date", "Backtest end", "Backtesting");

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "Parameters");
	}
	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_capitalRef = Portfolio?.CurrentValue ?? 0m;
		_cashOrder = _capitalRef * 0.95m;

		var roc = new RateOfChange { Length = RocLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(roc, ProcessCandle)
			.Start();

		StartProtection(
			stopLoss: new Unit(StopLossPercent, UnitTypes.Percent),
			takeProfit: null,
			useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, roc);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal roc)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var inRange = candle.OpenTime >= StartDate && candle.OpenTime <= EndDate;

		if (Position != 0 && !inRange)
		{
			LogInfo("End of backtesting period: closing position");
			ClosePosition();
		}

		var equity = Portfolio?.CurrentValue ?? 0m;
		if (equity > _capitalRef + FixedRatioValue)
		{
			var spread = (equity - _capitalRef) / FixedRatioValue;
			var nbLevel = (int)spread;
			var increasingOrder = nbLevel * IncreasingOrderAmount;
			_cashOrder += increasingOrder;
			_capitalRef += nbLevel * FixedRatioValue;
		}
		else if (equity < _capitalRef - FixedRatioValue)
		{
			var spread = (_capitalRef - equity) / FixedRatioValue;
			var nbLevel = (int)spread;
			var decreasingOrder = nbLevel * IncreasingOrderAmount;
			_cashOrder -= decreasingOrder;
			_capitalRef -= nbLevel * FixedRatioValue;
		}

		if (roc > BubbleThreshold && !_inBubble)
			_inBubble = true;
		if (roc < 0m && _inBubble)
			_inBubble = false;

		if (roc > BubbleThreshold)
			_bubbleCounter++;
		else
			_bubbleCounter = 0;

		var shortBubbleCondition = _bubbleCounter >= 7;

		if (Position == 0 && _prevPosition != 0 && inRange && _lastTrade != null)
		{
			var volumeRe = _cashOrder / candle.ClosePrice;
			if (_lastTrade == Sides.Sell)
			{
				BuyMarket(volumeRe);
				_lastTrade = Sides.Buy;
			}
			else if (_lastTrade == Sides.Buy)
			{
				SellMarket(volumeRe);
				_lastTrade = Sides.Sell;
			}
		}

		if (inRange)
		{
			if (Position <= 0 && _prevCrossUp && roc > 0m)
			{
				var volume = _cashOrder / candle.ClosePrice + Math.Abs(Position);
				BuyMarket(volume);
				_lastTrade = Sides.Buy;
			}
			else if (Position >= 0 && ((_prevCrossDown && roc < 0m) || (_inBubble && (_prevRoc > BubbleThreshold && roc < BubbleThreshold) && shortBubbleCondition)))
			{
				var volume = _cashOrder / candle.ClosePrice + Math.Abs(Position);
				SellMarket(volume);
				_lastTrade = Sides.Sell;
			}
		}

		var crossUp = _prevRoc <= 0m && roc > 0m;
		var crossDown = _prevRoc >= 0m && roc < 0m;

		_prevCrossUp = crossUp;
		_prevCrossDown = crossDown;
		_prevRoc = roc;
		_prevPosition = Position;
	}
}
