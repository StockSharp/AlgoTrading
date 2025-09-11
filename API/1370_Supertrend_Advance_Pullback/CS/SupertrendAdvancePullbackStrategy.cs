using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Supertrend Advance Pullback strategy.
/// Uses Supertrend for trend detection and optional EMA, RSI, MACD and CCI filters.
/// </summary>
public class SupertrendAdvancePullbackStrategy : Strategy
{
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _factor;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<bool> _useEmaFilter;
	private readonly StrategyParam<bool> _useRsiFilter;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiBuyLevel;
	private readonly StrategyParam<decimal> _rsiSellLevel;
	private readonly StrategyParam<bool> _useMacdFilter;
	private readonly StrategyParam<int> _macdFastLength;
	private readonly StrategyParam<int> _macdSlowLength;
	private readonly StrategyParam<int> _macdSignalLength;
	private readonly StrategyParam<bool> _useCciFilter;
	private readonly StrategyParam<int> _cciLength;
	private readonly StrategyParam<decimal> _cciBuyLevel;
	private readonly StrategyParam<decimal> _cciSellLevel;
	private readonly StrategyParam<string> _mode;
	private readonly StrategyParam<string> _tradeDirection;
	private readonly StrategyParam<DataType> _candleType;

	private AverageTrueRange _atr;
	private ExponentialMovingAverage _ema;
	private RelativeStrengthIndex _rsi;
	private MovingAverageConvergenceDivergence _macd;
	private CommodityChannelIndex _cci;

	private decimal _prevSupertrend;
	private decimal _prevHigh;
	private decimal _prevLow;
	private bool _prevIsAbove;

	/// <summary>
	/// ATR calculation length.
	/// </summary>
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }

	/// <summary>
	/// Multiplier for ATR.
	/// </summary>
	public decimal Factor { get => _factor.Value; set => _factor.Value = value; }

	/// <summary>
	/// EMA length for trend filter.
	/// </summary>
	public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }

	/// <summary>
	/// Enable EMA filter.
	/// </summary>
	public bool UseEmaFilter { get => _useEmaFilter.Value; set => _useEmaFilter.Value = value; }

	/// <summary>
	/// Enable RSI filter.
	/// </summary>
	public bool UseRsiFilter { get => _useRsiFilter.Value; set => _useRsiFilter.Value = value; }

	/// <summary>
	/// RSI calculation length.
	/// </summary>
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }

	/// <summary>
	/// RSI level for longs.
	/// </summary>
	public decimal RsiBuyLevel { get => _rsiBuyLevel.Value; set => _rsiBuyLevel.Value = value; }

	/// <summary>
	/// RSI level for shorts.
	/// </summary>
	public decimal RsiSellLevel { get => _rsiSellLevel.Value; set => _rsiSellLevel.Value = value; }

	/// <summary>
	/// Enable MACD filter.
	/// </summary>
	public bool UseMacdFilter { get => _useMacdFilter.Value; set => _useMacdFilter.Value = value; }

	/// <summary>
	/// Fast length for MACD.
	/// </summary>
	public int MacdFastLength { get => _macdFastLength.Value; set => _macdFastLength.Value = value; }

	/// <summary>
	/// Slow length for MACD.
	/// </summary>
	public int MacdSlowLength { get => _macdSlowLength.Value; set => _macdSlowLength.Value = value; }

	/// <summary>
	/// Signal length for MACD.
	/// </summary>
	public int MacdSignalLength { get => _macdSignalLength.Value; set => _macdSignalLength.Value = value; }

	/// <summary>
	/// Enable CCI filter.
	/// </summary>
	public bool UseCciFilter { get => _useCciFilter.Value; set => _useCciFilter.Value = value; }

	/// <summary>
	/// Length for CCI.
	/// </summary>
	public int CciLength { get => _cciLength.Value; set => _cciLength.Value = value; }

	/// <summary>
	/// CCI level for longs.
	/// </summary>
	public decimal CciBuyLevel { get => _cciBuyLevel.Value; set => _cciBuyLevel.Value = value; }

	/// <summary>
	/// CCI level for shorts.
	/// </summary>
	public decimal CciSellLevel { get => _cciSellLevel.Value; set => _cciSellLevel.Value = value; }

	/// <summary>
	/// Strategy mode: Pullback or Simple.
	/// </summary>
	public string Mode { get => _mode.Value; set => _mode.Value = value; }

	/// <summary>
	/// Allowed trade direction.
	/// </summary>
	public string TradeDirection { get => _tradeDirection.Value; set => _tradeDirection.Value = value; }

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of <see cref="SupertrendAdvancePullbackStrategy"/>.
	/// </summary>
	public SupertrendAdvancePullbackStrategy()
	{
		_atrLength = Param(nameof(AtrLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR period for Supertrend", "Supertrend")
			.SetCanOptimize(true);

		_factor = Param(nameof(Factor), 3m)
			.SetGreaterThan(0m)
			.SetDisplay("Factor", "ATR multiplier", "Supertrend")
			.SetCanOptimize(true);

		_emaLength = Param(nameof(EmaLength), 200)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "Length for EMA filter", "Filters")
			.SetCanOptimize(true);

		_useEmaFilter = Param(nameof(UseEmaFilter), true)
			.SetDisplay("Use EMA", "Enable EMA filter", "Filters");

		_useRsiFilter = Param(nameof(UseRsiFilter), true)
			.SetDisplay("Use RSI", "Enable RSI filter", "Filters");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "Length for RSI", "Filters")
			.SetCanOptimize(true);

		_rsiBuyLevel = Param(nameof(RsiBuyLevel), 50m)
			.SetDisplay("RSI Buy", "RSI level for longs", "Filters")
			.SetCanOptimize(true);

		_rsiSellLevel = Param(nameof(RsiSellLevel), 50m)
			.SetDisplay("RSI Sell", "RSI level for shorts", "Filters")
			.SetCanOptimize(true);

		_useMacdFilter = Param(nameof(UseMacdFilter), true)
			.SetDisplay("Use MACD", "Enable MACD filter", "Filters");

		_macdFastLength = Param(nameof(MacdFastLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "Fast EMA length", "Filters")
			.SetCanOptimize(true);

		_macdSlowLength = Param(nameof(MacdSlowLength), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "Slow EMA length", "Filters")
			.SetCanOptimize(true);

		_macdSignalLength = Param(nameof(MacdSignalLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "Signal length", "Filters")
			.SetCanOptimize(true);

		_useCciFilter = Param(nameof(UseCciFilter), true)
			.SetDisplay("Use CCI", "Enable CCI filter", "Filters");

		_cciLength = Param(nameof(CciLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("CCI Length", "Length for CCI", "Filters")
			.SetCanOptimize(true);

		_cciBuyLevel = Param(nameof(CciBuyLevel), 200m)
			.SetDisplay("CCI Buy", "CCI level for longs", "Filters")
			.SetCanOptimize(true);

		_cciSellLevel = Param(nameof(CciSellLevel), -200m)
			.SetDisplay("CCI Sell", "CCI level for shorts", "Filters")
			.SetCanOptimize(true);

		_mode = Param(nameof(Mode), "Pullback")
			.SetDisplay("Mode", "Entry mode", "General")
			.SetOptions("Pullback", "Simple");

		_tradeDirection = Param(nameof(TradeDirection), "Both")
			.SetDisplay("Trade Direction", "Allowed trade sides", "General")
			.SetOptions("Long", "Short", "Both");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		_prevSupertrend = 0m;
		_prevHigh = 0m;
		_prevLow = 0m;
		_prevIsAbove = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_atr = new AverageTrueRange { Length = AtrLength };
		_ema = new ExponentialMovingAverage { Length = EmaLength };
		_rsi = new RelativeStrengthIndex { Length = RsiLength };
		_macd = new MovingAverageConvergenceDivergence
		{
			ShortPeriod = MacdFastLength,
			LongPeriod = MacdSlowLength,
			SignalPeriod = MacdSignalLength
		};
		_cci = new CommodityChannelIndex { Length = CciLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_atr, _ema, _rsi, _macd, _cci, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atr, decimal ema, decimal rsi, decimal macd, decimal macdSignal, decimal _, decimal cci)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_atr.IsFormed)
			return;
		if (UseEmaFilter && !_ema.IsFormed)
			return;
		if (UseRsiFilter && !_rsi.IsFormed)
			return;
		if (UseMacdFilter && !_macd.IsFormed)
			return;
		if (UseCciFilter && !_cci.IsFormed)
			return;

		var median = (candle.HighPrice + candle.LowPrice) / 2m;
		var upper = median + Factor * atr;
		var lower = median - Factor * atr;

		decimal supertrend;
		bool isAbove;

		if (_prevSupertrend == 0m)
		{
			supertrend = candle.ClosePrice > median ? lower : upper;
			_prevSupertrend = supertrend;
			_prevIsAbove = candle.ClosePrice > supertrend;
			_prevHigh = candle.HighPrice;
			_prevLow = candle.LowPrice;
			return;
		}

		if (_prevSupertrend <= candle.HighPrice)
			supertrend = Math.Max(lower, _prevSupertrend);
		else if (_prevSupertrend >= candle.LowPrice)
			supertrend = Math.Min(upper, _prevSupertrend);
		else
			supertrend = candle.ClosePrice > _prevSupertrend ? lower : upper;

		isAbove = candle.ClosePrice > supertrend;

		bool buySignal;
		bool sellSignal;

		if (Mode == "Simple")
		{
			buySignal = isAbove && !_prevIsAbove;
			sellSignal = !isAbove && _prevIsAbove;
		}
		else
		{
			buySignal = isAbove && _prevIsAbove && _prevLow > _prevSupertrend && candle.LowPrice < supertrend && candle.HighPrice > _prevHigh;
			sellSignal = !isAbove && !_prevIsAbove && _prevHigh < _prevSupertrend && candle.HighPrice > supertrend && candle.LowPrice < _prevLow;
		}

		var emaBuy = !UseEmaFilter || candle.ClosePrice > ema;
		var emaSell = !UseEmaFilter || candle.ClosePrice < ema;
		var rsiBuy = !UseRsiFilter || rsi >= RsiBuyLevel;
		var rsiSell = !UseRsiFilter || rsi <= RsiSellLevel;
		var macdBuy = !UseMacdFilter || macd > 0m;
		var macdSell = !UseMacdFilter || macd < 0m;
		var cciBuy = !UseCciFilter || cci > CciBuyLevel;
		var cciSell = !UseCciFilter || cci < CciSellLevel;

		var longOk = TradeDirection == "Long" || TradeDirection == "Both";
		var shortOk = TradeDirection == "Short" || TradeDirection == "Both";

		if (buySignal && longOk && emaBuy && rsiBuy && macdBuy && cciBuy && Position <= 0)
		{
			var volume = Volume + (Position < 0 ? -Position : 0m);
			BuyMarket(volume);
		}
		else if (sellSignal && shortOk && emaSell && rsiSell && macdSell && cciSell && Position >= 0)
		{
			var volume = Volume + (Position > 0 ? Position : 0m);
			SellMarket(volume);
		}

		_prevSupertrend = supertrend;
		_prevIsAbove = isAbove;
		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;
	}
}
