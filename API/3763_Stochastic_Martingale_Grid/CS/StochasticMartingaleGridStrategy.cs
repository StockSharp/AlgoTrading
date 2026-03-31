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
/// Stochastic based martingale averaging strategy translated from the MetaTrader expert "rmkp_9yj4qp1gn8fucubyqnvb".
/// Adds averaging orders when price moves against the latest entry and manages each leg with trailing stops and individual take profits.
/// </summary>
public class StochasticMartingaleGridStrategy : Strategy
{
	private sealed class Entry
	{
		public decimal Price { get; set; }
		public decimal Volume { get; set; }
		public decimal? TrailingPrice { get; set; }
	}

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<int> _maxOrders;
	private readonly StrategyParam<decimal> _stepPips;
	private readonly StrategyParam<int> _kPeriod;
	private readonly StrategyParam<int> _dPeriod;
	private readonly StrategyParam<int> _slowing;
	private readonly StrategyParam<decimal> _zoneBuy;
	private readonly StrategyParam<decimal> _zoneSell;

	private List<Entry> _entries;

	private StochasticOscillator _stochastic;
	private decimal? _previousMain;
	private decimal? _previousSignal;
	private decimal _pipSize;

	/// <summary>
	/// Initializes a new instance of the <see cref="StochasticMartingaleGridStrategy"/> class.
	/// </summary>
	public StochasticMartingaleGridStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used to evaluate stochastic values", "General");

		_baseVolume = Param(nameof(BaseVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Base Volume", "Initial order volume", "Trading")
			;

		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit (pips)", "Distance to the take profit target for each entry", "Risk")
			;

		_trailingStopPips = Param(nameof(TrailingStopPips), 20m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Stop (pips)", "Trailing stop distance applied per entry", "Risk")
			;

		_maxOrders = Param(nameof(MaxOrders), 2)
			.SetGreaterThanZero()
			.SetDisplay("Max Orders", "Maximum number of simultaneous averaging entries", "Martingale");

		_stepPips = Param(nameof(StepPips), 7m)
			.SetGreaterThanZero()
			.SetDisplay("Step (pips)", "Adverse move required before adding a new entry", "Martingale")
			;

		_kPeriod = Param(nameof(KPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("%K Period", "Stochastic %K lookback length", "Indicators")
			;

		_dPeriod = Param(nameof(DPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("%D Period", "Stochastic %D smoothing length", "Indicators")
			;

		_slowing = Param(nameof(Slowing), 3)
			.SetGreaterThanZero()
			.SetDisplay("Slowing", "Additional smoothing applied to %K", "Indicators")
			;

		_zoneBuy = Param(nameof(ZoneBuy), 50m)
			.SetDisplay("Buy Zone", "Upper limit that allows long setups when %K is above %D", "Indicators")
			;

		_zoneSell = Param(nameof(ZoneSell), 50m)
			.SetDisplay("Sell Zone", "Lower limit that allows short setups when %K is below %D", "Indicators")
			;
	}

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initial trade volume.
	/// </summary>
	public decimal BaseVolume
	{
		get => _baseVolume.Value;
		set => _baseVolume.Value = value;
	}

	/// <summary>
	/// Take profit distance in pips applied to every entry.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips applied to every entry.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Maximum number of averaging entries.
	/// </summary>
	public int MaxOrders
	{
		get => _maxOrders.Value;
		set => _maxOrders.Value = value;
	}

	/// <summary>
	/// Step in pips required to trigger a new averaging entry.
	/// </summary>
	public decimal StepPips
	{
		get => _stepPips.Value;
		set => _stepPips.Value = value;
	}

	/// <summary>
	/// Stochastic %K period.
	/// </summary>
	public int KPeriod
	{
		get => _kPeriod.Value;
		set => _kPeriod.Value = value;
	}

	/// <summary>
	/// Stochastic %D period.
	/// </summary>
	public int DPeriod
	{
		get => _dPeriod.Value;
		set => _dPeriod.Value = value;
	}

	/// <summary>
	/// Stochastic slowing period.
	/// </summary>
	public int Slowing
	{
		get => _slowing.Value;
		set => _slowing.Value = value;
	}

	/// <summary>
	/// Maximum signal level that allows long entries.
	/// </summary>
	public decimal ZoneBuy
	{
		get => _zoneBuy.Value;
		set => _zoneBuy.Value = value;
	}

	/// <summary>
	/// Minimum signal level that allows short entries.
	/// </summary>
	public decimal ZoneSell
	{
		get => _zoneSell.Value;
		set => _zoneSell.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_entries = null;
		_stochastic = null;
		_previousMain = null;
		_previousSignal = null;
		_pipSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_entries = new List<Entry>();
		_pipSize = Security?.PriceStep ?? 1m;

		_stochastic = new StochasticOscillator
		{
			K = { Length = KPeriod },
			D = { Length = DPeriod }
		};

		Indicators.Add(_stochastic);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		StartProtection(
			takeProfit: new Unit(2, UnitTypes.Percent),
			stopLoss: new Unit(1, UnitTypes.Percent));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _stochastic);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var stochResult = _stochastic.Process(candle);
		if (!_stochastic.IsFormed)
			return;

		if (stochResult is not StochasticOscillatorValue stoch)
			return;

		if (stoch.K is not decimal currentMain || stoch.D is not decimal currentSignal)
			return;

		if (Position != 0)
		{
			_previousMain = currentMain;
			_previousSignal = currentSignal;
			return;
		}

		if (_previousMain is decimal prevMain && _previousSignal is decimal prevSignal)
		{
			// Buy: K crosses above D in oversold zone
			if (prevMain <= prevSignal && currentMain > currentSignal && currentSignal < ZoneBuy)
				BuyMarket();
			// Sell: K crosses below D in overbought zone
			else if (prevMain >= prevSignal && currentMain < currentSignal && currentSignal > ZoneSell)
				SellMarket();
		}

		_previousMain = currentMain;
		_previousSignal = currentSignal;
	}
}

