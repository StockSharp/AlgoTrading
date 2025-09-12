using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Inside candle breakout strategy with risk/reward based exits.
/// Waits for an inside candle and trades breakouts of its range.
/// </summary>
public class InsideCandleStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _riskReward;

	private ICandleMessage _previousCandle;
	private decimal _insideHigh;
	private decimal _insideLow;
	private bool _waitingForBreakout;
	private decimal _stopPrice;
	private decimal _takeProfitPrice;

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Risk/reward ratio.
	/// </summary>
	public decimal RiskReward
	{
		get => _riskReward.Value;
		set => _riskReward.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public InsideCandleStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_riskReward = Param(nameof(RiskReward), 2m)
			.SetDisplay("RR Ratio", "Risk/reward ratio for exits", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 5m, 0.5m);
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_previousCandle = null;
		_waitingForBreakout = false;
		_stopPrice = 0m;
		_takeProfitPrice = 0m;
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

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position > 0 && _stopPrice != 0m && _takeProfitPrice != 0m)
		{
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takeProfitPrice)
			{
				SellMarket(Math.Abs(Position));
				_stopPrice = 0m;
				_takeProfitPrice = 0m;
			}
		}
		else if (Position < 0 && _stopPrice != 0m && _takeProfitPrice != 0m)
		{
			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takeProfitPrice)
			{
				BuyMarket(Math.Abs(Position));
				_stopPrice = 0m;
				_takeProfitPrice = 0m;
			}
		}

		if (_previousCandle != null)
		{
			if (_waitingForBreakout)
			{
				if (candle.ClosePrice > _insideHigh)
				{
					BuyMarket(Volume + Math.Abs(Position));
					var entry = candle.ClosePrice;
					_stopPrice = _insideLow;
					_takeProfitPrice = entry + (entry - _insideLow) * RiskReward;
					_waitingForBreakout = false;
				}
				else if (candle.ClosePrice < _insideLow)
				{
					SellMarket(Volume + Math.Abs(Position));
					var entry = candle.ClosePrice;
					_stopPrice = _insideHigh;
					_takeProfitPrice = entry - (_insideHigh - entry) * RiskReward;
					_waitingForBreakout = false;
				}
				else
				{
					_waitingForBreakout = false;
				}
			}
			else if (candle.HighPrice < _previousCandle.HighPrice && candle.LowPrice > _previousCandle.LowPrice)
			{
				_insideHigh = _previousCandle.HighPrice;
				_insideLow = _previousCandle.LowPrice;
				_waitingForBreakout = true;
			}
		}

		_previousCandle = candle;
	}
}

