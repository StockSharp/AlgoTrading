using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi-timeframe breakout strategy converted from the original MetaTrader "A System" expert advisor.
/// Enters on momentum breakouts when close is above/below the midpoint of the previous candle range.
/// Uses ATR-based trailing stop for position management.
/// </summary>
public class ASystemChampionshipStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _takeFactor;
	private readonly StrategyParam<decimal> _trailFactor;

	private decimal _prevHigh;
	private decimal _prevLow;
	private decimal _prevClose;
	private bool _hasPrev;
	private decimal _entryPrice;
	private decimal _stopPrice;

	public ASystemChampionshipStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for breakout detection.", "General");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetDisplay("ATR Period", "Period for ATR used in trailing stop.", "Indicators");

		_takeFactor = Param(nameof(TakeFactor), 2.5m)
			.SetDisplay("Take Factor", "ATR multiplier for take profit.", "Risk");

		_trailFactor = Param(nameof(TrailFactor), 1.5m)
			.SetDisplay("Trail Factor", "ATR multiplier for trailing stop.", "Risk");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	public decimal TakeFactor
	{
		get => _takeFactor.Value;
		set => _takeFactor.Value = value;
	}

	public decimal TrailFactor
	{
		get => _trailFactor.Value;
		set => _trailFactor.Value = value;
	}

	/// <inheritdoc />
	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevHigh = 0;
		_prevLow = 0;
		_prevClose = 0;
		_hasPrev = false;
		_entryPrice = 0;
		_stopPrice = 0;
	}

		protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevHigh = 0;
		_prevLow = 0;
		_prevClose = 0;
		_hasPrev = false;
		_entryPrice = 0;
		_stopPrice = 0;

		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, atr);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (atrValue <= 0)
		{
			SaveCandle(candle);
			return;
		}

		// Manage open position
		if (Position > 0)
		{
			// Trail stop up
			var newStop = candle.ClosePrice - atrValue * TrailFactor;
			if (newStop > _stopPrice)
				_stopPrice = newStop;

			if (candle.LowPrice <= _stopPrice)
			{
				SellMarket();
				ResetPosition();
			}
			else if (_entryPrice > 0 && candle.HighPrice >= _entryPrice + atrValue * TakeFactor)
			{
				SellMarket();
				ResetPosition();
			}
		}
		else if (Position < 0)
		{
			// Trail stop down
			var newStop = candle.ClosePrice + atrValue * TrailFactor;
			if (_stopPrice == 0 || newStop < _stopPrice)
				_stopPrice = newStop;

			if (candle.HighPrice >= _stopPrice)
			{
				BuyMarket();
				ResetPosition();
			}
			else if (_entryPrice > 0 && candle.LowPrice <= _entryPrice - atrValue * TakeFactor)
			{
				BuyMarket();
				ResetPosition();
			}
		}

		// Entry logic
		if (_hasPrev && Position == 0)
		{
			var mid = (_prevHigh + _prevLow) / 2m;

			if (_prevClose > mid && candle.ClosePrice > _prevHigh)
			{
				// Bullish breakout
				BuyMarket();
				_entryPrice = candle.ClosePrice;
				_stopPrice = candle.ClosePrice - atrValue * TrailFactor;
			}
			else if (_prevClose < mid && candle.ClosePrice < _prevLow)
			{
				// Bearish breakout
				SellMarket();
				_entryPrice = candle.ClosePrice;
				_stopPrice = candle.ClosePrice + atrValue * TrailFactor;
			}
		}

		SaveCandle(candle);
	}

	private void SaveCandle(ICandleMessage candle)
	{
		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;
		_prevClose = candle.ClosePrice;
		_hasPrev = true;
	}

	private void ResetPosition()
	{
		_entryPrice = 0;
		_stopPrice = 0;
	}
}
