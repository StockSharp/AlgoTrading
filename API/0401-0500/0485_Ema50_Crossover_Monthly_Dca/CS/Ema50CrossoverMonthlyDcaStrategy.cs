namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>Monthly cash contributions invested above a formed weekly EMA, with complete long exits below it.</summary>
public class Ema50CrossoverMonthlyDcaStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<decimal> _dcaAmount;
	private readonly StrategyParam<DateTime> _startDate;
	private Order _pending;
	private ExponentialMovingAverage _ema;
	private decimal _cash;
	private int _lastMonth;
	/// <summary>Finished signal candle type.</summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>Weekly close EMA length.</summary>
	public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }

	/// <summary>Cash contribution per observed calendar month.</summary>
	public decimal DcaAmount { get => _dcaAmount.Value; set => _dcaAmount.Value = value; }

	/// <summary>Earliest eligible historical candle close.</summary>
	public DateTime StartDate { get => _startDate.Value; set => _startDate.Value = value; }

	/// <summary>Initialize the strategy parameters.</summary>
	public Ema50CrossoverMonthlyDcaStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(10080).TimeFrame())
			.SetDisplay("CandleType", "Finished signal candle type.", "Parameters");
		_emaLength = Param(nameof(EmaLength), 50).SetRange(1, int.MaxValue)
			.SetDisplay("EmaLength", "Weekly close EMA length.", "Parameters");
		_dcaAmount = Param(nameof(DcaAmount), 100000m).SetRange(0.000001m, decimal.MaxValue)
			.SetDisplay("DcaAmount", "Cash contribution per observed calendar month.", "Parameters");
		_startDate = Param(nameof(StartDate), new DateTime(1980, 1, 1))
			.SetDisplay("StartDate", "Earliest eligible historical candle close.", "Parameters");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_pending = null;
		_ema = null;
		_cash = 0m;
		_lastMonth = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		if (Security.VolumeStep is not > 0m || Security.MinVolume is not > 0m || Security.MaxVolume is not > 0m)
			throw new InvalidOperationException("Execution security needs native volume constraints.");
		base.OnStarted2(time);
		_ema = new ExponentialMovingAverage { Length = EmaLength };
		SubscribeCandles(CandleType).Bind(_ema, ProcessCandle).Start();
	}

	private bool IsPending => _pending is not null && _pending.State is not (OrderStates.Done or OrderStates.Failed);

	private void Submit(Sides side, decimal quantity, string comment)
	{
		_pending = CreateOrder(side, 0m, quantity);
		_pending.Type = OrderTypes.Market;
		_pending.Comment = comment;
		RegisterOrder(_pending);
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema)
	{
		if (candle.State != CandleStates.Finished || candle.CloseTime < StartDate || candle.ClosePrice <= 0m) return;
		var month = candle.CloseTime.Year * 12 + candle.CloseTime.Month;
		if (month > _lastMonth)
		{
			_cash += DcaAmount * (_lastMonth == 0 ? 1 : month - _lastMonth);
			_lastMonth = month;
		}
		if (!_ema.IsFormed || IsPending || !IsFormedAndOnlineAndAllowTrading()) return;
		if (candle.ClosePrice < ema && Position > 0m)
			Submit(Sides.Sell, Position, "0485Exit");
		else if (candle.ClosePrice > ema)
		{
			var quantity = Math.Floor(Math.Min(_cash / candle.ClosePrice, Security.MaxVolume.Value) / Security.VolumeStep.Value) * Security.VolumeStep.Value;
			if (quantity >= Security.MinVolume.Value)
				Submit(Sides.Buy, quantity, "0485Dca");
		}
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);
		_cash += (trade.Order.Side == Sides.Buy ? -1m : 1m) * trade.Trade.Price * trade.Trade.Volume;
	}

}
