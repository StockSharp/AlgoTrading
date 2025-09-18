using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Moving average crossover strategy converted from the MQL4 expert advisor.
/// Tracks two exponential moving averages and mirrors risk targets around the entry.
/// </summary>
public class MovingAverageCrossoverStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _maxSpreadPoints;
	private readonly StrategyParam<decimal> _tradeVolume;

	private ExponentialMovingAverage _fastMa;
	private ExponentialMovingAverage _slowMa;

	private decimal? _previousFast;
	private decimal? _previousSlow;
	private decimal? _previousFast2;
	private decimal? _previousSlow2;

	/// <summary>
	/// Candle type that feeds the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Period of the faster exponential moving average.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Period of the slower exponential moving average.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in instrument points.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Maximum allowed spread expressed in instrument points.
	/// </summary>
	public int MaxSpreadPoints
	{
		get => _maxSpreadPoints.Value;
		set => _maxSpreadPoints.Value = value;
	}

	/// <summary>
	/// Volume submitted with market orders.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="MovingAverageCrossoverStrategy"/>.
	/// </summary>
	public MovingAverageCrossoverStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to process", "General");

		_fastPeriod = Param(nameof(FastPeriod), 21)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Length of the fast moving average", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(5, 100, 1);

		_slowPeriod = Param(nameof(SlowPeriod), 84)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Length of the slow moving average", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(20, 200, 2);

		_stopLossPoints = Param(nameof(StopLossPoints), 60)
			.SetGreaterOrEqualZero()
			.SetDisplay("Stop Loss", "Stop-loss distance in points", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(10, 200, 5);

		_maxSpreadPoints = Param(nameof(MaxSpreadPoints), 20)
			.SetGreaterOrEqualZero()
			.SetDisplay("Max Spread", "Maximum allowed spread in points", "Risk");

		_tradeVolume = Param(nameof(TradeVolume), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Lot size sent with market orders", "Trading");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return new[]
		{
			(Security, CandleType)
		};
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_previousFast = null;
		_previousSlow = null;
		_previousFast2 = null;
		_previousSlow2 = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastMa = new ExponentialMovingAverage { Length = FastPeriod };
		_slowMa = new ExponentialMovingAverage { Length = SlowPeriod };

		Volume = TradeVolume;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fastMa, _slowMa, ProcessCandle)
			.Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_fastMa.IsFormed || !_slowMa.IsFormed)
		{
			UpdateHistory(fastValue, slowValue);
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			UpdateHistory(fastValue, slowValue);
			return;
		}

		if (Position == 0 && !HasActiveOrders() &&
			_previousFast.HasValue && _previousSlow.HasValue &&
			_previousFast2.HasValue && _previousSlow2.HasValue)
		{
			var fastPrev = _previousFast.Value;
			var slowPrev = _previousSlow.Value;
			var fastPrev2 = _previousFast2.Value;
			var slowPrev2 = _previousSlow2.Value;

			var goldenCross = fastPrev > slowPrev && fastPrev2 < slowPrev2 && fastPrev > fastPrev2;
			var deadCross = fastPrev < slowPrev && fastPrev2 > slowPrev2 && fastPrev < fastPrev2;

			if (goldenCross)
				TryEnterLong(slowPrev, candle);
			else if (deadCross)
				TryEnterShort(slowPrev, candle);
		}

		UpdateHistory(fastValue, slowValue);
	}

	private void TryEnterLong(decimal mediumValue, ICandleMessage candle)
	{
		var priceStep = GetPriceStep();
		if (priceStep > 0m && !IsSpreadAllowed(priceStep))
			return;

		var entryPrice = Security?.BestAskPrice ?? candle.OpenPrice;

		BuyMarket();

		if (priceStep <= 0m)
			return;

		var stopPrice = mediumValue - StopLossPoints * priceStep;
		var takeProfitPrice = entryPrice + (entryPrice - stopPrice);

		var resultingPosition = Position + Volume;
		SetProtectiveOrders(entryPrice, stopPrice, takeProfitPrice, resultingPosition);
	}

	private void TryEnterShort(decimal mediumValue, ICandleMessage candle)
	{
		var priceStep = GetPriceStep();
		if (priceStep > 0m && !IsSpreadAllowed(priceStep))
			return;

		var entryPrice = Security?.BestBidPrice ?? candle.OpenPrice;

		SellMarket();

		if (priceStep <= 0m)
			return;

		var stopPrice = mediumValue + StopLossPoints * priceStep;
		var takeProfitPrice = entryPrice - (stopPrice - entryPrice);

		var resultingPosition = Position - Volume;
		SetProtectiveOrders(entryPrice, stopPrice, takeProfitPrice, resultingPosition);
	}

	private void SetProtectiveOrders(decimal entryPrice, decimal stopPrice, decimal takeProfitPrice, decimal resultingPosition)
	{
		var priceStep = GetPriceStep();
		if (priceStep <= 0m)
			return;

		var stopSteps = GetDistanceInSteps(entryPrice, stopPrice, priceStep);
		var takeSteps = GetDistanceInSteps(entryPrice, takeProfitPrice, priceStep);

		if (stopSteps > 0)
			SetStopLoss(stopSteps, entryPrice, resultingPosition);

		if (takeSteps > 0)
			SetTakeProfit(takeSteps, entryPrice, resultingPosition);
	}

	private bool IsSpreadAllowed(decimal priceStep)
	{
		if (MaxSpreadPoints <= 0)
			return true;

		if (Security?.BestAskPrice is not decimal ask || Security?.BestBidPrice is not decimal bid)
			return true;

		var spreadPoints = (ask - bid) / priceStep;
		return spreadPoints <= MaxSpreadPoints;
	}

	private void UpdateHistory(decimal fastValue, decimal slowValue)
	{
		_previousFast2 = _previousFast;
		_previousSlow2 = _previousSlow;
		_previousFast = fastValue;
		_previousSlow = slowValue;
	}

	private static int GetDistanceInSteps(decimal fromPrice, decimal toPrice, decimal priceStep)
	{
		if (priceStep <= 0m)
			return 0;

		var distance = Math.Abs(fromPrice - toPrice);
		if (distance <= 0m)
			return 0;

		var steps = decimal.Divide(distance, priceStep);
		return (int)Math.Round(steps, MidpointRounding.AwayFromZero);
	}

	private bool HasActiveOrders()
	{
		foreach (var order in Orders)
		{
			if (order.State.IsActive())
				return true;
		}

		return false;
	}

	private decimal GetPriceStep()
	{
		return Security?.PriceStep ?? Security?.MinPriceStep ?? 0m;
	}
}
