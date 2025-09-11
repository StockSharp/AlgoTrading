using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// G-Channel with EMA strategy.
/// </summary>
public class GChannelEmaStrategy : Strategy
{
	private readonly StrategyParam<int> _channelLength;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _a;
	private decimal? _b;
	private decimal _prevClose;
	private bool _bullish;
	private bool _initialized;

	/// <summary>
	/// G-Channel period length.
	/// </summary>
	public int ChannelLength
	{
		get => _channelLength.Value;
		set => _channelLength.Value = value;
	}

	/// <summary>
	/// EMA period length.
	/// </summary>
	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize strategy parameters.
	/// </summary>
	public GChannelEmaStrategy()
	{
		_channelLength = Param(nameof(ChannelLength), 100)
			.SetGreaterThanZero()
			.SetDisplay("G-Channel Length", "G-Channel period length", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(50, 200, 10);

		_emaLength = Param(nameof(EmaLength), 200)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "EMA period length", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(100, 300, 10);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_a = null;
		_b = null;
		_prevClose = default;
		_bullish = false;
		_initialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var ema = new ExponentialMovingAverage { Length = EmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ema, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;

		if (!_initialized)
		{
			_a = close;
			_b = close;
			_prevClose = close;
			_initialized = true;
			return;
		}

		var prevA = _a!.Value;
		var prevB = _b!.Value;

		var newA = Math.Max(close, prevA) - (prevA - prevB) / ChannelLength;
		var newB = Math.Min(close, prevB) + (prevA - prevB) / ChannelLength;

		var crossUp = _prevClose <= prevB && close > newB;
		var crossDn = _prevClose >= prevA && close < newA;

		if (crossDn)
			_bullish = true;
		else if (crossUp)
			_bullish = false;

		var buySignal = _bullish && close < ema;
		var sellSignal = !_bullish && close > ema;

		if (buySignal && Position <= 0)
			BuyMarket();
		else if (sellSignal && Position >= 0)
			SellMarket();

		_prevClose = close;
		_a = newA;
		_b = newB;
	}
}
