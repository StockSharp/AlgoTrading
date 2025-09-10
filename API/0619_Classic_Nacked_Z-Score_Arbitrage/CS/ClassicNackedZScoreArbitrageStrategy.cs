using System;
using System.Collections.Generic;

using Ecng.ComponentModel;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Classic Nacked Z-Score arbitrage strategy.
/// Trades spread between two assets using Z-Score.
/// </summary>
public class ClassicNackedZScoreArbitrageStrategy : Strategy
{
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<decimal> _zScoreThreshold;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<Security> _secondSecurity;

	private SimpleMovingAverage _spreadMa;
	private StandardDeviation _spreadStdDev;

	private decimal _lastSecondPrice;

	/// <summary>
	/// Lookback period for spread mean and standard deviation.
	/// </summary>
	public int LookbackPeriod
	{
		get => _lookbackPeriod.Value;
		set => _lookbackPeriod.Value = value;
	}

	/// <summary>
	/// Z-Score threshold for entries.
	/// </summary>
	public decimal ZScoreThreshold
	{
		get => _zScoreThreshold.Value;
		set => _zScoreThreshold.Value = value;
	}

	/// <summary>
	/// Candle type used for both securities.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Second security in pair.
	/// </summary>
	public Security SecondSecurity
	{
		get => _secondSecurity.Value;
		set => _secondSecurity.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public ClassicNackedZScoreArbitrageStrategy()
	{
		_lookbackPeriod = Param(nameof(LookbackPeriod), 33)
			.SetGreaterThanZero()
			.SetDisplay("Lookback Period", "Period for spread mean and std deviation", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(20, 60, 5);

		_zScoreThreshold = Param(nameof(ZScoreThreshold), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Z-Score Threshold", "Threshold for spread Z-Score", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(1m, 3m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_secondSecurity = Param<Security>(nameof(SecondSecurity))
			.SetDisplay("Second Security", "Second asset in pair", "General")
			.SetRequired();
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return
		[
			(Security, CandleType),
			(SecondSecurity, CandleType)
		];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_spreadMa = null;
		_spreadStdDev = null;
		_lastSecondPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (SecondSecurity == null)
			throw new InvalidOperationException("Second security is not specified.");

		_spreadMa = new() { Length = LookbackPeriod };
		_spreadStdDev = new StandardDeviation { Length = LookbackPeriod };

		var firstSubscription = SubscribeCandles(CandleType);
		var secondSubscription = SubscribeCandles(CandleType, security: SecondSecurity);

		firstSubscription
			.Bind(ProcessFirstSecurityCandle)
			.Start();

		secondSubscription
			.Bind(ProcessSecondSecurityCandle)
			.Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, firstSubscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessFirstSecurityCandle(ICandleMessage candle)
	{
		if (_lastSecondPrice == 0m)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var spread = candle.ClosePrice - _lastSecondPrice;

		var maValue = _spreadMa.Process(spread, candle.ServerTime, candle.State == CandleStates.Finished);
		var stdValue = _spreadStdDev.Process(spread, candle.ServerTime, candle.State == CandleStates.Finished);

		if (!_spreadMa.IsFormed || !_spreadStdDev.IsFormed)
			return;

		var mean = maValue.ToDecimal();
		var std = stdValue.ToDecimal();

		if (std == 0)
			return;

		var z = (spread - mean) / std;

		if (z > ZScoreThreshold)
		{
			if (Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
		}
		else if (z < -ZScoreThreshold)
		{
			if (Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
		}
		else
		{
			if (Position > 0 && z >= 0)
				SellMarket(Position);
			else if (Position < 0 && z <= 0)
				BuyMarket(Math.Abs(Position));
		}
	}

	private void ProcessSecondSecurityCandle(ICandleMessage candle)
	{
		_lastSecondPrice = candle.ClosePrice;
	}
}
