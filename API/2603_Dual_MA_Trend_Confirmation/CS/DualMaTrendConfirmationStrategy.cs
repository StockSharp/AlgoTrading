using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Dual moving average trend confirmation strategy.
/// Uses a slow EMA and a fast LWMA to detect synchronized trends.
/// Enters long when both averages slope upward, price stays above the slow EMA, and the slow EMA is above the fast LWMA.
/// Enters short when both averages slope downward, price stays below the slow EMA, and the slow EMA is below the fast LWMA.
/// Built-in stop-loss and take-profit are defined in instrument points.
/// </summary>
public class DualMaTrendConfirmationStrategy : Strategy
{
	private readonly StrategyParam<int> _slowMaLength;
	private readonly StrategyParam<int> _fastMaLength;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _previousClose;
	private decimal _slowPrevious;
	private decimal _slowPrevious2;
	private decimal _fastPrevious;
	private decimal _fastPrevious2;
	private int _historyCount;

	/// <summary>
	/// Slow EMA period length.
	/// </summary>
	public int SlowMaLength
	{
		get => _slowMaLength.Value;
		set => _slowMaLength.Value = value;
	}

	/// <summary>
	/// Fast LWMA period length.
	/// </summary>
	public int FastMaLength
	{
		get => _fastMaLength.Value;
		set => _fastMaLength.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in instrument points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in instrument points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
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
	/// Initializes a new instance of the <see cref="DualMaTrendConfirmationStrategy"/> class.
	/// </summary>
	public DualMaTrendConfirmationStrategy()
	{
		_slowMaLength = Param(nameof(SlowMaLength), 57)
			.SetDisplay("Slow EMA Length", "Period for the slow EMA trend filter", "Moving Averages")
			.SetRange(10, 200)
			.SetCanOptimize(true);

		_fastMaLength = Param(nameof(FastMaLength), 3)
			.SetDisplay("Fast LWMA Length", "Period for the fast LWMA confirmation filter", "Moving Averages")
			.SetRange(1, 50)
			.SetCanOptimize(true);

		_stopLossPoints = Param(nameof(StopLossPoints), 100m)
			.SetDisplay("Stop Loss (points)", "Stop-loss distance measured in instrument points", "Risk Management")
			.SetRange(10m, 500m)
			.SetCanOptimize(true);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 100m)
			.SetDisplay("Take Profit (points)", "Take-profit distance measured in instrument points", "Risk Management")
			.SetRange(10m, 500m)
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles used for moving average calculations", "General");
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

		// Clear stored history so the next candle starts with a clean state.
		_previousClose = 0m;
		_slowPrevious = 0m;
		_slowPrevious2 = 0m;
		_fastPrevious = 0m;
		_fastPrevious2 = 0m;
		_historyCount = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var slowEma = new ExponentialMovingAverage
		{
			Length = SlowMaLength
		};

		var fastLwma = new WeightedMovingAverage
		{
			Length = FastMaLength
		};

		var subscription = SubscribeCandles(CandleType);

		var step = Security.PriceStep ?? 1m;

		// Enable automatic stop-loss and take-profit management based on point offsets.
		StartProtection(
			takeProfit: new Unit(TakeProfitPoints * step, UnitTypes.Absolute),
			stopLoss: new Unit(StopLossPoints * step, UnitTypes.Absolute),
			useMarketOrders: true);

		subscription
			.Bind(slowEma, fastLwma, (candle, slowValue, fastValue) => ProcessCandle(candle, slowValue, fastValue, slowEma, fastLwma))
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, slowEma);
			DrawIndicator(area, fastLwma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal slowValue, decimal fastValue, ExponentialMovingAverage slowEma, WeightedMovingAverage fastLwma)
	{
		// Work only with fully formed candles to avoid premature decisions.
		if (candle.State != CandleStates.Finished)
			return;

		// Ensure both indicators produced reliable values before trading logic.
		if (!slowEma.IsFormed || !fastLwma.IsFormed)
		{
			UpdateHistory(slowValue, fastValue, candle.ClosePrice);
			return;
		}

		// Accumulate at least two previous candles for slope calculations.
		if (_historyCount < 2)
		{
			UpdateHistory(slowValue, fastValue, candle.ClosePrice);
			return;
		}

		var slowRising = slowValue > _slowPrevious && _slowPrevious > _slowPrevious2;
		var fastRising = fastValue > _fastPrevious && _fastPrevious > _fastPrevious2;
		var slowFalling = slowValue < _slowPrevious && _slowPrevious < _slowPrevious2;
		var fastFalling = fastValue < _fastPrevious && _fastPrevious < _fastPrevious2;
		var priceAboveSlow = _previousClose > _slowPrevious;
		var priceBelowSlow = _previousClose < _slowPrevious;
		var slowAboveFast = slowValue > fastValue;
		var slowBelowFast = slowValue < fastValue;

		var canTrade = IsFormedAndOnlineAndAllowTrading();

		if (canTrade && slowRising && fastRising && priceAboveSlow && slowAboveFast && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			if (volume > 0)
			{
				BuyMarket(volume);
				LogInfo($"Long entry: slow EMA {slowValue:F5}, fast LWMA {fastValue:F5}, close {_previousClose:F5}.");
			}
		}
		else if (canTrade && slowFalling && fastFalling && priceBelowSlow && slowBelowFast && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			if (volume > 0)
			{
				SellMarket(volume);
				LogInfo($"Short entry: slow EMA {slowValue:F5}, fast LWMA {fastValue:F5}, close {_previousClose:F5}.");
			}
		}

		UpdateHistory(slowValue, fastValue, candle.ClosePrice);
	}

	private void UpdateHistory(decimal slowValue, decimal fastValue, decimal closePrice)
	{
		// Shift previous values so the last two candles are always available.
		_slowPrevious2 = _slowPrevious;
		_slowPrevious = slowValue;
		_fastPrevious2 = _fastPrevious;
		_fastPrevious = fastValue;
		_previousClose = closePrice;

		if (_historyCount < 2)
			_historyCount++;
	}
}
