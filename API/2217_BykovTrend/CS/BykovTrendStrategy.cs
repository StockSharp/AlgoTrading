using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bykov Trend strategy based on Williams %R indicator.
/// Generates buy or sell signals when trend changes occur.
/// </summary>
public class BykovTrendStrategy : Strategy
{
	private readonly StrategyParam<int> _risk;
	private readonly StrategyParam<int> _ssp;
	private readonly StrategyParam<DataType> _candleType;

	private bool _previousUptrend;

	/// <summary>
	/// Risk parameter from original indicator (K = 33 - Risk).
	/// </summary>
	public int Risk
	{
		get => _risk.Value;
		set => _risk.Value = value;
	}

	/// <summary>
	/// Williams %R period.
	/// </summary>
	public int Ssp
	{
		get => _ssp.Value;
		set => _ssp.Value = value;
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
	/// Initializes a new instance of <see cref="BykovTrendStrategy" />.
	/// </summary>
	public BykovTrendStrategy()
	{
		_risk = Param(nameof(Risk), 3)
			.SetGreaterThanZero()
			.SetDisplay("Risk", "Risk parameter from original indicator", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(1, 10, 1);

		_ssp = Param(nameof(Ssp), 9)
			.SetGreaterThanZero()
			.SetDisplay("SSP", "Williams %R period", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for indicator", "General");
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

		var wpr = new WilliamsR { Length = Ssp };
		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(wpr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, wpr);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal wprValue)
	{
		// Process only finished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Ensure that strategy is ready to trade
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var k = 33 - Risk;
		var uptrend = _previousUptrend;

		if (wprValue < -100 + k)
			uptrend = false;
		else if (wprValue > -k)
			uptrend = true;

		var buySignal = !_previousUptrend && uptrend;
		var sellSignal = _previousUptrend && !uptrend;

		_previousUptrend = uptrend;

		if (buySignal)
		{
			// Close short positions and enter long
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (sellSignal)
		{
			// Close long positions and enter short
			SellMarket(Volume + Math.Abs(Position));
		}
	}
}
