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

/// <summary>SMA pullbacks with entry-frozen ATR distances and native quote exits.</summary>
public class SmaPullbackAtrExitsStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastSmaLength;
	private readonly StrategyParam<int> _slowSmaLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrMultiplierSl;
	private readonly StrategyParam<decimal> _atrMultiplierTp;
	private Order _pending;
	private SimpleMovingAverage _fast;
	private SimpleMovingAverage _slow;
	private AverageTrueRange _atr;
	/// <summary>Finished signal candle type.</summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>Fast close SMA length.</summary>
	public int FastSmaLength { get => _fastSmaLength.Value; set => _fastSmaLength.Value = value; }

	/// <summary>Slow close SMA length.</summary>
	public int SlowSmaLength { get => _slowSmaLength.Value; set => _slowSmaLength.Value = value; }

	/// <summary>Wilder true-range window.</summary>
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }

	/// <summary>Frozen ATR stop-loss multiplier.</summary>
	public decimal AtrMultiplierSl { get => _atrMultiplierSl.Value; set => _atrMultiplierSl.Value = value; }

	/// <summary>Frozen ATR take-profit multiplier.</summary>
	public decimal AtrMultiplierTp { get => _atrMultiplierTp.Value; set => _atrMultiplierTp.Value = value; }

	/// <summary>Initialize the strategy parameters.</summary>
	public SmaPullbackAtrExitsStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("CandleType", "Finished signal candle type.", "Parameters");
		_fastSmaLength = Param(nameof(FastSmaLength), 8).SetRange(1, int.MaxValue)
			.SetDisplay("FastSmaLength", "Fast close SMA length.", "Parameters");
		_slowSmaLength = Param(nameof(SlowSmaLength), 30).SetRange(2, int.MaxValue)
			.SetDisplay("SlowSmaLength", "Slow close SMA length.", "Parameters");
		_atrLength = Param(nameof(AtrLength), 14).SetRange(1, int.MaxValue)
			.SetDisplay("AtrLength", "Wilder true-range window.", "Parameters");
		_atrMultiplierSl = Param(nameof(AtrMultiplierSl), 1.2m).SetRange(0.000001m, decimal.MaxValue)
			.SetDisplay("AtrMultiplierSl", "Frozen ATR stop-loss multiplier.", "Parameters");
		_atrMultiplierTp = Param(nameof(AtrMultiplierTp), 2m).SetRange(0.000001m, decimal.MaxValue)
			.SetDisplay("AtrMultiplierTp", "Frozen ATR take-profit multiplier.", "Parameters");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_pending = null;
		_fast = null;
		_slow = null;
		_atr = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		if (FastSmaLength >= SlowSmaLength) throw new InvalidOperationException("FastSmaLength must be smaller than SlowSmaLength.");
		base.OnStarted2(time);
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quote = new Subscription(DataType.Level1, Security);
			quote.MarketData.BuildField = field;
			SubscribeLevel1(quote).Start();
		}
		_fast = new SimpleMovingAverage { Length = FastSmaLength };
		_slow = new SimpleMovingAverage { Length = SlowSmaLength };
		_atr = new AverageTrueRange { Length = AtrLength };
		SubscribeCandles(CandleType).Bind(_fast, _slow, _atr, ProcessCandle).Start();
	}

	private bool IsPending => _pending is not null && _pending.State is not (OrderStates.Done or OrderStates.Failed);

	private void Submit(Sides side, decimal quantity, string comment)
	{
		_pending = CreateOrder(side, 0m, quantity);
		_pending.Type = OrderTypes.Market;
		_pending.Comment = comment;
		RegisterOrder(_pending);
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal atr)
	{
		if (candle.State != CandleStates.Finished || !_fast.IsFormed || !_slow.IsFormed || !_atr.IsFormed
			|| atr <= 0m || Position != 0m || IsPending || !IsFormedAndOnlineAndAllowTrading()) return;
		Sides? side = candle.ClosePrice < fast && fast > slow ? Sides.Buy
			: candle.ClosePrice > fast && fast < slow ? Sides.Sell : null;
		if (side is null) return;
		StartProtection(new Unit(atr * AtrMultiplierTp, UnitTypes.Absolute), new Unit(atr * AtrMultiplierSl, UnitTypes.Absolute),
			useMarketOrders: true, isLocalStop: true);
		Submit(side.Value, Volume, "0487Entry");
	}

}
