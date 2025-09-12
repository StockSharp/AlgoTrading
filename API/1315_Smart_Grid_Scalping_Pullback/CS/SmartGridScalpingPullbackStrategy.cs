using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Smart Grid Scalping Pullback strategy.
/// Grid-based scalping using ATR levels and RSI pullback filters.
/// </summary>
public class SmartGridScalpingPullbackStrategy : Strategy
{
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _gridFactor;
	private readonly StrategyParam<decimal> _profitTarget;
	private readonly StrategyParam<decimal> _noTradeZone;
	private readonly StrategyParam<int> _shortLevel;
	private readonly StrategyParam<int> _longLevel;
	private readonly StrategyParam<int> _minRsiShort;
	private readonly StrategyParam<int> _maxRsiLong;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private bool _trailing;
	private decimal _highestPrice;
	private decimal _lowestPrice;

	/// <summary>
	/// ATR period length.
	/// </summary>
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }

	/// <summary>
	/// Grid spacing factor.
	/// </summary>
	public decimal GridFactor { get => _gridFactor.Value; set => _gridFactor.Value = value; }

	/// <summary>
	/// Profit target as percentage.
	/// </summary>
	public decimal ProfitTarget { get => _profitTarget.Value; set => _profitTarget.Value = value; }

	/// <summary>
	/// Minimum candle range relative to price.
	/// </summary>
	public decimal NoTradeZone { get => _noTradeZone.Value; set => _noTradeZone.Value = value; }

	/// <summary>
	/// Grid level index triggering short entry.
	/// </summary>
	public int ShortLevel { get => _shortLevel.Value; set => _shortLevel.Value = value; }

	/// <summary>
	/// Grid level index triggering long entry.
	/// </summary>
	public int LongLevel { get => _longLevel.Value; set => _longLevel.Value = value; }

	/// <summary>
	/// Minimum RSI value for short entries.
	/// </summary>
	public int MinRsiShort { get => _minRsiShort.Value; set => _minRsiShort.Value = value; }

	/// <summary>
	/// Maximum RSI value for long entries.
	/// </summary>
	public int MaxRsiLong { get => _maxRsiLong.Value; set => _maxRsiLong.Value = value; }

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initialize <see cref="SmartGridScalpingPullbackStrategy"/>.
	/// </summary>
	public SmartGridScalpingPullbackStrategy()
	{
		_atrLength = Param(nameof(AtrLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR calculation period", "General")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_gridFactor = Param(nameof(GridFactor), 0.35m)
			.SetGreaterThanZero()
			.SetDisplay("Grid Factor", "Spacing factor for grid levels", "General")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 1m, 0.05m);

		_profitTarget = Param(nameof(ProfitTarget), 0.004m)
			.SetGreaterThanZero()
			.SetDisplay("Profit Target", "Desired profit target", "General")
			.SetCanOptimize(true)
			.SetOptimize(0.001m, 0.01m, 0.001m);

		_noTradeZone = Param(nameof(NoTradeZone), 0.003m)
			.SetGreaterThanZero()
			.SetDisplay("No-Trade Zone", "Minimum candle range to allow trades", "General")
			.SetCanOptimize(true)
			.SetOptimize(0.001m, 0.01m, 0.001m);

		_shortLevel = Param(nameof(ShortLevel), 5)
			.SetDisplay("Short Level", "Grid index for short", "General")
			.SetCanOptimize(true)
			.SetOptimize(0, 14, 1);

		_longLevel = Param(nameof(LongLevel), 5)
			.SetDisplay("Long Level", "Grid index for long", "General")
			.SetCanOptimize(true)
			.SetOptimize(0, 14, 1);

		_minRsiShort = Param(nameof(MinRsiShort), 70)
			.SetDisplay("Min RSI Short", "Minimum RSI for short entries", "General")
			.SetCanOptimize(true)
			.SetOptimize(50, 90, 5);

		_maxRsiLong = Param(nameof(MaxRsiLong), 30)
			.SetDisplay("Max RSI Long", "Maximum RSI for long entries", "General")
			.SetCanOptimize(true)
			.SetOptimize(10, 50, 5);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type and timeframe of candles", "General");
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

		_entryPrice = 0m;
		_trailing = false;
		_highestPrice = 0m;
		_lowestPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var atr = new AverageTrueRange { Length = AtrLength };
		var rsi = new RelativeStrengthIndex { Length = 14 };
		var shift = new Shift { Length = 20 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(atr, rsi, shift, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, atr);
			DrawIndicator(area, rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue, decimal rsiValue, decimal basePrice)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var shortLevelPrice = basePrice + (ShortLevel + 1) * atrValue * GridFactor;
		var longLevelPrice = basePrice - (LongLevel + 1) * atrValue * GridFactor;

		var shortCond = candle.ClosePrice > shortLevelPrice &&
			(candle.HighPrice - candle.LowPrice) / candle.HighPrice > NoTradeZone &&
			rsiValue > MinRsiShort && candle.ClosePrice < candle.OpenPrice;

		var longCond = candle.ClosePrice < longLevelPrice &&
			(candle.HighPrice - candle.LowPrice) / candle.LowPrice > NoTradeZone &&
			rsiValue < MaxRsiLong && candle.ClosePrice > candle.OpenPrice;

		if (shortCond && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_entryPrice = candle.ClosePrice;
			_trailing = false;
			_lowestPrice = _entryPrice;
		}
		else if (longCond && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_entryPrice = candle.ClosePrice;
			_trailing = false;
			_highestPrice = _entryPrice;
		}

		if (Position > 0)
		{
			_highestPrice = Math.Max(_highestPrice, candle.HighPrice);
			var target = _entryPrice * (1 + ProfitTarget);
			if (candle.HighPrice >= target)
			{
				SellMarket(Position);
				return;
			}

			if (!_trailing && candle.ClosePrice >= _entryPrice * (1 + ProfitTarget * 0.5m))
				_trailing = true;

			if (_trailing)
			{
				var stopPrice = _highestPrice - atrValue;
				if (candle.LowPrice <= stopPrice)
					SellMarket(Position);
			}
		}
		else if (Position < 0)
		{
			_lowestPrice = Math.Min(_lowestPrice, candle.LowPrice);
			var target = _entryPrice * (1 - ProfitTarget);
			if (candle.LowPrice <= target)
			{
				BuyMarket(Math.Abs(Position));
				return;
			}

			if (!_trailing && candle.ClosePrice <= _entryPrice * (1 - ProfitTarget * 0.5m))
				_trailing = true;

			if (_trailing)
			{
				var stopPrice = _lowestPrice + atrValue;
				if (candle.HighPrice >= stopPrice)
					BuyMarket(Math.Abs(Position));
			}
		}
	}
}
