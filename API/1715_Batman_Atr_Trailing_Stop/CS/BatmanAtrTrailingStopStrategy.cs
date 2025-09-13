using System;
using System.Collections.Generic;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on ATR trailing stop similar to "Batman" EA.
/// Opens long when price breaks above ATR-based support.
/// Opens short when price breaks below ATR-based resistance.
/// </summary>
public class BatmanAtrTrailingStopStrategy : Strategy
{
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _factor;
	private readonly StrategyParam<bool> _useTypicalPrice;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _levelUp;
	private decimal? _levelDown;
	private int _direction;
	private bool _isInitialized;

	/// <summary>
	/// ATR period length.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Multiplier for ATR distance.
	/// </summary>
	public decimal Factor
	{
		get => _factor.Value;
		set => _factor.Value = value;
	}

	/// <summary>
	/// Use typical price (H+L+C)/3 instead of close price.
	/// </summary>
	public bool UseTypicalPrice
	{
		get => _useTypicalPrice.Value;
		set => _useTypicalPrice.Value = value;
	}

	/// <summary>
	/// The candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public BatmanAtrTrailingStopStrategy()
	{
		_atrPeriod = Param(nameof(AtrPeriod), 7)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR indicator period", "General")
			.SetCanOptimize(true)
			.SetOptimize(3, 14, 1);

		_factor = Param(nameof(Factor), 1.1m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Factor", "Multiplier for ATR distance", "General")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 3m, 0.1m);

		_useTypicalPrice = Param(nameof(UseTypicalPrice), false)
			.SetDisplay("Use Typical Price", "Use (H+L+C)/3 instead of close price", "General");

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
		_levelUp = null;
		_levelDown = null;
		_direction = 1;
		_isInitialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(atr, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var priceLevel = UseTypicalPrice
			? (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m
			: candle.ClosePrice;

		var currUp = priceLevel - atrValue * Factor;
		var currDown = priceLevel + atrValue * Factor;

		if (!_isInitialized)
		{
			_levelUp = currUp;
			_levelDown = currDown;
			_isInitialized = true;
			return;
		}

		if (_direction == 1)
		{
			if (currUp > _levelUp)
				_levelUp = currUp;

			if (candle.LowPrice < _levelUp)
			{
				_direction = -1;
				_levelDown = currDown;
				SellMarket();
			}
		}
		else
		{
			if (currDown < _levelDown)
				_levelDown = currDown;

			if (candle.HighPrice > _levelDown)
			{
				_direction = 1;
				_levelUp = currUp;
				BuyMarket();
			}
		}
	}
}
