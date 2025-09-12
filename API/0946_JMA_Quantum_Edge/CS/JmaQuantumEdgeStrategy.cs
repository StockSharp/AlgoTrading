using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// JMA-based strategy detecting turning points with higher timeframe filter.
/// Enters long when JMA turns up and is above the higher timeframe JMA.
/// Enters short when JMA turns down and is below the higher timeframe JMA.
/// Applies optional stop loss and take profit.
/// </summary>
public class JmaQuantumEdgeStrategy : Strategy
{
	private readonly StrategyParam<int> _jmaLength;
	private readonly StrategyParam<int> _higherJmaLength;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _higherCandleType;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<bool> _enableStopLoss;
	private readonly StrategyParam<decimal> _takeProfitPercent;

	private decimal? _prevJma;
	private decimal? _prevPrevJma;
	private decimal? _higherJma;

	/// <summary>
	/// Main JMA period length.
	/// </summary>
	public int JmaLength
	{
		get => _jmaLength.Value;
		set => _jmaLength.Value = value;
	}

	/// <summary>
	/// Higher timeframe JMA period length.
	/// </summary>
	public int HigherJmaLength
	{
		get => _higherJmaLength.Value;
		set => _higherJmaLength.Value = value;
	}

	/// <summary>
	/// Candle type for main timeframe.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Candle type for higher timeframe.
	/// </summary>
	public DataType HigherCandleType
	{
		get => _higherCandleType.Value;
		set => _higherCandleType.Value = value;
	}

	/// <summary>
	/// Stop loss percent.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Enable stop loss.
	/// </summary>
	public bool EnableStopLoss
	{
		get => _enableStopLoss.Value;
		set => _enableStopLoss.Value = value;
	}

	/// <summary>
	/// Take profit percent.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	public JmaQuantumEdgeStrategy()
	{
		_jmaLength = Param(nameof(JmaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("JMA Length", "Period for main JMA", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(10, 50, 10);

		_higherJmaLength = Param(nameof(HigherJmaLength), 40)
			.SetGreaterThanZero()
			.SetDisplay("Higher JMA Length", "Period for higher timeframe JMA", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(20, 100, 10);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Main timeframe", "Parameters");

		_higherCandleType = Param(nameof(HigherCandleType), TimeSpan.FromMinutes(60).TimeFrame())
			.SetDisplay("Higher Candle Type", "Higher timeframe", "Parameters");

		_stopLossPercent = Param(nameof(StopLossPercent), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop loss percent", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 3m, 0.5m);

		_enableStopLoss = Param(nameof(EnableStopLoss), true)
			.SetDisplay("Enable Stop Loss", "Use stop loss", "Risk Management");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit %", "Take profit percent", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 1m);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var jma = new JurikMovingAverage { Length = JmaLength };
		var higherJma = new JurikMovingAverage { Length = HigherJmaLength };

		var candleSub = SubscribeCandles(CandleType);
		candleSub
			.Bind(jma, ProcessCandle)
			.Start();

		SubscribeCandles(HigherCandleType)
			.Bind(higherJma, ProcessHigherCandle)
			.Start();

		StartProtection(
			takeProfit: new Unit(TakeProfitPercent * 100m, UnitTypes.Percent),
			stopLoss: EnableStopLoss ? new Unit(StopLossPercent * 100m, UnitTypes.Percent) : null);
	}

	private void ProcessHigherCandle(ICandleMessage candle, decimal jmaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_higherJma = jmaValue;
	}

	private void ProcessCandle(ICandleMessage candle, decimal jmaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevJma is decimal prev && _prevPrevJma is decimal prev2 && _higherJma is decimal higher)
		{
			var turnUp = prev < prev2 && jmaValue >= prev;
			var turnDown = prev > prev2 && jmaValue <= prev;

			if (turnUp && jmaValue > higher && Position <= 0)
				BuyMarket();
			else if (turnDown && jmaValue < higher && Position >= 0)
				SellMarket();
		}

		_prevPrevJma = _prevJma;
		_prevJma = jmaValue;
	}
}
