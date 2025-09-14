using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// JFATL Digit System based on the slope of the Jurik moving average.
/// Opens long when the JMA turns upward and short when it turns downward.
/// Applies optional stop loss and take profit.
/// </summary>
public class JfatlDigitSystemStrategy : Strategy
{
	private readonly StrategyParam<int> _jmaLength;
	private readonly StrategyParam<int> _jmaPhase;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<bool> _enableStopLoss;
	private readonly StrategyParam<decimal> _takeProfitPercent;

	private decimal? _prevJma;
	private decimal? _prevSlope;

	/// <summary>
	/// JMA length.
	/// </summary>
	public int JmaLength
	{
		get => _jmaLength.Value;
		set => _jmaLength.Value = value;
	}

	/// <summary>
	/// JMA phase.
	/// </summary>
	public int JmaPhase
	{
		get => _jmaPhase.Value;
		set => _jmaPhase.Value = value;
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

	public JfatlDigitSystemStrategy()
	{
		_jmaLength = Param(nameof(JmaLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("JMA Length", "JMA period", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(3, 20, 1);

		_jmaPhase = Param(nameof(JmaPhase), -100)
			.SetDisplay("JMA Phase", "JMA phase", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(-100, 100, 20);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "Parameters");

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

		var jma = new JurikMovingAverage
		{
			Length = JmaLength,
			Phase = JmaPhase
		};

		SubscribeCandles(CandleType)
			.Bind(jma, ProcessCandle)
			.Start();

		StartProtection(
			takeProfit: new Unit(TakeProfitPercent * 100m, UnitTypes.Percent),
			stopLoss: EnableStopLoss ? new Unit(StopLossPercent * 100m, UnitTypes.Percent) : null);
	}

	private void ProcessCandle(ICandleMessage candle, decimal jmaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevJma is decimal prev)
		{
			var slope = jmaValue - prev;

			if (_prevSlope is decimal prevSlope)
			{
				var turnedUp = prevSlope <= 0 && slope > 0;
				var turnedDown = prevSlope >= 0 && slope < 0;

				if (turnedUp && Position <= 0)
					BuyMarket();
				else if (turnedDown && Position >= 0)
					SellMarket();
			}

			_prevSlope = slope;
		}

		_prevJma = jmaValue;
	}
}
