namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>Two-window BBTrend with a formed Wilder SuperTrend and complete target-position reversals.</summary>
public class BbtrendSupertrendDecisionStrategy : Strategy
{
	/// <summary>Optional native percentage protection components.</summary>
	[Flags]
	public enum TpSlModes
	{
		/// <summary>No percentage protection.</summary>
		None = 0,
		/// <summary>Take profit.</summary>
		TP = 1 << 0,
		/// <summary>Stop loss.</summary>
		SL = 1 << 1,
		/// <summary>Take profit and stop loss.</summary>
		Both = TP | SL
	}

	private readonly StrategyParam<int> _shortBbLength;
	private readonly StrategyParam<int> _longBbLength;
	private readonly StrategyParam<decimal> _stdDev;
	private readonly StrategyParam<int> _supertrendLength;
	private readonly StrategyParam<decimal> _supertrendMultiplier;
	private readonly StrategyParam<Sides?> _tradeDirection;
	private readonly StrategyParam<TpSlModes> _tpSlCondition;
	private readonly StrategyParam<decimal> _takeProfitPerc;
	private readonly StrategyParam<decimal> _stopLossPerc;
	private readonly StrategyParam<DataType> _candleType;
	private readonly Queue<decimal> _initialRanges = [];
	private decimal? _previousBbTrend, _prevUp, _prevDn, _prevAtr, _prevSt;
	private DateTime _lastTime;
	private Order _pending;

	/// <summary>Short Bollinger close window.</summary>
	public int ShortBbLength
	{
		get => _shortBbLength.Value;
		set => _shortBbLength.Value = value;
	}

	/// <summary>Long Bollinger close window.</summary>
	public int LongBbLength
	{
		get => _longBbLength.Value;
		set => _longBbLength.Value = value;
	}

	/// <summary>Population deviation multiplier.</summary>
	public decimal StdDev
	{
		get => _stdDev.Value;
		set => _stdDev.Value = value;
	}

	/// <summary>Wilder true range period on BBTrend.</summary>
	public int SupertrendLength
	{
		get => _supertrendLength.Value;
		set => _supertrendLength.Value = value;
	}

	/// <summary>SuperTrend band multiplier.</summary>
	public decimal SupertrendMultiplier
	{
		get => _supertrendMultiplier.Value;
		set => _supertrendMultiplier.Value = value;
	}

	/// <summary>Allowed entry side; null allows both.</summary>
	public Sides? TradeDirection
	{
		get => _tradeDirection.Value;
		set => _tradeDirection.Value = value;
	}

	/// <summary>Optional percentage protection mode.</summary>
	public TpSlModes TpSlCondition
	{
		get => _tpSlCondition.Value;
		set => _tpSlCondition.Value = value;
	}

	/// <summary>Optional profit distance in percent.</summary>
	public decimal TakeProfitPerc
	{
		get => _takeProfitPerc.Value;
		set => _takeProfitPerc.Value = value;
	}

	/// <summary>Optional loss distance in percent.</summary>
	public decimal StopLossPerc
	{
		get => _stopLossPerc.Value;
		set => _stopLossPerc.Value = value;
	}

	/// <summary>Finished signal candle interval.</summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>Initializes the published BBTrend, SuperTrend and optional protection parameters.</summary>
	public BbtrendSupertrendDecisionStrategy()
	{
		_shortBbLength = Param(nameof(ShortBbLength), 20).SetGreaterThanZero().SetOptimize(10, 40, 5)
			.SetDisplay("ShortBbLength", "Short Bollinger close window", "Parameters");
		_longBbLength = Param(nameof(LongBbLength), 50).SetGreaterThanZero().SetOptimize(30, 100, 5)
			.SetDisplay("LongBbLength", "Long Bollinger close window", "Parameters");
		_stdDev = Param(nameof(StdDev), 2m).SetGreaterThanZero()
			.SetDisplay("StdDev", "Population deviation multiplier", "Parameters");
		_supertrendLength = Param(nameof(SupertrendLength), 10).SetGreaterThanZero().SetOptimize(5, 20, 1)
			.SetDisplay("SupertrendLength", "Wilder true range period on BBTrend", "Parameters");
		_supertrendMultiplier = Param(nameof(SupertrendMultiplier), 7m).SetGreaterThanZero().SetOptimize(1m, 10m, 1m)
			.SetDisplay("SupertrendMultiplier", "SuperTrend band multiplier", "Parameters");
		_tradeDirection = Param(nameof(TradeDirection), (Sides?)null)
			.SetDisplay("TradeDirection", "Allowed entry side; null allows both", "Parameters");
		_tpSlCondition = Param(nameof(TpSlCondition), TpSlModes.None)
			.SetDisplay("TpSlCondition", "Optional percentage protection mode", "Parameters");
		_takeProfitPerc = Param(nameof(TakeProfitPerc), 30m).SetGreaterThanZero()
			.SetDisplay("TakeProfitPerc", "Optional profit distance in percent", "Parameters");
		_stopLossPerc = Param(nameof(StopLossPerc), 20m).SetGreaterThanZero()
			.SetDisplay("StopLossPerc", "Optional loss distance in percent", "Parameters");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("CandleType", "Finished signal candle interval", "Parameters");
		OrderRegistering += order => _pending = order;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType), (Security, DataType.Level1)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_initialRanges.Clear();
		_previousBbTrend = _prevUp = _prevDn = _prevAtr = _prevSt = null;
		_lastTime = default;
		_pending = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		if (ShortBbLength >= LongBbLength || (TpSlCondition & ~TpSlModes.Both) != TpSlModes.None)
			throw new InvalidOperationException("The short BB window must be smaller than the long window and the risk mode must be valid.");
		base.OnStarted2(time);
		StartProtection(TpSlCondition.HasFlag(TpSlModes.TP) ? TakeProfitPerc.Percents() : new Unit(),
			TpSlCondition.HasFlag(TpSlModes.SL) ? StopLossPerc.Percents() : new Unit(), useMarketOrders: true, isLocalStop: true);
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quote = new Subscription(DataType.Level1, Security);
			quote.MarketData.BuildField = field;
			SubscribeLevel1(quote).Start();
		}
		var shortBb = new BollingerBands { Length = ShortBbLength, Width = StdDev };
		var longBb = new BollingerBands { Length = LongBbLength, Width = StdDev };
		var candles = SubscribeCandles(CandleType);
		candles.BindEx([shortBb, longBb], ProcessCandle).Start();
		var area = CreateChartArea();
		if (area is not null)
		{
			DrawCandles(area, candles);
			DrawIndicator(area, shortBb);
			DrawIndicator(area, longBb);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue[] values)
	{
		if (candle.State != CandleStates.Finished || candle.OpenTime <= _lastTime || !values.All(v => v.IsFormed)
			|| values[0] is not BollingerBandsValue a || values[1] is not BollingerBandsValue b
			|| a.LowBand is not decimal shortLow || a.UpBand is not decimal shortHigh || a.MovingAverage is not decimal middle
			|| b.LowBand is not decimal longLow || b.UpBand is not decimal longHigh || middle <= 0m)
			return;
		_lastTime = candle.OpenTime;
		var score = (Math.Abs(shortLow - longLow) - Math.Abs(shortHigh - longHigh)) / middle * 100m;
		var previous = _previousBbTrend;
		_previousBbTrend = score;
		if (previous is null) return;
		var range = Math.Abs(score - previous.Value);
		if (_prevAtr is null)
		{
			_initialRanges.Enqueue(range);
			if (_initialRanges.Count < SupertrendLength) return;
			_prevAtr = _initialRanges.Average();
		}
		else _prevAtr = (_prevAtr.Value * (SupertrendLength - 1) + range) / SupertrendLength;
		var upper = (score + previous.Value) / 2m + SupertrendMultiplier * _prevAtr.Value;
		var lower = (score + previous.Value) / 2m - SupertrendMultiplier * _prevAtr.Value;
		if (_prevUp is decimal u && upper >= u && previous <= u) upper = u;
		if (_prevDn is decimal d && lower <= d && previous >= d) lower = d;
		var down = _prevSt is null || (_prevSt == _prevUp ? score <= upper : score < lower);
		_prevUp = upper;
		_prevDn = lower;
		_prevSt = down ? upper : lower;
		if (!IsFormedAndOnlineAndAllowTrading() || _pending is not null && _pending.State is not (OrderStates.Done or OrderStates.Failed))
			return;
		var side = down ? Sides.Sell : Sides.Buy;
		var target = TradeDirection is null || TradeDirection == side ? side == Sides.Buy ? Volume : -Volume : 0m;
		var delta = target - Position;
		if (delta > 0m) BuyMarket(delta);
		else if (delta < 0m) SellMarket(-delta);
	}
}
