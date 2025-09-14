using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simple Parabolic SAR based strategy with optional stop-loss,
/// take-profit and trailing stop management.
/// </summary>
public class SarAutomatedStrategy : Strategy
{
	private readonly StrategyParam<decimal> _sarStep;
	private readonly StrategyParam<decimal> _sarMax;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _trailingStop;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal _highestPrice;
	private decimal _lowestPrice;

	/// <summary>
	/// Parabolic SAR acceleration step.
	/// </summary>
	public decimal SarStep
	{
		get => _sarStep.Value;
		set => _sarStep.Value = value;
	}

	/// <summary>
	/// Parabolic SAR maximum acceleration.
	/// </summary>
	public decimal SarMax
	{
		get => _sarMax.Value;
		set => _sarMax.Value = value;
	}

	/// <summary>
	/// Stop loss in price units.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take profit in price units.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Trailing stop in price units.
	/// </summary>
	public decimal TrailingStop
	{
		get => _trailingStop.Value;
		set => _trailingStop.Value = value;
	}

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="SarAutomatedStrategy"/>.
	/// </summary>
	public SarAutomatedStrategy()
	{
		_sarStep = Param(nameof(SarStep), 0.02m)
			.SetDisplay("SAR Step", "Acceleration factor for SAR", "Indicators");

		_sarMax = Param(nameof(SarMax), 0.2m)
			.SetDisplay("SAR Max", "Maximum acceleration for SAR", "Indicators");

		_stopLoss = Param(nameof(StopLoss), 350m)
			.SetDisplay("Stop Loss", "Stop loss in price units", "Risk Management");

		_takeProfit = Param(nameof(TakeProfit), 650m)
			.SetDisplay("Take Profit", "Take profit in price units", "Risk Management");

		_trailingStop = Param(nameof(TrailingStop), 80m)
			.SetDisplay("Trailing Stop", "Trailing stop in price units", "Risk Management");

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

		_entryPrice = 0m;
		_highestPrice = 0m;
		_lowestPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var sar = new ParabolicSar
		{
			Acceleration = SarStep,
			AccelerationMax = SarMax
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sar, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sar);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal sarValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position == 0)
		{
			if (sarValue < candle.ClosePrice)
			{
				BuyMarket();
				_entryPrice = candle.ClosePrice;
				_highestPrice = candle.ClosePrice;
				_lowestPrice = candle.ClosePrice;
			}
			else if (sarValue > candle.ClosePrice)
			{
				SellMarket();
				_entryPrice = candle.ClosePrice;
				_highestPrice = candle.ClosePrice;
				_lowestPrice = candle.ClosePrice;
			}
		}
		else if (Position > 0)
		{
			_highestPrice = Math.Max(_highestPrice, candle.HighPrice);

			if (candle.HighPrice - _entryPrice >= TakeProfit)
			{
				SellMarket(Math.Abs(Position));
				return;
			}

			if (_entryPrice - candle.LowPrice >= StopLoss)
			{
				SellMarket(Math.Abs(Position));
				return;
			}

			if (TrailingStop > 0m && _highestPrice - candle.ClosePrice >= TrailingStop)
			{
				SellMarket(Math.Abs(Position));
				return;
			}

			if (sarValue > candle.ClosePrice)
			{
				SellMarket(Math.Abs(Position));
			}
		}
		else if (Position < 0)
		{
			_lowestPrice = Math.Min(_lowestPrice, candle.LowPrice);

			if (_entryPrice - candle.LowPrice >= TakeProfit)
			{
				BuyMarket(Math.Abs(Position));
				return;
			}

			if (candle.HighPrice - _entryPrice >= StopLoss)
			{
				BuyMarket(Math.Abs(Position));
				return;
			}

			if (TrailingStop > 0m && candle.ClosePrice - _lowestPrice >= TrailingStop)
			{
				BuyMarket(Math.Abs(Position));
				return;
			}

			if (sarValue < candle.ClosePrice)
			{
				BuyMarket(Math.Abs(Position));
			}
		}
	}
}
