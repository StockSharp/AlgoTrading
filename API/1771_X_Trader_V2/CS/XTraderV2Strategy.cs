using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Contrarian moving average crossover strategy based on "X trader v2".
/// Sells when the fast MA crosses above the slow MA and buys on the opposite signal.
/// </summary>
public class XTraderV2Strategy : Strategy
{
	private readonly StrategyParam<int> _ma1Period;
	private readonly StrategyParam<int> _ma2Period;
	private readonly StrategyParam<int> _takeProfitTicks;
	private readonly StrategyParam<int> _stopLossTicks;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _ma1 = null!;
	private SimpleMovingAverage _ma2 = null!;

	private decimal _ma1Prev;
	private decimal _ma1Prev2;
	private decimal _ma2Prev;
	private decimal _ma2Prev2;

	private bool _hasPrev1;
	private bool _hasPrev2;
	private int _lastSignal; // 1 buy, -1 sell, 0 none

	/// <summary>
	/// First MA period.
	/// </summary>
	public int Ma1Period
	{
		get => _ma1Period.Value;
		set => _ma1Period.Value = value;
	}

	/// <summary>
	/// Second MA period.
	/// </summary>
	public int Ma2Period
	{
		get => _ma2Period.Value;
		set => _ma2Period.Value = value;
	}

	/// <summary>
	/// Take profit in ticks.
	/// </summary>
	public int TakeProfitTicks
	{
		get => _takeProfitTicks.Value;
		set => _takeProfitTicks.Value = value;
	}

	/// <summary>
	/// Stop loss in ticks.
	/// </summary>
	public int StopLossTicks
	{
		get => _stopLossTicks.Value;
		set => _stopLossTicks.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="XTraderV2Strategy"/>.
	/// </summary>
	public XTraderV2Strategy()
	{
		_ma1Period = Param(nameof(Ma1Period), 16)
			.SetGreaterThanZero()
			.SetDisplay("MA1 Period", "Period for the first moving average", "Indicators");

		_ma2Period = Param(nameof(Ma2Period), 1)
			.SetGreaterThanZero()
			.SetDisplay("MA2 Period", "Period for the second moving average", "Indicators");

		_takeProfitTicks = Param(nameof(TakeProfitTicks), 150)
			.SetDisplay("Take Profit", "Take profit in ticks", "Risk");

		_stopLossTicks = Param(nameof(StopLossTicks), 100)
			.SetDisplay("Stop Loss", "Stop loss in ticks", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ma1 = new SimpleMovingAverage { Length = Ma1Period };
		_ma2 = new SimpleMovingAverage { Length = Ma2Period };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_ma1, _ma2, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ma1);
			DrawIndicator(area, _ma2);
			DrawOwnTrades(area);
		}

		var step = Security.PriceStep ?? 1m;
		StartProtection(
			takeProfit: new Unit(TakeProfitTicks * step, UnitTypes.Point),
			stopLoss: new Unit(StopLossTicks * step, UnitTypes.Point));
	}
	private void ProcessCandle(ICandleMessage candle, decimal ma1, decimal ma2)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_hasPrev1)
		{
			_ma1Prev = ma1;
			_ma2Prev = ma2;
			_hasPrev1 = true;
			return;
		}

		if (!_hasPrev2)
		{
			_ma1Prev2 = _ma1Prev;
			_ma2Prev2 = _ma2Prev;
			_ma1Prev = ma1;
			_ma2Prev = ma2;
			_hasPrev2 = true;
			return;
		}

		if (Position == 0)
		{
			if (ma1 > ma2 && _ma1Prev > _ma2Prev && _ma1Prev2 < _ma2Prev2 && _lastSignal != -1)
			{
				SellMarket(Volume);
				_lastSignal = -1;
			}
			else if (ma1 < ma2 && _ma1Prev < _ma2Prev && _ma1Prev2 > _ma2Prev2 && _lastSignal != 1)
			{
				BuyMarket(Volume);
				_lastSignal = 1;
			}
		}

		_ma1Prev2 = _ma1Prev;
		_ma2Prev2 = _ma2Prev;
		_ma1Prev = ma1;
		_ma2Prev = ma2;
	}
}
