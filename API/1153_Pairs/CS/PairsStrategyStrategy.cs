using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Buys when the reference asset closes above its open and the current symbol forms a down bar.
/// Closes the position when price closes above the previous candle's high.
/// </summary>
public class PairsStrategyStrategy : Strategy
{
	private readonly StrategyParam<Security> _referenceSecurity;
	private readonly StrategyParam<DataType> _candleType;

	private bool _referenceUp;
	private decimal _previousHigh;

	/// <summary>
	/// Reference security used for comparison.
	/// </summary>
	public Security ReferenceSecurity
	{
		get => _referenceSecurity.Value;
		set => _referenceSecurity.Value = value;
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
	/// Initializes a new instance of the <see cref="PairsStrategyStrategy"/>.
	/// </summary>
	public PairsStrategyStrategy()
	{
		_referenceSecurity = Param<Security>(nameof(ReferenceSecurity))
			.SetDisplay("Reference Security", "Security used for pair comparison", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (ReferenceSecurity, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_referenceUp = false;
		_previousHigh = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		if (ReferenceSecurity == null)
			throw new InvalidOperationException("ReferenceSecurity must be specified.");

		base.OnStarted(time);

		SubscribeCandles(CandleType, true, ReferenceSecurity)
			.Bind(ProcessReference)
			.Start();

		SubscribeCandles(CandleType)
			.Bind(ProcessMain)
			.Start();

		StartProtection();
	}

	private void ProcessReference(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_referenceUp = candle.ClosePrice > candle.OpenPrice;
	}

	private void ProcessMain(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (Position <= 0 && _referenceUp && candle.ClosePrice < candle.OpenPrice && IsFormedAndOnlineAndAllowTrading())
			BuyMarket();

		if (Position > 0 && candle.ClosePrice > _previousHigh && IsFormedAndOnlineAndAllowTrading())
			SellMarket();

		_previousHigh = candle.HighPrice;
	}
}
