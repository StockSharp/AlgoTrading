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
/// Potential entries strategy built around two-candle reversal and momentum patterns.
/// Evaluates the most recent finished candle pair and opens trades in the selected direction.
/// </summary>
public class PotentialEntriesStrategy : Strategy
{
	private readonly StrategyParam<PatternModes> _patternMode;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<DataType> _candleType;

	private CandleSnapshot? _previousCandle;
	private decimal? _longStopPrice;
	private decimal? _shortStopPrice;

	/// <summary>
	/// Pattern direction to evaluate.
	/// </summary>
	public PatternModes PatternSide
	{
		get => _patternMode.Value;
		set => _patternMode.Value = value;
	}

	/// <summary>
	/// Market order volume used for entries.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Candle type used for pattern recognition.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="PotentialEntriesStrategy"/>.
	/// </summary>
	public PotentialEntriesStrategy()
	{
		_patternMode = Param(nameof(PatternSide), PatternModes.Bullish)
		.SetDisplay("Pattern Side", "Candlestick direction to scan", "General")
		.SetCanOptimize(true);

		_tradeVolume = Param(nameof(TradeVolume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Trade Volume", "Volume for market orders", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles for analysis", "General");
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

		_previousCandle = null;
		_longStopPrice = null;
		_shortStopPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;

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
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (HandleStops(candle))
		return;

		var current = CandleSnapshot.FromCandle(candle);

		if (_previousCandle is CandleSnapshot previous)
		{
			if (PatternSide == PatternModes.Bullish)
			{
				if (IsBullishHammer(current, previous))
				{
					EnterLong(current, previous);
				}
				else if (IsBullishInvertedHammer(current, previous))
				{
					EnterLong(current, previous);
				}
				else if (IsBullishMomentum(current, previous))
				{
					EnterLong(current, previous);
				}
			}
			else if (PatternSide == PatternModes.Bearish)
			{
				if (IsBearishShootingStar(current, previous))
				{
					EnterShort(current, previous);
				}
				else if (IsBearishHangingMan(current, previous))
				{
					EnterShort(current, previous);
				}
				else if (IsBearishMomentum(current, previous))
				{
					EnterShort(current, previous);
				}
			}
		}

		_previousCandle = current;
	}

	private bool HandleStops(ICandleMessage candle)
	{
		var exitTriggered = false;

		if (Position > 0)
		{
			if (_longStopPrice is decimal longStop && candle.LowPrice <= longStop)
			{
				SellMarket(Position);
				_longStopPrice = null;
				exitTriggered = true;
			}
		}
		else
		{
			_longStopPrice = null;
		}

		if (Position < 0)
		{
			if (_shortStopPrice is decimal shortStop && candle.HighPrice >= shortStop)
			{
				BuyMarket(Math.Abs(Position));
				_shortStopPrice = null;
				exitTriggered = true;
			}
		}
		else
		{
			_shortStopPrice = null;
		}

		return exitTriggered;
	}

	private void EnterLong(CandleSnapshot current, CandleSnapshot previous)
	{
		if (Position < 0)
		BuyMarket(Math.Abs(Position));

		if (Position <= 0)
		{
			BuyMarket(TradeVolume);
			_longStopPrice = Math.Min(current.Low, previous.Low);
			_shortStopPrice = null;
		}
	}

	private void EnterShort(CandleSnapshot current, CandleSnapshot previous)
	{
		if (Position > 0)
		SellMarket(Position);

		if (Position >= 0)
		{
			SellMarket(TradeVolume);
			_shortStopPrice = Math.Max(current.High, previous.High);
			_longStopPrice = null;
		}
	}

	private static bool IsBullishHammer(CandleSnapshot current, CandleSnapshot previous)
	{
		return current.IsBullish
		&& previous.IsBearish
		&& current.Body * 2m < current.LowerWick
		&& current.LowerWick > current.UpperWick * 3m;
	}

	private static bool IsBullishInvertedHammer(CandleSnapshot current, CandleSnapshot previous)
	{
		return current.IsBullish
		&& previous.IsBearish
		&& current.Body * 2m < current.UpperWick
		&& current.LowerWick * 3m < current.UpperWick;
	}

	private static bool IsBullishMomentum(CandleSnapshot current, CandleSnapshot previous)
	{
		return current.IsBullish
		&& previous.IsBullish
		&& current.Range > previous.Range
		&& current.Body >= previous.Body * 2m;
	}

	private static bool IsBearishShootingStar(CandleSnapshot current, CandleSnapshot previous)
	{
		return current.IsBearish
		&& previous.IsBullish
		&& current.Body * 2m < current.UpperWick
		&& current.LowerWick * 3m < current.UpperWick;
	}

	private static bool IsBearishHangingMan(CandleSnapshot current, CandleSnapshot previous)
	{
		return current.IsBearish
		&& previous.IsBullish
		&& current.Body * 2m < current.LowerWick
		&& current.LowerWick > current.UpperWick * 3m;
	}

	private static bool IsBearishMomentum(CandleSnapshot current, CandleSnapshot previous)
	{
		return current.IsBearish
		&& previous.IsBearish
		&& current.Body > previous.Body
		&& current.Range >= previous.Range * 2m;
	}

	/// <summary>
	/// Snapshot of relevant candle attributes to replicate the original MQL logic.
	/// </summary>
	private readonly struct CandleSnapshot
	{
		public CandleSnapshot(decimal open, decimal high, decimal low, decimal close)
		{
			Open = open;
			High = high;
			Low = low;
			Close = close;
			Range = High - Low;
			Body = Math.Abs(Close - Open);
			UpperWick = High - Math.Max(Open, Close);
			LowerWick = Math.Min(Open, Close) - Low;
			IsBullish = Close > Open;
			IsBearish = Close < Open;
		}

		public decimal Open { get; }
		public decimal High { get; }
		public decimal Low { get; }
		public decimal Close { get; }
		public decimal Range { get; }
		public decimal Body { get; }
		public decimal UpperWick { get; }
		public decimal LowerWick { get; }
		public bool IsBullish { get; }
		public bool IsBearish { get; }

		public static CandleSnapshot FromCandle(ICandleMessage candle)
		{
			return new CandleSnapshot(candle.OpenPrice, candle.HighPrice, candle.LowPrice, candle.ClosePrice);
		}
	}

	/// <summary>
	/// Directional mode for candlestick patterns.
	/// </summary>
	public enum PatternModes
	{
		/// <summary>
		/// Evaluate bullish reversal and momentum signals only.
		/// </summary>
		Bullish = 1,

		/// <summary>
		/// Evaluate bearish reversal and momentum signals only.
		/// </summary>
		Bearish = 2
	}
}

