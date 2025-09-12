using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// VIX Futures Basis Strategy.
/// Trades the front VIX future based on contango/backwardation between the VIX index and two futures contracts.
/// </summary>
public class VixFuturesBasisStrategy : Strategy
{
	private readonly StrategyParam<decimal> _basisThreshold;
	private readonly StrategyParam<Security> _indexSecurity;
	private readonly StrategyParam<Security> _secondFutureSecurity;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _vixClose;
	private decimal _vx2Close;
	private bool _vixReady;
	private bool _vx2Ready;

	/// <summary>
	/// Basis threshold for contango/backwardation.
	/// </summary>
	public decimal BasisThreshold { get => _basisThreshold.Value; set => _basisThreshold.Value = value; }

	/// <summary>
	/// VIX index security.
	/// </summary>
	public Security IndexSecurity { get => _indexSecurity.Value; set => _indexSecurity.Value = value; }

	/// <summary>
	/// Second future security.
	/// </summary>
	public Security SecondFutureSecurity { get => _secondFutureSecurity.Value; set => _secondFutureSecurity.Value = value; }

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public VixFuturesBasisStrategy()
	{
		_basisThreshold = Param(nameof(BasisThreshold), 0.1m)
			.SetDisplay("Basis Threshold", "Contango/backwardation threshold", "General")
			.SetCanOptimize(true)
			.SetOptimize(0.05m, 0.5m, 0.05m);

		_indexSecurity = Param<Security>(nameof(IndexSecurity))
			.SetDisplay("VIX Index", "Underlying index security", "Data")
			.SetRequired();

		_secondFutureSecurity = Param<Security>(nameof(SecondFutureSecurity))
			.SetDisplay("Second Future", "Next VIX future security", "Data")
			.SetRequired();

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
		yield return (IndexSecurity, CandleType);
		yield return (SecondFutureSecurity, CandleType);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var mainSub = SubscribeCandles(CandleType);
		mainSub.Bind(ProcessMainCandle).Start();

		var indexSub = SubscribeCandles(CandleType, security: IndexSecurity);
		indexSub.Bind(ProcessIndexCandle).Start();

		var secondSub = SubscribeCandles(CandleType, security: SecondFutureSecurity);
		secondSub.Bind(ProcessSecondCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, mainSub);
			DrawOwnTrades(area);
		}
	}

	private void ProcessIndexCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_vixClose = candle.ClosePrice;
		_vixReady = true;
	}

	private void ProcessSecondCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_vx2Close = candle.ClosePrice;
		_vx2Ready = true;
	}

	private void ProcessMainCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished || !_vixReady || !_vx2Ready)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var basis1 = candle.ClosePrice - _vixClose;
		var basis2 = _vx2Close - _vixClose;

		var contango = basis1 > BasisThreshold && basis2 > BasisThreshold;
		var backwardation = basis1 < -BasisThreshold && basis2 < -BasisThreshold;

		if (contango && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (backwardation && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}
