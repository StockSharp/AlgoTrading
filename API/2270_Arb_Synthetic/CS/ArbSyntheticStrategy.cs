using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Triangular arbitrage strategy that compares a cross pair with its synthetic value.
/// </summary>
public class ArbSyntheticStrategy : Strategy
{
	private readonly StrategyParam<Security> _eurUsdParam;
	private readonly StrategyParam<Security> _gbpUsdParam;
	private readonly StrategyParam<Security> _eurGbpParam;
	private readonly StrategyParam<DataType> _candleTypeParam;
	private readonly StrategyParam<int> _spreadParam;

	private decimal _medianEurUsd;
	private decimal _medianGbpUsd;
	private decimal _medianEurGbp;

	/// <summary>
	/// EURUSD security.
	/// </summary>
	public Security EurUsd
	{
		get => _eurUsdParam.Value;
		set => _eurUsdParam.Value = value;
	}

	/// <summary>
	/// GBPUSD security.
	/// </summary>
	public Security GbpUsd
	{
		get => _gbpUsdParam.Value;
		set => _gbpUsdParam.Value = value;
	}

	/// <summary>
	/// EURGBP security.
	/// </summary>
	public Security EurGbp
	{
		get => _eurGbpParam.Value;
		set => _eurGbpParam.Value = value;
	}

	/// <summary>
	/// Candle type for price data.
	/// </summary>
	public DataType CandleType
	{
		get => _candleTypeParam.Value;
		set => _candleTypeParam.Value = value;
	}

	/// <summary>
	/// Spread threshold in points.
	/// </summary>
	public int Spread
	{
		get => _spreadParam.Value;
		set => _spreadParam.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="ArbSyntheticStrategy" />.
	/// </summary>
	public ArbSyntheticStrategy()
	{
		_eurUsdParam = Param<Security>(nameof(EurUsd))
			.SetDisplay("EURUSD", "EURUSD pair", "Instruments");

		_gbpUsdParam = Param<Security>(nameof(GbpUsd))
			.SetDisplay("GBPUSD", "GBPUSD pair", "Instruments");

		_eurGbpParam = Param<Security>(nameof(EurGbp))
			.SetDisplay("EURGBP", "EURGBP pair", "Instruments");

		_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_spreadParam = Param(nameof(Spread), 35)
			.SetDisplay("Spread", "Spread deviations in points", "Strategy")
			.SetCanOptimize(true)
			.SetGreaterThanZero();
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (EurUsd != null && CandleType != null)
			yield return (EurUsd, CandleType);

		if (GbpUsd != null && CandleType != null)
			yield return (GbpUsd, CandleType);

		if (EurGbp != null && CandleType != null)
			yield return (EurGbp, CandleType);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (EurUsd != null && GbpUsd != null && EurGbp != null)
		{
			var eurUsdSub = SubscribeCandles(CandleType, security: EurUsd);
			var gbpUsdSub = SubscribeCandles(CandleType, security: GbpUsd);
			var eurGbpSub = SubscribeCandles(CandleType, security: EurGbp);

			eurUsdSub.Bind(ProcessEurUsd).Start();
			gbpUsdSub.Bind(ProcessGbpUsd).Start();
			eurGbpSub.Bind(ProcessEurGbp).Start();

			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, eurUsdSub);
				DrawCandles(area, gbpUsdSub);
				DrawCandles(area, eurGbpSub);
				DrawOwnTrades(area);
			}
		}
		else
		{
			LogWarning("One or more securities are not specified.");
		}

		StartProtection();
	}

	private void ProcessEurUsd(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_medianEurUsd = candle.ClosePrice;
		Process();
	}

	private void ProcessGbpUsd(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_medianGbpUsd = candle.ClosePrice;
		Process();
	}

	private void ProcessEurGbp(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_medianEurGbp = candle.ClosePrice;
		Process();
	}

	private void Process()
	{
		if (_medianEurUsd == 0m || _medianGbpUsd == 0m || _medianEurGbp == 0m)
			return;

		var medianSynthetic = _medianEurUsd / _medianGbpUsd;
		var eurSynthetic = _medianGbpUsd * _medianEurGbp;
		var gbpSynthetic = _medianEurUsd / _medianEurGbp;

		var diff = medianSynthetic - _medianEurGbp;
		var eurDiff = eurSynthetic - _medianEurUsd;
		var gbpDiff = gbpSynthetic - _medianGbpUsd;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var eurGbpThreshold = Spread * (EurGbp?.PriceStep ?? 0.0001m);
		var eurUsdThreshold = Spread * (EurUsd?.PriceStep ?? 0.0001m);
		var gbpUsdThreshold = Spread * (GbpUsd?.PriceStep ?? 0.0001m);

		Trade(EurGbp, diff, eurGbpThreshold);
		Trade(EurUsd, eurDiff, eurUsdThreshold);
		Trade(GbpUsd, gbpDiff, gbpUsdThreshold);
	}

	private void Trade(Security security, decimal diff, decimal threshold)
	{
		var position = GetPositionValue(security);

		if (diff > threshold && position <= 0)
		{
			if (position < 0)
				BuyMarket(Math.Abs(position), security);

			BuyMarket(Volume, security);
		}
		else if (diff < -threshold && position >= 0)
		{
			if (position > 0)
				SellMarket(Math.Abs(position), security);

			SellMarket(Volume, security);
		}
	}

	private decimal GetPositionValue(Security security)
	{
		return GetPositionValue(security, Portfolio) ?? 0m;
	}
}
