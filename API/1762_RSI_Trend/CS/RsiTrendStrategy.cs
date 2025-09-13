using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Relative Strength Index based trend strategy with ATR trailing stop.
/// Enters positions on RSI barrier crossovers and manages risk using ATR trailing stop.
/// </summary>
public class RsiTrendStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiBuyLevel;
	private readonly StrategyParam<decimal> _rsiSellLevel;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiple;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _previousRsi;
	private bool _isRsiInitialized;
	private decimal _stopPrice;

	/// <summary>
	/// RSI period length.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Upper RSI barrier for long entries.
	/// </summary>
	public decimal RsiBuyLevel
	{
		get => _rsiBuyLevel.Value;
		set => _rsiBuyLevel.Value = value;
	}

	/// <summary>
	/// Lower RSI barrier for short entries.
	/// </summary>
	public decimal RsiSellLevel
	{
		get => _rsiSellLevel.Value;
		set => _rsiSellLevel.Value = value;
	}

	/// <summary>
	/// ATR period length used for trailing stop.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Multiplier applied to ATR for trailing stop distance.
	/// </summary>
	public decimal AtrMultiple
	{
		get => _atrMultiple.Value;
		set => _atrMultiple.Value = value;
	}

	/// <summary>
	/// The type of candles used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public RsiTrendStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "Period for RSI calculation", "RSI Settings")
			.SetCanOptimize(true)
			.SetOptimize(7, 21, 7);

		_rsiBuyLevel = Param(nameof(RsiBuyLevel), 73m)
			.SetDisplay("RSI Buy Level", "Upper RSI barrier for long entries", "RSI Settings")
			.SetCanOptimize(true)
			.SetOptimize(60m, 90m, 5m);

		_rsiSellLevel = Param(nameof(RsiSellLevel), 27m)
			.SetDisplay("RSI Sell Level", "Lower RSI barrier for short entries", "RSI Settings")
			.SetCanOptimize(true)
			.SetOptimize(10m, 40m, 5m);

		_atrPeriod = Param(nameof(AtrPeriod), 100)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period for trailing stop", "ATR Settings")
			.SetCanOptimize(true)
			.SetOptimize(50, 150, 25);

		_atrMultiple = Param(nameof(AtrMultiple), 3m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiple", "ATR multiplier for trailing stop", "ATR Settings")
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 1m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for processing", "General");
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

		_previousRsi = 0m;
		_isRsiInitialized = false;
		_stopPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var rsi = new RSI { Length = RsiPeriod };
		var atr = new ATR { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(rsi, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, rsi);
			DrawIndicator(area, atr);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_isRsiInitialized)
		{
			_previousRsi = rsiValue;
			_isRsiInitialized = true;
			return;
		}

		var bullish = rsiValue > RsiBuyLevel && _previousRsi <= RsiBuyLevel;
		var bearish = rsiValue < RsiSellLevel && _previousRsi >= RsiSellLevel;

		if (bullish && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_stopPrice = candle.ClosePrice - atrValue * AtrMultiple;
		}
		else if (bearish && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_stopPrice = candle.ClosePrice + atrValue * AtrMultiple;
		}

		if (Position > 0)
		{
			_stopPrice = Math.Max(_stopPrice, candle.ClosePrice - atrValue * AtrMultiple);
			if (candle.ClosePrice <= _stopPrice)
				SellMarket(Math.Abs(Position));
		}
		else if (Position < 0)
		{
			_stopPrice = Math.Min(_stopPrice, candle.ClosePrice + atrValue * AtrMultiple);
			if (candle.ClosePrice >= _stopPrice)
				BuyMarket(Math.Abs(Position));
		}

		_previousRsi = rsiValue;
	}
}
