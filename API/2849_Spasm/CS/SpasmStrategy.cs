namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Volatility breakout strategy converted from the MetaTrader Spasm expert advisor.
/// Tracks directional swings using adaptive thresholds derived from ATR.
/// Buys when price breaks above recent high + ATR*multiplier, sells when price breaks below recent low - ATR*multiplier.
/// </summary>
public class SpasmStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _volatilityMultiplier;
	private readonly StrategyParam<int> _atrPeriod;

	private decimal _highestPrice;
	private decimal _lowestPrice;
	private bool _initialized;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public decimal VolatilityMultiplier
	{
		get => _volatilityMultiplier.Value;
		set => _volatilityMultiplier.Value = value;
	}

	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	public SpasmStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for analysis", "General");

		_volatilityMultiplier = Param(nameof(VolatilityMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Volatility Multiplier", "Multiplier applied to ATR for breakout bands", "Trading");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "Average True Range period", "Indicators");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_highestPrice = 0;
		_lowestPrice = decimal.MaxValue;
		_initialized = false;

		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(atr, OnProcess)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, atr);
			DrawOwnTrades(area);
		}
	}

	private void OnProcess(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_initialized)
		{
			_highestPrice = candle.HighPrice;
			_lowestPrice = candle.LowPrice;
			_initialized = true;
			return;
		}

		// Update extremes
		if (candle.HighPrice > _highestPrice)
			_highestPrice = candle.HighPrice;
		if (candle.LowPrice < _lowestPrice)
			_lowestPrice = candle.LowPrice;

		var threshold = atrValue * VolatilityMultiplier;

		if (threshold <= 0)
			return;

		// Breakout above lowest + threshold => buy
		if (candle.ClosePrice > _lowestPrice + threshold && Position <= 0)
		{
			BuyMarket();
			// Reset extremes after entry
			_highestPrice = candle.HighPrice;
			_lowestPrice = candle.LowPrice;
		}
		// Breakout below highest - threshold => sell
		else if (candle.ClosePrice < _highestPrice - threshold && Position >= 0)
		{
			SellMarket();
			// Reset extremes after entry
			_highestPrice = candle.HighPrice;
			_lowestPrice = candle.LowPrice;
		}
	}
}
