using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Calculates Black-Scholes option price and performs periodic delta hedging.
/// </summary>
public class BlackScholesDeltaHedgeStrategy : Strategy
{
	private readonly StrategyParam<int> _daysInYear;

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _strikePrice;
	private readonly StrategyParam<decimal> _volatility;
	private readonly StrategyParam<int> _daysToExpiry;
	private readonly StrategyParam<int> _hedgeInterval;
	private readonly StrategyParam<string> _optionType;
	private readonly StrategyParam<string> _positionSide;
	private readonly StrategyParam<decimal> _positionSize;

	private int _candleCounter;
	private decimal _hedgePosition;

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Option strike price.
	/// </summary>
	public decimal StrikePrice { get => _strikePrice.Value; set => _strikePrice.Value = value; }

	/// <summary>
	/// Annual volatility.
	/// </summary>
	public decimal Volatility { get => _volatility.Value; set => _volatility.Value = value; }

	/// <summary>
	/// Days to option expiry.
	/// </summary>
	public int DaysToExpiry { get => _daysToExpiry.Value; set => _daysToExpiry.Value = value; }

	/// <summary>
	/// Number of finished candles between hedge operations.
	/// </summary>
	public int HedgeInterval { get => _hedgeInterval.Value; set => _hedgeInterval.Value = value; }

	/// <summary>
	/// Option type (Call or Put).
	/// </summary>
	public string OptionType { get => _optionType.Value; set => _optionType.Value = value; }

	/// <summary>
	/// Position side for hedging (Long or Short).
	/// </summary>
	public string PositionSide { get => _positionSide.Value; set => _positionSide.Value = value; }

	/// <summary>
	/// Number of option contracts.
	/// </summary>
	public decimal PositionSize { get => _positionSize.Value; set => _positionSize.Value = value; }

	/// <summary>
	/// Trading days per year for time decay calculations.
	/// </summary>
	public int DaysInYear { get => _daysInYear.Value; set => _daysInYear.Value = value; }

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public BlackScholesDeltaHedgeStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_strikePrice = Param(nameof(StrikePrice), 50m)
			.SetDisplay("Strike Price", "Option strike price", "Option Parameters");

		_volatility = Param(nameof(Volatility), 0.2m)
			.SetDisplay("Volatility", "Annual volatility", "Option Parameters");

		_daysToExpiry = Param(nameof(DaysToExpiry), 30)
			.SetDisplay("Days To Expiry", "Days until option expiry", "Option Parameters");

		_hedgeInterval = Param(nameof(HedgeInterval), 1)
			.SetDisplay("Hedge Interval", "Finished candles between hedges", "Trading");

		_optionType = Param(nameof(OptionType), "Call")
			.SetDisplay("Option Type", "Call or Put", "Option Parameters");

		_positionSide = Param(nameof(PositionSide), "Long")
			.SetDisplay("Position Side", "Long or Short", "Trading");

		_positionSize = Param(nameof(PositionSize), 1m)
			.SetDisplay("Position Size", "Number of option contracts", "Trading");

		_daysInYear = Param(nameof(DaysInYear), 365)
			.SetGreaterThanZero()
			.SetDisplay("Days In Year", "Trading days per year", "Option Parameters");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var s = candle.ClosePrice;
		var k = StrikePrice;
		var sigma = Volatility;
		var r = RiskFreeRate;
		var t = DaysToExpiry / (decimal)DaysInYear;

		var sqrtT = (decimal)Math.Sqrt((double)t);
		var d1 = ((decimal)Math.Log((double)(s / k)) + (r + 0.5m * sigma * sigma) * t) / (sigma * sqrtT);
		var d2 = d1 - sigma * sqrtT;

		var nd1 = (double)d1;
		var nd2 = (double)d2;

		decimal price;
		decimal delta;

		if (OptionType == "Put")
		{
			price = k * (decimal)Math.Exp(-(double)r * (double)t) * Cnd(-nd2) - s * Cnd(-nd1);
			delta = Cnd(nd1) - 1m;
		}
		else
		{
			price = s * Cnd(nd1) - k * (decimal)Math.Exp(-(double)r * (double)t) * Cnd(nd2);
			delta = Cnd(nd1);
		}

		LogInfo($"Price: {price:F4} Delta: {delta:F4}");

		if (++_candleCounter >= HedgeInterval)
		{
			var side = PositionSide == "Long" ? 1m : -1m;
			var desired = delta * PositionSize * side;
			var diff = desired - _hedgePosition;

			if (diff > 0)
				BuyMarket(diff);
			else if (diff < 0)
				SellMarket(-diff);

			_hedgePosition = desired;
			_candleCounter = 0;
		}
	}

	private static decimal Cnd(double x)
	{
		var l = x;
		var k = 1.0 / (1.0 + 0.2316419 * Math.Abs(l));
		var kSum = k * (0.319381530 + k * (-0.356563782 + k * (1.781477937 + k * (-1.821255978 + 1.330274429 * k))));
		var cnd = 1.0 - 1.0 / Math.Sqrt(2 * Math.PI) * Math.Exp(-0.5 * l * l) * kSum;
		if (l < 0)
			cnd = 1.0 - cnd;
		return (decimal)cnd;
	}
}
