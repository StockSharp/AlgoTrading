using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that goes long during high volatility evenings and exits before midnight.
/// Volatility is measured by standard deviation of log returns compared to median volatility.
/// </summary>
public class OvernightEffectHighVolatilityCryptoStrategy : Strategy
{
	private readonly StrategyParam<int> _volatilityPeriodDays;
	private readonly StrategyParam<int> _medianPeriodDays;
	private readonly StrategyParam<int> _entryHour;
	private readonly StrategyParam<int> _exitHour;
	private readonly StrategyParam<bool> _useVolatilityFilter;
	private readonly StrategyParam<DataType> _candleType;

	private StandardDeviation _volatilityStdDev = null!;
	private Median _medianVolatility = null!;
	private decimal _prevClose;
	private bool _inTrade;

	/// <summary>
	/// Days used for historical volatility calculation.
	/// </summary>
	public int VolatilityPeriodDays
	{
		get => _volatilityPeriodDays.Value;
		set => _volatilityPeriodDays.Value = value;
	}

	/// <summary>
	/// Days used for median volatility calculation.
	/// </summary>
	public int MedianPeriodDays
	{
		get => _medianPeriodDays.Value;
		set => _medianPeriodDays.Value = value;
	}

	/// <summary>
	/// Hour to enter the long position (UTC).
	/// </summary>
	public int EntryHour
	{
		get => _entryHour.Value;
		set => _entryHour.Value = value;
	}

	/// <summary>
	/// Hour to exit the position (UTC).
	/// </summary>
	public int ExitHour
	{
		get => _exitHour.Value;
		set => _exitHour.Value = value;
	}

	/// <summary>
	/// Enable high volatility filter.
	/// </summary>
	public bool UseVolatilityFilter
	{
		get => _useVolatilityFilter.Value;
		set => _useVolatilityFilter.Value = value;
	}

	/// <summary>
	/// Candle type for strategy calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public OvernightEffectHighVolatilityCryptoStrategy()
	{
		_volatilityPeriodDays = Param(nameof(VolatilityPeriodDays), 30)
			.SetGreaterThanZero()
			.SetDisplay("Volatility Period (Days)", "Days for historical volatility", "Parameters");

		_medianPeriodDays = Param(nameof(MedianPeriodDays), 208)
			.SetGreaterThanZero()
			.SetDisplay("Median Period (Days)", "Days for median volatility", "Parameters");

		_entryHour = Param(nameof(EntryHour), 21)
			.SetDisplay("Entry Hour", "Hour to enter long position (UTC)", "Parameters");

		_exitHour = Param(nameof(ExitHour), 23)
			.SetDisplay("Exit Hour", "Hour to exit position (UTC)", "Parameters");

		_useVolatilityFilter = Param(nameof(UseVolatilityFilter), true)
			.SetDisplay("Use Volatility Filter", "Require high volatility to enter", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for strategy", "General");
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

		_prevClose = 0m;
		_inTrade = false;
		_volatilityStdDev = null!;
		_medianVolatility = null!;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var volLength = VolatilityPeriodDays * 24;
		var medianLength = MedianPeriodDays * 24;

		_volatilityStdDev = new StandardDeviation { Length = volLength };
		_medianVolatility = new Median { Length = medianLength };

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

		if (_prevClose == 0m)
		{
			_prevClose = candle.ClosePrice;
			return;
		}

		var logReturn = (decimal)Math.Log((double)(candle.ClosePrice / _prevClose));
		_prevClose = candle.ClosePrice;

		var vol = _volatilityStdDev.Process(logReturn, candle.OpenTime, true);
		var median = _medianVolatility.Process(vol ?? 0m, candle.OpenTime, true);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var currentHour = candle.OpenTime.Hour;
		var isHighVol = vol is decimal v && median is decimal m && v > m;

		if (currentHour == EntryHour && !_inTrade && (!UseVolatilityFilter || isHighVol))
		{
			BuyMarket(Volume + Math.Abs(Position));
			_inTrade = true;
			return;
		}

		if (currentHour == ExitHour && _inTrade && Position > 0)
		{
			SellMarket(Math.Abs(Position));
			_inTrade = false;
		}
	}
}
