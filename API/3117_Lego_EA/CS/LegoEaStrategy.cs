using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi-indicator trend following strategy converted from the MetaTrader Lego EA.
/// Combines CCI, dual moving averages, stochastic oscillator, Accelerator, DeMarker and Awesome oscillators
/// to confirm entries and exits while increasing position size after losing trades.
/// </summary>
public class LegoEaStrategy : Strategy
{
	private readonly StrategyParam<decimal> _lotMultiplier;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<bool> _useCciForEntry;
	private readonly StrategyParam<bool> _useCciForExit;
	private readonly StrategyParam<bool> _useMaForEntry;
	private readonly StrategyParam<bool> _useMaForExit;
	private readonly StrategyParam<bool> _useStochasticForEntry;
	private readonly StrategyParam<bool> _useStochasticForExit;
	private readonly StrategyParam<bool> _useAcceleratorForEntry;
	private readonly StrategyParam<bool> _useAcceleratorForExit;
	private readonly StrategyParam<bool> _useDemarkerForEntry;
	private readonly StrategyParam<bool> _useDemarkerForExit;
	private readonly StrategyParam<bool> _useAwesomeForEntry;
	private readonly StrategyParam<bool> _useAwesomeForExit;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<int> _maFastPeriod;
	private readonly StrategyParam<int> _maSlowPeriod;
	private readonly StrategyParam<int> _maShift;
	private readonly StrategyParam<MaMethodOption> _maMethod;
	private readonly StrategyParam<CandlePrice> _maPrice;
	private readonly StrategyParam<int> _stochasticKPeriod;
	private readonly StrategyParam<int> _stochasticDPeriod;
	private readonly StrategyParam<int> _stochasticSlow;
	private readonly StrategyParam<decimal> _stochasticLevelUp;
	private readonly StrategyParam<decimal> _stochasticLevelDown;
	private readonly StrategyParam<int> _demarkerPeriod;
	private readonly StrategyParam<decimal> _demarkerLevelUp;
	private readonly StrategyParam<decimal> _demarkerLevelDown;
	private readonly StrategyParam<DataType> _candleType;

	private CommodityChannelIndex _cci = null!;
	private LengthIndicator<decimal> _maFast = null!;
	private LengthIndicator<decimal> _maSlow = null!;
	private StochasticOscillator _stochastic = null!;
	private AcceleratorOscillator _accelerator = null!;
	private DeMarker _deMarker = null!;
	private AwesomeOscillator _awesome = null!;

	private readonly List<decimal> _cciHistory = new();
	private readonly List<decimal> _maFastHistory = new();
	private readonly List<decimal> _maSlowHistory = new();
	private readonly List<decimal> _stochasticKHistory = new();
	private readonly List<decimal> _stochasticDHistory = new();
	private readonly List<decimal> _acceleratorHistory = new();
	private readonly List<decimal> _awesomeHistory = new();

	private decimal _lastTradeVolume;
	private bool _lastTradeWasLoss;

	/// <summary>
	/// Initializes a new instance of the <see cref="LegoEaStrategy"/> class.
	/// </summary>
	public LegoEaStrategy()
	{
		_lotMultiplier = Param(nameof(LotMultiplier), 2m)
			.SetDisplay("Lot Multiplier", "Multiplier applied after a losing trade", "Risk")
			.SetGreaterThanZero();

		_stopLossPips = Param(nameof(StopLossPips), 200)
			.SetDisplay("Stop Loss (pips)", "Distance to stop loss measured in pips", "Risk")
			.SetNotNegative();

		_takeProfitPips = Param(nameof(TakeProfitPips), 200)
			.SetDisplay("Take Profit (pips)", "Distance to take profit measured in pips", "Risk")
			.SetNotNegative();

		_useCciForEntry = Param(nameof(UseCciForEntry), false)
			.SetDisplay("Use CCI for Entries", "Enable Commodity Channel Index filter for entries", "Filters");

		_useCciForExit = Param(nameof(UseCciForExit), false)
			.SetDisplay("Use CCI for Exits", "Enable Commodity Channel Index filter for exits", "Filters");

		_useMaForEntry = Param(nameof(UseMaForEntry), true)
			.SetDisplay("Use MAs for Entries", "Enable dual moving averages filter for entries", "Filters");

		_useMaForExit = Param(nameof(UseMaForExit), true)
			.SetDisplay("Use MAs for Exits", "Enable dual moving averages filter for exits", "Filters");

		_useStochasticForEntry = Param(nameof(UseStochasticForEntry), false)
			.SetDisplay("Use Stochastic for Entries", "Enable stochastic oscillator filter for entries", "Filters");

		_useStochasticForExit = Param(nameof(UseStochasticForExit), false)
			.SetDisplay("Use Stochastic for Exits", "Enable stochastic oscillator filter for exits", "Filters");

		_useAcceleratorForEntry = Param(nameof(UseAcceleratorForEntry), false)
			.SetDisplay("Use Accelerator for Entries", "Enable Accelerator Oscillator confirmation for entries", "Filters");

		_useAcceleratorForExit = Param(nameof(UseAcceleratorForExit), false)
			.SetDisplay("Use Accelerator for Exits", "Enable Accelerator Oscillator confirmation for exits", "Filters");

		_useDemarkerForEntry = Param(nameof(UseDemarkerForEntry), false)
			.SetDisplay("Use DeMarker for Entries", "Enable DeMarker filter for entries", "Filters");

		_useDemarkerForExit = Param(nameof(UseDemarkerForExit), false)
			.SetDisplay("Use DeMarker for Exits", "Enable DeMarker filter for exits", "Filters");

		_useAwesomeForEntry = Param(nameof(UseAwesomeForEntry), false)
			.SetDisplay("Use Awesome for Entries", "Enable Awesome Oscillator filter for entries", "Filters");

		_useAwesomeForExit = Param(nameof(UseAwesomeForExit), false)
			.SetDisplay("Use Awesome for Exits", "Enable Awesome Oscillator filter for exits", "Filters");

		_cciPeriod = Param(nameof(CciPeriod), 14)
			.SetDisplay("CCI Period", "Averaging period for the Commodity Channel Index", "Indicators")
			.SetGreaterThanZero();

		_maFastPeriod = Param(nameof(MaFastPeriod), 14)
			.SetDisplay("Fast MA Period", "Lookback for the fast moving average", "Indicators")
			.SetGreaterThanZero();

		_maSlowPeriod = Param(nameof(MaSlowPeriod), 67)
			.SetDisplay("Slow MA Period", "Lookback for the slow moving average", "Indicators")
			.SetGreaterThanZero();

		_maShift = Param(nameof(MaShift), 1)
			.SetDisplay("MA Shift", "Number of completed bars used to offset moving averages", "Indicators")
			.SetNotNegative();

		_maMethod = Param(nameof(MaMethod), MaMethodOption.Simple)
			.SetDisplay("MA Method", "Smoothing method for moving averages", "Indicators");

		_maPrice = Param(nameof(MaPrice), CandlePrice.Close)
			.SetDisplay("MA Price", "Price source for moving averages", "Indicators");

		_stochasticKPeriod = Param(nameof(StochasticKPeriod), 5)
			.SetDisplay("Stochastic %K Period", "Number of bars for the %K calculation", "Indicators")
			.SetGreaterThanZero();

		_stochasticDPeriod = Param(nameof(StochasticDPeriod), 3)
			.SetDisplay("Stochastic %D Period", "Length of the %D smoothing", "Indicators")
			.SetGreaterThanZero();

		_stochasticSlow = Param(nameof(StochasticSlow), 3)
			.SetDisplay("Stochastic Smoothing", "Final smoothing factor for the stochastic", "Indicators")
			.SetGreaterThanZero();

		_stochasticLevelUp = Param(nameof(StochasticLevelUp), 30m)
			.SetDisplay("Stochastic Oversold", "Upper threshold treated as oversold", "Levels");

		_stochasticLevelDown = Param(nameof(StochasticLevelDown), 70m)
			.SetDisplay("Stochastic Overbought", "Lower threshold treated as overbought", "Levels");

		_demarkerPeriod = Param(nameof(DemarkerPeriod), 14)
			.SetDisplay("DeMarker Period", "Averaging period for the DeMarker oscillator", "Indicators")
			.SetGreaterThanZero();

		_demarkerLevelUp = Param(nameof(DemarkerLevelUp), 0.7m)
			.SetDisplay("DeMarker Upper Level", "Upper threshold for DeMarker", "Levels");

		_demarkerLevelDown = Param(nameof(DemarkerLevelDown), 0.3m)
			.SetDisplay("DeMarker Lower Level", "Lower threshold for DeMarker", "Levels");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame used for calculations", "General");
	}


	/// <summary>
	/// Multiplier applied to the previous trade volume after a losing trade.
	/// </summary>
	public decimal LotMultiplier
	{
		get => _lotMultiplier.Value;
		set => _lotMultiplier.Value = value;
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
	/// Enable Commodity Channel Index filter for entries.
	/// </summary>
	public bool UseCciForEntry
	{
		get => _useCciForEntry.Value;
		set => _useCciForEntry.Value = value;
	}

	/// <summary>
	/// Enable Commodity Channel Index filter for exits.
	/// </summary>
	public bool UseCciForExit
	{
		get => _useCciForExit.Value;
		set => _useCciForExit.Value = value;
	}

	/// <summary>
	/// Enable moving average filter for entries.
	/// </summary>
	public bool UseMaForEntry
	{
		get => _useMaForEntry.Value;
		set => _useMaForEntry.Value = value;
	}

	/// <summary>
	/// Enable moving average filter for exits.
	/// </summary>
	public bool UseMaForExit
	{
		get => _useMaForExit.Value;
		set => _useMaForExit.Value = value;
	}

	/// <summary>
	/// Enable stochastic oscillator filter for entries.
	/// </summary>
	public bool UseStochasticForEntry
	{
		get => _useStochasticForEntry.Value;
		set => _useStochasticForEntry.Value = value;
	}

	/// <summary>
	/// Enable stochastic oscillator filter for exits.
	/// </summary>
	public bool UseStochasticForExit
	{
		get => _useStochasticForExit.Value;
		set => _useStochasticForExit.Value = value;
	}

	/// <summary>
	/// Enable Accelerator Oscillator filter for entries.
	/// </summary>
	public bool UseAcceleratorForEntry
	{
		get => _useAcceleratorForEntry.Value;
		set => _useAcceleratorForEntry.Value = value;
	}

	/// <summary>
	/// Enable Accelerator Oscillator filter for exits.
	/// </summary>
	public bool UseAcceleratorForExit
	{
		get => _useAcceleratorForExit.Value;
		set => _useAcceleratorForExit.Value = value;
	}

	/// <summary>
	/// Enable DeMarker filter for entries.
	/// </summary>
	public bool UseDemarkerForEntry
	{
		get => _useDemarkerForEntry.Value;
		set => _useDemarkerForEntry.Value = value;
	}

	/// <summary>
	/// Enable DeMarker filter for exits.
	/// </summary>
	public bool UseDemarkerForExit
	{
		get => _useDemarkerForExit.Value;
		set => _useDemarkerForExit.Value = value;
	}

	/// <summary>
	/// Enable Awesome Oscillator filter for entries.
	/// </summary>
	public bool UseAwesomeForEntry
	{
		get => _useAwesomeForEntry.Value;
		set => _useAwesomeForEntry.Value = value;
	}

	/// <summary>
	/// Enable Awesome Oscillator filter for exits.
	/// </summary>
	public bool UseAwesomeForExit
	{
		get => _useAwesomeForExit.Value;
		set => _useAwesomeForExit.Value = value;
	}

	/// <summary>
	/// Averaging period for the Commodity Channel Index.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// Fast moving average lookback.
	/// </summary>
	public int MaFastPeriod
	{
		get => _maFastPeriod.Value;
		set => _maFastPeriod.Value = value;
	}

	/// <summary>
	/// Slow moving average lookback.
	/// </summary>
	public int MaSlowPeriod
	{
		get => _maSlowPeriod.Value;
		set => _maSlowPeriod.Value = value;
	}

	/// <summary>
	/// Number of completed bars used to offset moving averages.
	/// </summary>
	public int MaShift
	{
		get => _maShift.Value;
		set => _maShift.Value = value;
	}

	/// <summary>
	/// Moving average smoothing method.
	/// </summary>
	public MaMethodOption MaMethod
	{
		get => _maMethod.Value;
		set => _maMethod.Value = value;
	}

	/// <summary>
	/// Price source for moving averages.
	/// </summary>
	public CandlePrice MaPrice
	{
		get => _maPrice.Value;
		set => _maPrice.Value = value;
	}

	/// <summary>
	/// Length of the stochastic %K line.
	/// </summary>
	public int StochasticKPeriod
	{
		get => _stochasticKPeriod.Value;
		set => _stochasticKPeriod.Value = value;
	}

	/// <summary>
	/// Length of the stochastic %D smoothing.
	/// </summary>
	public int StochasticDPeriod
	{
		get => _stochasticDPeriod.Value;
		set => _stochasticDPeriod.Value = value;
	}

	/// <summary>
	/// Final smoothing applied to the stochastic oscillator.
	/// </summary>
	public int StochasticSlow
	{
		get => _stochasticSlow.Value;
		set => _stochasticSlow.Value = value;
	}

	/// <summary>
	/// Threshold treated as oversold by the stochastic oscillator.
	/// </summary>
	public decimal StochasticLevelUp
	{
		get => _stochasticLevelUp.Value;
		set => _stochasticLevelUp.Value = value;
	}

	/// <summary>
	/// Threshold treated as overbought by the stochastic oscillator.
	/// </summary>
	public decimal StochasticLevelDown
	{
		get => _stochasticLevelDown.Value;
		set => _stochasticLevelDown.Value = value;
	}

	/// <summary>
	/// Averaging period for the DeMarker indicator.
	/// </summary>
	public int DemarkerPeriod
	{
		get => _demarkerPeriod.Value;
		set => _demarkerPeriod.Value = value;
	}

	/// <summary>
	/// Upper DeMarker threshold.
	/// </summary>
	public decimal DemarkerLevelUp
	{
		get => _demarkerLevelUp.Value;
		set => _demarkerLevelUp.Value = value;
	}

	/// <summary>
	/// Lower DeMarker threshold.
	/// </summary>
	public decimal DemarkerLevelDown
	{
		get => _demarkerLevelDown.Value;
		set => _demarkerLevelDown.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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

		_cciHistory.Clear();
		_maFastHistory.Clear();
		_maSlowHistory.Clear();
		_stochasticKHistory.Clear();
		_stochasticDHistory.Clear();
		_acceleratorHistory.Clear();
		_awesomeHistory.Clear();

		_lastTradeVolume = Volume;
		_lastTradeWasLoss = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_cci = new CommodityChannelIndex
		{
			Length = Math.Max(1, CciPeriod),
			CandlePrice = CandlePrice.Typical
		};

		_maFast = CreateMovingAverage(MaMethod, MaFastPeriod, MaPrice);
		_maSlow = CreateMovingAverage(MaMethod, MaSlowPeriod, MaPrice);

		_stochastic = new StochasticOscillator
		{
			Length = Math.Max(1, StochasticKPeriod),
			K = { Length = Math.Max(1, StochasticKPeriod) },
			D = { Length = Math.Max(1, StochasticDPeriod) },
			Smooth = Math.Max(1, StochasticSlow)
		};

		_accelerator = new AcceleratorOscillator();
		_deMarker = new DeMarker { Length = Math.Max(1, DemarkerPeriod) };
		_awesome = new AwesomeOscillator();

		var subscription = SubscribeCandles(CandleType);

		subscription
			.BindEx(new IIndicator[] { _cci, _maFast, _maSlow, _stochastic, _accelerator, _deMarker, _awesome }, ProcessCandle)
			.Start();

		var pipSize = GetPipSize();

		StartProtection(
			takeProfit: TakeProfitPips > 0 ? new Unit(TakeProfitPips * pipSize, UnitTypes.Absolute) : default,
			stopLoss: StopLossPips > 0 ? new Unit(StopLossPips * pipSize, UnitTypes.Absolute) : default);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _maFast);
			DrawIndicator(area, _maSlow);
			DrawIndicator(area, _cci);
			DrawIndicator(area, _stochastic);
			DrawIndicator(area, _accelerator);
			DrawIndicator(area, _deMarker);
			DrawIndicator(area, _awesome);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue[] values)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_cci.IsFormed || !_maFast.IsFormed || !_maSlow.IsFormed || !_stochastic.IsFormed || !_accelerator.IsFormed || !_deMarker.IsFormed || !_awesome.IsFormed)
			return;

		var cciValue = values[0].ToDecimal();
		var maFastValue = values[1].ToDecimal();
		var maSlowValue = values[2].ToDecimal();

		var stochasticValue = (StochasticOscillatorValue)values[3];
		if (stochasticValue.K is not decimal stochK || stochasticValue.D is not decimal stochD)
			return;

		var acceleratorValue = values[4].ToDecimal();
		var demarkerValue = values[5].ToDecimal();
		var awesomeValue = values[6].ToDecimal();

		AddHistory(_cciHistory, cciValue, 10);
		AddHistory(_maFastHistory, maFastValue, Math.Max(10, MaShift + 3));
		AddHistory(_maSlowHistory, maSlowValue, Math.Max(10, MaShift + 3));
		AddHistory(_stochasticKHistory, stochK, 10);
		AddHistory(_stochasticDHistory, stochD, 10);
		AddHistory(_acceleratorHistory, acceleratorValue, 4);
		AddHistory(_awesomeHistory, awesomeValue, 4);

		var effectiveMaOffset = Math.Max(1, MaShift + 1);

		var cciReady = TryGetHistoryValue(_cciHistory, 1, out var prevCci);
		var maFastReady = TryGetHistoryValue(_maFastHistory, effectiveMaOffset, out var shiftedMaFast);
		var maSlowReady = TryGetHistoryValue(_maSlowHistory, effectiveMaOffset, out var shiftedMaSlow);
		var stochasticReady = TryGetHistoryValue(_stochasticKHistory, 1, out var prevK)
			&& TryGetHistoryValue(_stochasticDHistory, 1, out var prevD);
		var acceleratorReady = _acceleratorHistory.Count >= 4;
		var awesomeReady = _awesomeHistory.Count >= 2;

		if ((UseCciForEntry || UseCciForExit) && !cciReady)
			return;

		if ((UseMaForEntry || UseMaForExit) && (!maFastReady || !maSlowReady))
			return;

		if ((UseStochasticForEntry || UseStochasticForExit) && !stochasticReady)
			return;

		if ((UseAcceleratorForEntry || UseAcceleratorForExit) && !acceleratorReady)
			return;

		if ((UseAwesomeForEntry || UseAwesomeForExit) && !awesomeReady)
			return;

		var maBuy = false;
		var maSell = false;
		var stoBuy = false;
		var stoSell = false;
		var acceleratorBuy = false;
		var acceleratorSell = false;
		var awesomeBuy = false;
		var awesomeSell = false;
		var cciBuy = false;
		var cciSell = false;
		var demarkerBuy = false;
		var demarkerSell = false;

		if (UseCciForEntry || UseCciForExit)
		{
			cciBuy = prevCci < -100m;
			cciSell = prevCci > 100m;
		}

		if (UseMaForEntry || UseMaForExit)
		{
			maBuy = shiftedMaFast > shiftedMaSlow;
			maSell = shiftedMaFast < shiftedMaSlow;
		}

		if (UseStochasticForEntry || UseStochasticForExit)
		{
			stoBuy = prevK > prevD && prevD < StochasticLevelUp;
			stoSell = prevK < prevD && prevD > StochasticLevelDown;
		}

		if (UseAcceleratorForEntry || UseAcceleratorForExit)
		{
			var ac0 = _acceleratorHistory[^1];
			var ac1 = _acceleratorHistory[^2];
			var ac2 = _acceleratorHistory[^3];
			var ac3 = _acceleratorHistory[^4];

			acceleratorBuy = (ac0 >= 0m && ac0 > ac1 && ac1 > ac2) ||
				(ac0 <= 0m && ac0 > ac1 && ac1 > ac2 && ac2 > ac3);

			acceleratorSell = (ac0 <= 0m && ac0 < ac1 && ac1 < ac2) ||
				(ac0 >= 0m && ac0 < ac1 && ac1 < ac2 && ac2 < ac3);
		}

		if (UseDemarkerForEntry || UseDemarkerForExit)
		{
			demarkerBuy = demarkerValue < DemarkerLevelDown;
			demarkerSell = demarkerValue > DemarkerLevelUp;
		}

		if (UseAwesomeForEntry || UseAwesomeForExit)
		{
			var ao0 = _awesomeHistory[^1];
			var ao1 = _awesomeHistory[^2];
			awesomeBuy = ao0 > ao1;
			awesomeSell = ao0 < ao1;
		}

		var openBuy = (!UseCciForEntry || cciBuy)
			&& (!UseMaForEntry || maBuy)
			&& (!UseStochasticForEntry || stoBuy)
			&& (!UseAcceleratorForEntry || acceleratorBuy)
			&& (!UseDemarkerForEntry || demarkerBuy)
			&& (!UseAwesomeForEntry || awesomeBuy);

		var openSell = (!UseCciForEntry || cciSell)
			&& (!UseMaForEntry || maSell)
			&& (!UseStochasticForEntry || stoSell)
			&& (!UseAcceleratorForEntry || acceleratorSell)
			&& (!UseDemarkerForEntry || demarkerSell)
			&& (!UseAwesomeForEntry || awesomeSell);

		var closeBuy = (!UseCciForExit || cciSell)
			&& (!UseMaForExit || maSell)
			&& (!UseStochasticForExit || stoSell)
			&& (!UseAcceleratorForExit || acceleratorSell)
			&& (!UseDemarkerForExit || demarkerSell)
			&& (!UseAwesomeForExit || awesomeSell);

		var closeSell = (!UseCciForExit || cciBuy)
			&& (!UseMaForExit || maBuy)
			&& (!UseStochasticForExit || stoBuy)
			&& (!UseAcceleratorForExit || acceleratorBuy)
			&& (!UseDemarkerForExit || demarkerBuy)
			&& (!UseAwesomeForExit || awesomeBuy);

		if (Position == 0m)
		{
			if (openBuy && !openSell)
			{
				var volume = CalculateNextVolume();
				if (volume > 0m)
					BuyMarket(volume);
			}
			else if (openSell && !openBuy)
			{
				var volume = CalculateNextVolume();
				if (volume > 0m)
					SellMarket(volume);
			}
		}
		else if (Position > 0m)
		{
			if (closeBuy && !closeSell)
				SellMarket(Position);
		}
		else if (Position < 0m)
		{
			if (closeSell && !closeBuy)
				BuyMarket(Math.Abs(Position));
		}
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (trade.Trade?.Security != Security)
			return;

		if (trade.Trade.Volume <= 0m)
			return;

		if (Position != 0m)
			return;

		_lastTradeVolume = trade.Trade.Volume;
		_lastTradeWasLoss = trade.PnL < 0m;
	}

	private decimal CalculateNextVolume()
	{
		var baseVolume = _lastTradeWasLoss ? _lastTradeVolume * LotMultiplier : Volume;
		return ApplyVolumeLimits(baseVolume);
	}

	private decimal ApplyVolumeLimits(decimal volume)
	{
		var security = Security;
		if (security == null)
			return volume;

		var step = security.VolumeStep;
		if (step.HasValue && step.Value > 0m)
		{
			var steps = Math.Floor(volume / step.Value);
			volume = steps * step.Value;
		}

		var minVolume = security.MinVolume;
		if (minVolume.HasValue && volume < minVolume.Value)
			volume = minVolume.Value;

		var maxVolume = security.MaxVolume;
		if (maxVolume.HasValue && volume > maxVolume.Value)
			volume = maxVolume.Value;

		return volume;
	}

	private static void AddHistory(List<decimal> history, decimal value, int maxCount)
	{
		history.Add(value);
		if (history.Count > maxCount)
			history.RemoveAt(0);
	}

	private static bool TryGetHistoryValue(List<decimal> history, int offset, out decimal value)
	{
		value = 0m;
		var index = history.Count - offset - 1;
		if (index < 0)
			return false;

		value = history[index];
		return true;
	}

	private decimal GetPipSize()
	{
		var priceStep = Security?.PriceStep ?? 0m;
		if (priceStep <= 0m)
			return 1m;

		var decimals = Security?.Decimals;
		var pipFactor = decimals == 3 || decimals == 5 ? 10m : 1m;
		return priceStep * pipFactor;
	}

	private static LengthIndicator<decimal> CreateMovingAverage(MaMethodOption method, int length, CandlePrice price)
	{
		LengthIndicator<decimal> indicator = method switch
		{
			MaMethodOption.Simple => new SimpleMovingAverage(),
			MaMethodOption.Exponential => new ExponentialMovingAverage(),
			MaMethodOption.Smoothed => new SmoothedMovingAverage(),
			MaMethodOption.Weighted => new WeightedMovingAverage(),
			_ => new SimpleMovingAverage()
		};

		indicator.Length = Math.Max(1, length);
		indicator.CandlePrice = price;
		return indicator;
	}

	/// <summary>
	/// Available moving average smoothing methods.
	/// </summary>
	public enum MaMethodOption
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
		/// Smoothed moving average.
		/// </summary>
		Smoothed,
		/// <summary>
		/// Linear weighted moving average.
		/// </summary>
		Weighted
	}
}
