using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA cloud crossover strategy that trades long when short EMA crosses above long EMA
/// and short when short EMA crosses below long EMA.
/// </summary>
public class LongOnlyMtfEmaCloudStrategy : Strategy
{
	private readonly StrategyParam<int> _shortLength;
	private readonly StrategyParam<int> _longLength;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<DataType> _candleType;

	private EMA _shortEma;
	private EMA _longEma;
	private decimal _prevShort;
	private decimal _prevLong;
	private decimal _entryPrice;
	private bool _isInitialized;
	private int _cooldown;

	public int ShortLength { get => _shortLength.Value; set => _shortLength.Value = value; }
	public int LongLength { get => _longLength.Value; set => _longLength.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
	public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public LongOnlyMtfEmaCloudStrategy()
	{
		_shortLength = Param(nameof(ShortLength), 25)
			.SetGreaterThanZero()
			.SetDisplay("Short EMA", "Short EMA period", "Indicators");
		_longLength = Param(nameof(LongLength), 65)
			.SetGreaterThanZero()
			.SetDisplay("Long EMA", "Long EMA period", "Indicators");
		_stopLossPercent = Param(nameof(StopLossPercent), 7m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop loss percent", "Risk");
		_takeProfitPercent = Param(nameof(TakeProfitPercent), 12m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit %", "Take profit percent", "Risk");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(10).TimeFrame())
			.SetDisplay("Candle Type", "Candles", "General");
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

		_prevShort = default;
		_prevLong = default;
		_entryPrice = default;
		_isInitialized = false;
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_shortEma = new EMA { Length = ShortLength };
		_longEma = new EMA { Length = LongLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_shortEma, _longEma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _shortEma);
			DrawIndicator(area, _longEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal shortValue, decimal longValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_shortEma.IsFormed || !_longEma.IsFormed)
			return;

		if (!_isInitialized)
		{
			_prevShort = shortValue;
			_prevLong = longValue;
			_isInitialized = true;
			return;
		}

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevShort = shortValue;
			_prevLong = longValue;
			return;
		}

		var crossedUp = _prevShort <= _prevLong && shortValue > longValue;
		var crossedDown = _prevShort >= _prevLong && shortValue < longValue;

		// Close short and go long on bullish cross
		if (crossedUp && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));

			BuyMarket();
			_entryPrice = candle.ClosePrice;
			_cooldown = 10;
		}
		// Close long and go short on bearish cross
		else if (crossedDown && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));

			SellMarket();
			_entryPrice = candle.ClosePrice;
			_cooldown = 10;
		}

		// Stop loss / take profit for long
		if (Position > 0 && _entryPrice > 0)
		{
			var sl = _entryPrice * (1m - StopLossPercent / 100m);
			var tp = _entryPrice * (1m + TakeProfitPercent / 100m);

			if (candle.ClosePrice <= sl || candle.ClosePrice >= tp)
			{
				SellMarket(Math.Abs(Position));
				_cooldown = 20;
			}
		}

		// Stop loss / take profit for short
		if (Position < 0 && _entryPrice > 0)
		{
			var sl = _entryPrice * (1m + StopLossPercent / 100m);
			var tp = _entryPrice * (1m - TakeProfitPercent / 100m);

			if (candle.ClosePrice >= sl || candle.ClosePrice <= tp)
			{
				BuyMarket(Math.Abs(Position));
				_cooldown = 20;
			}
		}

		_prevShort = shortValue;
		_prevLong = longValue;
	}
}
