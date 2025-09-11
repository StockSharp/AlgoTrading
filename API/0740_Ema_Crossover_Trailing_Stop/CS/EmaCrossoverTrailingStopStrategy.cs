using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA crossover strategy with trailing stop.
/// </summary>
public class EmaCrossoverTrailingStopStrategy : Strategy
{
	private readonly StrategyParam<int> _shortLength;
	private readonly StrategyParam<int> _longLength;
	private readonly StrategyParam<decimal> _trailStopPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _highestPrice;
	private decimal _lowestPrice;
	private bool _wasFastLessThanSlow;
	private bool _isInitialized;

	private EMA _fastEma;
	private EMA _slowEma;

	/// <summary>
	/// Short EMA period.
	/// </summary>
	public int ShortLength
	{
		get => _shortLength.Value;
		set => _shortLength.Value = value;
	}

	/// <summary>
	/// Long EMA period.
	/// </summary>
	public int LongLength
	{
		get => _longLength.Value;
		set => _longLength.Value = value;
	}

	/// <summary>
	/// Trailing stop percent.
	/// </summary>
	public decimal TrailStopPercent
	{
		get => _trailStopPercent.Value;
		set => _trailStopPercent.Value = value;
	}

	/// <summary>
	/// Candle data type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public EmaCrossoverTrailingStopStrategy()
	{
		_shortLength = Param(nameof(ShortLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("Short EMA Length", "Period of the short EMA", "General")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_longLength = Param(nameof(LongLength), 21)
			.SetGreaterThanZero()
			.SetDisplay("Long EMA Length", "Period of the long EMA", "General")
			.SetCanOptimize(true)
			.SetOptimize(20, 50, 5);

		_trailStopPercent = Param(nameof(TrailStopPercent), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Stop %", "Percent for trailing stop", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 5m, 0.5m);

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

		_highestPrice = 0m;
		_lowestPrice = 0m;
		_wasFastLessThanSlow = false;
		_isInitialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastEma = new EMA { Length = ShortLength };
		_slowEma = new EMA { Length = LongLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fastEma, _slowEma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastEma);
			DrawIndicator(area, _slowEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_fastEma.IsFormed || !_slowEma.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_isInitialized)
		{
			_wasFastLessThanSlow = fastValue < slowValue;
			_isInitialized = true;
			return;
		}

		var isFastLessThanSlow = fastValue < slowValue;

		if (_wasFastLessThanSlow && !isFastLessThanSlow)
		{
			if (Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
				_highestPrice = candle.ClosePrice;
				LogInfo($"Long entry at {candle.ClosePrice}");
			}
		}
		else if (!_wasFastLessThanSlow && isFastLessThanSlow)
		{
			if (Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
				_lowestPrice = candle.ClosePrice;
				LogInfo($"Short entry at {candle.ClosePrice}");
			}
		}

		_wasFastLessThanSlow = isFastLessThanSlow;

		var trailPercent = TrailStopPercent / 100m;

		if (Position > 0 && _highestPrice != 0m)
		{
			_highestPrice = Math.Max(_highestPrice, candle.ClosePrice);
			var stopPrice = _highestPrice * (1 - trailPercent);

			if (candle.ClosePrice < stopPrice)
			{
				SellMarket(Math.Abs(Position));
				LogInfo($"Long exit by trailing stop at {candle.ClosePrice}");
				_highestPrice = 0m;
			}
		}
		else if (Position < 0 && _lowestPrice != 0m)
		{
			_lowestPrice = Math.Min(_lowestPrice, candle.ClosePrice);
			var stopPrice = _lowestPrice * (1 + trailPercent);

			if (candle.ClosePrice > stopPrice)
			{
				BuyMarket(Math.Abs(Position));
				LogInfo($"Short exit by trailing stop at {candle.ClosePrice}");
				_lowestPrice = 0m;
			}
		}
	}
}
