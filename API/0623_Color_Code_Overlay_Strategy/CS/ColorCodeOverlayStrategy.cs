using System;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Color Code Overlay strategy.
/// Trades on candle color changes with pip-based stops and time filter.
/// </summary>
public class ColorCodeOverlayStrategy : Strategy
{
	/// <summary>
	/// Trade direction options.
	/// </summary>
	public enum TradeType
	{
		Both,
		LongOnly,
		ShortOnly
	}

	private readonly StrategyParam<TradeType> _tradeType;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<TimeSpan> _startTime;
	private readonly StrategyParam<TimeSpan> _endTime;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevColorOpen;
	private decimal? _prevColorClose;
	private bool _prevBullish;

	private const decimal ThresholdPercent = 1m;

	/// <summary>
	/// Trade type.
	/// </summary>
	public TradeType Mode { get => _tradeType.Value; set => _tradeType.Value = value; }

	/// <summary>
	/// Stop loss in pips.
	/// </summary>
	public int StopLossPips { get => _stopLossPips.Value; set => _stopLossPips.Value = value; }

	/// <summary>
	/// Take profit in pips.
	/// </summary>
	public int TakeProfitPips { get => _takeProfitPips.Value; set => _takeProfitPips.Value = value; }

	/// <summary>
	/// Trading start time.
	/// </summary>
	public TimeSpan StartTime { get => _startTime.Value; set => _startTime.Value = value; }

	/// <summary>
	/// Trading end time.
	/// </summary>
	public TimeSpan EndTime { get => _endTime.Value; set => _endTime.Value = value; }

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public ColorCodeOverlayStrategy()
	{
		_tradeType = Param(nameof(Mode), TradeType.Both)
			.SetDisplay("Trade Type", "Trading mode", "General");

		_stopLossPips = Param(nameof(StopLossPips), 20)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss (pips)", "Stop distance", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 40)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit (pips)", "Take profit distance", "Risk");

		_startTime = Param(nameof(StartTime), new TimeSpan(9, 0, 0))
			.SetDisplay("Start Time", "Trading start", "General");

		_endTime = Param(nameof(EndTime), new TimeSpan(16, 0, 0))
			.SetDisplay("End Time", "Trading end", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevColorOpen = null;
		_prevColorClose = null;
		_prevBullish = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var pip = Security?.PriceStep ?? 1m;
		StartProtection(
			takeProfit: new Unit(TakeProfitPips * pip, UnitTypes.Absolute),
			stopLoss: new Unit(StopLossPips * pip, UnitTypes.Absolute));

		var sub = SubscribeCandles(CandleType);
		sub.ForEach(Process).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, sub);
			DrawOwnTrades(area);
		}
	}

	private void Process(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var colorClose = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m;
		decimal colorOpen;

		if (_prevColorOpen is null || _prevColorClose is null)
			colorOpen = (candle.OpenPrice + candle.ClosePrice) / 2m;
		else
			colorOpen = (_prevColorOpen.Value + _prevColorClose.Value) / 2m;

		var colorHigh = Math.Max(candle.HighPrice, Math.Max(colorOpen, colorClose));
		var colorLow = Math.Min(candle.LowPrice, Math.Min(colorOpen, colorClose));
		var range = colorHigh - colorLow;
		var threshold = (ThresholdPercent / 100m) * range;
		var isBullish = colorClose > colorOpen;
		var changeGreenToRed = _prevBullish && !isBullish && Math.Abs(colorClose - colorOpen) > threshold;
		var changeRedToGreen = !_prevBullish && isBullish && Math.Abs(colorClose - colorOpen) > threshold;

		var tod = candle.OpenTime.TimeOfDay;
		var inTime = tod >= StartTime && tod <= EndTime;

		if (inTime)
		{
			if (changeRedToGreen)
			{
				if ((Mode == TradeType.Both || Mode == TradeType.ShortOnly) && Position < 0)
					BuyMarket(-Position);
				if ((Mode == TradeType.Both || Mode == TradeType.LongOnly) && Position <= 0)
					BuyMarket();
			}
			else if (changeGreenToRed)
			{
				if ((Mode == TradeType.Both || Mode == TradeType.LongOnly) && Position > 0)
					SellMarket(Position);
				if ((Mode == TradeType.Both || Mode == TradeType.ShortOnly) && Position >= 0)
					SellMarket();
			}
		}

		_prevColorOpen = colorOpen;
		_prevColorClose = colorClose;
		_prevBullish = isBullish;
	}
}
