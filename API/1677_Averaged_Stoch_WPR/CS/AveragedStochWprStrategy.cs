using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Stochastic oscillator and Williams %R.
/// Buys when Stochastic is extremely low and Williams %R confirms deep oversold.
/// Sells when Stochastic is extremely high and Williams %R confirms overbought.
/// Optional percentage stop loss can be configured.
/// </summary>
public class AveragedStochWprStrategy : Strategy
{
	private readonly StrategyParam<int> _stochPeriod;
	private readonly StrategyParam<int> _wprPeriod;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Stochastic calculation period.
	/// </summary>
	public int StochPeriod
	{
		get => _stochPeriod.Value;
		set => _stochPeriod.Value = value;
	}

	/// <summary>
	/// Williams %R calculation period.
	/// </summary>
	public int WprPeriod
	{
		get => _wprPeriod.Value;
		set => _wprPeriod.Value = value;
	}

	/// <summary>
	/// Stop loss size in percent.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
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
	/// Constructor.
	/// </summary>
	public AveragedStochWprStrategy()
	{
		_stochPeriod = Param(nameof(StochPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic Period", "Period for Stochastic oscillator", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 50, 5);

		_wprPeriod = Param(nameof(WprPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("Williams %R Period", "Period for Williams %R", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 50, 5);

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Percentage based stop loss", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(1m, 10m, 1m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe for analysis", "General");
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

		var stoch = new Stochastic { Length = StochPeriod };
		var wpr = new WilliamsR { Length = WprPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(stoch, wpr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, stoch);
			DrawIndicator(area, wpr);
			DrawOwnTrades(area);
		}

		StartProtection(new(), new Unit(StopLossPercent, UnitTypes.Percent));
	}

	private void ProcessCandle(ICandleMessage candle, decimal stochValue, decimal wprValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Buy when both indicators show strong oversold
		if (stochValue < 0.1m && wprValue < -90m)
		{
			if (Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
		}
		// Sell when both indicators show strong overbought
		else if (stochValue > 99.9m && wprValue > -5m)
		{
			if (Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
		}
	}
}
