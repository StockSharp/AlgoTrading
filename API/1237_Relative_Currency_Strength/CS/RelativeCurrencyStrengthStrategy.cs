using System;
using System.Collections.Generic;
using Ecng.ComponentModel;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades based on relative currency strength among major pairs.
/// It buys when the traded pair outperforms the average of other majors and sells when it underperforms.
/// </summary>
public class RelativeCurrencyStrengthStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _threshold;
	private readonly StrategyParam<Security> _audUsd;
	private readonly StrategyParam<Security> _nzdUsd;
	private readonly StrategyParam<Security> _usdJpy;
	private readonly StrategyParam<Security> _usdChf;
	private readonly StrategyParam<Security> _usdCad;
	private readonly StrategyParam<Security> _gbpUsd;
	private readonly StrategyParam<Security> _eurUsd;
	private readonly StrategyParam<Security> _xauUsd;

	private decimal? _mainStart;
	private decimal _mainStrength;

	private decimal? _audUsdStart;
	private decimal _audUsdStrength;

	private decimal? _nzdUsdStart;
	private decimal _nzdUsdStrength;

	private decimal? _usdJpyStart;
	private decimal _usdJpyStrength;

	private decimal? _usdChfStart;
	private decimal _usdChfStrength;

	private decimal? _usdCadStart;
	private decimal _usdCadStrength;

	private decimal? _gbpUsdStart;
	private decimal _gbpUsdStrength;

	private decimal? _eurUsdStart;
	private decimal _eurUsdStrength;

	private decimal? _xauUsdStart;
	private decimal _xauUsdStrength;

	/// <summary>
	/// Type of candles used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Strength difference needed for entry signal.
	/// </summary>
	public decimal Threshold
	{
		get => _threshold.Value;
		set => _threshold.Value = value;
	}

	/// <summary>
	/// AUD/USD security.
	/// </summary>
	public Security AudUsd
	{
		get => _audUsd.Value;
		set => _audUsd.Value = value;
	}

	/// <summary>
	/// NZD/USD security.
	/// </summary>
	public Security NzdUsd
	{
		get => _nzdUsd.Value;
		set => _nzdUsd.Value = value;
	}

	/// <summary>
	/// USD/JPY security.
	/// </summary>
	public Security UsdJpy
	{
		get => _usdJpy.Value;
		set => _usdJpy.Value = value;
	}

	/// <summary>
	/// USD/CHF security.
	/// </summary>
	public Security UsdChf
	{
		get => _usdChf.Value;
		set => _usdChf.Value = value;
	}

	/// <summary>
	/// USD/CAD security.
	/// </summary>
	public Security UsdCad
	{
		get => _usdCad.Value;
		set => _usdCad.Value = value;
	}

	/// <summary>
	/// GBP/USD security.
	/// </summary>
	public Security GbpUsd
	{
		get => _gbpUsd.Value;
		set => _gbpUsd.Value = value;
	}

	/// <summary>
	/// EUR/USD security.
	/// </summary>
	public Security EurUsd
	{
		get => _eurUsd.Value;
		set => _eurUsd.Value = value;
	}

	/// <summary>
	/// XAU/USD security.
	/// </summary>
	public Security XauUsd
	{
		get => _xauUsd.Value;
		set => _xauUsd.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public RelativeCurrencyStrengthStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "Data");

		_threshold = Param(nameof(Threshold), 0.01m)
			.SetDisplay("Threshold", "Strength difference for signal", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(0.005m, 0.05m, 0.005m);

		_audUsd = Param<Security>(nameof(AudUsd))
			.SetDisplay("AUDUSD", "AUD/USD pair", "Data")
			.SetRequired();

		_nzdUsd = Param<Security>(nameof(NzdUsd))
			.SetDisplay("NZDUSD", "NZD/USD pair", "Data")
			.SetRequired();

		_usdJpy = Param<Security>(nameof(UsdJpy))
			.SetDisplay("USDJPY", "USD/JPY pair", "Data")
			.SetRequired();

		_usdChf = Param<Security>(nameof(UsdChf))
			.SetDisplay("USDCHF", "USD/CHF pair", "Data")
			.SetRequired();

		_usdCad = Param<Security>(nameof(UsdCad))
			.SetDisplay("USDCAD", "USD/CAD pair", "Data")
			.SetRequired();

		_gbpUsd = Param<Security>(nameof(GbpUsd))
			.SetDisplay("GBPUSD", "GBP/USD pair", "Data")
			.SetRequired();

		_eurUsd = Param<Security>(nameof(EurUsd))
			.SetDisplay("EURUSD", "EUR/USD pair", "Data")
			.SetRequired();

		_xauUsd = Param<Security>(nameof(XauUsd))
			.SetDisplay("XAUUSD", "Gold pair", "Data")
			.SetRequired();
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return
		[
			(Security, CandleType),
			(AudUsd, CandleType),
			(NzdUsd, CandleType),
			(UsdJpy, CandleType),
			(UsdChf, CandleType),
			(UsdCad, CandleType),
			(GbpUsd, CandleType),
			(EurUsd, CandleType),
			(XauUsd, CandleType)
		];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		SubscribeCandles(CandleType)
			.Bind(ProcessMain)
			.Start();

		SubscribeCandles(CandleType, security: AudUsd)
			.Bind(ProcessAudUsd)
			.Start();

		SubscribeCandles(CandleType, security: NzdUsd)
			.Bind(ProcessNzdUsd)
			.Start();

		SubscribeCandles(CandleType, security: UsdJpy)
			.Bind(ProcessUsdJpy)
			.Start();

		SubscribeCandles(CandleType, security: UsdChf)
			.Bind(ProcessUsdChf)
			.Start();

		SubscribeCandles(CandleType, security: UsdCad)
			.Bind(ProcessUsdCad)
			.Start();

		SubscribeCandles(CandleType, security: GbpUsd)
			.Bind(ProcessGbpUsd)
			.Start();

		SubscribeCandles(CandleType, security: EurUsd)
			.Bind(ProcessEurUsd)
			.Start();

		SubscribeCandles(CandleType, security: XauUsd)
			.Bind(ProcessXauUsd)
			.Start();
	}

	private void ProcessAudUsd(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_audUsdStart is null)
		{
			_audUsdStart = candle.ClosePrice;
			return;
		}

		_audUsdStrength = candle.ClosePrice / _audUsdStart.Value - 1m;
	}

	private void ProcessNzdUsd(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_nzdUsdStart is null)
		{
			_nzdUsdStart = candle.ClosePrice;
			return;
		}

		_nzdUsdStrength = candle.ClosePrice / _nzdUsdStart.Value - 1m;
	}

	private void ProcessUsdJpy(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_usdJpyStart is null)
		{
			_usdJpyStart = candle.ClosePrice;
			return;
		}

		_usdJpyStrength = candle.ClosePrice / _usdJpyStart.Value - 1m;
	}

	private void ProcessUsdChf(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_usdChfStart is null)
		{
			_usdChfStart = candle.ClosePrice;
			return;
		}

		_usdChfStrength = candle.ClosePrice / _usdChfStart.Value - 1m;
	}

	private void ProcessUsdCad(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_usdCadStart is null)
		{
			_usdCadStart = candle.ClosePrice;
			return;
		}

		_usdCadStrength = candle.ClosePrice / _usdCadStart.Value - 1m;
	}

	private void ProcessGbpUsd(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_gbpUsdStart is null)
		{
			_gbpUsdStart = candle.ClosePrice;
			return;
		}

		_gbpUsdStrength = candle.ClosePrice / _gbpUsdStart.Value - 1m;
	}

	private void ProcessEurUsd(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_eurUsdStart is null)
		{
			_eurUsdStart = candle.ClosePrice;
			return;
		}

		_eurUsdStrength = candle.ClosePrice / _eurUsdStart.Value - 1m;
	}

	private void ProcessXauUsd(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_xauUsdStart is null)
		{
			_xauUsdStart = candle.ClosePrice;
			return;
		}

		_xauUsdStrength = candle.ClosePrice / _xauUsdStart.Value - 1m;
	}

	private void ProcessMain(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_mainStart is null)
		{
			_mainStart = candle.ClosePrice;
			return;
		}

		_mainStrength = candle.ClosePrice / _mainStart.Value - 1m;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_audUsdStart is null || _nzdUsdStart is null || _usdJpyStart is null || _usdChfStart is null || _usdCadStart is null || _gbpUsdStart is null || _eurUsdStart is null || _xauUsdStart is null)
			return;

		var sum = _audUsdStrength + _nzdUsdStrength + _usdJpyStrength + _usdChfStrength + _usdCadStrength + _gbpUsdStrength + _eurUsdStrength + _xauUsdStrength;
		var average = sum / 8m;

		if (Position <= 0 && _mainStrength - average > Threshold)
			BuyMarket(Volume);
		else if (Position >= 0 && average - _mainStrength > Threshold)
			SellMarket(Volume);
	}
}
