using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bollinger Bands and EMA strategy.
/// Buys when price closes below the lower Bollinger Band and sells when price closes above the upper band.
/// Exits using a fixed stop based on a wider Bollinger Band and a profit target at EMA.
/// </summary>
public class BollingerEmaStatsStrategy : Strategy
{
	private readonly StrategyParam<int> _bbLength;
	private readonly StrategyParam<decimal> _entryMultiplier;
	private readonly StrategyParam<decimal> _stopMultiplier;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _stopPrice;
	private decimal _targetPrice;

	/// <summary>
	/// Bollinger Bands period length.
	/// </summary>
	public int BbLength
	{
		get => _bbLength.Value;
		set => _bbLength.Value = value;
	}

	/// <summary>
	/// Standard deviation multiplier for entry bands.
	/// </summary>
	public decimal EntryMultiplier
	{
		get => _entryMultiplier.Value;
		set => _entryMultiplier.Value = value;
	}

	/// <summary>
	/// Standard deviation multiplier for stop bands.
	/// </summary>
	public decimal StopMultiplier
	{
		get => _stopMultiplier.Value;
		set => _stopMultiplier.Value = value;
	}

	/// <summary>
	/// EMA period for exit target.
	/// </summary>
	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	/// <summary>
	/// Candle type used for analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="BollingerEmaStatsStrategy"/>.
	/// </summary>
	public BollingerEmaStatsStrategy()
	{
		_bbLength = Param(nameof(BbLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("BB Length", "Bollinger Bands period", "Bollinger Bands")
			.SetCanOptimize(true)
			.SetOptimize(10, 50, 5);

		_entryMultiplier = Param(nameof(EntryMultiplier), 2.0m)
			.SetDisplay("Entry StdDev Mult", "StdDev multiplier for entry bands", "Bollinger Bands")
			.SetCanOptimize(true)
			.SetOptimize(1.0m, 4.0m, 0.5m);

		_stopMultiplier = Param(nameof(StopMultiplier), 3.0m)
			.SetDisplay("Stop StdDev Mult", "StdDev multiplier for stop bands", "Bollinger Bands")
			.SetCanOptimize(true)
			.SetOptimize(2.0m, 5.0m, 0.5m);

		_emaPeriod = Param(nameof(EmaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA Exit Period", "EMA period for exit", "EMA")
			.SetCanOptimize(true)
			.SetOptimize(10, 50, 5);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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
		_stopPrice = 0m;
		_targetPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var bbEntry = new BollingerBands { Length = BbLength, Width = EntryMultiplier };
		var bbStop = new BollingerBands { Length = BbLength, Width = StopMultiplier };
		var ema = new EMA { Length = EmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(bbEntry, bbStop, ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bbEntry);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle,
		decimal entryMiddle, decimal entryUpper, decimal entryLower,
		decimal stopMiddle, decimal stopUpper, decimal stopLower,
		decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position == 0)
		{
			if (candle.ClosePrice < entryLower)
			{
				BuyMarket();
				_stopPrice = stopLower;
				_targetPrice = emaValue;
			}
			else if (candle.ClosePrice > entryUpper)
			{
				SellMarket();
				_stopPrice = stopUpper;
				_targetPrice = emaValue;
			}
		}
		else if (Position > 0)
		{
			if (_targetPrice != 0m &&
				(candle.ClosePrice >= _targetPrice || candle.ClosePrice <= _stopPrice))
			{
				SellMarket(Position);
				_stopPrice = 0m;
				_targetPrice = 0m;
			}
		}
		else if (Position < 0)
		{
			if (_targetPrice != 0m &&
				(candle.ClosePrice <= _targetPrice || candle.ClosePrice >= _stopPrice))
			{
				BuyMarket(Math.Abs(Position));
				_stopPrice = 0m;
				_targetPrice = 0m;
			}
		}
	}
}
