using System;
using System.Collections.Generic;

using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Keltner Channel strategy with Golden Cross filter.
/// </summary>
public class KeltnerChannelGoldenCrossStrategy : Strategy
{
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<decimal> _entryAtrMultiplier;
	private readonly StrategyParam<decimal> _profitAtrMultiplier;
	private readonly StrategyParam<decimal> _exitAtrMultiplier;
	private readonly StrategyParam<MovingAverageTypes> _maType;
	private readonly StrategyParam<int> _shortMaLength;
	private readonly StrategyParam<int> _longMaLength;
	private readonly StrategyParam<int> _maxEntries;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private int _entriesExecuted;
	private int _barsSinceSignal;

	/// <summary>
	/// Basis MA length.
	/// </summary>
	public int MaLength { get => _maLength.Value; set => _maLength.Value = value; }

	/// <summary>
	/// ATR multiplier for entry channel.
	/// </summary>
	public decimal EntryAtrMultiplier { get => _entryAtrMultiplier.Value; set => _entryAtrMultiplier.Value = value; }

	/// <summary>
	/// ATR multiplier for take profit.
	/// </summary>
	public decimal ProfitAtrMultiplier { get => _profitAtrMultiplier.Value; set => _profitAtrMultiplier.Value = value; }

	/// <summary>
	/// ATR multiplier for stop.
	/// </summary>
	public decimal ExitAtrMultiplier { get => _exitAtrMultiplier.Value; set => _exitAtrMultiplier.Value = value; }

	/// <summary>
	/// Basis MA type.
	/// </summary>
	public MovingAverageTypes MaType { get => _maType.Value; set => _maType.Value = value; }

	/// <summary>
	/// Short MA length for golden cross.
	/// </summary>
	public int ShortMaLength { get => _shortMaLength.Value; set => _shortMaLength.Value = value; }

	/// <summary>
	/// Long MA length for golden cross.
	/// </summary>
	public int LongMaLength { get => _longMaLength.Value; set => _longMaLength.Value = value; }

	/// <summary>
	/// Candle type for strategy.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Maximum entries per run.
	/// </summary>
	public int MaxEntries { get => _maxEntries.Value; set => _maxEntries.Value = value; }

	/// <summary>
	/// Minimum bars between entries.
	/// </summary>
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }

	/// <summary>
	/// Initializes a new instance of <see cref="KeltnerChannelGoldenCrossStrategy"/>.
	/// </summary>
	public KeltnerChannelGoldenCrossStrategy()
	{
		_maLength = Param(nameof(MaLength), 21)
		.SetDisplay("MA Length", "Length for basis moving average", "General")
		.SetGreaterThanZero()
		;

		_entryAtrMultiplier = Param(nameof(EntryAtrMultiplier), 1m)
		.SetDisplay("Entry ATR Mult", "ATR multiplier for entry channel", "Risk")
		;

		_profitAtrMultiplier = Param(nameof(ProfitAtrMultiplier), 4m)
		.SetDisplay("Profit Mult", "ATR multiplier for take profit", "Risk")
		;

		_exitAtrMultiplier = Param(nameof(ExitAtrMultiplier), -1m)
		.SetDisplay("Exit Mult", "ATR multiplier for stop", "Risk")
		;

		_maType = Param(nameof(MaType), MovingAverageTypes.Simple)
		.SetDisplay("MA Type", "Type of basis moving average", "General");

		_shortMaLength = Param(nameof(ShortMaLength), 10)
		.SetDisplay("Short MA", "Short moving average length", "Trend")
		.SetGreaterThanZero()
		;

		_longMaLength = Param(nameof(LongMaLength), 30)
		.SetDisplay("Long MA", "Long moving average length", "Trend")
		.SetGreaterThanZero()
		;

		_maxEntries = Param(nameof(MaxEntries), 45)
		.SetDisplay("Max Entries", "Maximum entries per run", "Risk")
		.SetGreaterThanZero();

		_cooldownBars = Param(nameof(CooldownBars), 5)
		.SetDisplay("Cooldown Bars", "Minimum bars between entries", "Risk")
		.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_entriesExecuted = 0;
		_barsSinceSignal = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_entriesExecuted = 0;
		_barsSinceSignal = CooldownBars;

		var basis = CreateMa(MaType, MaLength);
		var entryAtr = new AverageTrueRange { Length = 10 };
		var atr = new AverageTrueRange { Length = MaLength };
		var shortMa = new EMA { Length = ShortMaLength };
		var longMa = new EMA { Length = LongMaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(basis, entryAtr, atr, shortMa, longMa, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, basis);
			DrawIndicator(area, shortMa);
			DrawIndicator(area, longMa);
			DrawOwnTrades(area);
		}
	}

	private DecimalLengthIndicator CreateMa(MovingAverageTypes type, int length)
	{
		return type switch
		{
			MovingAverageTypes.Exponential => new EMA { Length = length },
			MovingAverageTypes.Weighted => new WeightedMovingAverage { Length = length },
			_ => new SMA { Length = length },
		};
	}

	private void ProcessCandle(ICandleMessage candle, decimal basis, decimal entryAtr, decimal atr, decimal shortMa, decimal longMa)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barsSinceSignal++;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var price = candle.ClosePrice;
		var upperEntry = basis + EntryAtrMultiplier * entryAtr;
		var lowerEntry = basis - EntryAtrMultiplier * entryAtr;
		var takeProfit = basis + ProfitAtrMultiplier * atr;
		var takeProfitShort = basis - ProfitAtrMultiplier * atr;
		var stopLong = basis + ExitAtrMultiplier * atr;
		var stopShort = basis - ExitAtrMultiplier * atr;
		var longTrend = shortMa > longMa;
		var shortTrend = shortMa < longMa;

		if (_barsSinceSignal < CooldownBars)
			return;

		if (Position > 0)
		{
			if (price >= takeProfit || price <= stopLong)
			{
				SellMarket(Math.Abs(Position));
				_barsSinceSignal = 0;
			}

			return;
		}

		if (Position < 0)
		{
			if (price <= takeProfitShort || price >= stopShort)
			{
				BuyMarket(Math.Abs(Position));
				_barsSinceSignal = 0;
			}

			return;
		}

		if (_entriesExecuted >= MaxEntries || _barsSinceSignal < CooldownBars)
			return;

		if (longTrend && price > upperEntry)
		{
			BuyMarket(Volume);
			_entriesExecuted++;
			_barsSinceSignal = 0;
		}
		else if (shortTrend && price < lowerEntry)
		{
			SellMarket(Volume);
			_entriesExecuted++;
			_barsSinceSignal = 0;
		}
	}

	/// <summary>
	/// Moving average type.
	/// </summary>
	public enum MovingAverageTypes
	{
		/// <summary>
		/// Simple moving average.
		/// </summary>
		Simple,

		/// <summary>
		/// Exponential moving average.
		/// </summary>
		Exponential,

		/// <summary>
		/// Weighted moving average.
		/// </summary>
		Weighted
	}
}
