using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the crossover of fast and slow smoothed Williams %R lines.
/// </summary>
public class UltraWprCrossStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _wprPeriod;
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;

	private readonly SimpleMovingAverage _fastMa = new();
	private readonly SimpleMovingAverage _slowMa = new();

	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _isFirst = true;

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Williams %R period.
	/// </summary>
	public int WprPeriod
	{
		get => _wprPeriod.Value;
		set => _wprPeriod.Value = value;
	}

	/// <summary>
	/// Fast smoothing length.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Slow smoothing length.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// Take profit in price.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Stop loss in price.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public UltraWprCrossStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_wprPeriod = Param(nameof(WprPeriod), 13)
			.SetGreaterThanZero()
			.SetDisplay("WPR Period", "Williams %R period", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 40, 1);

		_fastLength = Param(nameof(FastLength), 3)
			.SetGreaterThanZero()
			.SetDisplay("Fast Length", "Fast smoothing length", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(1, 20, 1);

		_slowLength = Param(nameof(SlowLength), 53)
			.SetGreaterThanZero()
			.SetDisplay("Slow Length", "Slow smoothing length", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 100, 5);

		_takeProfit = Param(nameof(TakeProfit), 0.2m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take profit in price", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0.05m, 0.5m, 0.05m);

		_stopLoss = Param(nameof(StopLoss), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop loss in price", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0.05m, 0.5m, 0.05m);
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

		_fastMa.Length = FastLength;
		_slowMa.Length = SlowLength;

		var wpr = new WilliamsR { Length = WprPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(wpr, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawOwnTrades(area);
		}

		StartProtection(new Unit(TakeProfit, UnitTypes.Price), new Unit(StopLoss, UnitTypes.Price));
	}

	private void ProcessCandle(ICandleMessage candle, decimal wprValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var fastValue = _fastMa.Process(wprValue);
		var slowValue = _slowMa.Process(wprValue);

		if (!fastValue.IsFinal || !slowValue.IsFinal)
			return;

		var fast = fastValue.GetValue<decimal>();
		var slow = slowValue.GetValue<decimal>();

		if (_isFirst)
		{
			_prevFast = fast;
			_prevSlow = slow;
			_isFirst = false;
			return;
		}

		var crossUp = _prevFast <= _prevSlow && fast > slow;
		var crossDown = _prevFast >= _prevSlow && fast < slow;

		if (crossUp && Position <= 0)
		{
			BuyMarket();
		}
		else if (crossDown && Position >= 0)
		{
			SellMarket();
		}

		_prevFast = fast;
		_prevSlow = slow;
	}
}
