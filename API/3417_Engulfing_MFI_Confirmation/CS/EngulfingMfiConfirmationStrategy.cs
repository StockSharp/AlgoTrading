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
/// Engulfing pattern strategy confirmed by Money Flow Index (MFI).
/// Replicates the MetaTrader Expert_ABE_BE_MFI voting logic using StockSharp high level API.
/// </summary>
public class EngulfingMfiConfirmationStrategy : Strategy
{
	private readonly StrategyParam<int> _mfiPeriod;
	private readonly StrategyParam<decimal> _oversoldLevel;
	private readonly StrategyParam<decimal> _overboughtLevel;
	private readonly StrategyParam<decimal> _exitLongLevel;
	private readonly StrategyParam<decimal> _exitShortLevel;
	private readonly StrategyParam<DataType> _candleType;

	private ICandleMessage _previousCandle;
	private decimal? _previousMfi;

	/// <summary>
	/// MFI calculation period.
	/// </summary>
	public int MfiPeriod
	{
		get => _mfiPeriod.Value;
		set => _mfiPeriod.Value = value;
	}

	/// <summary>
	/// MFI level used to confirm bullish engulfing entries.
	/// </summary>
	public decimal OversoldLevel
	{
		get => _oversoldLevel.Value;
		set => _oversoldLevel.Value = value;
	}

	/// <summary>
	/// MFI level used to confirm bearish engulfing entries.
	/// </summary>
	public decimal OverboughtLevel
	{
		get => _overboughtLevel.Value;
		set => _overboughtLevel.Value = value;
	}

	/// <summary>
	/// Lower MFI threshold for detecting momentum reversals.
	/// </summary>
	public decimal ExitLongLevel
	{
		get => _exitLongLevel.Value;
		set => _exitLongLevel.Value = value;
	}

	/// <summary>
	/// Upper MFI threshold for detecting momentum reversals.
	/// </summary>
	public decimal ExitShortLevel
	{
		get => _exitShortLevel.Value;
		set => _exitShortLevel.Value = value;
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
	/// Initializes a new instance of the <see cref="EngulfingMfiConfirmationStrategy"/> class.
	/// </summary>
	public EngulfingMfiConfirmationStrategy()
	{
		_mfiPeriod = Param(nameof(MfiPeriod), 37)
			.SetGreaterThanZero()
			.SetDisplay("MFI Period", "Length of the Money Flow Index", "Indicators")
			.SetCanOptimize(true);

		_oversoldLevel = Param(nameof(OversoldLevel), 40m)
			.SetDisplay("Oversold Level", "MFI level confirming bullish setups", "Indicators")
			.SetCanOptimize(true);

		_overboughtLevel = Param(nameof(OverboughtLevel), 60m)
			.SetDisplay("Overbought Level", "MFI level confirming bearish setups", "Indicators")
			.SetCanOptimize(true);

		_exitLongLevel = Param(nameof(ExitLongLevel), 30m)
			.SetDisplay("Exit Long Level", "Lower MFI threshold for exits", "Risk")
			.SetCanOptimize(true);

		_exitShortLevel = Param(nameof(ExitShortLevel), 70m)
			.SetDisplay("Exit Short Level", "Upper MFI threshold for exits", "Risk")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary candle series for analysis", "General");
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

		// Clear cached state when strategy is reset.
		_previousCandle = null;
		_previousMfi = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Initialize Money Flow Index indicator with configured period.
		var mfi = new MoneyFlowIndex
		{
			Length = MfiPeriod
		};

		// Subscribe to candle series and bind indicator calculations.
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(mfi, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal mfiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var previousCandle = _previousCandle;
		var previousMfi = _previousMfi;

		if (previousCandle != null)
		{
			// Determine bullish engulfing: previous bearish, current bullish, and current body engulfs previous body.
			var bullishEngulfing = previousCandle.ClosePrice < previousCandle.OpenPrice &&
				candle.ClosePrice > candle.OpenPrice &&
				candle.ClosePrice >= previousCandle.OpenPrice &&
				candle.OpenPrice <= previousCandle.ClosePrice;

			// Determine bearish engulfing: previous bullish, current bearish, and current body engulfs previous body.
			var bearishEngulfing = previousCandle.ClosePrice > previousCandle.OpenPrice &&
				candle.ClosePrice < candle.OpenPrice &&
				candle.ClosePrice <= previousCandle.OpenPrice &&
				candle.OpenPrice >= previousCandle.ClosePrice;

			if (bullishEngulfing && mfiValue < OversoldLevel)
			{
				// Close existing short and open fresh long.
				if (Position < 0m)
				{
					var coverVolume = Math.Abs(Position);
					if (coverVolume > 0m)
						BuyMarket(coverVolume);
				}

				if (Position <= 0m && Volume > 0m)
					BuyMarket(Volume);
			}
			else if (bearishEngulfing && mfiValue > OverboughtLevel)
			{
				// Close existing long and open fresh short.
				if (Position > 0m)
				{
					var exitVolume = Position;
					if (exitVolume > 0m)
						SellMarket(exitVolume);
				}

				if (Position >= 0m && Volume > 0m)
					SellMarket(Volume);
			}
		}

		if (previousMfi.HasValue)
		{
			// Detect MFI crosses to exit existing trades even without opposite patterns.
			var prev = previousMfi.Value;

			if (Position < 0m)
			{
				var crossedAboveLower = mfiValue > ExitLongLevel && prev <= ExitLongLevel;
				var crossedAboveUpper = mfiValue > ExitShortLevel && prev <= ExitShortLevel;

				if ((crossedAboveLower || crossedAboveUpper) && Math.Abs(Position) > 0m)
					BuyMarket(Math.Abs(Position));
			}
			else if (Position > 0m)
			{
				var crossedBelowUpper = mfiValue < ExitShortLevel && prev >= ExitShortLevel;
				var crossedBelowLower = mfiValue < ExitLongLevel && prev >= ExitLongLevel;

				if ((crossedBelowUpper || crossedBelowLower) && Position > 0m)
					SellMarket(Position);
			}
		}

		_previousMfi = mfiValue;
		_previousCandle = candle;
	}
}

