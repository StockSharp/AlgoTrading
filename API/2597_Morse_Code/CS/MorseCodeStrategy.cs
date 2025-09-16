using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Available Morse code style patterns where '1' is bullish and '0' is bearish.
/// </summary>
public enum MorsePatternMask
{
	_0 = 0,
	_1 = 1,
	_2 = 2,
	_3 = 3,
	_4 = 4,
	_5 = 5,
	_6 = 6,
	_7 = 7,
	_8 = 8,
	_9 = 9,
	_10 = 10,
	_11 = 11,
	_12 = 12,
	_13 = 13,
	_14 = 14,
	_15 = 15,
	_16 = 16,
	_17 = 17,
	_18 = 18,
	_19 = 19,
	_20 = 20,
	_21 = 21,
	_22 = 22,
	_23 = 23,
	_24 = 24,
	_25 = 25,
	_26 = 26,
	_27 = 27,
	_28 = 28,
	_29 = 29,
	_30 = 30,
	_31 = 31,
	_32 = 32,
	_33 = 33,
	_34 = 34,
	_35 = 35,
	_36 = 36,
	_37 = 37,
	_38 = 38,
	_39 = 39,
	_40 = 40,
	_41 = 41,
	_42 = 42,
	_43 = 43,
	_44 = 44,
	_45 = 45,
	_46 = 46,
	_47 = 47,
	_48 = 48,
	_49 = 49,
	_50 = 50,
	_51 = 51,
	_52 = 52,
	_53 = 53,
	_54 = 54,
	_55 = 55,
	_56 = 56,
	_57 = 57,
	_58 = 58,
	_59 = 59,
	_60 = 60,
	_61 = 61
}

/// <summary>
/// Strategy that trades when a selected Morse code candle pattern appears.
/// </summary>
public class MorseCodeStrategy : Strategy
{
	private static readonly string[] PatternValues = new[]
	{
		"0",
		"1",
		"00",
		"01",
		"10",
		"11",
		"000",
		"001",
		"010",
		"011",
		"100",
		"101",
		"110",
		"111",
		"0000",
		"0001",
		"0010",
		"0011",
		"0100",
		"0101",
		"0110",
		"0111",
		"1000",
		"1001",
		"1010",
		"1011",
		"1100",
		"1101",
		"1110",
		"1111",
		"00000",
		"00000",
		"00010",
		"00011",
		"00100",
		"00101",
		"00111",
		"00111",
		"01000",
		"01001",
		"01010",
		"01011",
		"01100",
		"01101",
		"01110",
		"01111",
		"10000",
		"10001",
		"10010",
		"10011",
		"10100",
		"10101",
		"10110",
		"10111",
		"11000",
		"11001",
		"11010",
		"11011",
		"11100",
		"11101",
		"11110",
		"11111"
	};

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<MorsePatternMask> _patternMask;
	private readonly StrategyParam<Sides> _direction;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _stopLossPips;

	private string _patternText = string.Empty;
	private int _patternLength;
	private int _maskLimit;
	private int _bullMask;
	private int _bearMask;
	private int _processedBars;
	private decimal _pipSize;
	private decimal _takeProfitDistance;
	private decimal _stopLossDistance;

	/// <summary>
	/// Initializes a new instance of the <see cref="MorseCodeStrategy"/> class.
	/// </summary>
	public MorseCodeStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for candle analysis", "General");

		_patternMask = Param(nameof(Pattern), MorsePatternMask._0)
			.SetDisplay("Pattern", "Morse code pattern where 1= bullish and 0 = bearish", "Pattern");

		_direction = Param(nameof(Direction), Sides.Buy)
			.SetDisplay("Direction", "Side to trade when the pattern appears", "Trading");

		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit (pips)", "Distance from entry to take profit in pips", "Risk Management");

		_stopLossPips = Param(nameof(StopLossPips), 50m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss (pips)", "Distance from entry to stop loss in pips", "Risk Management");
	}

	/// <summary>
	/// Candle type used for the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Selected Morse code pattern.
	/// </summary>
	public MorsePatternMask Pattern
	{
		get => _patternMask.Value;
		set => _patternMask.Value = value;
	}

	/// <summary>
	/// Trade direction used when the pattern is detected.
	/// </summary>
	public Sides Direction
	{
		get => _direction.Value;
		set => _direction.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
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

		_patternText = string.Empty;
		_patternLength = 0;
		_maskLimit = 0;
		_bullMask = 0;
		_bearMask = 0;
		_processedBars = 0;
		_pipSize = 0m;
		_takeProfitDistance = 0m;
		_stopLossDistance = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_patternText = GetPatternText(Pattern);
		_patternLength = _patternText.Length;
		if (_patternLength == 0)
			throw new InvalidOperationException("Pattern cannot be empty.");

		_maskLimit = (1 << _patternLength) - 1;
		_bullMask = 0;
		_bearMask = 0;
		_processedBars = 0;

		_pipSize = CalculatePipSize();
		_takeProfitDistance = TakeProfitPips * _pipSize;
		_stopLossDistance = StopLossPips * _pipSize;

		// Configure automatic take profit and stop loss handling
		StartProtection(
			takeProfit: new Unit(_takeProfitDistance, UnitTypes.Price),
			stopLoss: new Unit(_stopLossDistance, UnitTypes.Price),
			useMarketOrders: true);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Only act on completed candles
		if (candle.State != CandleStates.Finished)
			return;

		UpdatePatternMasks(candle);

		// Wait until enough candles were processed to match the pattern
		if (_processedBars < _patternLength)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!IsPatternMatched())
			return;

		var closePrice = candle.ClosePrice;

		if (Direction == Sides.Buy)
		{
			if (Position > 0m)
				return; // Already in a long position

			EnterLong(closePrice);
		}
		else
		{
			if (Position < 0m)
				return; // Already in a short position

			EnterShort(closePrice);
		}
	}

	private static string GetPatternText(MorsePatternMask mask)
	{
		var index = (int)mask;
		if (index < 0 || index >= PatternValues.Length)
			throw new ArgumentOutOfRangeException(nameof(mask));

		return PatternValues[index];
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 1m;
		if (step <= 0m)
			return 1m;

		var value = step;
		var digits = 0;
		while (value < 1m && digits < 10)
		{
			value *= 10m;
			digits++;
		}

		if (digits == 3 || digits == 5)
			step *= 10m;

		return step;
	}

	private void UpdatePatternMasks(ICandleMessage candle)
	{
		if (_patternLength == 0)
			return;

		var strictBull = candle.ClosePrice > candle.OpenPrice;
		var strictBear = candle.ClosePrice < candle.OpenPrice;

		_bullMask = ((_bullMask << 1) | (strictBull ? 1 : 0)) & _maskLimit;
		_bearMask = ((_bearMask << 1) | (strictBear ? 1 : 0)) & _maskLimit;

		if (_processedBars < _patternLength)
			_processedBars++;
	}

	private bool IsPatternMatched()
	{
		for (var i = 0; i < _patternLength; i++)
		{
			var expected = _patternText[i];
			var isStrictBull = ((_bullMask >> i) & 1) == 1;
			var isStrictBear = ((_bearMask >> i) & 1) == 1;

			if (expected == '1')
			{
				if (isStrictBear)
					return false; // Pattern expects bullish or neutral candle
			}
			else
			{
				if (isStrictBull)
					return false; // Pattern expects bearish or neutral candle
			}
		}

		return true;
	}

	private void EnterLong(decimal price)
	{
		var volume = Volume;
		if (volume <= 0m)
			return;

		if (Position < 0m)
			volume += Math.Abs(Position); // Close short position and flip long

		BuyMarket(volume);
		LogInfo($"Entered long position with volume {volume} at price {price}.");
	}

	private void EnterShort(decimal price)
	{
		var volume = Volume;
		if (volume <= 0m)
			return;

		if (Position > 0m)
			volume += Math.Abs(Position); // Close long position and flip short

		SellMarket(volume);
		LogInfo($"Entered short position with volume {volume} at price {price}.");
	}
}
