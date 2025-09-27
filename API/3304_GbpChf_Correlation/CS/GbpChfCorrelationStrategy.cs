using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// GBPCHF correlation strategy that mirrors the original "GbpChf 4" MetaTrader expert.
/// It analyses hourly MACD histograms and signal lines for GBPUSD and USDCHF,
/// then trades the configured instrument when both currency legs align.
/// </summary>
public class GbpChfCorrelationStrategy : Strategy
{
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<bool> _onlyOnePosition;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _signalPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<string> _gbpUsdSymbol;
	private readonly StrategyParam<string> _usdChfSymbol;

	private Security _gbpUsdSecurity;
	private Security _usdChfSecurity;

	private MovingAverageConvergenceDivergenceSignal _gbpUsdMacd;
	private MovingAverageConvergenceDivergenceSignal _usdChfMacd;

	private decimal? _gbpUsdHistogram;
	private decimal? _usdChfHistogram;
	private decimal? _gbpUsdSignal;
	private decimal? _usdChfSignal;

	private DateTimeOffset? _gbpUsdBarTime;
	private DateTimeOffset? _usdChfBarTime;
	private DateTimeOffset? _lastBuyBarTime;
	private DateTimeOffset? _lastSellBarTime;

	/// <summary>
	/// Initializes a new instance of the <see cref="GbpChfCorrelationStrategy"/> class.
	/// </summary>
	public GbpChfCorrelationStrategy()
	{

		_stopLossPips = Param(nameof(StopLossPips), 90)
		.SetDisplay("Stop Loss (pips)", "Stop loss distance expressed in pips", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(30, 150, 10);

		_takeProfitPips = Param(nameof(TakeProfitPips), 45)
		.SetDisplay("Take Profit (pips)", "Take profit distance expressed in pips", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(30, 120, 5);

		_onlyOnePosition = Param(nameof(OnlyOnePosition), true)
		.SetDisplay("Single Position", "Allow only one open position at a time", "Trading");

		_fastPeriod = Param(nameof(FastPeriod), 12)
		.SetGreaterThanZero()
		.SetDisplay("Fast EMA", "Fast EMA length for MACD", "Indicators");

		_slowPeriod = Param(nameof(SlowPeriod), 26)
		.SetGreaterThanZero()
		.SetDisplay("Slow EMA", "Slow EMA length for MACD", "Indicators");

		_signalPeriod = Param(nameof(SignalPeriod), 9)
		.SetGreaterThanZero()
		.SetDisplay("Signal SMA", "Signal moving average length for MACD", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe for all calculations", "General");

		_gbpUsdSymbol = Param(nameof(GbpUsdSymbol), "GBPUSD")
		.SetDisplay("GBPUSD Symbol", "Identifier of the GBPUSD instrument", "Data")
		.SetCanOptimize(false);

		_usdChfSymbol = Param(nameof(UsdChfSymbol), "USDCHF")
		.SetDisplay("USDCHF Symbol", "Identifier of the USDCHF instrument", "Data")
		.SetCanOptimize(false);
	}


	/// <summary>
	/// Stop-loss distance expressed in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Allow only one position at a time when true.
	/// </summary>
	public bool OnlyOnePosition
	{
		get => _onlyOnePosition.Value;
		set => _onlyOnePosition.Value = value;
	}

	/// <summary>
	/// Fast EMA length for MACD.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA length for MACD.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Signal moving average length for MACD.
	/// </summary>
	public int SignalPeriod
	{
		get => _signalPeriod.Value;
		set => _signalPeriod.Value = value;
	}

	/// <summary>
	/// Candle type shared by every subscription.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Symbol identifier used to resolve the GBPUSD security.
	/// </summary>
	public string GbpUsdSymbol
	{
		get => _gbpUsdSymbol.Value;
		set
		{
			_gbpUsdSymbol.Value = value ?? string.Empty;
			_gbpUsdSecurity = null;
		}
	}

	/// <summary>
	/// Symbol identifier used to resolve the USDCHF security.
	/// </summary>
	public string UsdChfSymbol
	{
		get => _usdChfSymbol.Value;
		set
		{
			_usdChfSymbol.Value = value ?? string.Empty;
			_usdChfSecurity = null;
		}
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		var security = Security;
		if (security != null)
			yield return (security, CandleType);

		EnsureExternalSecuritiesResolved(false);

		if (_gbpUsdSecurity != null)
			yield return (_gbpUsdSecurity, CandleType);

		if (_usdChfSecurity != null)
			yield return (_usdChfSecurity, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_gbpUsdMacd = null;
		_usdChfMacd = null;

		_gbpUsdHistogram = null;
		_usdChfHistogram = null;
		_gbpUsdSignal = null;
		_usdChfSignal = null;

		_gbpUsdBarTime = null;
		_usdChfBarTime = null;
		_lastBuyBarTime = null;
		_lastSellBarTime = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var tradedSecurity = Security ?? throw new InvalidOperationException("Security is not configured.");

		EnsureExternalSecuritiesResolved(true);

		_gbpUsdMacd = CreateMacd();
		_usdChfMacd = CreateMacd();

		var mainSubscription = SubscribeCandles(CandleType);
		mainSubscription
		.Bind(ProcessMainCandle)
		.Start();

		if (_gbpUsdSecurity == null || _usdChfSecurity == null)
			throw new InvalidOperationException("Currency leg securities are not available.");

		var gbpSubscription = SubscribeCandles(CandleType, security: _gbpUsdSecurity);
		gbpSubscription
		.BindEx(_gbpUsdMacd, ProcessGbpUsdCandle)
		.Start();

		var usdSubscription = SubscribeCandles(CandleType, security: _usdChfSecurity);
		usdSubscription
		.BindEx(_usdChfMacd, ProcessUsdChfCandle)
		.Start();

		var step = tradedSecurity.PriceStep ?? 0m;
		var pipSize = step == 0m ? 0m : step;
		Unit stopLoss = StopLossPips > 0 && pipSize > 0m ? new Unit(StopLossPips * pipSize, UnitTypes.Absolute) : null;
		Unit takeProfit = TakeProfitPips > 0 && pipSize > 0m ? new Unit(TakeProfitPips * pipSize, UnitTypes.Absolute) : null;

		StartProtection(takeProfit, stopLoss);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, mainSubscription);
			DrawIndicator(area, _gbpUsdMacd);
			DrawIndicator(area, _usdChfMacd);
			DrawOwnTrades(area);
		}
	}

	private void ProcessGbpUsdCandle(ICandleMessage candle, IIndicatorValue value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var typed = (MovingAverageConvergenceDivergenceSignalValue)value;
		if (typed.Macd is not decimal macd || typed.Signal is not decimal signal)
			return;

		_gbpUsdHistogram = macd - signal;
		_gbpUsdSignal = signal;
		_gbpUsdBarTime = candle.OpenTime;
	}

	private void ProcessUsdChfCandle(ICandleMessage candle, IIndicatorValue value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var typed = (MovingAverageConvergenceDivergenceSignalValue)value;
		if (typed.Macd is not decimal macd || typed.Signal is not decimal signal)
			return;

		_usdChfHistogram = macd - signal;
		_usdChfSignal = signal;
		_usdChfBarTime = candle.OpenTime;
	}

	private void ProcessMainCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var barTime = candle.OpenTime;

		if (_gbpUsdBarTime != barTime || _usdChfBarTime != barTime)
			return;

		if (_gbpUsdHistogram is not decimal gbpHist ||
			_usdChfHistogram is not decimal usdHist ||
		_gbpUsdSignal is not decimal gbpSignal ||
		_usdChfSignal is not decimal usdSignal)
			return;

		var longSignal = gbpHist > 0m && usdHist > 0m && gbpHist < usdHist && gbpSignal > usdSignal;
		var shortSignal = gbpHist < 0m && usdHist < 0m && gbpHist > usdHist && gbpSignal < usdSignal;

		if (longSignal && barTime != _lastBuyBarTime && (Position <= 0 || !OnlyOnePosition))
		{
			CancelActiveOrders();
			var volume = Volume + Math.Max(0m, -Position);
			if (volume > 0m)
				BuyMarket(volume);
			_lastBuyBarTime = barTime;
		}
		else if (shortSignal && barTime != _lastSellBarTime && (Position >= 0 || !OnlyOnePosition))
		{
			CancelActiveOrders();
			var volume = Volume + Math.Max(0m, Position);
			if (volume > 0m)
				SellMarket(volume);
			_lastSellBarTime = barTime;
		}
	}

	private MovingAverageConvergenceDivergenceSignal CreateMacd()
	{
		return new()
		{
			Macd =
			{
				ShortMa = { Length = FastPeriod },
				LongMa = { Length = SlowPeriod }
			},
			SignalMa = { Length = SignalPeriod }
		};
	}

	private void EnsureExternalSecuritiesResolved(bool throwOnError)
	{
		_gbpUsdSecurity ??= ResolveSecurity(GbpUsdSymbol, throwOnError);
		_usdChfSecurity ??= ResolveSecurity(UsdChfSymbol, throwOnError);
	}

	private Security ResolveSecurity(string symbol, bool throwOnError)
	{
		if (symbol.IsEmptyOrWhiteSpace())
		{
			if (throwOnError)
				throw new InvalidOperationException("Security identifier is not specified.");

			return null;
		}

		var security = this.GetSecurity(symbol);
		if (security != null)
			return security;

		var message = $"Security '{symbol}' could not be resolved.";
		if (throwOnError)
			throw new InvalidOperationException(message);

		LogWarning(message);
		return null;
	}
}
