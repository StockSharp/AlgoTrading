using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Economic Policy Uncertainty strategy.
/// Long entry when SMA crosses above threshold and exit after fixed bars.
/// </summary>
public class EconomicPolicyUncertaintyStrategy : Strategy
{
	private readonly StrategyParam<decimal> _threshold;
	private readonly StrategyParam<int> _smaLength;
	private readonly StrategyParam<int> _exitPeriods;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _previousSma;
	private bool _isFirst;
	private int? _barsInTrade;

	/// <summary>
	/// Threshold for SMA cross.
	/// </summary>
	public decimal Threshold
	{
		get => _threshold.Value;
		set => _threshold.Value = value;
	}

	/// <summary>
	/// Length of SMA indicator.
	/// </summary>
	public int SmaLength
	{
		get => _smaLength.Value;
		set => _smaLength.Value = value;
	}

	/// <summary>
	/// Number of bars to hold position before exit.
	/// </summary>
	public int ExitPeriods
	{
		get => _exitPeriods.Value;
		set => _exitPeriods.Value = value;
	}

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="EconomicPolicyUncertaintyStrategy"/>.
	/// </summary>
	public EconomicPolicyUncertaintyStrategy()
	{
		_threshold = Param(nameof(Threshold), 187m)
			.SetDisplay("Threshold", "Dynamic threshold", "Strategy Parameters")
			.SetCanOptimize(true)
			.SetOptimize(150m, 250m, 10m);

		_smaLength = Param(nameof(SmaLength), 2)
			.SetDisplay("SMA Length", "SMA calculation length", "Strategy Parameters")
			.SetCanOptimize(true)
			.SetOptimize(2, 20, 1);

		_exitPeriods = Param(nameof(ExitPeriods), 10)
			.SetDisplay("Exit Periods", "Exit after specified bars", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 5);

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for strategy", "Data");
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
		_previousSma = 0m;
		_isFirst = true;
		_barsInTrade = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var sma = new SimpleMovingAverage { Length = SmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_isFirst)
		{
			_previousSma = smaValue;
			_isFirst = false;
			return;
		}

		var longCondition = _previousSma <= Threshold && smaValue > Threshold;

		if (longCondition && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_barsInTrade = 0;
		}

		if (_barsInTrade.HasValue)
		{
			_barsInTrade++;
			if (_barsInTrade >= ExitPeriods && Position > 0)
			{
				SellMarket(Position);
				_barsInTrade = null;
			}
		}

		_previousSma = smaValue;
	}
}
