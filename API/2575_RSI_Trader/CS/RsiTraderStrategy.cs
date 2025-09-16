using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSI trader strategy aligning price and RSI moving average trends.
/// </summary>
public class RsiTraderStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _shortRsiMaPeriod;
	private readonly StrategyParam<int> _longRsiMaPeriod;
	private readonly StrategyParam<int> _shortPriceMaPeriod;
	private readonly StrategyParam<int> _longPriceMaPeriod;
	private readonly StrategyParam<bool> _reverse;
	private readonly StrategyParam<DataType> _candleType;

	private RSI _rsi = null!;
	private SMA _rsiShortSma = null!;
	private SMA _rsiLongSma = null!;
	private SMA _priceShortSma = null!;
	private WeightedMovingAverage _priceLongWma = null!;

	/// <summary>
	/// RSI calculation period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set
		{
			_rsiPeriod.Value = value;
			if (_rsi != null)
				_rsi.Length = value;
		}
	}

	/// <summary>
	/// Short moving average period applied to RSI.
	/// </summary>
	public int ShortRsiMaPeriod
	{
		get => _shortRsiMaPeriod.Value;
		set
		{
			_shortRsiMaPeriod.Value = value;
			if (_rsiShortSma != null)
				_rsiShortSma.Length = value;
		}
	}

	/// <summary>
	/// Long moving average period applied to RSI.
	/// </summary>
	public int LongRsiMaPeriod
	{
		get => _longRsiMaPeriod.Value;
		set
		{
			_longRsiMaPeriod.Value = value;
			if (_rsiLongSma != null)
				_rsiLongSma.Length = value;
		}
	}

	/// <summary>
	/// Short moving average period for price trend detection.
	/// </summary>
	public int ShortPriceMaPeriod
	{
		get => _shortPriceMaPeriod.Value;
		set
		{
			_shortPriceMaPeriod.Value = value;
			if (_priceShortSma != null)
				_priceShortSma.Length = value;
		}
	}

	/// <summary>
	/// Long moving average period for price trend detection.
	/// </summary>
	public int LongPriceMaPeriod
	{
		get => _longPriceMaPeriod.Value;
		set
		{
			_longPriceMaPeriod.Value = value;
			if (_priceLongWma != null)
				_priceLongWma.Length = value;
		}
	}

	/// <summary>
	/// Executes opposite orders when enabled.
	/// </summary>
	public bool Reverse
	{
		get => _reverse.Value;
		set => _reverse.Value = value;
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
	/// Initializes default parameters.
	/// </summary>
	public RsiTraderStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetDisplay("RSI Period", "RSI calculation length", "RSI")
			.SetCanOptimize(true)
			.SetOptimize(5, 40, 1);

		_shortRsiMaPeriod = Param(nameof(ShortRsiMaPeriod), 9)
			.SetDisplay("Short RSI MA", "Short moving average on RSI", "RSI")
			.SetCanOptimize(true)
			.SetOptimize(3, 20, 1);

		_longRsiMaPeriod = Param(nameof(LongRsiMaPeriod), 45)
			.SetDisplay("Long RSI MA", "Long moving average on RSI", "RSI")
			.SetCanOptimize(true)
			.SetOptimize(20, 100, 5);

		_shortPriceMaPeriod = Param(nameof(ShortPriceMaPeriod), 9)
			.SetDisplay("Short Price MA", "Short simple moving average", "Price")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 1);

		_longPriceMaPeriod = Param(nameof(LongPriceMaPeriod), 45)
			.SetDisplay("Long Price MA", "Long weighted moving average", "Price")
			.SetCanOptimize(true)
			.SetOptimize(20, 100, 5);

		_reverse = Param(nameof(Reverse), false)
			.SetDisplay("Reverse", "Flip buy/sell signals", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary candle type", "Data");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_rsi?.Reset();
		_rsiShortSma?.Reset();
		_rsiLongSma?.Reset();
		_priceShortSma?.Reset();
		_priceLongWma?.Reset();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_rsi = new RSI { Length = RsiPeriod };
		_rsiShortSma = new SMA { Length = ShortRsiMaPeriod };
		_rsiLongSma = new SMA { Length = LongRsiMaPeriod };
		_priceShortSma = new SMA { Length = ShortPriceMaPeriod };
		_priceLongWma = new WeightedMovingAverage { Length = LongPriceMaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_priceShortSma, _priceLongWma, ProcessCandle)
			.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal priceShort, decimal priceLong)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var rsiValue = _rsi.Process(candle);
		if (!rsiValue.IsFinal || !_rsi.IsFormed)
			return;

		var rsiShortValue = _rsiShortSma.Process(rsiValue);
		if (!rsiShortValue.IsFinal || !_rsiShortSma.IsFormed)
			return;

		var rsiLongValue = _rsiLongSma.Process(rsiValue);
		if (!rsiLongValue.IsFinal || !_rsiLongSma.IsFormed)
			return;

		if (!_priceShortSma.IsFormed || !_priceLongWma.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (rsiShortValue is not DecimalIndicatorValue rsiShortDecimal ||
			rsiLongValue is not DecimalIndicatorValue rsiLongDecimal)
			return;

		var rsiShort = rsiShortDecimal.Value;
		var rsiLong = rsiLongDecimal.Value;

		var goLong = priceShort > priceLong && rsiShort > rsiLong;
		var goShort = priceShort < priceLong && rsiShort < rsiLong;
		var sideways = (priceShort > priceLong && rsiShort < rsiLong) || (priceShort < priceLong && rsiShort > rsiLong);

		if (sideways && Position != 0)
		{
			// Close position when price and RSI trends disagree.
			if (Position > 0)
				SellMarket(Position);
			else
				BuyMarket(-Position);

			return;
		}

		if (Position != 0)
			return;

		if (goLong)
		{
			// Align long entries with both bullish price and RSI slopes.
			if (Reverse)
				SellMarket();
			else
				BuyMarket();
		}
		else if (goShort)
		{
			// Enter short trades when both price and RSI confirm the downtrend.
			if (Reverse)
				BuyMarket();
			else
				SellMarket();
		}
	}
}
