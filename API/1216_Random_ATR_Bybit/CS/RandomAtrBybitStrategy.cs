using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class RandomAtrBybitStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _slAtrRatio;
	private readonly StrategyParam<decimal> _tpSlRatio;

	private bool _hasPrev;
	private decimal _prevClose;
	private Order _stopOrder;
	private Order _tpOrder;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }
	public decimal SlAtrRatio { get => _slAtrRatio.Value; set => _slAtrRatio.Value = value; }
	public decimal TpSlRatio { get => _tpSlRatio.Value; set => _tpSlRatio.Value = value; }

	public RandomAtrBybitStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame());
		_atrLength = Param(nameof(AtrLength), 14);
		_slAtrRatio = Param(nameof(SlAtrRatio), 3m);
		_tpSlRatio = Param(nameof(TpSlRatio), 1m);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(atr, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_hasPrev)
		{
			_prevClose = candle.ClosePrice;
			_hasPrev = true;
			return;
		}

		var highestClose = _prevClose > candle.ClosePrice ? _prevClose : candle.ClosePrice;
		var lowestClose = _prevClose < candle.ClosePrice ? _prevClose : candle.ClosePrice;
		var randNumber = Math.Abs(highestClose - lowestClose);

		var openTime = candle.OpenTime;
		var timeSeed = openTime.Year * 10000 + openTime.Month * 100 + openTime.Day;
		var randomSignal = ((randNumber * timeSeed) % 2m) == 1m;

		var stopOffset = SlAtrRatio * atr;
		var takeOffset = stopOffset * TpSlRatio;

		if (Position == 0)
		{
			if (randomSignal)
			{
				BuyMarket(Volume);
				RegisterProtection(true, candle.ClosePrice, stopOffset, takeOffset);
			}
			else
			{
				SellMarket(Volume);
				RegisterProtection(false, candle.ClosePrice, stopOffset, takeOffset);
			}
		}

		_prevClose = candle.ClosePrice;
	}

	private void RegisterProtection(bool isLong, decimal entryPrice, decimal stopOffset, decimal takeOffset)
	{
		if (_stopOrder != null && _stopOrder.State == OrderStates.Active)
			CancelOrder(_stopOrder);
		if (_tpOrder != null && _tpOrder.State == OrderStates.Active)
			CancelOrder(_tpOrder);

		_stopOrder = isLong
			? SellStop(Volume, entryPrice - stopOffset)
			: BuyStop(Volume, entryPrice + stopOffset);

		_tpOrder = isLong
			? SellLimit(Volume, entryPrice + takeOffset)
			: BuyLimit(Volume, entryPrice - takeOffset);
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position != 0)
			return;

		if (_stopOrder != null && _stopOrder.State == OrderStates.Active)
			CancelOrder(_stopOrder);
		if (_tpOrder != null && _tpOrder.State == OrderStates.Active)
			CancelOrder(_tpOrder);

		_stopOrder = null;
		_tpOrder = null;
	}
}
