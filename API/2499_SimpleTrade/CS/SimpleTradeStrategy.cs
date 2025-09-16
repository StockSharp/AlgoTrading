using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simple trade strategy converted from MetaTrader that compares the current open with the open three bars ago.
/// The logic holds positions for a single bar and protects the trade with a fixed stop distance.
/// </summary>
public class SimpleTradeStrategy : Strategy
{
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _openCurrent;
	private decimal _openMinus1;
	private decimal _openMinus2;
	private decimal _openMinus3;
	private int _historyCount;
	private decimal? _stopPrice;

	/// <summary>
	/// Stop loss distance expressed in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Trade volume used for market orders.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Candle type processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="SimpleTradeStrategy"/> parameters.
	/// </summary>
	public SimpleTradeStrategy()
	{
		_stopLossPips = Param(nameof(StopLossPips), 120)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss (pips)", "Fixed protective distance in pips", "Risk Management")
			.SetCanOptimize();

		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume in lots", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary candle source for decisions", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_openCurrent = 0m;
		_openMinus1 = 0m;
		_openMinus2 = 0m;
		_openMinus3 = 0m;
		_historyCount = 0;
		_stopPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

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

		// Exit existing trades before evaluating new entries to mimic the original MQL behaviour.
		if (TryCloseExistingPosition(candle))
			return;

		UpdateHistory(candle.OpenPrice);

		// Need at least four opens to compare with the value three bars ago.
		if (_historyCount < 4)
			return;

		ExecuteEntry(candle);
	}

	private bool TryCloseExistingPosition(ICandleMessage candle)
	{
		if (Position > 0)
		{
			var volume = Position;

			// Close long trades at the protective stop or at the bar change.
			if (_stopPrice is decimal stop && candle.LowPrice <= stop)
			{
				SellMarket(volume);
			}
			else
			{
				ClosePosition();
			}

			_stopPrice = null;
			return true;
		}

		if (Position < 0)
		{
			var volume = Math.Abs(Position);

			// Close short trades at the protective stop or at the bar change.
			if (_stopPrice is decimal stop && candle.HighPrice >= stop)
			{
				BuyMarket(volume);
			}
			else
			{
				ClosePosition();
			}

			_stopPrice = null;
			return true;
		}

		return false;
	}

	private void UpdateHistory(decimal currentOpen)
	{
		// Shift the stored opens so that _openMinus3 keeps the value three candles back.
		_openMinus3 = _openMinus2;
		_openMinus2 = _openMinus1;
		_openMinus1 = _openCurrent;
		_openCurrent = currentOpen;

		if (_historyCount < 4)
			_historyCount++;
	}

	private void ExecuteEntry(ICandleMessage candle)
	{
		var pipSize = CalculatePipSize();
		var stopOffset = pipSize * StopLossPips;

		// Enter long when the current open is above the open three bars ago, otherwise enter short.
		if (_openCurrent > _openMinus3)
		{
			BuyMarket(TradeVolume);
			_stopPrice = candle.OpenPrice - stopOffset;
		}
		else
		{
			SellMarket(TradeVolume);
			_stopPrice = candle.OpenPrice + stopOffset;
		}
	}

	private decimal CalculatePipSize()
	{
		// Reproduce the MetaTrader adjustment: multiply by ten for symbols with 3 or 5 decimals.
		var step = Security?.PriceStep ?? 1m;
		var decimals = Security?.Decimals;

		if (decimals == 3 || decimals == 5)
			step *= 10m;

		return step;
	}
}
