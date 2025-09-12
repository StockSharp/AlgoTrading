using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Outside Bar strategy.
/// </summary>
public class OutsideBarStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _entryPercentage;
	private readonly StrategyParam<decimal> _tpPercentage;
	private readonly StrategyParam<decimal> _partialRR;
	private readonly StrategyParam<decimal> _partialExitPercent;
	private readonly StrategyParam<int> _stopLossOffset;
	private readonly StrategyParam<bool> _usePartialExit;
	private readonly StrategyParam<bool> _enableBreakeven;

	private decimal _entryPrice;
	private decimal _stopLoss;
	private decimal _takeProfit;
	private decimal _partialTp;
	private bool _partialHit;
	private ICandleMessage _prevCandle;

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Entry level percentage of bar size.
	/// </summary>
	public decimal EntryPercentage
	{
		get => _entryPercentage.Value;
		set => _entryPercentage.Value = value;
	}

	/// <summary>
	/// Take profit percentage of bar move.
	/// </summary>
	public decimal TpPercentage
	{
		get => _tpPercentage.Value;
		set => _tpPercentage.Value = value;
	}

	/// <summary>
	/// Risk/reward ratio for partial exit.
	/// </summary>
	public decimal PartialRR
	{
		get => _partialRR.Value;
		set => _partialRR.Value = value;
	}

	/// <summary>
	/// Volume percent to exit at partial target.
	/// </summary>
	public decimal PartialExitPercent
	{
		get => _partialExitPercent.Value;
		set => _partialExitPercent.Value = value;
	}

	/// <summary>
	/// Stop loss offset in ticks.
	/// </summary>
	public int StopLossOffset
	{
		get => _stopLossOffset.Value;
		set => _stopLossOffset.Value = value;
	}

	/// <summary>
	/// Enable partial take profit.
	/// </summary>
	public bool UsePartialExit
	{
		get => _usePartialExit.Value;
		set => _usePartialExit.Value = value;
	}

	/// <summary>
	/// Move stop to breakeven after partial exit.
	/// </summary>
	public bool EnableBreakeven
	{
		get => _enableBreakeven.Value;
		set => _enableBreakeven.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="OutsideBarStrategy"/>.
	/// </summary>
	public OutsideBarStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_entryPercentage = Param(nameof(EntryPercentage), 0.5m)
			.SetGreaterThanZero()
			.SetLessOrEqual(1m)
			.SetDisplay("Entry %", "Entry level as % of bar size", "Trading");

		_tpPercentage = Param(nameof(TpPercentage), 1m)
			.SetGreaterThanZero()
			.SetDisplay("TP %", "Take profit as % of bar move", "Trading");

		_partialRR = Param(nameof(PartialRR), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Partial RR", "Risk/reward for partial target", "Trading");

		_partialExitPercent = Param(nameof(PartialExitPercent), 0.5m)
			.SetGreaterThanZero()
			.SetLessOrEqual(1m)
			.SetDisplay("Partial Exit %", "Volume percent to exit", "Risk");

		_stopLossOffset = Param(nameof(StopLossOffset), 10)
			.SetNotNegative()
			.SetDisplay("SL Offset", "Stop loss offset in ticks", "Risk");

		_usePartialExit = Param(nameof(UsePartialExit), true)
			.SetDisplay("Use Partial Exit", "Enable partial take profit", "Risk");

		_enableBreakeven = Param(nameof(EnableBreakeven), true)
			.SetDisplay("Enable Breakeven", "Move stop to entry after partial TP", "Risk");
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

		_prevCandle = null;
		_partialHit = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

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

		if (_prevCandle is null)
		{
			_prevCandle = candle;
			return;
		}

		bool isOutside = candle.HighPrice > _prevCandle.HighPrice && candle.LowPrice < _prevCandle.LowPrice;
		bool isBullish = isOutside && candle.ClosePrice > candle.OpenPrice;
		bool isBearish = isOutside && candle.ClosePrice < candle.OpenPrice;

		if (Position == 0)
		{
			CancelActiveOrders();
			_partialHit = false;

			if (isBullish)
			{
				var barSize = candle.HighPrice - candle.LowPrice;
				_entryPrice = candle.LowPrice + barSize * EntryPercentage;
				_stopLoss = candle.LowPrice - Security.PriceStep * StopLossOffset;
				_takeProfit = candle.HighPrice + barSize * TpPercentage;
				_partialTp = _entryPrice + (_entryPrice - _stopLoss) * PartialRR;

				BuyStop(Volume, _entryPrice);
			}
			else if (isBearish)
			{
				var barSize = candle.HighPrice - candle.LowPrice;
				_entryPrice = candle.HighPrice - barSize * EntryPercentage;
				_stopLoss = candle.HighPrice + Security.PriceStep * StopLossOffset;
				_takeProfit = candle.LowPrice - barSize * TpPercentage;
				_partialTp = _entryPrice - (_stopLoss - _entryPrice) * PartialRR;

				SellStop(Volume, _entryPrice);
			}
		}
		else if (Position > 0)
		{
			if (UsePartialExit && !_partialHit && candle.HighPrice >= _partialTp)
			{
				SellMarket(Position * PartialExitPercent);
				_partialHit = true;
				if (EnableBreakeven)
				_stopLoss = _entryPrice;
			}

			if (candle.LowPrice <= _stopLoss)
			{
				SellMarket(Position);
			}
			else if ((!UsePartialExit || _partialHit) && candle.HighPrice >= _takeProfit)
			{
				SellMarket(Position);
			}
		}
		else if (Position < 0)
		{
			if (UsePartialExit && !_partialHit && candle.LowPrice <= _partialTp)
			{
				BuyMarket(-Position * PartialExitPercent);
				_partialHit = true;
				if (EnableBreakeven)
				_stopLoss = _entryPrice;
			}

			if (candle.HighPrice >= _stopLoss)
			{
				BuyMarket(-Position);
			}
			else if ((!UsePartialExit || _partialHit) && candle.LowPrice <= _takeProfit)
			{
				BuyMarket(-Position);
			}
		}
	}
	_prevCandle = candle;
}
}
