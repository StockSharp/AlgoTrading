using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Super Woodies CCI strategy.
/// Opens long when CCI crosses above zero and short when crosses below.
/// </summary>
public class SuperWoodiesCciStrategy : Strategy
{
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _allowLongEntry;
	private readonly StrategyParam<bool> _allowShortEntry;
	private readonly StrategyParam<bool> _allowLongExit;
	private readonly StrategyParam<bool> _allowShortExit;

	private CommodityChannelIndex _cci = null!;
	private bool _hasPrev;
	private bool _wasPositive;

	/// <summary>
	/// CCI calculation period.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool AllowLongEntry
	{
		get => _allowLongEntry.Value;
		set => _allowLongEntry.Value = value;
	}

	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool AllowShortEntry
	{
		get => _allowShortEntry.Value;
		set => _allowShortEntry.Value = value;
	}

	/// <summary>
	/// Allow closing long positions when CCI goes below zero.
	/// </summary>
	public bool AllowLongExit
	{
		get => _allowLongExit.Value;
		set => _allowLongExit.Value = value;
	}

	/// <summary>
	/// Allow closing short positions when CCI goes above zero.
	/// </summary>
	public bool AllowShortExit
	{
		get => _allowShortExit.Value;
		set => _allowShortExit.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SuperWoodiesCciStrategy"/>.
	/// </summary>
	public SuperWoodiesCciStrategy()
	{
		_cciPeriod = Param(nameof(CciPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "CCI lookback length", "General")
			.SetCanOptimize(true)
			.SetOptimize(20, 100, 10);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_allowLongEntry = Param(nameof(AllowLongEntry), true)
			.SetDisplay("Allow Long Entry", "Enable opening long positions", "Trading");

		_allowShortEntry = Param(nameof(AllowShortEntry), true)
			.SetDisplay("Allow Short Entry", "Enable opening short positions", "Trading");

		_allowLongExit = Param(nameof(AllowLongExit), true)
			.SetDisplay("Allow Long Exit", "Enable closing long positions", "Trading");

		_allowShortExit = Param(nameof(AllowShortExit), true)
			.SetDisplay("Allow Short Exit", "Enable closing short positions", "Trading");
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
		_hasPrev = false;
		_wasPositive = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_cci = new CommodityChannelIndex { Length = CciPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_cci, ProcessCandle)
			.Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _cci);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal cciValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_cci.IsFormed || !IsFormedAndOnlineAndAllowTrading())
			return;

		var isPositive = cciValue > 0m;

		if (_hasPrev && isPositive != _wasPositive)
		{
			if (isPositive)
			{
				if (AllowLongEntry && Position <= 0)
					BuyMarket(Volume + Math.Abs(Position));
			}
			else
			{
				if (AllowShortEntry && Position >= 0)
					SellMarket(Volume + Math.Abs(Position));
			}
		}
		else
		{
			if (isPositive)
			{
				if (AllowShortExit && Position < 0)
					BuyMarket(Math.Abs(Position));
			}
			else
			{
				if (AllowLongExit && Position > 0)
					SellMarket(Math.Abs(Position));
			}
		}

		_wasPositive = isPositive;
		_hasPrev = true;
	}
}
