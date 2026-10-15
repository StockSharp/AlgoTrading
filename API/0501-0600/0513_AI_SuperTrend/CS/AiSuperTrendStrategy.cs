using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// AI SuperTrend strategy.
/// Goes long when the SuperTrend flips up while the WMA of price is above the WMA of the SuperTrend line, and short on the
/// mirrored setup, reversing an opposite position. A position is closed when the SuperTrend turns against it or when price
/// hits an ATR trailing stop that follows the close at AtrFactor times ATR.
/// </summary>
public class AiSuperTrendStrategy : Strategy
{
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrFactor;
	private readonly StrategyParam<int> _priceWmaLength;
	private readonly StrategyParam<int> _superWmaLength;
	private readonly StrategyParam<bool> _enableLong;
	private readonly StrategyParam<bool> _enableShort;
	private readonly StrategyParam<DataType> _candleType;

	private WeightedMovingAverage _superWma;
	private bool? _prevIsUpTrend;
	private decimal _trailingStop;

	/// <summary>
	/// ATR period of the SuperTrend and the trailing stop.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier of the SuperTrend and the trailing stop.
	/// </summary>
	public decimal AtrFactor
	{
		get => _atrFactor.Value;
		set => _atrFactor.Value = value;
	}

	/// <summary>
	/// Period of the price WMA.
	/// </summary>
	public int PriceWmaLength
	{
		get => _priceWmaLength.Value;
		set => _priceWmaLength.Value = value;
	}

	/// <summary>
	/// Period of the WMA of the SuperTrend line.
	/// </summary>
	public int SuperWmaLength
	{
		get => _superWmaLength.Value;
		set => _superWmaLength.Value = value;
	}

	/// <summary>
	/// Allow long entries.
	/// </summary>
	public bool EnableLong
	{
		get => _enableLong.Value;
		set => _enableLong.Value = value;
	}

	/// <summary>
	/// Allow short entries.
	/// </summary>
	public bool EnableShort
	{
		get => _enableShort.Value;
		set => _enableShort.Value = value;
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
	/// Constructor.
	/// </summary>
	public AiSuperTrendStrategy()
	{
		_atrPeriod = Param(nameof(AtrPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period of the SuperTrend and the trailing stop", "SuperTrend");

		_atrFactor = Param(nameof(AtrFactor), 3m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Factor", "ATR multiplier of the SuperTrend and the trailing stop", "SuperTrend");

		_priceWmaLength = Param(nameof(PriceWmaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Price WMA Length", "Period of the price WMA", "Filter");

		_superWmaLength = Param(nameof(SuperWmaLength), 100)
			.SetGreaterThanZero()
			.SetDisplay("SuperTrend WMA Length", "Period of the WMA of the SuperTrend line", "Filter");

		_enableLong = Param(nameof(EnableLong), true)
			.SetDisplay("Enable Long", "Allow long entries", "Trading");

		_enableShort = Param(nameof(EnableShort), true)
			.SetDisplay("Enable Short", "Allow short entries", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_superWma = null;
		_prevIsUpTrend = null;
		_trailingStop = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevIsUpTrend = null;
		_trailingStop = 0m;

		var superTrend = new SuperTrend { Length = AtrPeriod, Multiplier = AtrFactor };
		var priceWma = new WeightedMovingAverage { Length = PriceWmaLength };
		var atr = new AverageTrueRange { Length = AtrPeriod };
		_superWma = new WeightedMovingAverage { Length = SuperWmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(superTrend, priceWma, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, superTrend);
			DrawIndicator(area, priceWma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue superTrendValue, IIndicatorValue priceWmaValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!superTrendValue.IsFormed || superTrendValue is not SuperTrendIndicatorValue st)
			return;

		var superWma = _superWma.Process(st.ToDecimal(), candle.ServerTime, true).ToDecimal();

		var isUpTrend = st.IsUpTrend;
		var prevIsUpTrend = _prevIsUpTrend;
		_prevIsUpTrend = isUpTrend;

		if (prevIsUpTrend is not bool prevUp || !priceWmaValue.IsFormed || !atrValue.IsFormed || !_superWma.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var priceWma = priceWmaValue.ToDecimal();
		var atr = atrValue.ToDecimal();
		var close = candle.ClosePrice;
		var distance = AtrFactor * atr;

		var flipUp = !prevUp && isUpTrend;
		var flipDown = prevUp && !isUpTrend;

		if (EnableLong && flipUp && priceWma > superWma && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_trailingStop = close - distance;
		}
		else if (EnableShort && flipDown && priceWma < superWma && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_trailingStop = close + distance;
		}
		else if (Position > 0)
		{
			if (candle.LowPrice <= _trailingStop || !isUpTrend)
				SellMarket(Position);
			else
				_trailingStop = Math.Max(_trailingStop, close - distance);
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _trailingStop || isUpTrend)
				BuyMarket(-Position);
			else
				_trailingStop = Math.Min(_trailingStop, close + distance);
		}
	}
}
