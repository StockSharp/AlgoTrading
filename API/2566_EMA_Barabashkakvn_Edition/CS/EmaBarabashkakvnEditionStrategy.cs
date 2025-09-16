using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA crossover strategy with virtual take profit and stop loss distances.
/// Converted from the MQL5 expert "EMA (barabashkakvn's edition)".
/// </summary>
public class EmaBarabashkakvnEditionStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _virtualProfitPips;
	private readonly StrategyParam<int> _moveBackPips;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<decimal> _pipSize;
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _fastEma;
	private ExponentialMovingAverage _slowEma;
	private bool _hasCrossSignal;
	private decimal? _prevFast;
	private decimal? _prevSlow;
	private decimal? _prevHigh;
	private decimal? _prevLow;
	private decimal? _entryPrice;
	private decimal? _virtualTarget;
	private decimal? _virtualStop;

	/// <summary>
	/// Order volume in lots.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Virtual take profit distance in pips.
	/// </summary>
	public int VirtualProfitPips
	{
		get => _virtualProfitPips.Value;
		set => _virtualProfitPips.Value = value;
	}

	/// <summary>
	/// Retracement distance after a crossover in pips.
	/// </summary>
	public int MoveBackPips
	{
		get => _moveBackPips.Value;
		set => _moveBackPips.Value = value;
	}

	/// <summary>
	/// Virtual stop loss distance in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Pip size in price units.
	/// </summary>
	public decimal PipSize
	{
		get => _pipSize.Value;
		set => _pipSize.Value = value;
	}

	/// <summary>
	/// Fast EMA length applied to median price.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Slow EMA length applied to median price.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
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
	/// Initializes a new instance of <see cref="EmaBarabashkakvnEditionStrategy"/>.
	/// </summary>
	public EmaBarabashkakvnEditionStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume in lots", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(0.05m, 1m, 0.05m);

		_virtualProfitPips = Param(nameof(VirtualProfitPips), 5)
			.SetGreaterThanZero()
			.SetDisplay("Virtual Profit", "Take profit distance in pips", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(2, 20, 1);

		_moveBackPips = Param(nameof(MoveBackPips), 3)
			.SetGreaterThanZero()
			.SetDisplay("Move Back", "Retracement after crossover in pips", "Entries")
			.SetCanOptimize(true)
			.SetOptimize(1, 10, 1);

		_stopLossPips = Param(nameof(StopLossPips), 20)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Virtual stop loss distance in pips", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(10, 60, 2);

		_pipSize = Param(nameof(PipSize), 0.0001m)
			.SetGreaterThanZero()
			.SetDisplay("Pip Size", "Instrument pip size in price units", "General");

		_fastLength = Param(nameof(FastLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA length on median price", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(3, 15, 1);

		_slowLength = Param(nameof(SlowLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA length on median price", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(8, 40, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Source candles", "General");
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

		_fastEma = default;
		_slowEma = default;
		_hasCrossSignal = false;
		_prevFast = default;
		_prevSlow = default;
		_prevHigh = default;
		_prevLow = default;
		_entryPrice = default;
		_virtualTarget = default;
		_virtualStop = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		_fastEma = new ExponentialMovingAverage { Length = FastLength };
		_slowEma = new ExponentialMovingAverage { Length = SlowLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastEma);
			DrawIndicator(area, _slowEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Calculate median price as in the original expert (PRICE_MEDIAN).
		var medianPrice = (candle.HighPrice + candle.LowPrice) / 2m;

		// Update EMA values using the median price.
		var fastValue = _fastEma.Process(medianPrice, candle.OpenTime, true);
		var slowValue = _slowEma.Process(medianPrice, candle.OpenTime, true);

		if (!_fastEma.IsFormed || !_slowEma.IsFormed)
		{
			_prevHigh = candle.HighPrice;
			_prevLow = candle.LowPrice;
			_prevFast = fastValue.ToDecimal();
			_prevSlow = slowValue.ToDecimal();
			return;
		}

		var fast = fastValue.ToDecimal();
		var slow = slowValue.ToDecimal();

		if (_prevFast is decimal prevFast && _prevSlow is decimal prevSlow)
		{
			var bullishCross = prevFast <= prevSlow && fast > slow;
			var bearishCross = prevFast >= prevSlow && fast < slow;

			if (bullishCross || bearishCross)
				_hasCrossSignal = true;
		}

		_prevFast = fast;
		_prevSlow = slow;

		var pipValue = PipSize;
		if (pipValue <= 0m)
			pipValue = Security?.PriceStep ?? 0.0001m;

		var moveBackPrice = MoveBackPips * pipValue;
		var profitDistance = VirtualProfitPips * pipValue;
		var stopDistance = StopLossPips * pipValue;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevHigh = candle.HighPrice;
			_prevLow = candle.LowPrice;
			return;
		}

		if (Position == 0 && _hasCrossSignal && _prevHigh is decimal prevHigh && _prevLow is decimal prevLow)
		{
			var bearishSpread = slow - fast;
			var bullishSpread = fast - slow;

			var bearishReady = bearishSpread > 2m * pipValue && candle.HighPrice >= prevLow + moveBackPrice;
			var bullishReady = bullishSpread > 2m * pipValue && candle.LowPrice <= prevHigh - moveBackPrice;

			if (bearishReady)
			{
				// Enter short after bearish cross and retracement above the previous low.
				_entryPrice = candle.ClosePrice;
				_virtualTarget = _entryPrice - profitDistance;
				_virtualStop = _entryPrice + stopDistance;
				SellMarket(OrderVolume);
				_hasCrossSignal = false;
			}
			else if (bullishReady)
			{
				// Enter long after bullish cross and retracement below the previous high.
				_entryPrice = candle.ClosePrice;
				_virtualTarget = _entryPrice + profitDistance;
				_virtualStop = _entryPrice - stopDistance;
				BuyMarket(OrderVolume);
				_hasCrossSignal = false;
			}
		}
		else if (Position != 0 && _entryPrice is decimal && _virtualTarget is decimal target && _virtualStop is decimal stop)
		{
			if (Position > 0)
			{
				// Long position: use high for profit target and low for stop.
				var hitTarget = candle.HighPrice >= target;
				var hitStop = candle.LowPrice <= stop;

				if (hitTarget || hitStop)
				{
					SellMarket(Math.Abs(Position));
					_hasCrossSignal = false;
					_entryPrice = null;
					_virtualTarget = null;
					_virtualStop = null;
				}
			}
			else if (Position < 0)
			{
				// Short position: use low for profit target and high for stop.
				var hitTarget = candle.LowPrice <= target;
				var hitStop = candle.HighPrice >= stop;

				if (hitTarget || hitStop)
				{
					BuyMarket(Math.Abs(Position));
					_hasCrossSignal = false;
					_entryPrice = null;
					_virtualTarget = null;
					_virtualStop = null;
				}
			}
		}

		if (Position == 0)
		{
			_entryPrice = null;
			_virtualTarget = null;
			_virtualStop = null;
		}

		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;
	}
}
