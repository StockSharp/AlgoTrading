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

/// <summary>Long-only shifted median-price Alligator crosses with an entry-frozen native ATR stop.</summary>
public class WilliamsAlligatorAtrStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _jawLength;
	private readonly StrategyParam<int> _teethLength;
	private readonly StrategyParam<int> _lipsLength;
	private readonly StrategyParam<int> _jawShift;
	private readonly StrategyParam<int> _teethShift;
	private readonly StrategyParam<int> _lipsShift;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private Order _pending;
	private AlligatorLine _jaw;
	private AlligatorLine _teeth;
	private AlligatorLine _lips;
	private AverageTrueRange _atr;
	private decimal? _previousSpread;
	/// <summary>Finished signal candle type.</summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>Jaw smoothed median-price window.</summary>
	public int JawLength { get => _jawLength.Value; set => _jawLength.Value = value; }

	/// <summary>Teeth smoothed median-price window.</summary>
	public int TeethLength { get => _teethLength.Value; set => _teethLength.Value = value; }

	/// <summary>Lips smoothed median-price window.</summary>
	public int LipsLength { get => _lipsLength.Value; set => _lipsLength.Value = value; }

	/// <summary>Completed bars delaying the Jaw line.</summary>
	public int JawShift { get => _jawShift.Value; set => _jawShift.Value = value; }

	/// <summary>Completed bars delaying the Teeth line.</summary>
	public int TeethShift { get => _teethShift.Value; set => _teethShift.Value = value; }

	/// <summary>Completed bars delaying the Lips line.</summary>
	public int LipsShift { get => _lipsShift.Value; set => _lipsShift.Value = value; }

	/// <summary>Wilder true-range window.</summary>
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }

	/// <summary>Entry-frozen ATR stop multiplier.</summary>
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }

	/// <summary>Initialize the strategy parameters.</summary>
	public WilliamsAlligatorAtrStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("CandleType", "Finished signal candle type.", "Parameters");
		_jawLength = Param(nameof(JawLength), 13).SetRange(1, int.MaxValue)
			.SetDisplay("JawLength", "Jaw smoothed median-price window.", "Parameters");
		_teethLength = Param(nameof(TeethLength), 8).SetRange(1, int.MaxValue)
			.SetDisplay("TeethLength", "Teeth smoothed median-price window.", "Parameters");
		_lipsLength = Param(nameof(LipsLength), 5).SetRange(1, int.MaxValue)
			.SetDisplay("LipsLength", "Lips smoothed median-price window.", "Parameters");
		_jawShift = Param(nameof(JawShift), 8).SetRange(1, int.MaxValue)
			.SetDisplay("JawShift", "Completed bars delaying the Jaw line.", "Parameters");
		_teethShift = Param(nameof(TeethShift), 5).SetRange(1, int.MaxValue)
			.SetDisplay("TeethShift", "Completed bars delaying the Teeth line.", "Parameters");
		_lipsShift = Param(nameof(LipsShift), 3).SetRange(1, int.MaxValue)
			.SetDisplay("LipsShift", "Completed bars delaying the Lips line.", "Parameters");
		_atrPeriod = Param(nameof(AtrPeriod), 14).SetRange(1, int.MaxValue)
			.SetDisplay("AtrPeriod", "Wilder true-range window.", "Parameters");
		_atrMultiplier = Param(nameof(AtrMultiplier), 2m).SetRange(0.000001m, decimal.MaxValue)
			.SetDisplay("AtrMultiplier", "Entry-frozen ATR stop multiplier.", "Parameters");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_pending = null;
		_jaw = null;
		_teeth = null;
		_lips = null;
		_atr = null;
		_previousSpread = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{

		base.OnStarted2(time);
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quote = new Subscription(DataType.Level1, Security);
			quote.MarketData.BuildField = field;
			SubscribeLevel1(quote).Start();
		}
		_jaw = new AlligatorLine { Length = JawLength, Shift = JawShift };
		_teeth = new AlligatorLine { Length = TeethLength, Shift = TeethShift };
		_lips = new AlligatorLine { Length = LipsLength, Shift = LipsShift };
		_atr = new AverageTrueRange { Length = AtrPeriod };
		SubscribeCandles(CandleType).Bind(ProcessCandle).Start();
	}

	private bool IsPending => _pending is not null && _pending.State is not (OrderStates.Done or OrderStates.Failed);

	private void Submit(Sides side, decimal quantity, string comment)
	{
		_pending = CreateOrder(side, 0m, quantity);
		_pending.Type = OrderTypes.Market;
		_pending.Comment = comment;
		RegisterOrder(_pending);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished) return;
		var jaw = _jaw.Process(candle);
		var teeth = _teeth.Process(candle);
		var lips = _lips.Process(candle);
		var atr = _atr.Process(candle);
		if (!_jaw.IsFormed || !_teeth.IsFormed || !_lips.IsFormed || !_atr.IsFormed
			|| jaw.IsEmpty || teeth.IsEmpty || lips.IsEmpty || atr.IsEmpty) return;
		var spread = lips.ToDecimal() - jaw.ToDecimal();
		var previous = _previousSpread;
		_previousSpread = spread;
		if (previous is null || IsPending || !IsFormedAndOnlineAndAllowTrading()) return;
		if (previous >= 0m && spread < 0m && Position > 0m)
			Submit(Sides.Sell, Position, "0512CrossExit");
		else if (previous <= 0m && spread > 0m && Position == 0m && atr.ToDecimal() > 0m)
		{
			StartProtection(new Unit(), new Unit(atr.ToDecimal() * AtrMultiplier, UnitTypes.Absolute), useMarketOrders: true, isLocalStop: true);
			Submit(Sides.Buy, Volume, "0512Entry");
		}
	}

}
