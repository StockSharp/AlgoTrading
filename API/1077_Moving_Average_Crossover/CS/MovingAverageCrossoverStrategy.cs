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
/// Moving average crossover strategy.
/// Buys when the short SMA crosses above the long SMA and sells on the opposite signal.
/// </summary>
public class MovingAverageCrossoverStrategy : Strategy
{
	private readonly StrategyParam<int> _shortLength;
	private readonly StrategyParam<int> _longLength;
	private readonly StrategyParam<int> _cooldownCandles;
	private readonly StrategyParam<decimal> _minSpreadPercent;
	private readonly StrategyParam<DataType> _candleType;
	
	private decimal _prevShort;
	private decimal _prevLong;
	private bool _isFirst;
	private int _barIndex;
	private int _lastTradeBar = int.MinValue;
	
	/// <summary>
	/// Short SMA period length.
	/// </summary>
	public int ShortLength
	{
		get => _shortLength.Value;
		set => _shortLength.Value = value;
	}
	
	/// <summary>
	/// Long SMA period length.
	/// </summary>
	public int LongLength
	{
		get => _longLength.Value;
		set => _longLength.Value = value;
	}
	
	/// <summary>
	/// The type of candles to use for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Minimum finished candles between entries.
	/// </summary>
	public int CooldownCandles
	{
		get => _cooldownCandles.Value;
		set => _cooldownCandles.Value = value;
	}

	/// <summary>
	/// Minimum spread between moving averages in percent.
	/// </summary>
	public decimal MinSpreadPercent
	{
		get => _minSpreadPercent.Value;
		set => _minSpreadPercent.Value = value;
	}
	
	/// <summary>
	/// Initializes a new instance of the <see cref="MovingAverageCrossoverStrategy"/> class.
	/// </summary>
	public MovingAverageCrossoverStrategy()
	{
		_shortLength = Param(nameof(ShortLength), 10)
		.SetGreaterThanZero()
		.SetDisplay("Short MA Length", "Period of the short moving average", "General")
		
		.SetOptimize(5, 20, 1);
		
		_longLength = Param(nameof(LongLength), 34)
		.SetGreaterThanZero()
		.SetDisplay("Long MA Length", "Period of the long moving average", "General")
		
		.SetOptimize(20, 100, 5);

		_cooldownCandles = Param(nameof(CooldownCandles), 8)
		.SetGreaterThanZero()
		.SetDisplay("Cooldown Candles", "Finished candles between entries", "General");

		_minSpreadPercent = Param(nameof(MinSpreadPercent), 0.02m)
		.SetGreaterThanZero()
		.SetDisplay("Min Spread %", "Minimum MA spread on crossover", "General");
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe for analysis", "General");
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
		_prevShort = 0m;
		_prevLong = 0m;
		_isFirst = true;
		_barIndex = 0;
		_lastTradeBar = int.MinValue;
	}
	
	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		
		var shortMa = new SMA { Length = ShortLength };
		var longMa = new SMA { Length = LongLength };
		
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(shortMa, longMa, ProcessCandle).Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, shortMa);
			DrawIndicator(area, longMa);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal shortValue, decimal longValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barIndex++;
		
		if (!IsFormedAndOnlineAndAllowTrading())
			return;
		
		if (_isFirst)
		{
			_prevShort = shortValue;
			_prevLong = longValue;
			_isFirst = false;
			return;
		}
		
		var spreadPercent = candle.ClosePrice != 0m
			? Math.Abs(shortValue - longValue) / candle.ClosePrice * 100m
			: 0m;
		var canTrade = _barIndex - _lastTradeBar >= CooldownCandles;
		var crossUp = _prevShort <= _prevLong && shortValue > longValue && spreadPercent >= MinSpreadPercent;
		var crossDown = _prevShort >= _prevLong && shortValue < longValue && spreadPercent >= MinSpreadPercent;
		
		if (canTrade && crossUp && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_lastTradeBar = _barIndex;
		}
		else if (canTrade && crossDown && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_lastTradeBar = _barIndex;
		}
		
		_prevShort = shortValue;
		_prevLong = longValue;
	}
}
