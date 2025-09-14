namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// CCI Woodies crossover strategy.
/// Buys when the fast CCI crosses below the slow CCI and sells on the opposite crossover.
/// Includes optional signal inversion and risk management via stop-loss and take-profit.
/// </summary>
public class CciWoodiesStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _invertSignals;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<int> _stopLossPoints;

	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _isInitialized;

	/// <summary>
	/// Fast CCI period.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Slow CCI period.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Candle type used for analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Invert buy and sell signals.
	/// </summary>
	public bool InvertSignals
	{
		get => _invertSignals.Value;
		set => _invertSignals.Value = value;
	}

	/// <summary>
	/// Take profit in points.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop loss in points.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public CciWoodiesStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 6)
			.SetGreaterThanZero()
			.SetDisplay("Fast CCI Period", "Period for fast Commodity Channel Index", "Indicator");

		_slowPeriod = Param(nameof(SlowPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Slow CCI Period", "Period for slow Commodity Channel Index", "Indicator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for calculation", "General");

		_invertSignals = Param(nameof(InvertSignals), false)
			.SetDisplay("Invert Signals", "Reverse buy and sell conditions", "Trading");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take profit in price points", "Risk Management");

		_stopLossPoints = Param(nameof(StopLossPoints), 1000)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop loss in price points", "Risk Management");
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
		_prevFast = 0;
		_prevSlow = 0;
		_isInitialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var fastCci = new CommodityChannelIndex { Length = FastPeriod };
		var slowCci = new CommodityChannelIndex { Length = SlowPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastCci, slowCci, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastCci);
			DrawIndicator(area, slowCci);
			DrawOwnTrades(area);
		}

		var step = Security.PriceStep ?? 1m;
		StartProtection(
			takeProfit: new Unit(TakeProfitPoints * step, UnitTypes.Point),
			stopLoss: new Unit(StopLossPoints * step, UnitTypes.Point));
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_isInitialized)
		{
			_prevFast = fast;
			_prevSlow = slow;
			_isInitialized = true;
			return;
		}

		var crossDown = _prevFast > _prevSlow && fast <= slow;
		var crossUp = _prevFast < _prevSlow && fast >= slow;

		if (InvertSignals)
		{
			var temp = crossDown;
			crossDown = crossUp;
			crossUp = temp;
		}

		if (crossDown && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (crossUp && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}

		_prevFast = fast;
		_prevSlow = slow;
	}
}
