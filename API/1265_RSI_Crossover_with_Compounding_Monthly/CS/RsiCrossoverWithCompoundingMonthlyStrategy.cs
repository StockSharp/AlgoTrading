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
/// Strategy that buys when monthly RSI crosses above its SimpleMovingAverage and sells when it drops below.
/// Reinvests profits to compound capital.
/// </summary>
public class RsiCrossoverWithCompoundingMonthlyStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _initialCapital;

	private RelativeStrengthIndex _rsi;
	private SimpleMovingAverage _rsiSma;
	private decimal _capital;

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// RSI period length.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Starting capital for compounding.
	/// </summary>
	public decimal InitialCapital
	{
		get => _initialCapital.Value;
		set => _initialCapital.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public RsiCrossoverWithCompoundingMonthlyStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(30).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for RSI", "General");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "Length of RSI", "RSI");

		_initialCapital = Param(nameof(InitialCapital), 100000m)
			.SetGreaterThanZero()
			.SetDisplay("Initial Capital", "Starting capital for compounding", "General");
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

		_rsi = null;
		_rsiSma = null;
		_capital = InitialCapital;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_capital = InitialCapital;
		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		_rsiSma = new SimpleMovingAverage { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _rsi);
			DrawIndicator(area, _rsiSma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var smaVal = _rsiSma.Process(new DecimalIndicatorValue(_rsiSma, rsiValue, candle.ServerTime));
		if (!smaVal.IsFinal)
			return;

		var rsiSmaValue = smaVal.GetValue<decimal>();

		if (rsiValue > rsiSmaValue && Position <= 0)
		{
			BuyMarket();
		}
		else if (rsiValue < rsiSmaValue && Position > 0)
		{
			SellMarket();
		}
	}
}
