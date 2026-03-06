using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class MartinNoLossExitV3Strategy : Strategy
{
	private readonly StrategyParam<decimal> _priceStepPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _increaseFactor;
	private readonly StrategyParam<int> _maxOrders;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaLength;

	private ExponentialMovingAverage _ema;
	private decimal _entryPrice;
	private decimal _totalCost;
	private decimal _totalQty;
	private decimal _lastCash;
	private int _orderCount;
	private bool _inPosition;

	public decimal PriceStepPercent { get => _priceStepPercent.Value; set => _priceStepPercent.Value = value; }
	public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }
	public decimal IncreaseFactor { get => _increaseFactor.Value; set => _increaseFactor.Value = value; }
	public int MaxOrders { get => _maxOrders.Value; set => _maxOrders.Value = value; }
	public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MartinNoLossExitV3Strategy()
	{
		_priceStepPercent = Param(nameof(PriceStepPercent), 2.5m);
		_takeProfitPercent = Param(nameof(TakeProfitPercent), 2.5m);
		_increaseFactor = Param(nameof(IncreaseFactor), 1.10m);
		_maxOrders = Param(nameof(MaxOrders), 8);
		_emaLength = Param(nameof(EmaLength), 50);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame());
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_ema = null;
		_entryPrice = 0m;
		_totalCost = 0m;
		_totalQty = 0m;
		_lastCash = 0m;
		_orderCount = 0;
		_inPosition = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_entryPrice = 0m;
		_totalCost = 0m;
		_totalQty = 0m;
		_lastCash = 0m;
		_orderCount = 0;
		_inPosition = false;
		_ema = new ExponentialMovingAverage { Length = EmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_ema, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_ema.IsFormed)
			return;

		var initialCash = 100m;

		if (_inPosition)
		{
			var avgPrice = _totalQty > 0m ? _totalCost / _totalQty : 0m;
			var takeProfitPrice = avgPrice * (1 + TakeProfitPercent / 100m);

			if (candle.HighPrice >= takeProfitPrice && Position > 0)
			{
				SellMarket();
				_inPosition = false;
				_entryPrice = 0m;
				_totalCost = 0m;
				_totalQty = 0m;
				_lastCash = 0m;
				_orderCount = 0;
				return;
			}

			var nextEntryPrice = _entryPrice * (1 - PriceStepPercent / 100m * _orderCount);
			if (_orderCount < MaxOrders && candle.ClosePrice <= nextEntryPrice)
			{
				BuyMarket();
				var newCash = _lastCash * IncreaseFactor;
				_totalCost += newCash;
				_totalQty += newCash / candle.ClosePrice;
				_lastCash = newCash;
				_orderCount++;
			}
		}
		else
		{
			if (candle.ClosePrice > ema)
			{
				BuyMarket();
				_entryPrice = candle.ClosePrice;
				_totalCost = initialCash;
				_totalQty = initialCash / candle.ClosePrice;
				_lastCash = initialCash;
				_orderCount = 1;
				_inPosition = true;
			}
		}
	}
}
