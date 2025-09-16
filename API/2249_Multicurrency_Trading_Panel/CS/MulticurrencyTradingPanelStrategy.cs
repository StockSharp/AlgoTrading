using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that imitates a multicurrency trading panel and trades three pairs.
/// </summary>
public class MulticurrencyTradingPanelStrategy : Strategy
{
	private readonly StrategyParam<Security> _eurUsdParam;
	private readonly StrategyParam<Security> _usdJpyParam;
	private readonly StrategyParam<Security> _gbpUsdParam;
	private readonly StrategyParam<DataType> _candleTypeParam;
	private readonly StrategyParam<bool> _autoTradeParam;

	private ICandleMessage _eurUsdPrev;
	private ICandleMessage _usdJpyPrev;
	private ICandleMessage _gbpUsdPrev;

	/// <summary>
	/// EURUSD trading instrument.
	/// </summary>
	public Security EurUsd
	{
		get => _eurUsdParam.Value;
		set => _eurUsdParam.Value = value;
	}

	/// <summary>
	/// USDJPY trading instrument.
	/// </summary>
	public Security UsdJpy
	{
		get => _usdJpyParam.Value;
		set => _usdJpyParam.Value = value;
	}

	/// <summary>
	/// GBPUSD trading instrument.
	/// </summary>
	public Security GbpUsd
	{
		get => _gbpUsdParam.Value;
		set => _gbpUsdParam.Value = value;
	}

	/// <summary>
	/// Candle type used for signal calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleTypeParam.Value;
		set => _candleTypeParam.Value = value;
	}

	/// <summary>
	/// Enable automatic trading decisions.
	/// </summary>
	public bool AutoTrade
	{
		get => _autoTradeParam.Value;
		set => _autoTradeParam.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public MulticurrencyTradingPanelStrategy()
	{
		_eurUsdParam = Param<Security>(nameof(EurUsd))
			.SetDisplay("EURUSD", "First currency pair", "Instruments");

		_usdJpyParam = Param<Security>(nameof(UsdJpy))
			.SetDisplay("USDJPY", "Second currency pair", "Instruments");

		_gbpUsdParam = Param<Security>(nameof(GbpUsd))
			.SetDisplay("GBPUSD", "Third currency pair", "Instruments");

		_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for analysis", "General");

		_autoTradeParam = Param(nameof(AutoTrade), false)
			.SetDisplay("Auto Trade", "Enable automatic trading", "Trading");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (EurUsd != null && CandleType != null)
			yield return (EurUsd, CandleType);

		if (UsdJpy != null && CandleType != null)
			yield return (UsdJpy, CandleType);

		if (GbpUsd != null && CandleType != null)
			yield return (GbpUsd, CandleType);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (EurUsd != null)
			SubscribeCandles(CandleType, EurUsd).Bind(ProcessEurUsd).Start();

		if (UsdJpy != null)
			SubscribeCandles(CandleType, UsdJpy).Bind(ProcessUsdJpy).Start();

		if (GbpUsd != null)
			SubscribeCandles(CandleType, GbpUsd).Bind(ProcessGbpUsd).Start();
	}

	private void ProcessEurUsd(ICandleMessage candle) => Process(EurUsd, ref _eurUsdPrev, candle);
	private void ProcessUsdJpy(ICandleMessage candle) => Process(UsdJpy, ref _usdJpyPrev, candle);
	private void ProcessGbpUsd(ICandleMessage candle) => Process(GbpUsd, ref _gbpUsdPrev, candle);

	private void Process(Security security, ref ICandleMessage prev, ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (prev == null)
		{
			prev = candle;
			return;
		}

		var buy = 0;
		var sell = 0;

		void Compare(decimal current, decimal previous)
		{
			if (current > previous)
				buy++;
			else
				sell++;
		}

		Compare(candle.OpenPrice, prev.OpenPrice);
		Compare(candle.HighPrice, prev.HighPrice);
		Compare(candle.LowPrice, prev.LowPrice);
		Compare((candle.HighPrice + candle.LowPrice) / 2m, (prev.HighPrice + prev.LowPrice) / 2m);
		Compare(candle.ClosePrice, prev.ClosePrice);
		Compare((candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			(prev.HighPrice + prev.LowPrice + prev.ClosePrice) / 3m);
		Compare((candle.HighPrice + candle.LowPrice + candle.ClosePrice + candle.ClosePrice) / 4m,
			(prev.HighPrice + prev.LowPrice + prev.ClosePrice + prev.ClosePrice) / 4m);

		if (AutoTrade)
		{
			var pos = GetPosition(security);

			if (buy > sell && pos <= 0)
				BuyMarket(Volume + Math.Abs(pos), security);
			else if (sell > buy && pos >= 0)
				SellMarket(Volume + Math.Abs(pos), security);
		}

		prev = candle;
	}

	private decimal GetPosition(Security security)
	{
		return security == null ? 0 : GetPositionValue(security, Portfolio) ?? 0;
	}
}
