using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Merovinh mean reversion strategy using lowest low breakouts.
/// </summary>
public class MerovinhMeanReversionLowestLowStrategy : Strategy {
	private readonly StrategyParam<int> _bars;
	private readonly StrategyParam<int> _numberOfLows;
	private readonly StrategyParam<DateTimeOffset> _startDate;
	private readonly StrategyParam<DateTimeOffset> _endDate;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _highest = null!;
	private Lowest _lowest = null!;

	private decimal _prevLow;
	private decimal _prevLow2;
	private decimal _prevLow3;
	private decimal _prevLow4;
	private decimal _prevHigh;

	/// <summary>
	/// Lookback period for highest and lowest.
	/// </summary>
	public int Bars {
		get => _bars.Value;
		set => _bars.Value = value;
	}

	/// <summary>
	/// Required number of consecutive broken lows for entry.
	/// </summary>
	public int NumberOfLows {
		get => _numberOfLows.Value;
		set => _numberOfLows.Value = value;
	}

	/// <summary>
	/// Start date for trading.
	/// </summary>
	public DateTimeOffset StartDate {
		get => _startDate.Value;
		set => _startDate.Value = value;
	}

	/// <summary>
	/// End date for trading.
	/// </summary>
	public DateTimeOffset EndDate {
		get => _endDate.Value;
		set => _endDate.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType {
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see
	/// cref="MerovinhMeanReversionLowestLowStrategy"/>.
	/// </summary>
	public MerovinhMeanReversionLowestLowStrategy() {
		_bars = Param(nameof(Bars), 9)
					.SetGreaterThanZero()
					.SetDisplay("Bars", "Lookback length", "General")
					.SetCanOptimize(true);

		_numberOfLows =
			Param(nameof(NumberOfLows), 1)
				.SetRange(1, 4)
				.SetDisplay("Number Of Lows", "Required broken lows", "General")
				.SetCanOptimize(true);

		_startDate =
			Param(nameof(StartDate),
				  new DateTimeOffset(2021, 7, 27, 0, 0, 0, TimeSpan.Zero))
				.SetDisplay("Start Date", "Start date", "Time");

		_endDate =
			Param(nameof(EndDate),
				  new DateTimeOffset(2030, 12, 31, 23, 59, 0, TimeSpan.Zero))
				.SetDisplay("End Date", "End date", "Time");

		_candleType =
			Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)>
	GetWorkingSecurities() {
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted() {
		base.OnReseted();

		_prevLow = 0;
		_prevLow2 = 0;
		_prevLow3 = 0;
		_prevLow4 = 0;
		_prevHigh = 0;

		_highest = null!;
		_lowest = null!;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time) {
		base.OnStarted(time);

		_highest = new Highest { Length = Bars };
		_lowest = new Lowest { Length = Bars };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_highest, _lowest, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null) {
			DrawCandles(area, subscription);
			DrawIndicator(area, _lowest);
			DrawIndicator(area, _highest);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal highestHigh,
							   decimal lowestLow) {
		if (candle.State != CandleStates.Finished)
			return;

		var inPeriod = candle.OpenTime > StartDate && candle.OpenTime < EndDate;

		if (inPeriod && _prevLow > 0 && lowestLow < _prevLow) {
			var condition = NumberOfLows switch {
				1 => true,
				2 => _prevLow < _prevLow2,
				3 => _prevLow < _prevLow2 && _prevLow2 < _prevLow3,
				4 => _prevLow < _prevLow2 && _prevLow2 < _prevLow3 &&
					 _prevLow3 < _prevLow4,
				_ => false
			};

			if (condition)
				BuyMarket();
		}

		if (inPeriod && _prevHigh > 0 && highestHigh > _prevHigh)
			ClosePosition();

		if (_prevLow != lowestLow) {
			_prevLow4 = _prevLow3;
			_prevLow3 = _prevLow2;
			_prevLow2 = _prevLow;
			_prevLow = lowestLow;
		}

		_prevHigh = highestHigh;
	}
}
