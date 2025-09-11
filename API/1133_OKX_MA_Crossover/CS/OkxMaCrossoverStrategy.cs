using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// OKX MA crossover strategy.
/// Enters long when price dips below the previous MA value.
/// Enters short when price rises above the previous MA value.
/// Uses take profit and stop loss percentages.
/// </summary>
public class OkxMaCrossoverStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DateTimeOffset> _startDate;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevMa;
	private bool _hasPrevMa;
	private bool _prevDoLong1;
	private bool _prevDoLong2;
	private bool _prevDoShort1;
	private bool _prevDoShort2;

	/// <summary>
	/// MA length.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Take profit percentage.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Stop loss percentage.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Ignore data before this date.
	/// </summary>
	public DateTimeOffset StartDate
	{
		get => _startDate.Value;
		set => _startDate.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="OkxMaCrossoverStrategy"/>.
	/// </summary>
	public OkxMaCrossoverStrategy()
	{
		_length = Param(nameof(Length), 13)
					  .SetGreaterThanZero()
					  .SetDisplay("MA Length", "Simple moving average length", "Parameters");

		_takeProfit = Param(nameof(TakeProfitPercent), 7m)
						  .SetDisplay("Take Profit %", "Take profit in percent", "Protection")
						  .SetRange(0.01m, 100m);

		_stopLoss = Param(nameof(StopLossPercent), 7m)
						.SetDisplay("Stop Loss %", "Stop loss in percent", "Protection")
						.SetRange(0.01m, 100m);

		_startDate = Param(nameof(StartDate), new DateTimeOffset(2022, 1, 1, 9, 30, 0, TimeSpan.Zero))
						 .SetDisplay("Start Date", "Ignore data before this date", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
						  .SetDisplay("Candle Type", "Type of candles for calculations", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_hasPrevMa = false;
		_prevDoLong1 = false;
		_prevDoLong2 = false;
		_prevDoShort1 = false;
		_prevDoShort2 = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var sma = new SimpleMovingAverage {
			Length = Length,
			CandlePrice = CandlePrice.Close,
		};

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(sma, ProcessCandle).Start();

		StartProtection(takeProfit: new Unit(TakeProfitPercent, UnitTypes.Percent),
						stopLoss: new Unit(StopLossPercent, UnitTypes.Percent));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (candle.OpenTime < StartDate)
		{
			_prevMa = maValue;
			_hasPrevMa = true;
			ShiftSignals(false, false);
			return;
		}

		if (!_hasPrevMa)
		{
			_prevMa = maValue;
			_hasPrevMa = true;
			ShiftSignals(false, false);
			return;
		}

		var doLong = candle.Low < _prevMa;
		var doShort = candle.High > _prevMa;

		if (!_prevDoLong2 && doLong && Position <= 0 && IsFormedAndOnlineAndAllowTrading())
			BuyMarket(Volume + Math.Abs(Position));
		else if (!_prevDoShort2 && doShort && Position >= 0 && IsFormedAndOnlineAndAllowTrading())
			SellMarket(Volume + Math.Abs(Position));

		_prevMa = maValue;
		ShiftSignals(doLong, doShort);
	}

	private void ShiftSignals(bool currentLong, bool currentShort)
	{
		_prevDoLong2 = _prevDoLong1;
		_prevDoLong1 = currentLong;
		_prevDoShort2 = _prevDoShort1;
		_prevDoShort1 = currentShort;
	}
}
