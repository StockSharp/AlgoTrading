using System;
using System.Collections.Generic;
using Ecng.ComponentModel;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simple statistical arbitrage strategy.
/// Buys the first asset when the spread between two securities
/// falls below the mean by a multiple of its standard deviation.
/// Exits when the spread reverts to the mean.
/// </summary>
public class StatisticalArbitrageSpreadStrategy : Strategy
{
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<decimal> _stdMultiplier;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<Security> _secondSecurity;

	private SimpleMovingAverage _spreadMa;
	private StandardDeviation _spreadStd;
	private decimal _lastSecondPrice;

	/// <summary>
	/// Lookback period for mean and standard deviation calculations.
	/// </summary>
	public int LookbackPeriod
	{
		get => _lookbackPeriod.Value;
		set => _lookbackPeriod.Value = value;
	}

	/// <summary>
	/// Standard deviation multiplier for entry.
	/// </summary>
	public decimal StdMultiplier
	{
		get => _stdMultiplier.Value;
		set => _stdMultiplier.Value = value;
	}

	/// <summary>
	/// Candle type parameter.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Second security used to calculate the spread.
	/// </summary>
	public Security SecondSecurity
	{
		get => _secondSecurity.Value;
		set => _secondSecurity.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public StatisticalArbitrageSpreadStrategy()
	{
		_lookbackPeriod = Param(nameof(LookbackPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Lookback Period", "Period for mean and std dev", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(10, 40, 5);

		_stdMultiplier = Param(nameof(StdMultiplier), 2.0m)
			.SetGreaterThanZero()
			.SetDisplay("Std Multiplier", "Standard deviation multiplier", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(1.0m, 3.0m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_secondSecurity = Param<Security>(nameof(SecondSecurity))
			.SetDisplay("Second Security", "Second security for spread", "General")
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
		_spreadStd = null;
		_lastSecondPrice = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (SecondSecurity == null)
			throw new InvalidOperationException("Second security is not specified.");

		_spreadMa = new() { Length = LookbackPeriod };
		_spreadStd = new StandardDeviation { Length = LookbackPeriod };

		var firstSub = SubscribeCandles(CandleType);
		var secondSub = SubscribeCandles(CandleType, security: SecondSecurity);

		firstSub.Bind(ProcessFirstCandle).Start();
		secondSub.Bind(ProcessSecondCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, firstSub);
			DrawIndicator(area, _spreadMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessSecondCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_lastSecondPrice = candle.ClosePrice;
	}

	private void ProcessFirstCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_lastSecondPrice == 0 || !IsFormedAndOnlineAndAllowTrading())
			return;

		var spread = candle.ClosePrice - _lastSecondPrice;
		var maValue = _spreadMa.Process(spread, candle.ServerTime, true).ToDecimal();
		var stdValue = _spreadStd.Process(spread, candle.ServerTime, true).ToDecimal();

		if (!_spreadMa.IsFormed || !_spreadStd.IsFormed)
			return;

		var lowerBand = maValue - (stdValue * StdMultiplier);

		if (spread < lowerBand && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			LogInfo($"Long entry: spread {spread:F4} < lower band {lowerBand:F4}");
		}
		else if (Position > 0 && spread > maValue)
		{
			SellMarket(Math.Abs(Position));
			LogInfo($"Exit long: spread {spread:F4} > mean {maValue:F4}");
		}
	}
}
