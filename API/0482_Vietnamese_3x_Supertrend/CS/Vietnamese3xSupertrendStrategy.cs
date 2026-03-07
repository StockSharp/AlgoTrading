namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Vietnamese 3x SuperTrend strategy.
/// Uses three SuperTrend indicators with different settings.
/// Enters long when SuperTrend conditions align.
/// Exits based on break-even, all-uptrend-red-candle, or avg price in loss.
/// </summary>
public class Vietnamese3xSupertrendStrategy : Strategy
{
	private readonly StrategyParam<int> _fastAtrLength;
	private readonly StrategyParam<decimal> _fastMultiplier;
	private readonly StrategyParam<int> _mediumAtrLength;
	private readonly StrategyParam<decimal> _mediumMultiplier;
	private readonly StrategyParam<int> _slowAtrLength;
	private readonly StrategyParam<decimal> _slowMultiplier;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _highestGreen;
	private bool _breakEvenActive;
	private decimal _avgEntryPrice;
	private int _entryCount;
	private int _cooldownRemaining;

	public int FastAtrLength { get => _fastAtrLength.Value; set => _fastAtrLength.Value = value; }
	public decimal FastMultiplier { get => _fastMultiplier.Value; set => _fastMultiplier.Value = value; }
	public int MediumAtrLength { get => _mediumAtrLength.Value; set => _mediumAtrLength.Value = value; }
	public decimal MediumMultiplier { get => _mediumMultiplier.Value; set => _mediumMultiplier.Value = value; }
	public int SlowAtrLength { get => _slowAtrLength.Value; set => _slowAtrLength.Value = value; }
	public decimal SlowMultiplier { get => _slowMultiplier.Value; set => _slowMultiplier.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }

	public Vietnamese3xSupertrendStrategy()
	{
		_fastAtrLength = Param(nameof(FastAtrLength), 10)
			.SetDisplay("Fast ATR Length", "ATR length for fast SuperTrend", "SuperTrend");

		_fastMultiplier = Param(nameof(FastMultiplier), 1m)
			.SetDisplay("Fast Multiplier", "ATR multiplier for fast SuperTrend", "SuperTrend");

		_mediumAtrLength = Param(nameof(MediumAtrLength), 11)
			.SetDisplay("Medium ATR Length", "ATR length for medium SuperTrend", "SuperTrend");

		_mediumMultiplier = Param(nameof(MediumMultiplier), 2m)
			.SetDisplay("Medium Multiplier", "ATR multiplier for medium SuperTrend", "SuperTrend");

		_slowAtrLength = Param(nameof(SlowAtrLength), 12)
			.SetDisplay("Slow ATR Length", "ATR length for slow SuperTrend", "SuperTrend");

		_slowMultiplier = Param(nameof(SlowMultiplier), 3m)
			.SetDisplay("Slow Multiplier", "ATR multiplier for slow SuperTrend", "SuperTrend");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_cooldownBars = Param(nameof(CooldownBars), 10)
			.SetDisplay("Cooldown Bars", "Bars between trades", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_highestGreen = 0;
		_breakEvenActive = false;
		_avgEntryPrice = 0;
		_entryCount = 0;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fast = new SuperTrend { Length = FastAtrLength, Multiplier = FastMultiplier };
		var medium = new SuperTrend { Length = MediumAtrLength, Multiplier = MediumMultiplier };
		var slow = new SuperTrend { Length = SlowAtrLength, Multiplier = SlowMultiplier };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(fast, medium, slow, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fast);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue fastVal, IIndicatorValue medVal, IIndicatorValue slowVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var fastSt = (SuperTrendIndicatorValue)fastVal;
		var medSt = (SuperTrendIndicatorValue)medVal;
		var slowSt = (SuperTrendIndicatorValue)slowVal;

		var dir1 = fastSt.IsUpTrend ? 1 : -1;
		var dir2 = medSt.IsUpTrend ? 1 : -1;
		var dir3 = slowSt.IsUpTrend ? 1 : -1;

		// Track highest green candle for breakout entry
		if (dir1 < 0 && _highestGreen == 0)
			_highestGreen = candle.HighPrice;
		if (_highestGreen > 0 && dir1 < 0)
			_highestGreen = Math.Max(_highestGreen, candle.HighPrice);
		if (dir1 >= 0)
			_highestGreen = 0;

		// Exit logic for longs
		if (Position > 0)
		{
			// Break-even stop
			if (dir1 > 0 && dir2 < 0 && dir3 < 0)
			{
				if (!_breakEvenActive && candle.LowPrice > _avgEntryPrice)
					_breakEvenActive = true;
				if (_breakEvenActive && candle.LowPrice <= _avgEntryPrice)
				{
					SellMarket(Math.Abs(Position));
					ResetEntries();
					_cooldownRemaining = CooldownBars;
					return;
				}
			}

			// All uptrend + red candle exit
			if (dir3 > 0 && dir2 > 0 && dir1 > 0 && candle.ClosePrice < candle.OpenPrice)
			{
				SellMarket(Math.Abs(Position));
				ResetEntries();
				_cooldownRemaining = CooldownBars;
				return;
			}

			// Avg price in loss exit
			if (_avgEntryPrice > candle.ClosePrice)
			{
				SellMarket(Math.Abs(Position));
				ResetEntries();
				_cooldownRemaining = CooldownBars;
				return;
			}
		}

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			return;
		}

		// Entry logic - max 3 entries
		if (_entryCount < 3)
		{
			if (dir3 < 0)
			{
				if (dir2 > 0 && dir1 < 0)
				{
					BuyMarket(Volume);
					AddEntry(candle.ClosePrice);
					_cooldownRemaining = CooldownBars;
				}
				else if (dir2 < 0 && candle.ClosePrice > fastSt.Value)
				{
					BuyMarket(Volume);
					AddEntry(candle.ClosePrice);
					_cooldownRemaining = CooldownBars;
				}
			}
			else
			{
				if (dir1 < 0 && _highestGreen > 0 && candle.ClosePrice > _highestGreen)
				{
					BuyMarket(Volume);
					AddEntry(candle.ClosePrice);
					_cooldownRemaining = CooldownBars;
				}
			}
		}
	}

	private void AddEntry(decimal price)
	{
		_avgEntryPrice = (_avgEntryPrice * _entryCount + price) / (_entryCount + 1);
		_entryCount++;
	}

	private void ResetEntries()
	{
		_avgEntryPrice = 0;
		_entryCount = 0;
		_breakEvenActive = false;
	}
}
