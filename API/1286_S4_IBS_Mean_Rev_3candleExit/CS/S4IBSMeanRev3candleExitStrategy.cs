using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// S4 IBS Mean Rev 3candleExit strategy.
/// Buys when previous candle's internal bar strength is below threshold and
/// exits on profit or after three candles if still losing.
/// </summary>
public class S4IBSMeanRev3candleExitStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _ibsThreshold;
	private readonly StrategyParam<DateTimeOffset> _startTime;
	private readonly StrategyParam<DateTimeOffset> _endTime;

	private decimal? _prevHigh;
	private decimal? _prevLow;
	private decimal? _prevClose;

	private decimal? _entryPrice;
	private int _barsSinceEntry;

	public S4IBSMeanRev3candleExitStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_ibsThreshold = Param(nameof(IbsThreshold), 0.25m)
			.SetDisplay("IBS Threshold", "Internal bar strength threshold", "General")
			.SetCanOptimize(true);

		_startTime = Param(nameof(StartTime), new DateTimeOffset(2024, 1, 1, 5, 0, 0, TimeSpan.Zero))
			.SetDisplay("Start Time", "Start time for trading", "Time");

		_endTime = Param(nameof(EndTime), new DateTimeOffset(2024, 12, 31, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("End Time", "End time for trading", "Time");
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// IBS threshold for entries.
	/// </summary>
	public decimal IbsThreshold
	{
		get => _ibsThreshold.Value;
		set => _ibsThreshold.Value = value;
	}

	/// <summary>
	/// Start time for candle processing.
	/// </summary>
	public DateTimeOffset StartTime
	{
		get => _startTime.Value;
		set => _startTime.Value = value;
	}

	/// <summary>
	/// End time for candle processing.
	/// </summary>
	public DateTimeOffset EndTime
	{
		get => _endTime.Value;
		set => _endTime.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevHigh = default;
		_prevLow = default;
		_prevClose = default;
		_entryPrice = default;
		_barsSinceEntry = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.WhenNew(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var inRange = candle.OpenTime >= StartTime && candle.OpenTime <= EndTime;

		decimal? ibs = null;
		if (_prevHigh is decimal h && _prevLow is decimal l && _prevClose is decimal c && h != l)
			ibs = (c - l) / (h - l);

		if (IsFormedAndOnlineAndAllowTrading())
		{
			if (ibs is decimal value && inRange && value <= IbsThreshold && Position <= 0)
			{
				var volume = Volume + (Position < 0 ? -Position : 0m);
				BuyMarket(volume);
				_entryPrice = candle.ClosePrice;
				_barsSinceEntry = 1;
			}
			else if (Position > 0 && _entryPrice is decimal entry)
			{
				_barsSinceEntry++;
				var profit = candle.ClosePrice > entry;
				var lossTooLong = _barsSinceEntry >= 3 && candle.ClosePrice < entry;
				var forceExit = candle.OpenTime > EndTime;

				if ((inRange && (profit || lossTooLong)) || forceExit)
				{
					SellMarket(Position);
					_entryPrice = null;
					_barsSinceEntry = 0;
				}
			}
		}

		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;
		_prevClose = candle.ClosePrice;
	}
}
