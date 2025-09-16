using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simple strategy based on SMA of open prices and previous close comparison.
/// Buys when the previous close is below the slow SMA.
/// Sells short when the previous close is above the fast SMA.
/// Uses fixed take profit and stop loss levels for both directions.
/// </summary>
public class EscapeStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<decimal> _takeProfitLong;
	private readonly StrategyParam<decimal> _takeProfitShort;
	private readonly StrategyParam<decimal> _stopLossLong;
	private readonly StrategyParam<decimal> _stopLossShort;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage? _fastMa;
	private SimpleMovingAverage? _slowMa;
	private bool _initialized;
	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _takePrice;

	/// <summary>
	/// Length of fast SMA.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Length of slow SMA.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// Take profit distance for long positions in price units.
	/// </summary>
	public decimal TakeProfitLong
	{
		get => _takeProfitLong.Value;
		set => _takeProfitLong.Value = value;
	}

	/// <summary>
	/// Take profit distance for short positions in price units.
	/// </summary>
	public decimal TakeProfitShort
	{
		get => _takeProfitShort.Value;
		set => _takeProfitShort.Value = value;
	}

	/// <summary>
	/// Stop loss distance for long positions in price units.
	/// </summary>
	public decimal StopLossLong
	{
		get => _stopLossLong.Value;
		set => _stopLossLong.Value = value;
	}

	/// <summary>
	/// Stop loss distance for short positions in price units.
	/// </summary>
	public decimal StopLossShort
	{
		get => _stopLossShort.Value;
		set => _stopLossShort.Value = value;
	}

	/// <summary>
	/// Candle type to analyze.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public EscapeStrategy()
	{
		_fastLength = Param(nameof(FastLength), 4)
			.SetDisplay("Fast SMA Length", "Length of fast SMA", "Parameters")
			.SetGreaterThanZero()
			.SetCanOptimize(true);

		_slowLength = Param(nameof(SlowLength), 5)
			.SetDisplay("Slow SMA Length", "Length of slow SMA", "Parameters")
			.SetGreaterThanZero()
			.SetCanOptimize(true);

		_takeProfitLong = Param(nameof(TakeProfitLong), 25m)
			.SetDisplay("Take Profit Long", "Take profit for long trades", "Trading")
			.SetGreaterThanZero();

		_takeProfitShort = Param(nameof(TakeProfitShort), 26m)
			.SetDisplay("Take Profit Short", "Take profit for short trades", "Trading")
			.SetGreaterThanZero();

		_stopLossLong = Param(nameof(StopLossLong), 25m)
			.SetDisplay("Stop Loss Long", "Stop loss for long trades", "Trading")
			.SetGreaterThanZero();

		_stopLossShort = Param(nameof(StopLossShort), 3m)
			.SetDisplay("Stop Loss Short", "Stop loss for short trades", "Trading")
			.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for candles", "General");
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

		_fastMa = null;
		_slowMa = null;
		_initialized = false;
		_entryPrice = 0m;
		_stopPrice = 0m;
		_takePrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastMa = new SimpleMovingAverage { Length = FastLength };
		_slowMa = new SimpleMovingAverage { Length = SlowLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var fast = _fastMa!.Process(candle.OpenPrice, candle.OpenTime, true).ToDecimal();
		var slow = _slowMa!.Process(candle.OpenPrice, candle.OpenTime, true).ToDecimal();

		if (!_initialized)
		{
			_initialized = true;
			return;
		}

		var close = candle.ClosePrice;

		if (Position == 0)
		{
			if (close < slow)
			{
				BuyMarket();
				_entryPrice = close;
				_stopPrice = _entryPrice - StopLossLong;
				_takePrice = _entryPrice + TakeProfitLong;
			}
			else if (close > fast)
			{
				SellMarket();
				_entryPrice = close;
				_stopPrice = _entryPrice + StopLossShort;
				_takePrice = _entryPrice - TakeProfitShort;
			}
		}
		else if (Position > 0)
		{
			if (close >= _takePrice || candle.HighPrice >= _takePrice)
				SellMarket(Position);
			else if (close <= _stopPrice || candle.LowPrice <= _stopPrice)
				SellMarket(Position);
		}
		else if (Position < 0)
		{
			if (close <= _takePrice || candle.LowPrice <= _takePrice)
				BuyMarket(-Position);
			else if (close >= _stopPrice || candle.HighPrice >= _stopPrice)
				BuyMarket(-Position);
		}
	}
}
