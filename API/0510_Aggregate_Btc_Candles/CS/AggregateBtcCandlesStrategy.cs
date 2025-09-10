using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Builds synthetic BTC candles by averaging data from multiple exchanges.
/// </summary>
public class AggregateBtcCandlesStrategy : Strategy
{
	private readonly StrategyParam<Security> _coinbaseParam;
	private readonly StrategyParam<Security> _bitfinexParam;
	private readonly StrategyParam<Security> _binanceParam;
	private readonly StrategyParam<DataType> _candleTypeParam;

	private decimal _baseOpen;
	private decimal _baseHigh;
	private decimal _baseLow;
	private decimal _baseClose;

	private decimal _coinbaseOpen;
	private decimal _coinbaseHigh;
	private decimal _coinbaseLow;
	private decimal _coinbaseClose;

	private decimal _bitfinexOpen;
	private decimal _bitfinexHigh;
	private decimal _bitfinexLow;
	private decimal _bitfinexClose;

	private decimal _binanceOpen;
	private decimal _binanceHigh;
	private decimal _binanceLow;
	private decimal _binanceClose;

	/// <summary>
	/// Secondary security from Coinbase exchange.
	/// </summary>
	public Security Coinbase
	{
		get => _coinbaseParam.Value;
		set => _coinbaseParam.Value = value;
	}

	/// <summary>
	/// Secondary security from Bitfinex exchange.
	/// </summary>
	public Security Bitfinex
	{
		get => _bitfinexParam.Value;
		set => _bitfinexParam.Value = value;
	}

	/// <summary>
	/// Secondary security from Binance exchange.
	/// </summary>
	public Security Binance
	{
		get => _binanceParam.Value;
		set => _binanceParam.Value = value;
	}

	/// <summary>
	/// Candle type for subscriptions.
	/// </summary>
	public DataType CandleType
	{
		get => _candleTypeParam.Value;
		set => _candleTypeParam.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="AggregateBtcCandlesStrategy"/>.
	/// </summary>
	public AggregateBtcCandlesStrategy()
	{
		_coinbaseParam = Param<Security>(nameof(Coinbase))
			.SetDisplay("Coinbase", "Coinbase BTC/USD", "Parameters");

		_bitfinexParam = Param<Security>(nameof(Bitfinex))
			.SetDisplay("Bitfinex", "Bitfinex BTC/USD", "Parameters");

		_binanceParam = Param<Security>(nameof(Binance))
			.SetDisplay("Binance", "Binance BTC/USDT", "Parameters");

		_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "Common");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security != null)
			yield return (Security, CandleType);
		if (Coinbase != null)
			yield return (Coinbase, CandleType);
		if (Bitfinex != null)
			yield return (Bitfinex, CandleType);
		if (Binance != null)
			yield return (Binance, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_baseOpen = _baseHigh = _baseLow = _baseClose = 0m;
		_coinbaseOpen = _coinbaseHigh = _coinbaseLow = _coinbaseClose = 0m;
		_bitfinexOpen = _bitfinexHigh = _bitfinexLow = _bitfinexClose = 0m;
		_binanceOpen = _binanceHigh = _binanceLow = _binanceClose = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		SubscribeCandles(CandleType)
			.Bind(ProcessBase)
			.Start();

		if (Coinbase != null)
		{
			SubscribeCandles(CandleType, security: Coinbase)
				.Bind(c => ProcessExternal(c, ref _coinbaseOpen, ref _coinbaseHigh, ref _coinbaseLow, ref _coinbaseClose))
				.Start();
		}

		if (Bitfinex != null)
		{
			SubscribeCandles(CandleType, security: Bitfinex)
				.Bind(c => ProcessExternal(c, ref _bitfinexOpen, ref _bitfinexHigh, ref _bitfinexLow, ref _bitfinexClose))
				.Start();
		}

		if (Binance != null)
		{
			SubscribeCandles(CandleType, security: Binance)
				.Bind(c => ProcessExternal(c, ref _binanceOpen, ref _binanceHigh, ref _binanceLow, ref _binanceClose))
				.Start();
		}
	}

	private void ProcessBase(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_baseOpen = candle.OpenPrice;
		_baseHigh = candle.HighPrice;
		_baseLow = candle.LowPrice;
		_baseClose = candle.ClosePrice;

		PublishAggregate();
	}

	private void ProcessExternal(ICandleMessage candle, ref decimal open, ref decimal high, ref decimal low, ref decimal close)
	{
		if (candle.State != CandleStates.Finished)
		return;

		open = candle.OpenPrice;
		high = candle.HighPrice;
		low = candle.LowPrice;
		close = candle.ClosePrice;

		PublishAggregate();
	}

	private void PublishAggregate()
	{
		var openSum = 0m;
		var highSum = 0m;
		var lowSum = 0m;
		var closeSum = 0m;
		var count = 0;

		if (_baseOpen != 0m)
		{
			openSum += _baseOpen;
			highSum += _baseHigh;
			lowSum += _baseLow;
			closeSum += _baseClose;
			count++;
		}

		if (_coinbaseOpen != 0m)
		{
			openSum += _coinbaseOpen;
			highSum += _coinbaseHigh;
			lowSum += _coinbaseLow;
			closeSum += _coinbaseClose;
			count++;
		}

		if (_bitfinexOpen != 0m)
		{
			openSum += _bitfinexOpen;
			highSum += _bitfinexHigh;
			lowSum += _bitfinexLow;
			closeSum += _bitfinexClose;
			count++;
		}

		if (_binanceOpen != 0m)
		{
			openSum += _binanceOpen;
			highSum += _binanceHigh;
			lowSum += _binanceLow;
			closeSum += _binanceClose;
			count++;
		}

		if (count == 0)
		return;

		var open = openSum / count;
		var high = highSum / count;
		var low = lowSum / count;
		var close = closeSum / count;

		AddInfoLog($"Aggregated O:{open} H:{high} L:{low} C:{close}");
	}
}
