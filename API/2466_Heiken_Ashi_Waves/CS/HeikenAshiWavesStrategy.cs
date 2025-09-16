using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Heiken Ashi Waves strategy combining Heikin-Ashi candles and moving averages.
/// Opens long positions when a bullish Heikin-Ashi candle aligns with a fast SMA crossing above a slow SMA.
/// Opens short positions on bearish candles when the fast SMA crosses below the slow SMA.
/// </summary>
public class HeikenAshiWavesStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<Unit> _stopLoss;
	private readonly StrategyParam<bool> _useTrailing;

	private decimal _prevFast;
	private decimal _prevSlow;
	private decimal _prevHaOpen;
	private decimal _prevHaClose;
	private bool _isInitialized;

	/// <summary>
	/// Fast SMA period.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Slow SMA period.
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
	/// Trailing stop loss distance.
	/// </summary>
	public Unit StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Enables trailing stop behavior.
	/// </summary>
	public bool UseTrailing
	{
		get => _useTrailing.Value;
		set => _useTrailing.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="HeikenAshiWavesStrategy"/>.
	/// </summary>
	public HeikenAshiWavesStrategy()
	{
		_fastLength = Param(nameof(FastLength), 2)
		.SetGreaterThanZero()
		.SetDisplay("Fast SMA", "Period of the fast moving average", "Parameters")
		.SetCanOptimize(true)
		.SetOptimize(1, 10, 1);

		_slowLength = Param(nameof(SlowLength), 30)
		.SetGreaterThanZero()
		.SetDisplay("Slow SMA", "Period of the slow moving average", "Parameters")
		.SetCanOptimize(true)
		.SetOptimize(20, 60, 5);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles for calculations", "General");

		_stopLoss = Param(nameof(StopLoss), new Unit(20, UnitTypes.Point))
		.SetDisplay("Stop Loss", "Trailing stop distance in points", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(10m, 50m, 10m);

		_useTrailing = Param(nameof(UseTrailing), true)
		.SetDisplay("Use Trailing", "Enable trailing stop protection", "Risk Management");
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
		_prevFast = _prevSlow = 0m;
		_prevHaOpen = _prevHaClose = 0m;
		_isInitialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection(
			takeProfit: null,
			stopLoss: StopLoss,
			isStopTrailing: UseTrailing,
			useMarketOrders: true
		);

		var fastSma = new SMA { Length = FastLength };
		var slowSma = new SMA { Length = SlowLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(fastSma, slowSma, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastSma);
			DrawIndicator(area, slowSma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		decimal haOpen;
		decimal haClose;

		if (!_isInitialized)
		{
		haOpen = (candle.OpenPrice + candle.ClosePrice) / 2m;
		haClose = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m;
		_prevHaOpen = haOpen;
		_prevHaClose = haClose;
		_prevFast = fastValue;
		_prevSlow = slowValue;
		_isInitialized = true;
		return;
		}

		haOpen = (_prevHaOpen + _prevHaClose) / 2m;
		haClose = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m;

		var isBullish = haClose > haOpen;
		var crossUp = _prevFast <= _prevSlow && fastValue > slowValue;
		var crossDown = _prevFast >= _prevSlow && fastValue < slowValue;

		if (isBullish && crossUp && Position <= 0)
		{
		BuyMarket(Volume + Math.Abs(Position));
		}
		else if (!isBullish && crossDown && Position >= 0)
		{
		SellMarket(Volume + Math.Abs(Position));
		}

		_prevFast = fastValue;
		_prevSlow = slowValue;
		_prevHaOpen = haOpen;
		_prevHaClose = haClose;
	}
}
