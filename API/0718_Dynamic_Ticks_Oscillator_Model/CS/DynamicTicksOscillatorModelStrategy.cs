using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Rate of Change of NYSE down ticks with dynamic thresholds.
/// </summary>
public class DynamicTicksOscillatorModelStrategy : Strategy
{
	private readonly StrategyParam<Security> _downTicksSecurity;
	private readonly StrategyParam<int> _rocLength;
	private readonly StrategyParam<int> _volatilityLookback;
	private readonly StrategyParam<decimal> _entryStdDevMultiplier;
	private readonly StrategyParam<decimal> _exitStdDevMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private RateOfChange _roc;
	private StandardDeviation _stdDev;

	/// <summary>
	/// NYSE Down Ticks security.
	/// </summary>
	public Security DownTicksSecurity
	{
		get => _downTicksSecurity.Value;
		set => _downTicksSecurity.Value = value;
	}

	/// <summary>
	/// Lookback period for ROC calculation.
	/// </summary>
	public int RocLength
	{
		get => _rocLength.Value;
		set => _rocLength.Value = value;
	}

	/// <summary>
	/// Lookback period for standard deviation.
	/// </summary>
	public int VolatilityLookback
	{
		get => _volatilityLookback.Value;
		set => _volatilityLookback.Value = value;
	}

	/// <summary>
	/// Entry threshold multiplier.
	/// </summary>
	public decimal EntryStdDevMultiplier
	{
		get => _entryStdDevMultiplier.Value;
		set => _entryStdDevMultiplier.Value = value;
	}

	/// <summary>
	/// Exit threshold multiplier.
	/// </summary>
	public decimal ExitStdDevMultiplier
	{
		get => _exitStdDevMultiplier.Value;
		set => _exitStdDevMultiplier.Value = value;
	}

	/// <summary>
	/// Candle type for down ticks data.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="DynamicTicksOscillatorModelStrategy"/>.
	/// </summary>
	public DynamicTicksOscillatorModelStrategy()
	{
		_downTicksSecurity = Param(nameof(DownTicksSecurity), new Security { Id = "USI:DNTKS.NY" })
			.SetDisplay("Down Ticks Security", "NYSE Down Ticks symbol", "General");

		_rocLength = Param(nameof(RocLength), 5)
			.SetRange(1, 100)
			.SetDisplay("Down Ticks ROC Length", "Lookback period for ROC calculation", "Indicators")
			.SetCanOptimize(true);

		_volatilityLookback = Param(nameof(VolatilityLookback), 24)
			.SetRange(1, 100)
			.SetDisplay("Down Ticks Volatility Lookback", "Lookback period for standard deviation", "Indicators")
			.SetCanOptimize(true);

		_entryStdDevMultiplier = Param(nameof(EntryStdDevMultiplier), 1.6m)
			.SetRange(0.1m, 5m)
			.SetDisplay("Entry Standard Deviation Multiplier", "Multiplier for long entry threshold", "Indicators")
			.SetCanOptimize(true);

		_exitStdDevMultiplier = Param(nameof(ExitStdDevMultiplier), 1.4m)
			.SetRange(0.1m, 5m)
			.SetDisplay("Exit Standard Deviation Multiplier", "Multiplier for exit threshold", "Indicators")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for down ticks data", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return
		[
			(Security, CandleType),
			(DownTicksSecurity, CandleType)
		];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_roc = null;
		_stdDev = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (DownTicksSecurity == null)
			throw new InvalidOperationException("Down ticks security is not specified.");

		_roc = new RateOfChange { Length = RocLength };
		_stdDev = new StandardDeviation { Length = VolatilityLookback };

		var subscription = SubscribeCandles(CandleType, security: DownTicksSecurity);
		subscription
			.Bind(_roc, ProcessDownTicks)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _roc);
			DrawIndicator(area, _stdDev);
			DrawOwnTrades(area);
		}
	}

	private void ProcessDownTicks(ICandleMessage candle, decimal rocValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var stdDevValue = _stdDev.Process(rocValue, candle.ServerTime, true).ToDecimal();

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var entryThreshold = -stdDevValue * EntryStdDevMultiplier;
		var exitThreshold = stdDevValue * ExitStdDevMultiplier;

		if (rocValue < entryThreshold && Position <= 0)
		{
			BuyMarket();
		}
		else if (Position > 0 && rocValue > exitThreshold)
		{
			SellMarket();
		}
	}
}

