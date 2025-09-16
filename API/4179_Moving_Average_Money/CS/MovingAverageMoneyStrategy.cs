namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Conversion of the "Moving Average Money" MetaTrader expert advisor.
/// The strategy opens positions when the previous candle crosses a shifted simple moving average and
/// sizes the protective stop by risking a configurable percentage of the current portfolio value.
/// Take profit distance is expressed as a multiplier of the stop distance.
/// </summary>
public class MovingAverageMoneyStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _movingPeriod;
	private readonly StrategyParam<int> _movingShift;
	private readonly StrategyParam<decimal> _maximumRiskPercent;
	private readonly StrategyParam<decimal> _profitLossFactor;
	private readonly StrategyParam<decimal> _tradeVolume;

	private SimpleMovingAverage _movingAverage = null!;
	private readonly Queue<decimal> _shiftBuffer = new();

	private decimal? _previousOpen;
	private decimal? _previousClose;

	private decimal? _pendingStopDistance;
	private decimal? _pendingTakeDistance;
	private Sides? _pendingEntrySide;

	private decimal _signedPosition;
	private decimal? _longStop;
	private decimal? _longTake;
	private decimal? _shortStop;
	private decimal? _shortTake;
	private bool _isExiting;

	private decimal _priceStep;
	private decimal _stepPrice;

	public MovingAverageMoneyStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle type", "Time frame used for signal evaluation.", "General");

		_movingPeriod = Param(nameof(MovingPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("Moving period", "Length of the simple moving average.", "Indicator");

		_movingShift = Param(nameof(MovingShift), 6)
			.SetNotNegative()
			.SetDisplay("Moving shift", "Number of completed candles used to shift the moving average.", "Indicator");

		_maximumRiskPercent = Param(nameof(MaximumRiskPercent), 10m)
			.SetNotNegative()
			.SetDisplay("Risk percent", "Percentage of portfolio value risked per trade.", "Risk");

		_profitLossFactor = Param(nameof(ProfitLossFactor), 3m)
			.SetGreaterThanZero()
			.SetDisplay("Profit/loss factor", "Take profit distance expressed as a multiple of the stop distance.", "Risk");

		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade volume", "Base volume used for market orders.", "Trading");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int MovingPeriod
	{
		get => _movingPeriod.Value;
		set => _movingPeriod.Value = value;
	}

	public int MovingShift
	{
		get => _movingShift.Value;
		set => _movingShift.Value = value;
	}

	public decimal MaximumRiskPercent
	{
		get => _maximumRiskPercent.Value;
		set => _maximumRiskPercent.Value = value;
	}

	public decimal ProfitLossFactor
	{
		get => _profitLossFactor.Value;
		set => _profitLossFactor.Value = value;
	}

	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set
		{
			_tradeVolume.Value = value;
			Volume = value;
		}
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_shiftBuffer.Clear();
		_previousOpen = null;
		_previousClose = null;
		_pendingStopDistance = null;
		_pendingTakeDistance = null;
		_pendingEntrySide = null;
		_signedPosition = 0m;
		_longStop = null;
		_longTake = null;
		_shortStop = null;
		_shortTake = null;
		_isExiting = false;
		_priceStep = 0m;
		_stepPrice = 0m;
		Volume = TradeVolume;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;
		_signedPosition = Position;

		var security = Security;
		_priceStep = security?.PriceStep ?? 0m;
		if (_priceStep <= 0m)
			_priceStep = 0.0001m;

		_stepPrice = security?.StepPrice ?? 0m;
		if (_stepPrice <= 0m)
			_stepPrice = _priceStep;

		_movingAverage = new SimpleMovingAverage
		{
			Length = MovingPeriod
		};

		_shiftBuffer.Clear();
		_previousOpen = null;
		_previousClose = null;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_movingAverage, OnProcessCandle)
			.Start();
	}

	private void OnProcessCandle(ICandleMessage candle, decimal maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var shiftedMa = GetShiftedAverage(maValue);
		if (shiftedMa is null)
		{
			UpdatePreviousCandle(candle);
			return;
		}

		if (CheckExit(candle))
		{
			UpdatePreviousCandle(candle);
			return;
		}

		if (_pendingEntrySide != null || _isExiting)
		{
			UpdatePreviousCandle(candle);
			return;
		}

		if (_previousOpen is decimal prevOpen && _previousClose is decimal prevClose)
		{
			if (prevOpen > shiftedMa.Value && prevClose < shiftedMa.Value)
				TryEnterShort();
			else if (prevOpen < shiftedMa.Value && prevClose > shiftedMa.Value)
				TryEnterLong();
		}

		UpdatePreviousCandle(candle);
	}

	private decimal? GetShiftedAverage(decimal maValue)
	{
		var shift = MovingShift;

		if (shift <= 0)
			return maValue;

		_shiftBuffer.Enqueue(maValue);

		while (_shiftBuffer.Count > shift + 1)
			_shiftBuffer.Dequeue();

		if (_shiftBuffer.Count <= shift)
			return null;

		return _shiftBuffer.Peek();
	}

	private void UpdatePreviousCandle(ICandleMessage candle)
	{
		_previousOpen = candle.OpenPrice;
		_previousClose = candle.ClosePrice;
	}

	private bool CheckExit(ICandleMessage candle)
	{
		if (_isExiting)
			return false;

		if (_signedPosition > 0m)
		{
			if (_longStop is decimal stop && candle.LowPrice <= stop)
			{
				ExitLong();
				return true;
			}

			if (_longTake is decimal take && candle.HighPrice >= take)
			{
				ExitLong();
				return true;
			}
		}
		else if (_signedPosition < 0m)
		{
			if (_shortStop is decimal stop && candle.HighPrice >= stop)
			{
				ExitShort();
				return true;
			}

			if (_shortTake is decimal take && candle.LowPrice <= take)
			{
				ExitShort();
				return true;
			}
		}

		return false;
	}

	private void TryEnterLong()
	{
		if (_signedPosition > 0m)
			return;

		var volume = GetTradeVolume();
		if (volume <= 0m)
			return;

		var stopDistance = CalculateStopDistance(volume);
		if (stopDistance is null || stopDistance <= 0m)
			return;

		var takeDistance = stopDistance.Value * ProfitLossFactor;
		if (takeDistance <= 0m)
			takeDistance = stopDistance.Value;

		var closingVolume = _signedPosition < 0m ? NormalizeVolume(Math.Abs(_signedPosition), true) : 0m;
		var totalVolume = volume + closingVolume;

		_pendingEntrySide = Sides.Buy;
		_pendingStopDistance = stopDistance;
		_pendingTakeDistance = takeDistance;

		if (totalVolume > 0m)
			BuyMarket(totalVolume);
	}

	private void TryEnterShort()
	{
		if (_signedPosition < 0m)
			return;

		var volume = GetTradeVolume();
		if (volume <= 0m)
			return;

		var stopDistance = CalculateStopDistance(volume);
		if (stopDistance is null || stopDistance <= 0m)
			return;

		var takeDistance = stopDistance.Value * ProfitLossFactor;
		if (takeDistance <= 0m)
			takeDistance = stopDistance.Value;

		var closingVolume = _signedPosition > 0m ? NormalizeVolume(Math.Abs(_signedPosition), true) : 0m;
		var totalVolume = volume + closingVolume;

		_pendingEntrySide = Sides.Sell;
		_pendingStopDistance = stopDistance;
		_pendingTakeDistance = takeDistance;

		if (totalVolume > 0m)
			SellMarket(totalVolume);
	}

	private void ExitLong()
	{
		var volume = NormalizeVolume(Math.Abs(_signedPosition), true);
		if (volume <= 0m)
			return;

		_isExiting = true;
		_longStop = null;
		_longTake = null;
		_pendingEntrySide = null;
		_pendingStopDistance = null;
		_pendingTakeDistance = null;

		SellMarket(volume);
	}

	private void ExitShort()
	{
		var volume = NormalizeVolume(Math.Abs(_signedPosition), true);
		if (volume <= 0m)
			return;

		_isExiting = true;
		_shortStop = null;
		_shortTake = null;
		_pendingEntrySide = null;
		_pendingStopDistance = null;
		_pendingTakeDistance = null;

		BuyMarket(volume);
	}

	private decimal GetTradeVolume()
	{
		var volume = TradeVolume > 0m ? TradeVolume : Volume;
		if (volume <= 0m)
			volume = 1m;

		return NormalizeVolume(volume);
	}

	private decimal NormalizeVolume(decimal volume, bool allowZero = false)
	{
		var security = Security;
		if (security != null)
		{
			var step = security.VolumeStep ?? 1m;
			if (step <= 0m)
				step = 1m;

			if (volume > 0m)
			{
				var steps = Math.Floor(volume / step);
				if (steps < 1m)
					steps = 1m;

				volume = steps * step;
			}
		}

		if (volume <= 0m)
			return allowZero ? 0m : 1m;

		return volume;
	}

	private decimal? CalculateStopDistance(decimal volume)
	{
		if (MaximumRiskPercent <= 0m)
			return null;

		var portfolio = Portfolio;
		if (portfolio == null)
			return null;

		var equity = portfolio.CurrentValue;
		if (equity <= 0m)
			return null;

		if (_priceStep <= 0m || _stepPrice <= 0m)
			return null;

		var riskAmount = equity * MaximumRiskPercent / 100m;
		if (riskAmount <= 0m)
			return null;

		var spreadSteps = GetSpreadSteps();
		var rawSteps = riskAmount / (volume * _stepPrice) - spreadSteps;
		if (rawSteps <= 0m)
			return null;

		var steps = Math.Floor(rawSteps);
		if (steps <= 0m)
			return null;

		return steps * _priceStep;
	}

	private decimal GetSpreadSteps()
	{
		var security = Security;
		if (security == null || _priceStep <= 0m)
			return 0m;

		var bestBid = security.BestBid?.Price;
		var bestAsk = security.BestAsk?.Price;
		if (bestBid is null || bestAsk is null)
			return 0m;

		var spread = bestAsk.Value - bestBid.Value;
		if (spread <= 0m)
			return 0m;

		return spread / _priceStep;
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		var volume = trade.Trade.Volume;
		if (volume <= 0m)
			return;

		var delta = trade.Order.Side == Sides.Buy ? volume : -volume;
		var previous = _signedPosition;
		_signedPosition += delta;

		if (_pendingEntrySide == Sides.Buy && delta > 0m)
		{
			if (_pendingStopDistance is decimal stopDistance && _signedPosition > 0m)
				_longStop = trade.Trade.Price - stopDistance;
			else
				_longStop = null;

			if (_pendingTakeDistance is decimal takeDistance && _signedPosition > 0m)
				_longTake = trade.Trade.Price + takeDistance;
			else
				_longTake = null;

			_shortStop = null;
			_shortTake = null;
			_pendingEntrySide = null;
			_pendingStopDistance = null;
			_pendingTakeDistance = null;
			_isExiting = false;
		}
		else if (_pendingEntrySide == Sides.Sell && delta < 0m)
		{
			if (_pendingStopDistance is decimal stopDistance && _signedPosition < 0m)
				_shortStop = trade.Trade.Price + stopDistance;
			else
				_shortStop = null;

			if (_pendingTakeDistance is decimal takeDistance && _signedPosition < 0m)
				_shortTake = trade.Trade.Price - takeDistance;
			else
				_shortTake = null;

			_longStop = null;
			_longTake = null;
			_pendingEntrySide = null;
			_pendingStopDistance = null;
			_pendingTakeDistance = null;
			_isExiting = false;
		}

		if (previous != 0m && _signedPosition == 0m)
		{
			_longStop = null;
			_longTake = null;
			_shortStop = null;
			_shortTake = null;
			_pendingEntrySide = null;
			_pendingStopDistance = null;
			_pendingTakeDistance = null;
			_isExiting = false;
		}
	}
}
