using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trend-following strategy based on the slope of Jurik Moving Average.
/// Opens long when the JMA turns up and short when it turns down.
/// </summary>
public class ColorJsatlDigitStrategy : Strategy
{
	private readonly StrategyParam<int> _jmaLength;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _directMode;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;

	private decimal? _prevJma;
	private decimal? _prevPrevJma;

	/// <summary>
	/// JMA period length.
	/// </summary>
	public int JmaLength
	{
		get => _jmaLength.Value;
		set => _jmaLength.Value = value;
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
	/// Trade in direction of the signal.
	/// </summary>
	public bool DirectMode
	{
		get => _directMode.Value;
		set => _directMode.Value = value;
	}

	/// <summary>
	/// Stop loss percent.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take profit percent.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	public ColorJsatlDigitStrategy()
	{
		_jmaLength = Param(nameof(JmaLength), 30)
			.SetGreaterThanZero()
			.SetDisplay("JMA Length", "JMA period length", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(10, 60, 5);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Indicator timeframe", "Parameters");

		_directMode = Param(nameof(DirectMode), true)
			.SetDisplay("Direct Mode", "Trade in direction of signal", "Parameters");

		_stopLoss = Param(nameof(StopLoss), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop loss percent", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 5m, 0.5m);

		_takeProfit = Param(nameof(TakeProfit), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit %", "Take profit percent", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(1m, 10m, 1m);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var jma = new JurikMovingAverage { Length = JmaLength };

		var sub = SubscribeCandles(CandleType);
		sub.Bind(jma, ProcessCandle).Start();

		StartProtection(
			takeProfit: new Unit(TakeProfit * 100m, UnitTypes.Percent),
			stopLoss: new Unit(StopLoss * 100m, UnitTypes.Percent));
	}

	private void ProcessCandle(ICandleMessage candle, decimal jmaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevJma is decimal prev && _prevPrevJma is decimal prev2)
		{
			var turnUp = prev > prev2 && jmaValue >= prev;
			var turnDown = prev < prev2 && jmaValue <= prev;

			if (DirectMode)
			{
				if (turnUp && Position <= 0)
					BuyMarket();
				else if (turnDown && Position >= 0)
					SellMarket();
			}
			else
			{
				if (turnDown && Position <= 0)
					BuyMarket();
				else if (turnUp && Position >= 0)
					SellMarket();
			}
		}

		_prevPrevJma = _prevJma;
		_prevJma = jmaValue;
	}
}

