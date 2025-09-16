namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Multi-timeframe trend strategy based on multiple simple moving averages
/// and Accelerator Oscillator alignment across three timeframes.
/// </summary>
public class TrendAlexcudStrategy : Strategy
{
	private readonly StrategyParam<DataType> _tf1;
	private readonly StrategyParam<DataType> _tf2;
	private readonly StrategyParam<DataType> _tf3;

	private readonly StrategyParam<int> _ma1;
	private readonly StrategyParam<int> _ma2;
	private readonly StrategyParam<int> _ma3;
	private readonly StrategyParam<int> _ma4;
	private readonly StrategyParam<int> _ma5;

	private readonly TimeframeContext _ctx1 = new();
	private readonly TimeframeContext _ctx2 = new();
	private readonly TimeframeContext _ctx3 = new();

	/// <summary>
	/// First timeframe.
	/// </summary>
	public DataType Timeframe1 { get => _tf1.Value; set => _tf1.Value = value; }

	/// <summary>
	/// Second timeframe.
	/// </summary>
	public DataType Timeframe2 { get => _tf2.Value; set => _tf2.Value = value; }

	/// <summary>
	/// Third timeframe.
	/// </summary>
	public DataType Timeframe3 { get => _tf3.Value; set => _tf3.Value = value; }

	/// <summary>
	/// Period of the shortest moving average.
	/// </summary>
	public int MaPeriod1 { get => _ma1.Value; set => _ma1.Value = value; }

	/// <summary>
	/// Period of the second moving average.
	/// </summary>
	public int MaPeriod2 { get => _ma2.Value; set => _ma2.Value = value; }

	/// <summary>
	/// Period of the third moving average.
	/// </summary>
	public int MaPeriod3 { get => _ma3.Value; set => _ma3.Value = value; }

	/// <summary>
	/// Period of the fourth moving average.
	/// </summary>
	public int MaPeriod4 { get => _ma4.Value; set => _ma4.Value = value; }

	/// <summary>
	/// Period of the longest moving average.
	/// </summary>
	public int MaPeriod5 { get => _ma5.Value; set => _ma5.Value = value; }

	/// <summary>
	/// Constructor.
	/// </summary>
	public TrendAlexcudStrategy()
	{
		_tf1 = Param(nameof(Timeframe1), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Timeframe 1", "Primary timeframe", "General");
		_tf2 = Param(nameof(Timeframe2), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Timeframe 2", "Secondary timeframe", "General");
		_tf3 = Param(nameof(Timeframe3), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Timeframe 3", "Tertiary timeframe", "General");

		_ma1 = Param(nameof(MaPeriod1), 5)
		.SetGreaterThanZero()
		.SetDisplay("MA 1", "Shortest MA period", "Indicators");
		_ma2 = Param(nameof(MaPeriod2), 8)
		.SetGreaterThanZero()
		.SetDisplay("MA 2", "Second MA period", "Indicators");
		_ma3 = Param(nameof(MaPeriod3), 13)
		.SetGreaterThanZero()
		.SetDisplay("MA 3", "Third MA period", "Indicators");
		_ma4 = Param(nameof(MaPeriod4), 21)
		.SetGreaterThanZero()
		.SetDisplay("MA 4", "Fourth MA period", "Indicators");
		_ma5 = Param(nameof(MaPeriod5), 34)
		.SetGreaterThanZero()
		.SetDisplay("MA 5", "Longest MA period", "Indicators");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		InitContext(_ctx1, MaPeriod1, MaPeriod2, MaPeriod3, MaPeriod4, MaPeriod5);
		InitContext(_ctx2, MaPeriod1, MaPeriod2, MaPeriod3, MaPeriod4, MaPeriod5);
		InitContext(_ctx3, MaPeriod1, MaPeriod2, MaPeriod3, MaPeriod4, MaPeriod5);

		Subscribe(Timeframe1, _ctx1, ProcessTf1);
		Subscribe(Timeframe2, _ctx2, ProcessTf2);
		Subscribe(Timeframe3, _ctx3, ProcessTf3);

		StartProtection();
	}

	private void Subscribe(DataType timeframe, TimeframeContext ctx, Action<ICandleMessage, IIndicatorValue, IIndicatorValue, IIndicatorValue, IIndicatorValue, IIndicatorValue, IIndicatorValue> handler)
	{
		var sub = SubscribeCandles(timeframe);
		sub
		.BindEx(ctx.Ma1, ctx.Ma2, ctx.Ma3, ctx.Ma4, ctx.Ma5, ctx.Ao, ctx.AoSma, handler)
		.Start();
	}

	private static void InitContext(TimeframeContext ctx, int p1, int p2, int p3, int p4, int p5)
	{
		ctx.Ma1 = new SimpleMovingAverage { Length = p1 };
		ctx.Ma2 = new SimpleMovingAverage { Length = p2 };
		ctx.Ma3 = new SimpleMovingAverage { Length = p3 };
		ctx.Ma4 = new SimpleMovingAverage { Length = p4 };
		ctx.Ma5 = new SimpleMovingAverage { Length = p5 };
		ctx.Ao = new AwesomeOscillator();
		ctx.AoSma = new SimpleMovingAverage { Length = p1 }; // Smoothing period for AC
	}

	private void ProcessTf1(ICandleMessage candle, IIndicatorValue ma1, IIndicatorValue ma2, IIndicatorValue ma3, IIndicatorValue ma4, IIndicatorValue ma5, IIndicatorValue ao, IIndicatorValue aoSma)
	{
		ProcessTimeframe(candle, ma1, ma2, ma3, ma4, ma5, ao, aoSma, _ctx1);
	}

	private void ProcessTf2(ICandleMessage candle, IIndicatorValue ma1, IIndicatorValue ma2, IIndicatorValue ma3, IIndicatorValue ma4, IIndicatorValue ma5, IIndicatorValue ao, IIndicatorValue aoSma)
	{
		ProcessTimeframe(candle, ma1, ma2, ma3, ma4, ma5, ao, aoSma, _ctx2);
	}

	private void ProcessTf3(ICandleMessage candle, IIndicatorValue ma1, IIndicatorValue ma2, IIndicatorValue ma3, IIndicatorValue ma4, IIndicatorValue ma5, IIndicatorValue ao, IIndicatorValue aoSma)
	{
		ProcessTimeframe(candle, ma1, ma2, ma3, ma4, ma5, ao, aoSma, _ctx3);
	}

	private void ProcessTimeframe(ICandleMessage candle, IIndicatorValue ma1, IIndicatorValue ma2, IIndicatorValue ma3, IIndicatorValue ma4, IIndicatorValue ma5, IIndicatorValue ao, IIndicatorValue aoSma, TimeframeContext ctx)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!ma1.IsFinal || !ma2.IsFinal || !ma3.IsFinal || !ma4.IsFinal || !ma5.IsFinal || !ao.IsFinal || !aoSma.IsFinal)
		return;

		var price = candle.ClosePrice;
		var maVal1 = ma1.GetValue<decimal>();
		var maVal2 = ma2.GetValue<decimal>();
		var maVal3 = ma3.GetValue<decimal>();
		var maVal4 = ma4.GetValue<decimal>();
		var maVal5 = ma5.GetValue<decimal>();
		var aoVal = ao.GetValue<decimal>();
		var acVal = aoVal - aoSma.GetValue<decimal>();

		ctx.IsBull = price > maVal1 && price > maVal2 && price > maVal3 && price > maVal4 && price > maVal5 && acVal > 0;
		ctx.IsBear = price < maVal1 && price < maVal2 && price < maVal3 && price < maVal4 && price < maVal5 && acVal < 0;

		TryTrade();
	}

	private void TryTrade()
	{
		if (_ctx1.IsBull && _ctx2.IsBull && _ctx3.IsBull && Position <= 0)
		{
		BuyMarket();
		}
		else if (_ctx1.IsBear && _ctx2.IsBear && _ctx3.IsBear && Position >= 0)
		{
		SellMarket();
		}
	}

	private sealed class TimeframeContext
	{
		public SimpleMovingAverage Ma1 { get; set; }
		public SimpleMovingAverage Ma2 { get; set; }
		public SimpleMovingAverage Ma3 { get; set; }
		public SimpleMovingAverage Ma4 { get; set; }
		public SimpleMovingAverage Ma5 { get; set; }
		public AwesomeOscillator Ao { get; set; }
		public SimpleMovingAverage AoSma { get; set; }
		public bool IsBull { get; set; }
		public bool IsBear { get; set; }
	}
}
