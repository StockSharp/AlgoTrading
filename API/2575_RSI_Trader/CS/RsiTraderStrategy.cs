using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

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

	private decimal _prevRsiShort;
	private decimal _prevRsiLong;
	private bool _hasRsiValues;

	/// <summary>
	/// RSI calculation period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Short moving average period applied to RSI.
	/// </summary>
	public int ShortRsiMaPeriod
	{
		get => _shortRsiMaPeriod.Value;
		set => _shortRsiMaPeriod.Value = value;
	}

	/// <summary>
	/// Long moving average period applied to RSI.
	/// </summary>
	public int LongRsiMaPeriod
	{
		get => _longRsiMaPeriod.Value;
		set => _longRsiMaPeriod.Value = value;
	}

	/// <summary>
	/// Short moving average period for price trend detection.
	/// </summary>
	public int ShortPriceMaPeriod
	{
		get => _shortPriceMaPeriod.Value;
		set => _shortPriceMaPeriod.Value = value;
	}

	/// <summary>
	/// Long moving average period for price trend detection.
	/// </summary>
	public int LongPriceMaPeriod
	{
		get => _longPriceMaPeriod.Value;
		set => _longPriceMaPeriod.Value = value;
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
			
			.SetOptimize(5, 40, 1);

		_shortRsiMaPeriod = Param(nameof(ShortRsiMaPeriod), 9)
			.SetDisplay("Short RSI MA", "Short moving average on RSI", "RSI")
			
			.SetOptimize(3, 20, 1);

		_longRsiMaPeriod = Param(nameof(LongRsiMaPeriod), 45)
			.SetDisplay("Long RSI MA", "Long moving average on RSI", "RSI")
			
			.SetOptimize(20, 100, 5);

		_shortPriceMaPeriod = Param(nameof(ShortPriceMaPeriod), 9)
			.SetDisplay("Short Price MA", "Short simple moving average", "Price")
			
			.SetOptimize(5, 30, 1);

		_longPriceMaPeriod = Param(nameof(LongPriceMaPeriod), 45)
			.SetDisplay("Long Price MA", "Long weighted moving average", "Price")
			
			.SetOptimize(20, 100, 5);

		_reverse = Param(nameof(Reverse), false)
			.SetDisplay("Reverse", "Flip buy/sell signals", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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

		_prevRsiShort = 0m;
		_prevRsiLong = 0m;
		_hasRsiValues = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var rsi = new RSI { Length = RsiPeriod };
		var rsiShortSma = new SMA { Length = ShortRsiMaPeriod };
		var rsiLongSma = new SMA { Length = LongRsiMaPeriod };
		var priceShortSma = new SMA { Length = ShortPriceMaPeriod };
		var priceLongWma = new ExponentialMovingAverage { Length = LongPriceMaPeriod };

		var subscription = SubscribeCandles(CandleType);

		// Bind RSI chain: RSI -> short/long SMA on RSI
		subscription.Bind(rsi, (candle, rsiValue) =>
		{
			if (candle.State != CandleStates.Finished)
				return;

			var rsiInput = new DecimalIndicatorValue(rsiShortSma, rsiValue, candle.OpenTime) { IsFinal = true };
			var shortResult = rsiShortSma.Process(rsiInput);
			var longResult = rsiLongSma.Process(new DecimalIndicatorValue(rsiLongSma, rsiValue, candle.OpenTime) { IsFinal = true });

			if (!rsiShortSma.IsFormed || !rsiLongSma.IsFormed)
				return;

			if (shortResult is not DecimalIndicatorValue shortDec || longResult is not DecimalIndicatorValue longDec)
				return;

			_prevRsiShort = shortDec.Value;
			_prevRsiLong = longDec.Value;
			_hasRsiValues = true;
		});

		subscription.Bind(priceShortSma, priceLongWma, ProcessCandle);
		subscription.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal priceShort, decimal priceLong)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_hasRsiValues)
			return;

		var rsiShort = _prevRsiShort;
		var rsiLong = _prevRsiLong;

		var goLong = priceShort > priceLong && rsiShort > rsiLong;
		var goShort = priceShort < priceLong && rsiShort < rsiLong;
		var sideways = (priceShort > priceLong && rsiShort < rsiLong) || (priceShort < priceLong && rsiShort > rsiLong);

		if (sideways && Position != 0)
		{
			if (Position > 0)
				SellMarket();
			else
				BuyMarket();

			return;
		}

		if (Position != 0)
			return;

		if (goLong)
		{
			if (Reverse)
				SellMarket();
			else
				BuyMarket();
		}
		else if (goShort)
		{
			if (Reverse)
				BuyMarket();
			else
				SellMarket();
		}
	}
}