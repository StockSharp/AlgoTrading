using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Probe CCI breakout strategy converted from the original MetaTrader 5 expert advisor.
/// Listens for Commodity Channel Index threshold crossovers and enters with market orders.
/// </summary>
public class ProbeStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cciLength;
	private readonly StrategyParam<decimal> _cciChannelLevel;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;

	private decimal? _previousCci;
	private decimal? _entryPrice;
	private decimal? _stopPrice;
	private decimal _pipSize;

	public ProbeStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe used for indicator calculations", "General");

		_cciLength = Param(nameof(CciLength), 60)
			.SetGreaterThanZero()
			.SetDisplay("CCI Length", "Averaging period of the Commodity Channel Index", "Indicators");

		_cciChannelLevel = Param(nameof(CciChannelLevel), 120m)
			.SetGreaterThanZero()
			.SetDisplay("CCI Channel", "Absolute CCI level used as the channel boundary", "Indicators");

		_stopLossPips = Param(nameof(StopLossPips), 50m)
			.SetNotNegative()
			.SetDisplay("Stop Loss (pips)", "Protective stop loss distance expressed in pips", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 5m)
			.SetNotNegative()
			.SetDisplay("Trailing Stop (pips)", "Minimum profit required before trailing activates", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
			.SetNotNegative()
			.SetDisplay("Trailing Step (pips)", "Additional profit required before the stop is moved again", "Risk");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int CciLength
	{
		get => _cciLength.Value;
		set => _cciLength.Value = value;
	}

	public decimal CciChannelLevel
	{
		get => _cciChannelLevel.Value;
		set => _cciChannelLevel.Value = value;
	}

	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_previousCci = null;
		_entryPrice = null;
		_stopPrice = null;
		_pipSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_pipSize = CalculatePipSize();
		_previousCci = null;
		_entryPrice = null;
		_stopPrice = null;

		var cci = new CommodityChannelIndex { Length = CciLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(cci, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, cci);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal cciValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_pipSize <= 0m)
			_pipSize = CalculatePipSize();

		// Manage existing position (stop loss / trailing)
		var exited = ManagePosition(candle);
		if (exited)
		{
			_previousCci = cciValue;
			return;
		}

		// Check for CCI crossover signals
		if (_previousCci.HasValue && Position == 0m)
		{
			var channel = CciChannelLevel;
			var lower = -channel;

			// CCI crosses up from below -channel -> buy signal
			var crossUp = _previousCci.Value < lower && cciValue > lower;
			// CCI crosses down from above +channel -> sell signal
			var crossDown = _previousCci.Value > channel && cciValue < channel;

			if (crossUp)
			{
				BuyMarket();
				_entryPrice = candle.ClosePrice;

				var stopDistance = StopLossPips * _pipSize;
				_stopPrice = stopDistance > 0m ? candle.ClosePrice - stopDistance : null;
			}
			else if (crossDown)
			{
				SellMarket();
				_entryPrice = candle.ClosePrice;

				var stopDistance = StopLossPips * _pipSize;
				_stopPrice = stopDistance > 0m ? candle.ClosePrice + stopDistance : null;
			}
		}

		_previousCci = cciValue;
	}

	private bool ManagePosition(ICandleMessage candle)
	{
		if (Position == 0m)
		{
			_entryPrice = null;
			_stopPrice = null;
			return false;
		}

		var trailingStop = TrailingStopPips * _pipSize;
		var trailingStep = TrailingStepPips * _pipSize;

		if (Position > 0m)
		{
			if (_entryPrice == null)
				_entryPrice = candle.ClosePrice;

			// Check stop loss
			if (_stopPrice.HasValue && candle.LowPrice <= _stopPrice.Value)
			{
				SellMarket();
				ResetTradeState();
				return true;
			}

			// Trailing stop logic
			if (TrailingStopPips > 0m && trailingStop > 0m && _entryPrice.HasValue)
			{
				var profit = candle.ClosePrice - _entryPrice.Value;
				var threshold = trailingStop + trailingStep;

				if (profit > threshold)
				{
					var desiredStop = candle.ClosePrice - trailingStop;
					if (!_stopPrice.HasValue || desiredStop > _stopPrice.Value)
						_stopPrice = desiredStop;
				}
			}
		}
		else if (Position < 0m)
		{
			if (_entryPrice == null)
				_entryPrice = candle.ClosePrice;

			// Check stop loss
			if (_stopPrice.HasValue && candle.HighPrice >= _stopPrice.Value)
			{
				BuyMarket();
				ResetTradeState();
				return true;
			}

			// Trailing stop logic
			if (TrailingStopPips > 0m && trailingStop > 0m && _entryPrice.HasValue)
			{
				var profit = _entryPrice.Value - candle.ClosePrice;
				var threshold = trailingStop + trailingStep;

				if (profit > threshold)
				{
					var desiredStop = candle.ClosePrice + trailingStop;
					if (!_stopPrice.HasValue || desiredStop < _stopPrice.Value)
						_stopPrice = desiredStop;
				}
			}
		}

		return false;
	}

	private void ResetTradeState()
	{
		_entryPrice = null;
		_stopPrice = null;
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			return 0.01m;

		return step;
	}
}
