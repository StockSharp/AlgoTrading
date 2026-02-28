using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Breakout strategy with martingale-style recovery.
/// Detects abnormally large candles relative to recent history and enters in the breakout direction.
/// After a stop-loss, enters recovery mode with a wider take-profit target.
/// </summary>
public class MartinGaleBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _requiredHistory;
	private readonly StrategyParam<decimal> _breakoutFactor;
	private readonly StrategyParam<decimal> _takeProfitPct;
	private readonly StrategyParam<decimal> _stopLossPct;
	private readonly StrategyParam<decimal> _recoveryMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _ranges = new();
	private decimal _entryPrice;
	private Sides? _entrySide;
	private bool _recovering;

	public int RequiredHistory
	{
		get => _requiredHistory.Value;
		set => _requiredHistory.Value = value;
	}

	public decimal BreakoutFactor
	{
		get => _breakoutFactor.Value;
		set => _breakoutFactor.Value = value;
	}

	public decimal TakeProfitPct
	{
		get => _takeProfitPct.Value;
		set => _takeProfitPct.Value = value;
	}

	public decimal StopLossPct
	{
		get => _stopLossPct.Value;
		set => _stopLossPct.Value = value;
	}

	public decimal RecoveryMultiplier
	{
		get => _recoveryMultiplier.Value;
		set => _recoveryMultiplier.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public MartinGaleBreakoutStrategy()
	{
		_requiredHistory = Param(nameof(RequiredHistory), 10)
			.SetDisplay("Lookback", "Number of candles for average range", "General");

		_breakoutFactor = Param(nameof(BreakoutFactor), 2.5m)
			.SetDisplay("Breakout Factor", "Multiplier for abnormal candle detection", "General");

		_takeProfitPct = Param(nameof(TakeProfitPct), 0.5m)
			.SetDisplay("TP %", "Take profit percent of entry", "Trading");

		_stopLossPct = Param(nameof(StopLossPct), 0.3m)
			.SetDisplay("SL %", "Stop loss percent of entry", "Trading");

		_recoveryMultiplier = Param(nameof(RecoveryMultiplier), 1.5m)
			.SetDisplay("Recovery Mult", "TP multiplier during recovery", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle series", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_ranges.Clear();
		_entryPrice = 0;
		_entrySide = null;
		_recovering = false;

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

		var close = candle.ClosePrice;
		var range = candle.HighPrice - candle.LowPrice;

		// Check exit
		if (Position != 0 && _entryPrice > 0)
		{
			var tpPct = _recovering ? TakeProfitPct * RecoveryMultiplier : TakeProfitPct;

			if (_entrySide == Sides.Buy)
			{
				var pnl = (close - _entryPrice) / _entryPrice * 100m;
				if (pnl >= tpPct || pnl <= -StopLossPct)
				{
					var wasLoss = pnl < 0;
					SellMarket();
					_entryPrice = 0;
					_entrySide = null;
					_recovering = wasLoss;
					AddRange(range);
					return;
				}
			}
			else if (_entrySide == Sides.Sell)
			{
				var pnl = (_entryPrice - close) / _entryPrice * 100m;
				if (pnl >= tpPct || pnl <= -StopLossPct)
				{
					var wasLoss = pnl < 0;
					BuyMarket();
					_entryPrice = 0;
					_entrySide = null;
					_recovering = wasLoss;
					AddRange(range);
					return;
				}
			}
		}

		// Entry - only when flat
		if (Position == 0 && _ranges.Count >= RequiredHistory)
		{
			decimal sum = 0;
			for (int i = 0; i < _ranges.Count; i++)
				sum += _ranges[i];
			var avgRange = sum / _ranges.Count;

			if (avgRange > 0 && range > avgRange * BreakoutFactor)
			{
				var body = candle.ClosePrice - candle.OpenPrice;

				if (body > 0 && body > range * 0.4m)
				{
					BuyMarket();
					_entryPrice = close;
					_entrySide = Sides.Buy;
				}
				else if (body < 0 && Math.Abs(body) > range * 0.4m)
				{
					SellMarket();
					_entryPrice = close;
					_entrySide = Sides.Sell;
				}
			}
		}

		AddRange(range);
	}

	private void AddRange(decimal range)
	{
		_ranges.Add(range);
		while (_ranges.Count > RequiredHistory)
			_ranges.RemoveAt(0);
	}
}
