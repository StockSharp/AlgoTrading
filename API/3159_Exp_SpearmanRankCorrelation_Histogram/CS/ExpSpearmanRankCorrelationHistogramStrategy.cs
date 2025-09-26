using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trading mode options from the original expert advisor.
/// </summary>
public enum ExpSpearmanTradeMode
{
	/// <summary>
	/// Mode 1: close opposite positions and open when the histogram crosses neutral levels.
	/// </summary>
	Mode1 = 0,

	/// <summary>
	/// Mode 2: open only when the histogram leaves extreme zones.
	/// </summary>
	Mode2 = 1,

	/// <summary>
	/// Mode 3: open and close positions exclusively on extreme values.
	/// </summary>
	Mode3 = 2,
}

/// <summary>
/// Conversion of the MetaTrader expert Exp_SpearmanRankCorrelation_Histogram.
/// The strategy analyses the Spearman Rank Correlation histogram colours to open or close positions.
/// </summary>
public class ExpSpearmanRankCorrelationHistogramStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rangeLength;
	private readonly StrategyParam<int> _maxRange;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<decimal> _highLevel;
	private readonly StrategyParam<decimal> _lowLevel;
	private readonly StrategyParam<bool> _allowBuyEntries;
	private readonly StrategyParam<bool> _allowSellEntries;
	private readonly StrategyParam<bool> _allowBuyExits;
	private readonly StrategyParam<bool> _allowSellExits;
	private readonly StrategyParam<ExpSpearmanTradeMode> _tradeMode;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<bool> _invertCorrelation;

	private RankCorrelationIndex _spearman;
	private readonly List<int> _colorHistory = new();

	/// <summary>
	/// Initializes a new instance of the <see cref="ExpSpearmanRankCorrelationHistogramStrategy"/> class.
	/// </summary>
	public ExpSpearmanRankCorrelationHistogramStrategy()
	{
		Volume = 1m;

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Time frame used for the indicator and trading", "General");

		_rangeLength = Param(nameof(RangeLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("Range Length", "Lookback period for the Spearman calculation", "Indicator");

		_maxRange = Param(nameof(MaxRange), 30)
			.SetGreaterThanZero()
			.SetDisplay("Max Range", "Upper bound for the allowed lookback length", "Indicator");

		_signalBar = Param(nameof(SignalBar), 1)
			.SetMinMax(0, 50)
			.SetDisplay("Signal Bar", "Historical offset where the histogram is checked", "Signals");

		_highLevel = Param(nameof(HighLevel), 0.5m)
			.SetDisplay("High Level", "Upper threshold for the bullish zone", "Signals");

		_lowLevel = Param(nameof(LowLevel), -0.5m)
			.SetDisplay("Low Level", "Lower threshold for the bearish zone", "Signals");

		_allowBuyEntries = Param(nameof(AllowBuyEntries), true)
			.SetDisplay("Allow Buy Entries", "Enable opening long positions", "Trading");

		_allowSellEntries = Param(nameof(AllowSellEntries), true)
			.SetDisplay("Allow Sell Entries", "Enable opening short positions", "Trading");

		_allowBuyExits = Param(nameof(AllowBuyExits), true)
			.SetDisplay("Allow Buy Exits", "Enable automatic closing of long positions", "Trading");

		_allowSellExits = Param(nameof(AllowSellExits), true)
			.SetDisplay("Allow Sell Exits", "Enable automatic closing of short positions", "Trading");

		_tradeMode = Param(nameof(TradeMode), ExpSpearmanTradeMode.Mode1)
			.SetDisplay("Trade Mode", "Replica of the original expert trade mode switch", "Trading");

		_stopLossPoints = Param(nameof(StopLossPoints), 0m)
			.SetDisplay("Stop Loss", "Protective stop distance expressed in price units", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 0m)
			.SetDisplay("Take Profit", "Protective target distance expressed in price units", "Risk");

		_invertCorrelation = Param(nameof(InvertCorrelation), false)
			.SetDisplay("Invert Correlation", "Flip the histogram sign to emulate the direction flag", "Indicator");
	}

	/// <summary>
	/// Candle timeframe used for analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Lookback period of the Spearman calculation.
	/// </summary>
	public int RangeLength
	{
		get => _rangeLength.Value;
		set => _rangeLength.Value = value;
	}

	/// <summary>
	/// Maximum allowed lookback length.
	/// </summary>
	public int MaxRange
	{
		get => _maxRange.Value;
		set => _maxRange.Value = value;
	}

	/// <summary>
	/// Number of completed bars to skip before evaluating the histogram.
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	/// <summary>
	/// Upper correlation threshold that defines the bullish zone.
	/// </summary>
	public decimal HighLevel
	{
		get => _highLevel.Value;
		set => _highLevel.Value = value;
	}

	/// <summary>
	/// Lower correlation threshold that defines the bearish zone.
	/// </summary>
	public decimal LowLevel
	{
		get => _lowLevel.Value;
		set => _lowLevel.Value = value;
	}

	/// <summary>
	/// Enables long entries.
	/// </summary>
	public bool AllowBuyEntries
	{
		get => _allowBuyEntries.Value;
		set => _allowBuyEntries.Value = value;
	}

	/// <summary>
	/// Enables short entries.
	/// </summary>
	public bool AllowSellEntries
	{
		get => _allowSellEntries.Value;
		set => _allowSellEntries.Value = value;
	}

	/// <summary>
	/// Enables automatic exit from long positions.
	/// </summary>
	public bool AllowBuyExits
	{
		get => _allowBuyExits.Value;
		set => _allowBuyExits.Value = value;
	}

	/// <summary>
	/// Enables automatic exit from short positions.
	/// </summary>
	public bool AllowSellExits
	{
		get => _allowSellExits.Value;
		set => _allowSellExits.Value = value;
	}

	/// <summary>
	/// Trade mode copied from the MetaTrader implementation.
	/// </summary>
	public ExpSpearmanTradeMode TradeMode
	{
		get => _tradeMode.Value;
		set => _tradeMode.Value = value;
	}

	/// <summary>
	/// Stop loss distance in absolute price units.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance in absolute price units.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Inverts the correlation sign when the original indicator direction flag is disabled.
	/// </summary>
	public bool InvertCorrelation
	{
		get => _invertCorrelation.Value;
		set => _invertCorrelation.Value = value;
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

		_spearman = null;
		_colorHistory.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var effectiveLength = Math.Max(1, Math.Min(RangeLength, MaxRange > 0 ? MaxRange : 10));

		_spearman = new RankCorrelationIndex
		{
			Length = effectiveLength
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_spearman, ProcessCandle)
			.Start();

		ConfigureProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _spearman);
			DrawOwnTrades(area);
		}
	}

	private void ConfigureProtection()
	{
		var stopLoss = StopLossPoints > 0m ? new Unit(StopLossPoints, UnitTypes.Price) : null;
		var takeProfit = TakeProfitPoints > 0m ? new Unit(TakeProfitPoints, UnitTypes.Price) : null;

		if (stopLoss != null || takeProfit != null)
		{
			StartProtection(takeProfit: takeProfit, stopLoss: stopLoss);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal spearmanValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (_spearman == null)
		return;

		var normalized = spearmanValue / 100m;
		normalized = Math.Max(-1m, Math.Min(1m, normalized));

		if (InvertCorrelation)
		normalized = -normalized;

		UpdateColorHistory(normalized);

		var signalBar = Math.Max(0, SignalBar);
		var required = signalBar + 2;

		if (_colorHistory.Count < required)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var recentColor = _colorHistory[signalBar];
		var olderColor = _colorHistory[signalBar + 1];

		var openBuy = false;
		var openSell = false;
		var closeBuy = false;
		var closeSell = false;

		switch (TradeMode)
		{
		case ExpSpearmanTradeMode.Mode1:
		{
		if (olderColor > 2)
		{
		if (AllowBuyEntries && recentColor < 3)
		openBuy = true;

		if (AllowSellExits)
		closeSell = true;
		}

		if (olderColor < 2)
		{
		if (AllowSellEntries && recentColor > 1)
		openSell = true;

		if (AllowBuyExits)
		closeBuy = true;
		}

		break;
		}

		case ExpSpearmanTradeMode.Mode2:
		{
		if (olderColor == 4)
		{
		if (AllowBuyEntries && recentColor < 4)
		openBuy = true;
		}

		if (AllowSellExits && olderColor > 2)
		closeSell = true;

		if (olderColor == 0)
		{
		if (AllowSellEntries && recentColor > 0)
		openSell = true;
		}

		if (AllowBuyExits && olderColor < 2)
		closeBuy = true;

		break;
		}

		case ExpSpearmanTradeMode.Mode3:
		{
		if (olderColor == 4)
		{
		if (AllowBuyEntries && recentColor < 4)
		openBuy = true;

		if (AllowSellExits)
		closeSell = true;
		}

		if (olderColor == 0)
		{
		if (AllowSellEntries && recentColor > 0)
		openSell = true;

		if (AllowBuyExits)
		closeBuy = true;
		}

		break;
		}
		}

		ExecuteSignals(openBuy, openSell, closeBuy, closeSell);
	}

	private void UpdateColorHistory(decimal normalizedValue)
	{
		var color = 2;

		if (normalizedValue > 0m)
		{
		color = normalizedValue > HighLevel ? 4 : 3;
		}
		else if (normalizedValue < 0m)
		{
		color = normalizedValue < LowLevel ? 0 : 1;
		}

		_colorHistory.Insert(0, color);

		var maxSize = Math.Max(2, SignalBar + 2);
		while (_colorHistory.Count > maxSize)
		{
		_colorHistory.RemoveAt(_colorHistory.Count - 1);
		}
	}

	private void ExecuteSignals(bool openBuy, bool openSell, bool closeBuy, bool closeSell)
	{
		var hasSignal = openBuy || openSell || closeBuy || closeSell;
		if (!hasSignal)
		return;

		CancelActiveOrders();

		if (closeBuy && Position > 0m)
		{
		SellMarket(Position);
		}

		if (closeSell && Position < 0m)
		{
		BuyMarket(-Position);
		}

		if (openBuy && Position <= 0m)
		{
		var volume = Volume + Math.Abs(Position);
		if (volume > 0m)
		BuyMarket(volume);
		}
		else if (openSell && Position >= 0m)
		{
		var volume = Volume + Math.Abs(Position);
		if (volume > 0m)
		SellMarket(volume);
		}
	}
}
