using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that aggregates technical ratings across three time frames.
/// Buys when the combined rating is positive and sells when it is negative.
/// </summary>
public class TechnicalRatingsOnMultiFramesAssetsStrategy : Strategy
{
	private readonly StrategyParam<DataType> _shortCandleType;
	private readonly StrategyParam<DataType> _midCandleType;
	private readonly StrategyParam<DataType> _longCandleType;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _rsiPeriod;

	private decimal _shortRating;
	private decimal _midRating;
	private decimal _longRating;

	private bool _shortReady;
	private bool _midReady;
	private bool _longReady;

	/// <summary>
	/// Short time frame candle type.
	/// </summary>
	public DataType ShortCandleType
	{
		get => _shortCandleType.Value;
		set => _shortCandleType.Value = value;
	}

	/// <summary>
	/// Middle time frame candle type.
	/// </summary>
	public DataType MidCandleType
	{
		get => _midCandleType.Value;
		set => _midCandleType.Value = value;
	}

	/// <summary>
	/// Long time frame candle type.
	/// </summary>
	public DataType LongCandleType
	{
		get => _longCandleType.Value;
		set => _longCandleType.Value = value;
	}

	/// <summary>
	/// Moving average period.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public TechnicalRatingsOnMultiFramesAssetsStrategy()
	{
		_shortCandleType = Param(nameof(ShortCandleType), TimeSpan.FromMinutes(60).TimeFrame())
			.SetDisplay("Short Time Frame", "Shortest candle interval", "General");

		_midCandleType = Param(nameof(MidCandleType), TimeSpan.FromMinutes(240).TimeFrame())
			.SetDisplay("Middle Time Frame", "Middle candle interval", "General");

		_longCandleType = Param(nameof(LongCandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Long Time Frame", "Longest candle interval", "General");

		_maPeriod = Param(nameof(MaPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Moving average length", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(20, 100, 10);

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI length", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 30, 5);
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, ShortCandleType), (Security, MidCandleType), (Security, LongCandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_shortRating = 0;
		_midRating = 0;
		_longRating = 0;
		_shortReady = false;
		_midReady = false;
		_longReady = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		CreateSubscription(ShortCandleType, ProcessShort);
		CreateSubscription(MidCandleType, ProcessMid);
		CreateSubscription(LongCandleType, ProcessLong);
	}

	private void CreateSubscription(DataType type, Action<ICandleMessage, decimal, decimal> handler)
	{
		var ma = new SMA { Length = MaPeriod };
		var rsi = new RSI { Length = RsiPeriod };
		var subscription = SubscribeCandles(type);
		subscription.Bind(ma, rsi, handler).Start();
	}

	private void ProcessShort(ICandleMessage candle, decimal maValue, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_shortRating = CalcRating(candle, maValue, rsiValue);
		_shortReady = true;
		TryTrade();
	}

	private void ProcessMid(ICandleMessage candle, decimal maValue, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_midRating = CalcRating(candle, maValue, rsiValue);
		_midReady = true;
		TryTrade();
	}

	private void ProcessLong(ICandleMessage candle, decimal maValue, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_longRating = CalcRating(candle, maValue, rsiValue);
		_longReady = true;
		TryTrade();
	}

	private static decimal CalcRating(ICandleMessage candle, decimal maValue, decimal rsiValue)
	{
		var maRating = candle.ClosePrice > maValue ? 1m : candle.ClosePrice < maValue ? -1m : 0m;
		var rsiRating = rsiValue < 30m ? 1m : rsiValue > 70m ? -1m : 0m;
		return (maRating + rsiRating) / 2m;
	}

	private void TryTrade()
	{
		if (!(_shortReady && _midReady && _longReady))
			return;

		var total = (_shortRating + _midRating + _longRating) / 3m;

		if (total > 0 && Position <= 0)
			BuyMarket();
		else if (total < 0 && Position >= 0)
			SellMarket();
	}
}
