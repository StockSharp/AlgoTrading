using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// XDerivative strategy based on smoothed rate of change.
/// Enters long when the smoothed derivative forms a trough and short when it forms a peak.
/// </summary>
public class XDerivativeStrategy : Strategy
{
	private readonly StrategyParam<int> _rocPeriod;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private readonly JurikMovingAverage _jma = new();

	private decimal? _prevValue;
	private decimal? _prevPrevValue;

	/// <summary>
	/// Rate of Change period.
	/// </summary>
	public int RocPeriod
	{
		get => _rocPeriod.Value;
		set => _rocPeriod.Value = value;
	}

	/// <summary>
	/// Jurik Moving Average length used for smoothing.
	/// </summary>
	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	/// <summary>
	/// Take profit percentage.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Stop loss percentage.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="XDerivativeStrategy"/>.
	/// </summary>
	public XDerivativeStrategy()
	{
		_rocPeriod = Param(nameof(RocPeriod), 34)
			.SetGreaterThanZero()
			.SetDisplay("ROC Period", "Period for rate of change calculation", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(10, 50, 5);

		_maLength = Param(nameof(MaLength), 7)
			.SetGreaterThanZero()
			.SetDisplay("JMA Length", "Period for Jurik MA smoothing", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 5);

		_takeProfit = Param(nameof(TakeProfitPercent), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit %", "Take profit percentage", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 1m);

		_stopLoss = Param(nameof(StopLossPercent), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 3m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for calculation", "Parameters");
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
		_jma.Length = MaLength;
		_prevValue = null;
		_prevPrevValue = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_jma.Length = MaLength;

		var roc = new RateOfChange { Length = RocPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(roc, ProcessCandle)
			.Start();

		StartProtection(
			takeProfit: new Unit(TakeProfitPercent * 100m, UnitTypes.Percent),
			stopLoss: new Unit(StopLossPercent * 100m, UnitTypes.Percent)
		);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, roc);
			DrawIndicator(area, _jma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rocValue)
	{
		// Skip unfinished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Ensure trading is allowed
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Smooth the ROC value with JMA
		var jmaValue = _jma.Process(rocValue);
		if (!jmaValue.IsFinal)
			return;

		var value = jmaValue.GetValue<decimal>();

		if (_prevValue is decimal prev && _prevPrevValue is decimal prev2)
		{
			var turnUp = prev < prev2 && value > prev;
			var turnDown = prev > prev2 && value < prev;

			if (turnUp && Position <= 0)
			{
				// Flip to long on upward turn
				BuyMarket(Volume + Math.Abs(Position));
			}
			else if (turnDown && Position >= 0)
			{
				// Flip to short on downward turn
				SellMarket(Volume + Math.Abs(Position));
			}
		}

		_prevPrevValue = _prevValue;
		_prevValue = value;
	}
}
