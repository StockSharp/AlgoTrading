namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// PLC (Price Level Channel) strategy.
/// Buys when price breaks above the previous candle's high plus an offset,
/// sells when price breaks below the previous candle's low minus an offset.
/// Uses ATR to dynamically adjust the offset.
/// </summary>
public class PlcStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _shiftPips;
	private readonly StrategyParam<int> _atrPeriod;

	private decimal _prevHigh;
	private decimal _prevLow;
	private bool _initialized;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int ShiftPips
	{
		get => _shiftPips.Value;
		set => _shiftPips.Value = value;
	}

	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	public PlcStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for calculations", "General");

		_shiftPips = Param(nameof(ShiftPips), 15)
			.SetGreaterThanZero()
			.SetDisplay("Shift Pips", "Offset added to candle high/low for breakout", "Trading");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period for volatility measurement", "Indicators");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevHigh = 0;
		_prevLow = 0;
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
			_prevHigh = candle.HighPrice;
			_prevLow = candle.LowPrice;
			_initialized = true;
			return;
		}

		var shift = atrValue * ShiftPips / 100m;
		var buyLevel = _prevHigh + shift;
		var sellLevel = _prevLow - shift;

		// Buy breakout: close above previous high + shift
		if (candle.ClosePrice > buyLevel && Position <= 0)
		{
			BuyMarket();
		}
		// Sell breakout: close below previous low - shift
		else if (candle.ClosePrice < sellLevel && Position >= 0)
		{
			SellMarket();
		}

		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;
	}
}
