using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// SuperTrend expert strategy.
/// Opens positions following the SuperTrend direction.
/// </summary>
public class ExpSuperTrendStrategy : Strategy
{
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<DataType> _candleType;

	private SuperTrend _superTrend;

	/// <summary>
	/// ATR period for SuperTrend.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier for SuperTrend.
	/// </summary>
	public decimal Multiplier
	{
		get => _multiplier.Value;
		set => _multiplier.Value = value;
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
	/// Initializes a new instance of <see cref="ExpSuperTrendStrategy"/>.
	/// </summary>
	public ExpSuperTrendStrategy()
	{
		_atrPeriod = Param(nameof(AtrPeriod), 10)
			.SetDisplay("ATR Period", "ATR period for SuperTrend", "SuperTrend")
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_multiplier = Param(nameof(Multiplier), 3m)
			.SetDisplay("Multiplier", "ATR multiplier for SuperTrend", "SuperTrend")
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for indicator calculation", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_superTrend = new SuperTrend
		{
			Length = AtrPeriod,
			Multiplier = Multiplier
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_superTrend, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (stValue is not SuperTrendIndicatorValue st || !stValue.IsFinal)
			return;

		var isUpTrend = st.IsUpTrend;

		if (isUpTrend && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (!isUpTrend && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}
	}
}
