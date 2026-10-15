using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Derivatives;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Volatility risk premium strategy. Sells an out-of-the-money option while its Black-Scholes implied
/// volatility exceeds the realized volatility of the underlying, keeps the short delta-hedged with the
/// underlying on every bar and buys the option back at expiration, on a realized volatility spike or on
/// a vega stop.
/// </summary>
public class VolatilityRiskPremiumStrategy : Strategy
{
	private const string _sellComment = "Sell option";
	private const string _hedgeComment = "Delta hedge";
	private const string _closeHedgeComment = "Close delta hedge";
	private const string _buyBackComment = "Buy back option: ";

	private readonly StrategyParam<Security> _option;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _realizedVolPeriod;
	private readonly StrategyParam<decimal> _tradingHoursPerYear;
	private readonly StrategyParam<decimal> _spikeRatio;
	private readonly StrategyParam<decimal> _vegaStopPoints;
	private readonly StrategyParam<decimal> _dividendYield;

	private BlackScholes _model;
	private StandardDeviation _returnDeviation;
	private TimeSpan _timeFrame;
	private decimal _annualization;
	private decimal? _previousClose;
	private decimal? _realizedVol;
	private DateTime? _underlyingTime;
	private decimal _underlyingClose;
	private DateTime? _optionTime;
	private decimal _optionClose;
	private DateTime? _matchedTime;
	private DateTime? _buyBackTime;
	private decimal? _lastImpliedVol;
	private decimal _entryImpliedVol;
	private decimal _entryRealizedVol;

	/// <summary>
	/// Initializes a new instance of the <see cref="VolatilityRiskPremiumStrategy"/>.
	/// </summary>
	public VolatilityRiskPremiumStrategy()
	{
		_option = Param<Security>(nameof(Option))
			.SetDisplay("Option", "Option contract to sell; its underlying is the strategy security", "General")
			.SetRequired();

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Bars on which volatility is measured, the option is priced and the hedge is rebalanced", "General");

		_realizedVolPeriod = Param(nameof(RealizedVolPeriod), 24)
			.SetGreaterThanZero()
			.SetDisplay("Realized Vol Period", "Number of bar-to-bar log returns in the realized volatility window", "Volatility")
			.SetOptimize(12, 96, 12);

		_tradingHoursPerYear = Param(nameof(TradingHoursPerYear), 8760m)
			.SetGreaterThanZero()
			.SetDisplay("Trading Hours Per Year", "Hours a year the underlying trades; realized volatility is annualized by the bars they hold (8760 round the clock, about 6240 for FX)", "Volatility");

		_spikeRatio = Param(nameof(SpikeRatio), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Spike Ratio", "Realized volatility, as a multiple of its level at the sale, that counts as a spike", "Risk")
			.SetOptimize(1.5m, 3m, 0.5m);

		_vegaStopPoints = Param(nameof(VegaStopPoints), 5m)
			.SetGreaterThanZero()
			.SetDisplay("Vega Stop (vol points)", "Rise of implied volatility above its level at the sale that stops out the short", "Risk")
			.SetOptimize(2m, 10m, 1m);

		_dividendYield = Param(nameof(DividendYield), 0m)
			.SetDisplay("Dividend Yield", "Annual dividend yield of the underlying, or the foreign rate for FX options", "Option Model");
	}

	/// <summary>
	/// Option contract to sell. Its <see cref="Security.UnderlyingSecurityId"/> must be the strategy security,
	/// which supplies the realized volatility and carries the delta hedge; its <see cref="Security.ExpiryDate"/>
	/// is the exact expiration moment in UTC and its <see cref="Security.Multiplier"/> the units of the
	/// underlying per contract.
	/// </summary>
	public Security Option
	{
		get => _option.Value;
		set => _option.Value = value;
	}

	/// <summary>
	/// Time-frame candles on which realized volatility is measured, the option is priced and the hedge is rebalanced.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Number of bar-to-bar log returns of the underlying in the realized volatility window.
	/// </summary>
	public int RealizedVolPeriod
	{
		get => _realizedVolPeriod.Value;
		set => _realizedVolPeriod.Value = value;
	}

	/// <summary>
	/// Hours a year in which the underlying trades. Realized volatility is annualized by the number of bars that
	/// fit in them: 8760 for a round-the-clock market, about 6240 for FX traded five days a week.
	/// </summary>
	public decimal TradingHoursPerYear
	{
		get => _tradingHoursPerYear.Value;
		set => _tradingHoursPerYear.Value = value;
	}

	/// <summary>
	/// Realized volatility, as a multiple of its level when the option was sold, at which the option is bought back.
	/// </summary>
	public decimal SpikeRatio
	{
		get => _spikeRatio.Value;
		set => _spikeRatio.Value = value;
	}

	/// <summary>
	/// Rise of implied volatility above its level when the option was sold, in volatility points, at which the
	/// short is stopped out: the loss it caps is this many vegas of the position.
	/// </summary>
	public decimal VegaStopPoints
	{
		get => _vegaStopPoints.Value;
		set => _vegaStopPoints.Value = value;
	}

	/// <summary>
	/// Annual continuous dividend yield of the underlying used by the Black-Scholes model; for FX options, the foreign
	/// rate. The model discounts the strike with <see cref="Strategy.RiskFreeRate"/>.
	/// </summary>
	public decimal DividendYield
	{
		get => _dividendYield.Value;
		set => _dividendYield.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security != null)
			yield return (Security, CandleType);

		if (Option != null)
			yield return (Option, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		ClearState();
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		// Reject an incomplete contract before any subscription starts.
		ValidateOption();

		base.OnStarted2(time);
		ClearState();

		_timeFrame = (TimeSpan)CandleType.Arg;
		// Variance per bar times the bars traded in a year; time to expiry stays in calendar time, as the option model needs.
		_annualization = (decimal)Math.Sqrt((double)TradingHoursPerYear * TimeSpan.TicksPerHour / _timeFrame.Ticks);
		_model = new BlackScholes(Option, Security, this, Option.ExpiryDate)
		{
			// The annual risk-free rate every strategy carries discounts the strike.
			RiskFree = RiskFreeRate,
			Dividend = DividendYield,
		};
		_returnDeviation = new StandardDeviation { Length = RealizedVolPeriod };
		Indicators.Add(_returnDeviation);

		var underlying = SubscribeCandles(CandleType);
		underlying.Bind(ProcessUnderlyingCandle).Start();

		SubscribeCandles(CandleType, security: Option)
			.Bind(ProcessOptionCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, underlying);
			DrawOwnTrades(area);
		}
	}

	private void ValidateOption()
	{
		var option = Option;

		if (Security is null || option is null || option.Id.EqualsIgnoreCase(Security.Id))
			throw new InvalidOperationException("Option must be an option contract distinct from the strategy security.");

		if (option.Type != SecurityTypes.Option || option.OptionType is null || option.Strike is not > 0m || option.ExpiryDate is null)
			throw new InvalidOperationException("Option must be an option contract with its type, a positive strike and an expiry.");

		if (!option.UnderlyingSecurityId.EqualsIgnoreCase(Security.Id))
			throw new InvalidOperationException("Option must be written on the strategy security, which measures realized volatility and carries the hedge.");

		if (!CandleType.IsTFCandles || CandleType.Arg is not TimeSpan frame || frame <= TimeSpan.Zero)
			throw new InvalidOperationException("CandleType must be a time-frame candle type for the Option bars.");
	}

	private void ClearState()
	{
		_model = null;
		_returnDeviation = null;
		_timeFrame = default;
		_annualization = 0m;
		_previousClose = null;
		_realizedVol = null;
		_underlyingTime = null;
		_underlyingClose = 0m;
		_optionTime = null;
		_optionClose = 0m;
		_matchedTime = null;
		_buyBackTime = null;
		_lastImpliedVol = null;
		_entryImpliedVol = 0m;
		_entryRealizedVol = 0m;
	}

	private void ProcessUnderlyingCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished || _underlyingTime is DateTime last && candle.OpenTime <= last)
			return;

		var close = candle.ClosePrice;

		if (_previousClose is decimal previous && previous > 0m && close > 0m)
		{
			var logReturn = (decimal)Math.Log((double)(close / previous));
			var deviation = _returnDeviation.Process(logReturn, candle.OpenTime, true).GetValue<decimal>();
			_realizedVol = _returnDeviation.IsFormed ? deviation * _annualization : null;
		}

		_previousClose = close;
		_underlyingTime = candle.OpenTime;
		_underlyingClose = close;

		ProcessUnderlyingBar();
		ProcessMatchedBar();
	}

	private void ProcessOptionCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished || _optionTime is DateTime last && candle.OpenTime <= last)
			return;

		_optionTime = candle.OpenTime;
		_optionClose = candle.ClosePrice;
		ProcessMatchedBar();
	}

	// Expiration, the spike exit and the hedge need no option price, so they never wait for the option to trade.
	// A hedge left without its option no longer offsets anything and is closed.
	private void ProcessUnderlyingBar()
	{
		if (_realizedVol is not decimal realizedVol || !IsFormedAndOnlineAndAllowTrading() || HasActiveOrders())
			return;

		var optionPosition = GetPositionValue(Option, Portfolio) ?? 0m;

		if (optionPosition < 0m)
			ManageShort(_underlyingTime.Value, realizedVol, -optionPosition);
		else if (optionPosition == 0m)
			CloseHedge();
	}

	private void ManageShort(DateTime openTime, decimal realizedVol, decimal shortContracts)
	{
		var time = openTime + _timeFrame;

		// The last bar that closes before expiration is the last chance to buy the option back.
		if (Option.ExpiryDate.Value - time <= _timeFrame)
			BuyBack(openTime, shortContracts, "expiration");
		else if (realizedVol >= SpikeRatio * _entryRealizedVol)
			BuyBack(openTime, shortContracts, "volatility spike");
		else if (_lastImpliedVol is decimal impliedVol)
			Rebalance(time, impliedVol, shortContracts);
	}

	// The sale and the vega stop price the option, so they need its close from the same bar as the underlying's.
	private void ProcessMatchedBar()
	{
		if (_underlyingTime is not DateTime openTime || _optionTime != openTime || _matchedTime == openTime)
			return;

		_matchedTime = openTime;

		var time = openTime + _timeFrame;
		var remaining = Option.ExpiryDate.Value - time;

		if (_realizedVol is not decimal realizedVol || remaining <= TimeSpan.Zero || ImpliedVolatility(time) is not decimal impliedVol)
			return;

		_lastImpliedVol = impliedVol;

		// One option trade per bar: a bar that bought the option back does not sell it again.
		if (_buyBackTime == openTime || !IsFormedAndOnlineAndAllowTrading() || HasActiveOrders())
			return;

		var optionPosition = GetPositionValue(Option, Portfolio) ?? 0m;

		if (optionPosition < 0m && impliedVol - _entryImpliedVol >= VegaStopPoints / 100m)
			BuyBack(openTime, -optionPosition, "vega stop");
		else if (optionPosition == 0m && Position == 0m)
			TrySell(time, remaining, realizedVol, impliedVol);
	}

	private void TrySell(DateTime time, TimeSpan remaining, decimal realizedVol, decimal impliedVol)
	{
		var strike = Option.Strike.Value;
		var isOutOfTheMoney = Option.OptionType == OptionTypes.Call ? strike > _underlyingClose : strike < _underlyingClose;

		if (impliedVol <= realizedVol || !isOutOfTheMoney || remaining <= _timeFrame)
			return;

		LogInfo($"Selling {Volume} option(s): IV={impliedVol} above RV={realizedVol}.");

		_entryImpliedVol = impliedVol;
		_entryRealizedVol = realizedVol;
		SendOrder(Sides.Sell, Volume, Option, _sellComment);
		Rebalance(time, impliedVol, Volume);
	}

	private void BuyBack(DateTime openTime, decimal shortContracts, string reason)
	{
		LogInfo($"Buying back {shortContracts} option(s) on {reason}: IV={_lastImpliedVol}, RV={_realizedVol}.");

		_buyBackTime = openTime;
		SendOrder(Sides.Buy, shortContracts, Option, _buyBackComment + reason);
		CloseHedge();
	}

	private void Rebalance(DateTime time, decimal impliedVol, decimal shortContracts)
	{
		if (_model.Delta(time, impliedVol, _underlyingClose) is not decimal delta)
			return;

		var multiplier = Option.Multiplier is decimal contractSize && contractSize > 0m ? contractSize : 1m;
		// Holding delta units of the underlying per unit of the short option neutralizes it: long for a call, short for a put.
		var target = shortContracts * multiplier * delta;
		var step = Security.VolumeStep ?? 0m;

		if (step > 0m)
			target = Math.Round(target / step, MidpointRounding.AwayFromZero) * step;

		var change = target - Position;

		if (change != 0m)
			SendOrder(change > 0m ? Sides.Buy : Sides.Sell, Math.Abs(change), Security, _hedgeComment);
	}

	private void CloseHedge()
	{
		var position = Position;

		if (position != 0m)
			SendOrder(position > 0m ? Sides.Sell : Sides.Buy, Math.Abs(position), Security, _closeHedgeComment);
	}

	private decimal? ImpliedVolatility(DateTime time)
		=> DerivativesHelper.ImpliedVolatility(_optionClose, deviation => _model.Premium(time, deviation, _underlyingClose)) / 100m;

	private bool HasActiveOrders()
		=> Orders.Any(order => order.State is not (OrderStates.Done or OrderStates.Failed));

	private void SendOrder(Sides side, decimal volume, Security security, string comment)
	{
		var order = CreateOrder(side, 0m, volume, security);
		order.Comment = comment;
		RegisterOrder(order);
	}
}
