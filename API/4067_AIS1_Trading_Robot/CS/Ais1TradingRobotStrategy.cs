using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// AIS1 breakout strategy with ATR-based stops and trailing.
/// Trades breakouts above/below EMA with ATR-based risk management.
/// </summary>
public class Ais1TradingRobotStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _takeFactor;
	private readonly StrategyParam<decimal> _stopFactor;

	private decimal _entryPrice;

	public Ais1TradingRobotStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for analysis.", "General");

		_emaLength = Param(nameof(EmaLength), 50)
			.SetDisplay("EMA Length", "Period for trend EMA.", "Indicators");

		_atrLength = Param(nameof(AtrLength), 20)
			.SetDisplay("ATR Length", "Period for ATR.", "Indicators");

		_takeFactor = Param(nameof(TakeFactor), 3.0m)
			.SetDisplay("Take Factor", "ATR multiplier for take profit.", "Risk");

		_stopFactor = Param(nameof(StopFactor), 1.5m)
			.SetDisplay("Stop Factor", "ATR multiplier for stop loss.", "Risk");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	public decimal TakeFactor
	{
		get => _takeFactor.Value;
		set => _takeFactor.Value = value;
	}

	public decimal StopFactor
	{
		get => _stopFactor.Value;
		set => _stopFactor.Value = value;
	}

	/// <inheritdoc />
	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_entryPrice = 0;
	}

		protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_entryPrice = 0;

		var ema = new ExponentialMovingAverage { Length = EmaLength };
		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (atrValue <= 0)
			return;

		var close = candle.ClosePrice;
		var takeDistance = atrValue * TakeFactor;
		var stopDistance = atrValue * StopFactor;

		// Position management with ATR-based stops
		if (Position > 0)
		{
			if (_entryPrice > 0)
			{
				if (close >= _entryPrice + takeDistance || close <= _entryPrice - stopDistance)
				{
					SellMarket();
				}
			}
		}
		else if (Position < 0)
		{
			if (_entryPrice > 0)
			{
				if (close <= _entryPrice - takeDistance || close >= _entryPrice + stopDistance)
				{
					BuyMarket();
				}
			}
		}

		// Entry: breakout above/below EMA
		if (Position == 0)
		{
			if (close > emaValue + atrValue * 1.5m)
			{
				_entryPrice = close;
				BuyMarket();
			}
			else if (close < emaValue - atrValue * 1.5m)
			{
				_entryPrice = close;
				SellMarket();
			}
		}
	}
}
