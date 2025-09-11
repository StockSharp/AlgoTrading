using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi-timeframe trend following strategy using fast and slow EMAs across 5,
/// 15 and 30 minute timeframes. Enters long when all timeframes are bullish and
/// price is above the 200 EMA on 5 minute. Exits when any timeframe turns
/// bearish or price drops below the 200 EMA.
/// </summary>
public class MultiTimeframeTrend200EmaFilterLongsOnlyStrategy : Strategy {
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _ema200Length;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<DataType> _candleType;

	private EMA _fast5;
	private EMA _slow5;
	private EMA _ema200;
	private EMA _fast15;
	private EMA _slow15;
	private EMA _fast30;
	private EMA _slow30;

	private int _trend5;
	private int _trend15;
	private int _trend30;

	/// <summary>
	/// Fast EMA period.
	/// </summary>
	public int FastLength {
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Slow EMA period.
	/// </summary>
	public int SlowLength {
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// 200 EMA filter length.
	/// </summary>
	public int Ema200Length {
		get => _ema200Length.Value;
		set => _ema200Length.Value = value;
	}

	/// <summary>
	/// Stop loss in percent.
	/// </summary>
	public decimal StopLossPercent {
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Take profit in percent.
	/// </summary>
	public decimal TakeProfitPercent {
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Base candle type.
	/// </summary>
	public DataType CandleType {
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see
	/// cref="MultiTimeframeTrend200EmaFilterLongsOnlyStrategy"/> class.
	/// </summary>
	public MultiTimeframeTrend200EmaFilterLongsOnlyStrategy() {
		_fastLength =
			Param(nameof(FastLength), 9)
				.SetGreaterThanZero()
				.SetDisplay("Fast EMA Length", "Fast EMA period", "Parameters");

		_slowLength =
			Param(nameof(SlowLength), 21)
				.SetGreaterThanZero()
				.SetDisplay("Slow EMA Length", "Slow EMA period", "Parameters");

		_ema200Length = Param(nameof(Ema200Length), 200)
							.SetGreaterThanZero()
							.SetDisplay("200 EMA Length",
										"200 EMA filter length", "Parameters");

		_stopLossPercent =
			Param(nameof(StopLossPercent), 1m)
				.SetGreaterThanZero()
				.SetDisplay("Stop Loss %", "Stop loss percent", "Risk");

		_takeProfitPercent =
			Param(nameof(TakeProfitPercent), 3m)
				.SetGreaterThanZero()
				.SetDisplay("Take Profit %", "Take profit percent", "Risk");

		_candleType =
			Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Base timeframe", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)>
	GetWorkingSecurities() {
		yield return (Security, CandleType);
		yield return (Security, TimeSpan.FromMinutes(15).TimeFrame());
		yield return (Security, TimeSpan.FromMinutes(30).TimeFrame());
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time) {
		base.OnStarted(time);

		_fast5 = new EMA { Length = FastLength };
		_slow5 = new EMA { Length = SlowLength };
		_ema200 = new EMA { Length = Ema200Length };

		_fast15 = new EMA { Length = FastLength };
		_slow15 = new EMA { Length = SlowLength };

		_fast30 = new EMA { Length = FastLength };
		_slow30 = new EMA { Length = SlowLength };

		StartProtection(new Unit(TakeProfitPercent, UnitTypes.Percent),
						new Unit(StopLossPercent, UnitTypes.Percent));

		var sub5 = SubscribeCandles(CandleType);
		sub5.Bind(_fast5, _slow5, _ema200, ProcessCandle5).Start();

		var sub15 = SubscribeCandles(TimeSpan.FromMinutes(15).TimeFrame());
		sub15.Bind(_fast15, _slow15, ProcessCandle15).Start();

		var sub30 = SubscribeCandles(TimeSpan.FromMinutes(30).TimeFrame());
		sub30.Bind(_fast30, _slow30, ProcessCandle30).Start();

		var area = CreateChartArea();
		if (area != null) {
			DrawCandles(area, sub5);
			DrawIndicator(area, _fast5);
			DrawIndicator(area, _slow5);
			DrawIndicator(area, _ema200);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle5(ICandleMessage candle, decimal fast,
								decimal slow, decimal ema200) {
		if (candle.State != CandleStates.Finished)
			return;

		if (!_fast5.IsFormed || !_slow5.IsFormed || !_ema200.IsFormed ||
			_trend15 == 0 || _trend30 == 0)
			return;

		_trend5 = fast > slow ? 1 : -1;

		var combined = _trend5 + _trend15 + _trend30;
		var price = candle.ClosePrice;

		var enterLong = combined == 3 && price > ema200;
		var exitLong = combined < 3 || price < ema200;

		if (enterLong && Position <= 0) {
			CancelActiveOrders();
			BuyMarket(Volume + Math.Abs(Position));
		} else if (exitLong && Position > 0) {
			CancelActiveOrders();
			SellMarket(Position);
		}
	}

	private void ProcessCandle15(ICandleMessage candle, decimal fast,
								 decimal slow) {
		if (candle.State != CandleStates.Finished)
			return;

		if (!_fast15.IsFormed || !_slow15.IsFormed)
			return;

		_trend15 = fast > slow ? 1 : -1;
	}

	private void ProcessCandle30(ICandleMessage candle, decimal fast,
								 decimal slow) {
		if (candle.State != CandleStates.Finished)
			return;

		if (!_fast30.IsFormed || !_slow30.IsFormed)
			return;

		_trend30 = fast > slow ? 1 : -1;
	}
}
