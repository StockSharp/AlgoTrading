using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Four WMA strategy with TP and SL.
/// A long opens when Long MA1 crosses above Long MA2 and a short when Short MA1 crosses below Short MA2, in the directions Direction
/// allows; an opposite entry reverses the position. With EnableTpSl percent take profit and stop loss protect each position. With
/// EnableAltExit a long also closes when the close crosses below the moving average chosen by AltExitMaOption and a short when it
/// crosses above it.
/// </summary>
public class FourWmaTpSlStrategy : Strategy
{
	/// <summary>
	/// Moving average types.
	/// </summary>
	public enum MaTypes
	{
		/// <summary>
		/// Simple moving average.
		/// </summary>
		Sma,

		/// <summary>
		/// Exponential moving average.
		/// </summary>
		Ema,

		/// <summary>
		/// Weighted moving average.
		/// </summary>
		Wma,

		/// <summary>
		/// Hull moving average.
		/// </summary>
		Hma,

		/// <summary>
		/// Smoothed moving average.
		/// </summary>
		Rma,
	}

	/// <summary>
	/// Allowed trade directions.
	/// </summary>
	public enum TradeDirections
	{
		/// <summary>
		/// Long trades only.
		/// </summary>
		Long,

		/// <summary>
		/// Short trades only.
		/// </summary>
		Short,

		/// <summary>
		/// Both directions.
		/// </summary>
		Both,
	}

	/// <summary>
	/// Moving averages the alternate exit can use.
	/// </summary>
	public enum AltExitMaOptions
	{
		/// <summary>
		/// Long MA1.
		/// </summary>
		LongMa1,

		/// <summary>
		/// Long MA2.
		/// </summary>
		LongMa2,

		/// <summary>
		/// Short MA1.
		/// </summary>
		ShortMa1,

		/// <summary>
		/// Short MA2.
		/// </summary>
		ShortMa2,
	}

	private readonly StrategyParam<int> _longMa1Length;
	private readonly StrategyParam<int> _longMa2Length;
	private readonly StrategyParam<int> _shortMa1Length;
	private readonly StrategyParam<int> _shortMa2Length;
	private readonly StrategyParam<MaTypes> _maType;
	private readonly StrategyParam<bool> _enableTpSl;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<TradeDirections> _direction;
	private readonly StrategyParam<bool> _enableAltExit;
	private readonly StrategyParam<AltExitMaOptions> _altExitMaOption;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevLongMa1;
	private decimal? _prevLongMa2;
	private decimal? _prevShortMa1;
	private decimal? _prevShortMa2;
	private decimal? _prevClose;

	/// <summary>
	/// Length of Long MA1.
	/// </summary>
	public int LongMa1Length
	{
		get => _longMa1Length.Value;
		set => _longMa1Length.Value = value;
	}

	/// <summary>
	/// Length of Long MA2.
	/// </summary>
	public int LongMa2Length
	{
		get => _longMa2Length.Value;
		set => _longMa2Length.Value = value;
	}

	/// <summary>
	/// Length of Short MA1.
	/// </summary>
	public int ShortMa1Length
	{
		get => _shortMa1Length.Value;
		set => _shortMa1Length.Value = value;
	}

	/// <summary>
	/// Length of Short MA2.
	/// </summary>
	public int ShortMa2Length
	{
		get => _shortMa2Length.Value;
		set => _shortMa2Length.Value = value;
	}

	/// <summary>
	/// Moving average type.
	/// </summary>
	public MaTypes MaType
	{
		get => _maType.Value;
		set => _maType.Value = value;
	}

	/// <summary>
	/// Use percent take profit and stop loss.
	/// </summary>
	public bool EnableTpSl
	{
		get => _enableTpSl.Value;
		set => _enableTpSl.Value = value;
	}

	/// <summary>
	/// Take profit percentage from entry price.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Stop loss percentage from entry price.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Allowed trade directions.
	/// </summary>
	public TradeDirections Direction
	{
		get => _direction.Value;
		set => _direction.Value = value;
	}

	/// <summary>
	/// Close positions on a close crossing the chosen moving average.
	/// </summary>
	public bool EnableAltExit
	{
		get => _enableAltExit.Value;
		set => _enableAltExit.Value = value;
	}

	/// <summary>
	/// Moving average used by the alternate exit.
	/// </summary>
	public AltExitMaOptions AltExitMaOption
	{
		get => _altExitMaOption.Value;
		set => _altExitMaOption.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public FourWmaTpSlStrategy()
	{
		_longMa1Length = Param(nameof(LongMa1Length), 10)
			.SetGreaterThanZero()
			.SetDisplay("Long MA1", "Length of Long MA1", "Moving Averages");

		_longMa2Length = Param(nameof(LongMa2Length), 20)
			.SetGreaterThanZero()
			.SetDisplay("Long MA2", "Length of Long MA2", "Moving Averages");

		_shortMa1Length = Param(nameof(ShortMa1Length), 30)
			.SetGreaterThanZero()
			.SetDisplay("Short MA1", "Length of Short MA1", "Moving Averages");

		_shortMa2Length = Param(nameof(ShortMa2Length), 40)
			.SetGreaterThanZero()
			.SetDisplay("Short MA2", "Length of Short MA2", "Moving Averages");

		_maType = Param(nameof(MaType), MaTypes.Wma)
			.SetDisplay("MA Type", "Moving average type", "Moving Averages");

		_enableTpSl = Param(nameof(EnableTpSl), true)
			.SetDisplay("Enable TP/SL", "Use percent take profit and stop loss", "Risk");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 1m)
			.SetNotNegative()
			.SetDisplay("Take Profit %", "Take profit percentage from entry price", "Risk");

		_stopLossPercent = Param(nameof(StopLossPercent), 1m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk");

		_direction = Param(nameof(Direction), TradeDirections.Both)
			.SetDisplay("Direction", "Allowed trade directions", "Trading");

		_enableAltExit = Param(nameof(EnableAltExit), false)
			.SetDisplay("Enable Alt Exit", "Close positions on a close crossing the chosen moving average", "Exit");

		_altExitMaOption = Param(nameof(AltExitMaOption), AltExitMaOptions.LongMa1)
			.SetDisplay("Alt Exit MA", "Moving average used by the alternate exit", "Exit");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		ResetState();
	}

	private void ResetState()
	{
		_prevLongMa1 = null;
		_prevLongMa2 = null;
		_prevShortMa1 = null;
		_prevShortMa2 = null;
		_prevClose = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var longMa1 = CreateMovingAverage(MaType, LongMa1Length);
		var longMa2 = CreateMovingAverage(MaType, LongMa2Length);
		var shortMa1 = CreateMovingAverage(MaType, ShortMa1Length);
		var shortMa2 = CreateMovingAverage(MaType, ShortMa2Length);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(longMa1, longMa2, shortMa1, shortMa2, ProcessCandle)
			.Start();

		if (EnableTpSl)
		{
			StartProtection(
				new Unit(TakeProfitPercent, UnitTypes.Percent),
				new Unit(StopLossPercent, UnitTypes.Percent),
				useMarketOrders: true,
				isLocalStop: true);

			// The stop and target have to see prices between candles, not only at their close.
			foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
			{
				var quotes = new Subscription(DataType.Level1, Security);
				quotes.MarketData.BuildField = field;
				SubscribeLevel1(quotes).Bind(ObserveProtectionQuote).Start();
			}
		}

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, longMa1);
			DrawIndicator(area, longMa2);
			DrawIndicator(area, shortMa1);
			DrawIndicator(area, shortMa2);
			DrawOwnTrades(area);
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// The high-level handler activates native protection before this callback, also between signal bars.
	}

	private static DecimalLengthIndicator CreateMovingAverage(MaTypes type, int length)
	{
		return type switch
		{
			MaTypes.Sma => new SimpleMovingAverage { Length = length },
			MaTypes.Ema => new ExponentialMovingAverage { Length = length },
			MaTypes.Hma => new HullMovingAverage { Length = length },
			MaTypes.Rma => new SmoothedMovingAverage { Length = length },
			_ => new WeightedMovingAverage { Length = length },
		};
	}

	private void ProcessCandle(ICandleMessage candle, decimal longMa1, decimal longMa2, decimal shortMa1, decimal shortMa2)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var prevLongMa1 = _prevLongMa1;
		var prevLongMa2 = _prevLongMa2;
		var prevShortMa1 = _prevShortMa1;
		var prevShortMa2 = _prevShortMa2;
		var prevClose = _prevClose;

		_prevLongMa1 = longMa1;
		_prevLongMa2 = longMa2;
		_prevShortMa1 = shortMa1;
		_prevShortMa2 = shortMa2;
		_prevClose = candle.ClosePrice;

		if (prevLongMa1 is not decimal lastLongMa1 || prevLongMa2 is not decimal lastLongMa2 ||
			prevShortMa1 is not decimal lastShortMa1 || prevShortMa2 is not decimal lastShortMa2 || prevClose is not decimal lastClose)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;
		var allowLong = Direction != TradeDirections.Short;
		var allowShort = Direction != TradeDirections.Long;

		var longSignal = lastLongMa1 <= lastLongMa2 && longMa1 > longMa2;
		var shortSignal = lastShortMa1 >= lastShortMa2 && shortMa1 < shortMa2;

		if (allowLong && longSignal && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			return;
		}

		if (allowShort && shortSignal && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			return;
		}

		if (!EnableAltExit || Position == 0)
			return;

		var (exitMa, lastExitMa) = AltExitMaOption switch
		{
			AltExitMaOptions.LongMa2 => (longMa2, lastLongMa2),
			AltExitMaOptions.ShortMa1 => (shortMa1, lastShortMa1),
			AltExitMaOptions.ShortMa2 => (shortMa2, lastShortMa2),
			_ => (longMa1, lastLongMa1),
		};

		if (Position > 0 && lastClose >= lastExitMa && close < exitMa)
			SellMarket(Position);
		else if (Position < 0 && lastClose <= lastExitMa && close > exitMa)
			BuyMarket(-Position);
	}
}
