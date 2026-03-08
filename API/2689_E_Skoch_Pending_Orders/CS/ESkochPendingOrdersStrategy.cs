using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Pending breakout strategy based on the e-Skoch pending orders idea.
/// Detects falling highs or rising lows across two timeframes to enter on breakouts.
/// </summary>
public class ESkochPendingOrdersStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _takeProfitBuyPips;
	private readonly StrategyParam<decimal> _stopLossBuyPips;
	private readonly StrategyParam<decimal> _takeProfitSellPips;
	private readonly StrategyParam<decimal> _stopLossSellPips;
	private readonly StrategyParam<decimal> _indentHighPips;
	private readonly StrategyParam<decimal> _indentLowPips;
	private readonly StrategyParam<bool> _checkExistingTrade;

	private decimal? _prevHigh1;
	private decimal? _prevHigh2;
	private decimal? _prevLow1;
	private decimal? _prevLow2;

	private decimal? _pendingBuyPrice;
	private decimal? _pendingSellPrice;

	private decimal _entryPrice;
	private decimal _longStop;
	private decimal _longTake;
	private decimal _shortStop;
	private decimal _shortTake;

	private decimal _pipValue;

	/// <summary>
	/// Main candle type for signal evaluation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public decimal TakeProfitBuyPips
	{
		get => _takeProfitBuyPips.Value;
		set => _takeProfitBuyPips.Value = value;
	}

	public decimal StopLossBuyPips
	{
		get => _stopLossBuyPips.Value;
		set => _stopLossBuyPips.Value = value;
	}

	public decimal TakeProfitSellPips
	{
		get => _takeProfitSellPips.Value;
		set => _takeProfitSellPips.Value = value;
	}

	public decimal StopLossSellPips
	{
		get => _stopLossSellPips.Value;
		set => _stopLossSellPips.Value = value;
	}

	public decimal IndentHighPips
	{
		get => _indentHighPips.Value;
		set => _indentHighPips.Value = value;
	}

	public decimal IndentLowPips
	{
		get => _indentLowPips.Value;
		set => _indentLowPips.Value = value;
	}

	public bool CheckExistingTrade
	{
		get => _checkExistingTrade.Value;
		set => _checkExistingTrade.Value = value;
	}

	public ESkochPendingOrdersStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe", "General");

		_takeProfitBuyPips = Param(nameof(TakeProfitBuyPips), 2000m)
			.SetGreaterThanZero()
			.SetDisplay("Buy TP (pips)", "Long take profit distance", "Trading");

		_stopLossBuyPips = Param(nameof(StopLossBuyPips), 500m)
			.SetGreaterThanZero()
			.SetDisplay("Buy SL (pips)", "Long stop loss distance", "Trading");

		_takeProfitSellPips = Param(nameof(TakeProfitSellPips), 2000m)
			.SetGreaterThanZero()
			.SetDisplay("Sell TP (pips)", "Short take profit distance", "Trading");

		_stopLossSellPips = Param(nameof(StopLossSellPips), 500m)
			.SetGreaterThanZero()
			.SetDisplay("Sell SL (pips)", "Short stop loss distance", "Trading");

		_indentHighPips = Param(nameof(IndentHighPips), 500m)
			.SetGreaterThanZero()
			.SetDisplay("High Indent", "Buy stop offset", "Trading");

		_indentLowPips = Param(nameof(IndentLowPips), 500m)
			.SetGreaterThanZero()
			.SetDisplay("Low Indent", "Sell stop offset", "Trading");

		_checkExistingTrade = Param(nameof(CheckExistingTrade), true)
			.SetDisplay("Block During Position", "Skip signals when a position exists", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevHigh1 = null;
		_prevHigh2 = null;
		_prevLow1 = null;
		_prevLow2 = null;
		_pendingBuyPrice = null;
		_pendingSellPrice = null;
		_entryPrice = 0m;
		_longStop = 0m;
		_longTake = 0m;
		_shortStop = 0m;
		_shortTake = 0m;
		_pipValue = 1m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var priceStep = Security?.PriceStep ?? 0m;
		_pipValue = priceStep <= 0m ? 1m : priceStep;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

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

		// Check pending entries against current candle.
		CheckPendingEntries(candle);

		// Manage SL/TP for open positions.
		ManagePosition(candle);

		// Need at least 2 previous bars.
		if (_prevHigh1 is null)
		{
			_prevHigh1 = candle.HighPrice;
			_prevLow1 = candle.LowPrice;
			return;
		}

		if (_prevHigh2 is null)
		{
			_prevHigh2 = _prevHigh1;
			_prevLow2 = _prevLow1;
			_prevHigh1 = candle.HighPrice;
			_prevLow1 = candle.LowPrice;
			return;
		}

		var hasPosition = Position != 0;

		// Falling highs => place buy stop above recent high.
		if (_prevHigh2 > _prevHigh1 && !hasPosition)
		{
			if (!CheckExistingTrade || Position == 0)
			{
				var buyPrice = _prevHigh1.Value + _pipValue * IndentHighPips;
				_pendingBuyPrice = buyPrice;
				_longStop = buyPrice - _pipValue * StopLossBuyPips;
				_longTake = buyPrice + _pipValue * TakeProfitBuyPips;
			}
		}

		// Rising lows => place sell stop below recent low.
		if (_prevLow2 < _prevLow1 && !hasPosition)
		{
			if (!CheckExistingTrade || Position == 0)
			{
				var sellPrice = _prevLow1.Value - _pipValue * IndentLowPips;
				_pendingSellPrice = sellPrice;
				_shortStop = sellPrice + _pipValue * StopLossSellPips;
				_shortTake = sellPrice - _pipValue * TakeProfitSellPips;
			}
		}

		// Shift history.
		_prevHigh2 = _prevHigh1;
		_prevLow2 = _prevLow1;
		_prevHigh1 = candle.HighPrice;
		_prevLow1 = candle.LowPrice;
	}

	private void CheckPendingEntries(ICandleMessage candle)
	{
		if (Position != 0)
			return;

		if (_pendingBuyPrice is decimal buyPrice && candle.HighPrice >= buyPrice)
		{
			BuyMarket();
			_entryPrice = buyPrice;
			_pendingBuyPrice = null;
			_pendingSellPrice = null;
			return;
		}

		if (_pendingSellPrice is decimal sellPrice && candle.LowPrice <= sellPrice)
		{
			SellMarket();
			_entryPrice = sellPrice;
			_pendingBuyPrice = null;
			_pendingSellPrice = null;
		}
	}

	private void ManagePosition(ICandleMessage candle)
	{
		if (Position > 0)
		{
			if (_longStop > 0m && candle.LowPrice <= _longStop)
			{
				SellMarket();
				ResetPositionState();
				return;
			}
			if (_longTake > 0m && candle.HighPrice >= _longTake)
			{
				SellMarket();
				ResetPositionState();
			}
		}
		else if (Position < 0)
		{
			if (_shortStop > 0m && candle.HighPrice >= _shortStop)
			{
				BuyMarket();
				ResetPositionState();
				return;
			}
			if (_shortTake > 0m && candle.LowPrice <= _shortTake)
			{
				BuyMarket();
				ResetPositionState();
			}
		}
	}

	private void ResetPositionState()
	{
		_entryPrice = 0m;
		_longStop = 0m;
		_longTake = 0m;
		_shortStop = 0m;
		_shortTake = 0m;
		_pendingBuyPrice = null;
		_pendingSellPrice = null;
	}
}
