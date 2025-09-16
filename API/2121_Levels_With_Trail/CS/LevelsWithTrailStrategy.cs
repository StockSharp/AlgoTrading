namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Strategy trading price level breakouts with optional trailing stop loss.
/// </summary>
public class LevelsWithTrailStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _levelPrice;
	private readonly StrategyParam<TrailMode> _trail;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal _currentStop;

	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	public decimal LevelPrice
	{
		get => _levelPrice.Value;
		set => _levelPrice.Value = value;
	}

	public TrailMode Trail
	{
		get => _trail.Value;
		set => _trail.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public LevelsWithTrailStrategy()
	{
		_stopLoss = Param(nameof(StopLoss), 300m)
			.SetDisplay("Stop Loss", "Stop loss size in price units", "Risk");

		_takeProfit = Param(nameof(TakeProfit), 900m)
			.SetDisplay("Take Profit", "Take profit size in price units", "Risk");

		_levelPrice = Param(nameof(LevelPrice), 0m)
			.SetDisplay("Level Price", "Price level for breakout", "Parameters");

		_trail = Param(nameof(Trail), TrailMode.Off)
			.SetDisplay("Trail Stop", "Enable trailing stop", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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

		_entryPrice = default;
		_currentStop = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;

		if (Position > 0)
		{
			if (Trail == TrailMode.On)
			{
				var newStop = close - StopLoss;
				if (newStop > _currentStop)
					_currentStop = newStop;
			}

			if (close <= _currentStop || close >= _entryPrice + TakeProfit)
				ClosePosition();
		}
		else if (Position < 0)
		{
			if (Trail == TrailMode.On)
			{
				var newStop = close + StopLoss;
				if (newStop < _currentStop)
					_currentStop = newStop;
			}

			if (close >= _currentStop || close <= _entryPrice - TakeProfit)
				ClosePosition();
		}
		else
		{
			if (close > LevelPrice)
			{
				BuyMarket();
				_entryPrice = close;
				_currentStop = close - StopLoss;
			}
			else if (close < LevelPrice)
			{
				SellMarket();
				_entryPrice = close;
				_currentStop = close + StopLoss;
			}
		}
	}

	public enum TrailMode
	{
		On,
		Off
	}
}
