using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy using Jurik moving averages of open and close prices.
/// Goes long when the JMA of open crosses above the JMA of close.
/// Goes short when the JMA of open crosses below the JMA of close.
/// Applies take profit and stop loss in points.
/// </summary>
public class JmaCandleSignStrategy : Strategy
{
	private readonly StrategyParam<int> _jmaLength;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;

	private decimal? _prevOpenJma;
	private decimal? _prevCloseJma;

	/// <summary>
	/// JMA period length.
	/// </summary>
	public int JmaLength
	{
		get => _jmaLength.Value;
		set => _jmaLength.Value = value;
	}

	/// <summary>
	/// Candle type for strategy timeframe.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Take profit in points.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Stop loss in points.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	public JmaCandleSignStrategy()
	{
		_jmaLength = Param(nameof(JmaLength), 7)
			.SetGreaterThanZero()
			.SetDisplay("JMA Length", "Period for Jurik moving averages", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for strategy", "Parameters");

		_takeProfit = Param(nameof(TakeProfit), 2000m)
			.SetDisplay("Take Profit", "Profit target in points", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(500m, 5000m, 500m);

		_stopLoss = Param(nameof(StopLoss), 1000m)
			.SetDisplay("Stop Loss", "Maximum loss in points", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(500m, 5000m, 500m);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var jmaOpen = new JurikMovingAverage
		{
			Length = JmaLength,
			CandlePrice = CandlePrice.Open
		};

		var jmaClose = new JurikMovingAverage
		{
			Length = JmaLength,
			CandlePrice = CandlePrice.Close
		};

		SubscribeCandles(CandleType)
			.Bind(jmaOpen, jmaClose, ProcessCandle)
			.Start();

		StartProtection(
			takeProfit: new Unit(TakeProfit, UnitTypes.Point),
			stopLoss: new Unit(StopLoss, UnitTypes.Point));
	}

	private void ProcessCandle(ICandleMessage candle, decimal jmaOpen, decimal jmaClose)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevOpenJma is decimal prevOpen && _prevCloseJma is decimal prevClose)
		{
			var crossUp = prevOpen >= prevClose && jmaOpen < jmaClose;
			var crossDown = prevOpen <= prevClose && jmaOpen > jmaClose;

			if (crossUp && Position <= 0)
				BuyMarket();
			else if (crossDown && Position >= 0)
				SellMarket();
		}

		_prevOpenJma = jmaOpen;
		_prevCloseJma = jmaClose;
	}
}
