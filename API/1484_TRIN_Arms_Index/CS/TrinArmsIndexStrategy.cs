using System;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Long-only strategy using the TRIN (Arms Index) with a volatility-based stop.
/// </summary>
public class TrinArmsIndexStrategy : Strategy
{
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<decimal> _stopLossMultiplier;
	private readonly StrategyParam<int> _volatilityLookback;
	private readonly StrategyParam<Security> _trinSecurity;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _trinIndex;
	private decimal _trinThreshold;
	private decimal _prevHigh;
	private decimal _entryPrice;
	private bool _trinReady;
	private bool _hasPrevHigh;

	/// <summary>
	/// Initializes a new instance of <see cref="TrinArmsIndexStrategy"/>.
	/// </summary>
	public TrinArmsIndexStrategy()
	{
		_lookbackPeriod = Param(nameof(LookbackPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("Lookback Period", "Lookback for TRIN threshold", "Parameters");

		_multiplier = Param(nameof(Multiplier), 2.7m)
			.SetGreaterThanZero()
			.SetDisplay("Multiplier", "Multiplier for dynamic threshold", "Parameters");

		_stopLossMultiplier = Param(nameof(StopLossMultiplier), 3.4m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss Multiplier", "Multiplier for volatility stop", "Risk");

		_volatilityLookback = Param(nameof(VolatilityLookback), 24)
			.SetGreaterThanZero()
			.SetDisplay("Volatility Lookback", "Lookback for volatility", "Parameters");

		_trinSecurity = Param(nameof(TrinSecurity), new Security { Id = "INDEX:TRIN" })
			.SetDisplay("TRIN Security", "Security for TRIN index", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <summary>
	/// Lookback period for TRIN calculations.
	/// </summary>
	public int LookbackPeriod
	{
		get => _lookbackPeriod.Value;
		set => _lookbackPeriod.Value = value;
	}

	/// <summary>
	/// Multiplier for TRIN threshold.
	/// </summary>
	public decimal Multiplier
	{
		get => _multiplier.Value;
		set => _multiplier.Value = value;
	}

	/// <summary>
	/// Multiplier for volatility-based stop.
	/// </summary>
	public decimal StopLossMultiplier
	{
		get => _stopLossMultiplier.Value;
		set => _stopLossMultiplier.Value = value;
	}

	/// <summary>
	/// Lookback period for volatility calculation.
	/// </summary>
	public int VolatilityLookback
	{
		get => _volatilityLookback.Value;
		set => _volatilityLookback.Value = value;
	}

	/// <summary>
	/// Security providing TRIN index data.
	/// </summary>
	public Security TrinSecurity
	{
		get => _trinSecurity.Value;
		set => _trinSecurity.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_trinIndex = 0m;
		_trinThreshold = 0m;
		_prevHigh = 0m;
		_entryPrice = 0m;
		_trinReady = false;
		_hasPrevHigh = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var trinMa = new SimpleMovingAverage { Length = LookbackPeriod };
		var trinStd = new StandardDeviation { Length = LookbackPeriod };
		var priceStd = new StandardDeviation { Length = VolatilityLookback };

		SubscribeCandles(CandleType, false, TrinSecurity)
			.Bind(trinMa, trinStd, ProcessTrin)
			.Start();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(priceStd, ProcessMain)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessTrin(ICandleMessage candle, decimal ma, decimal std)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_trinIndex = candle.ClosePrice;
		_trinThreshold = ma + Multiplier * std;
		_trinReady = true;
	}

	private void ProcessMain(ICandleMessage candle, decimal priceStd)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position > 0)
		{
			var stopPrice = _entryPrice - StopLossMultiplier * priceStd;
			if (candle.ClosePrice <= stopPrice || (_hasPrevHigh && candle.ClosePrice > _prevHigh))
			{
				SellMarket(Math.Abs(Position));
			}
		}
		else if (_trinReady && _trinIndex > _trinThreshold)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_entryPrice = candle.ClosePrice;
		}

		_prevHigh = candle.HighPrice;
		_hasPrevHigh = true;
	}
}
