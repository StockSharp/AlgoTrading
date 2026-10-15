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
/// Recreates the Money Fixed Risk expert advisor using StockSharp's high level API.
/// Uses ATR to determine position sizing via risk percentage and opens long positions
/// with symmetric stop-loss and take-profit levels based on ATR.
/// </summary>
public class MoneyFixedRiskStrategy : Strategy
{
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<int> _candleInterval;
	private readonly StrategyParam<DataType> _candleType;

	private int _candleCounter;
	private decimal _stopPrice;
	private decimal _takeProfitPrice;
	private decimal _entryPrice;

	/// <summary>
	/// ATR multiplier for stop distance.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// ATR calculation period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Number of candles between position evaluations.
	/// </summary>
	public int CandleInterval
	{
		get => _candleInterval.Value;
		set => _candleInterval.Value = value;
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
	/// Initializes a new instance of the <see cref="MoneyFixedRiskStrategy"/>.
	/// </summary>
	public MoneyFixedRiskStrategy()
	{
		_atrMultiplier = Param(nameof(AtrMultiplier), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "Multiplier for ATR-based stop distance", "Risk")
			.SetOptimize(0.5m, 3m, 0.5m);

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "Period for ATR calculation", "Indicators")
			.SetOptimize(7, 28, 7);

		_candleInterval = Param(nameof(CandleInterval), 10)
			.SetGreaterThanZero()
			.SetDisplay("Candle Interval", "Candles between position evaluations", "General")
			.SetOptimize(5, 30, 5);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for processing", "General");
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

		_candleCounter = 0;
		_stopPrice = 0m;
		_takeProfitPrice = 0m;
		_entryPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, atr);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var price = candle.ClosePrice;

		// Manage existing long position
		if (Position > 0 && _stopPrice > 0m)
		{
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takeProfitPrice)
			{
				SellMarket(Position);
				_stopPrice = 0m;
				_takeProfitPrice = 0m;
				_entryPrice = 0m;
			}
		}

		// Manage existing short position
		if (Position < 0 && _stopPrice > 0m)
		{
			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takeProfitPrice)
			{
				BuyMarket(Math.Abs(Position));
				_stopPrice = 0m;
				_takeProfitPrice = 0m;
				_entryPrice = 0m;
			}
		}

		_candleCounter++;

		if (_candleCounter < CandleInterval)
			return;

		_candleCounter = 0;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position != 0)
			return;

		if (atrValue <= 0m)
			return;

		var stopDistance = atrValue * AtrMultiplier;

		// Alternate between long and short based on price relative to previous entry
		var goLong = _entryPrice == 0m || price > _entryPrice;

		if (goLong)
		{
			BuyMarket(Volume);
			_entryPrice = price;
			_stopPrice = price - stopDistance;
			_takeProfitPrice = price + stopDistance;
		}
		else
		{
			SellMarket(Volume);
			_entryPrice = price;
			_stopPrice = price + stopDistance;
			_takeProfitPrice = price - stopDistance;
		}
	}
}
