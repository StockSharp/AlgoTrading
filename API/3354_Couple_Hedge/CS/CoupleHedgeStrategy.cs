using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi-pair hedging strategy converted from the CoupleHedge MT4 expert.
/// Combines several FX pairs into hedged baskets and manages them using
/// grid-style scaling rules.
/// </summary>
public class CoupleHedgeStrategy : Strategy
{
	/// <summary>
	/// Operating mode for the strategy.
	/// </summary>
	public enum Oper
	{
		StandByMode,
		NormalOperation,
		CloseInProfitAndStop,
		CloseImmediatelyAllOrders
	}

	/// <summary>
	/// Behaviour for opening additional orders while in drawdown.
	/// </summary>
	public enum StepMode
	{
		NotOpenInLoss,
		OpenWithManualStep,
		OpenWithAutoStep
	}

	/// <summary>
	/// Profit closing logic.
	/// </summary>
	public enum CloseProfitMode
	{
		TicketOrders,
		BasketOrders,
		HybridMode
	}

	/// <summary>
	/// Loss closing logic.
	/// </summary>
	public enum CloseLossMode
	{
		WholeTicket,
		OnlyFirstOrder,
		NotCloseInLoss
	}

	/// <summary>
	/// Progression type for spacing between additional entries.
	/// </summary>
	public enum StepProgression
	{
		StaticalStep,
		GeometricalStep,
		ExponentialStep
	}

	/// <summary>
	/// Progression type for order size scaling.
	/// </summary>
	public enum LotProgression
	{
		StaticalLot,
		GeometricalLot,
		ExponentialLot
	}

	/// <summary>
	/// Which sides are allowed to trade.
	/// </summary>
	public enum SideMode
	{
		TradeOnlyPlus,
		TradeOnlyMinus,
		TradePlusAndMinus
	}

	private sealed class PairState
	{
		public required Security Security { get; init; }
		public required string Name { get; init; }
		public SimpleMovingAverage Trend { get; } = new();
		public AverageTrueRange Atr { get; } = new();
		public int LevelsOpened { get; set; }
		public int Direction { get; set; }
		public int ProfitDelayCounter { get; set; }
		public int LossDelayCounter { get; set; }
		public decimal BaseVolume { get; set; }
	}

	private readonly StrategyParam<Oper> _typeOperation;
	private readonly StrategyParam<StepMode> _openOrdersInLoss;
	private readonly StrategyParam<CloseProfitMode> _typeCloseInProfit;
	private readonly StrategyParam<CloseLossMode> _typeCloseInLoss;
	private readonly StrategyParam<StepProgression> _stepOrdersProgress;
	private readonly StrategyParam<LotProgression> _lotOrdersProgress;
	private readonly StrategyParam<SideMode> _sideToOpenOrders;
	private readonly StrategyParam<string> _currencyTrade;
	private readonly StrategyParam<decimal> _stepOpenNextOrders;
	private readonly StrategyParam<decimal> _targetCloseProfit;
	private readonly StrategyParam<int> _delayCloseProfit;
	private readonly StrategyParam<decimal> _targetCloseLoss;
	private readonly StrategyParam<int> _delayCloseLoss;
	private readonly StrategyParam<int> _maximumOrders;
	private readonly StrategyParam<bool> _autoLotSize;
	private readonly StrategyParam<decimal> _riskFactor;
	private readonly StrategyParam<decimal> _manualLotSize;
	private readonly StrategyParam<int> _trendPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _signalThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private List<PairState> _pairs;
	private int _basketProfitDelay;

	/// <summary>
	/// Initializes strategy parameters with defaults derived from the original EA.
	/// </summary>
	public CoupleHedgeStrategy()
	{
		_typeOperation = Param(nameof(TypeOperation), Oper.NormalOperation)
		.SetDisplay("Operation Mode", "Overall operating mode for the hedging engine", "Operation")
		.SetCanOptimize(true);

		_openOrdersInLoss = Param(nameof(OpenOrdersInLoss), StepMode.OpenWithAutoStep)
		.SetDisplay("Open Orders In Loss", "How additional orders are triggered during drawdown", "Grid")
		.SetCanOptimize(true);

		_typeCloseInProfit = Param(nameof(TypeCloseInProfit), CloseProfitMode.BasketOrders)
		.SetDisplay("Profit Close Mode", "How profits trigger position exits", "Risk Management")
		.SetCanOptimize(true);

		_typeCloseInLoss = Param(nameof(TypeCloseInLoss), CloseLossMode.NotCloseInLoss)
		.SetDisplay("Loss Close Mode", "How losses are handled", "Risk Management")
		.SetCanOptimize(true);

		_stepOrdersProgress = Param(nameof(StepOrdersProgress), StepProgression.GeometricalStep)
		.SetDisplay("Step Progression", "Progression for distance between entries", "Grid")
		.SetCanOptimize(true);

		_lotOrdersProgress = Param(nameof(LotOrdersProgress), LotProgression.StaticalLot)
		.SetDisplay("Lot Progression", "How order sizes scale when adding layers", "Grid")
		.SetCanOptimize(true);

		_sideToOpenOrders = Param(nameof(SideToOpenOrders), SideMode.TradePlusAndMinus)
		.SetDisplay("Sides", "Allowed trading direction", "Operation")
		.SetCanOptimize(true);

		_currencyTrade = Param(nameof(CurrencyTrade), "EUR/GBP/USD")
		.SetDisplay("Currencies", "Ordered list of currencies to generate FX pairs", "General");

		_stepOpenNextOrders = Param(nameof(StepOpenNextOrders), 50m)
		.SetGreaterThanZero()
		.SetDisplay("Step ($/lot)", "Loss per lot required to add the next order", "Grid")
		.SetCanOptimize(true);

		_targetCloseProfit = Param(nameof(TargetCloseProfit), 50m)
		.SetNotNegative()
		.SetDisplay("Target Profit", "Profit per lot required to close", "Risk Management")
		.SetCanOptimize(true);

		_delayCloseProfit = Param(nameof(DelayCloseProfit), 3)
		.SetNotNegative()
		.SetDisplay("Profit Delay", "Number of candles to wait before closing in profit", "Risk Management");

		_targetCloseLoss = Param(nameof(TargetCloseLoss), 1000m)
		.SetNotNegative()
		.SetDisplay("Target Loss", "Loss per lot that triggers protective exit", "Risk Management")
		.SetCanOptimize(true);

		_delayCloseLoss = Param(nameof(DelayCloseLoss), 3)
		.SetNotNegative()
		.SetDisplay("Loss Delay", "Number of candles to wait before closing in loss", "Risk Management");

		_maximumOrders = Param(nameof(MaximumOrders), 0)
		.SetNotNegative()
		.SetDisplay("Maximum Orders", "Maximum number of layers per pair (0 = unlimited)", "Grid");

		_autoLotSize = Param(nameof(AutoLotSize), false)
		.SetDisplay("Auto Lot", "Enable balance-based position sizing", "Money Management");

		_riskFactor = Param(nameof(RiskFactor), 0.1m)
		.SetNotNegative()
		.SetDisplay("Risk Factor", "Risk factor used for automatic sizing", "Money Management");

		_manualLotSize = Param(nameof(ManualLotSize), 0.01m)
		.SetGreaterThanZero()
		.SetDisplay("Manual Lot", "Manual lot size when auto sizing is disabled", "Money Management");

		_trendPeriod = Param(nameof(TrendPeriod), 34)
		.SetGreaterThanZero()
		.SetDisplay("Trend Period", "Period for the trend moving average", "Indicators");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("ATR Period", "Period for the ATR indicator", "Indicators");

		_signalThreshold = Param(nameof(SignalThreshold), 0.3m)
		.SetNotNegative()
		.SetDisplay("Signal Threshold", "Minimum ATR-normalized deviation required for entry", "Indicators")
		.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Time frame used for analysis", "General");
	}

	/// <summary>
	/// Selected operation mode.
	/// </summary>
	public Oper TypeOperation
	{
		get => _typeOperation.Value;
		set => _typeOperation.Value = value;
	}

	/// <summary>
	/// How to open additional orders when the basket is in loss.
	/// </summary>
	public StepMode OpenOrdersInLoss
	{
		get => _openOrdersInLoss.Value;
		set => _openOrdersInLoss.Value = value;
	}

	/// <summary>
	/// Profit closing mode.
	/// </summary>
	public CloseProfitMode TypeCloseInProfit
	{
		get => _typeCloseInProfit.Value;
		set => _typeCloseInProfit.Value = value;
	}

	/// <summary>
	/// Loss closing mode.
	/// </summary>
	public CloseLossMode TypeCloseInLoss
	{
		get => _typeCloseInLoss.Value;
		set => _typeCloseInLoss.Value = value;
	}

	/// <summary>
	/// Step progression.
	/// </summary>
	public StepProgression StepOrdersProgress
	{
		get => _stepOrdersProgress.Value;
		set => _stepOrdersProgress.Value = value;
	}

	/// <summary>
	/// Lot progression.
	/// </summary>
	public LotProgression LotOrdersProgress
	{
		get => _lotOrdersProgress.Value;
		set => _lotOrdersProgress.Value = value;
	}

	/// <summary>
	/// Allowed trading side.
	/// </summary>
	public SideMode SideToOpenOrders
	{
		get => _sideToOpenOrders.Value;
		set => _sideToOpenOrders.Value = value;
	}

	/// <summary>
	/// Ordered currencies used to create hedging pairs.
	/// </summary>
	public string CurrencyTrade
	{
		get => _currencyTrade.Value;
		set => _currencyTrade.Value = value;
	}

	/// <summary>
	/// Step value (per lot) that must be lost before adding another layer.
	/// </summary>
	public decimal StepOpenNextOrders
	{
		get => _stepOpenNextOrders.Value;
		set => _stepOpenNextOrders.Value = value;
	}

	/// <summary>
	/// Profit target per lot.
	/// </summary>
	public decimal TargetCloseProfit
	{
		get => _targetCloseProfit.Value;
		set => _targetCloseProfit.Value = value;
	}

	/// <summary>
	/// Delay (in candles) before closing in profit.
	/// </summary>
	public int DelayCloseProfit
	{
		get => _delayCloseProfit.Value;
		set => _delayCloseProfit.Value = value;
	}

	/// <summary>
	/// Loss target per lot.
	/// </summary>
	public decimal TargetCloseLoss
	{
		get => _targetCloseLoss.Value;
		set => _targetCloseLoss.Value = value;
	}

	/// <summary>
	/// Delay (in candles) before closing in loss.
	/// </summary>
	public int DelayCloseLoss
	{
		get => _delayCloseLoss.Value;
		set => _delayCloseLoss.Value = value;
	}

	/// <summary>
	/// Maximum number of layers per pair.
	/// </summary>
	public int MaximumOrders
	{
		get => _maximumOrders.Value;
		set => _maximumOrders.Value = value;
	}

	/// <summary>
	/// Whether automatic lot sizing is enabled.
	/// </summary>
	public bool AutoLotSize
	{
		get => _autoLotSize.Value;
		set => _autoLotSize.Value = value;
	}

	/// <summary>
	/// Risk factor used when automatic sizing is enabled.
	/// </summary>
	public decimal RiskFactor
	{
		get => _riskFactor.Value;
		set => _riskFactor.Value = value;
	}

	/// <summary>
	/// Manual lot size when automatic sizing is disabled.
	/// </summary>
	public decimal ManualLotSize
	{
		get => _manualLotSize.Value;
		set => _manualLotSize.Value = value;
	}

	/// <summary>
	/// Trend moving average period.
	/// </summary>
	public int TrendPeriod
	{
		get => _trendPeriod.Value;
		set => _trendPeriod.Value = value;
	}

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// ATR-normalized entry threshold.
	/// </summary>
	public decimal SignalThreshold
	{
		get => _signalThreshold.Value;
		set => _signalThreshold.Value = value;
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		EnsurePairsInitialized();
		return _pairs!.Select(p => (p.Security, CandleType));
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_pairs = null;
		_basketProfitDelay = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (TypeOperation == Oper.CloseImmediatelyAllOrders)
		{
			CloseAllPairs(time);
			Stop();
			return;
		}

		EnsurePairsInitialized();

		foreach (var pair in _pairs!)
		{
			pair.Trend.Length = TrendPeriod;
			pair.Atr.Length = AtrPeriod;
			pair.BaseVolume = NormalizeVolume(CalculateBaseVolume(pair.Security), pair.Security);

			var subscription = SubscribeCandles(CandleType, true, pair.Security);
			subscription
			.Bind(pair.Trend, pair.Atr, (candle, trend, atr) => ProcessPair(candle, pair, trend, atr))
			.Start();
		}

		var area = CreateChartArea();
		if (area != null && _pairs!.Count > 0)
		{
			// Draw candles for the primary security so the user can follow the basket visually.
			var subscription = SubscribeCandles(CandleType);
			subscription.Start();
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessPair(ICandleMessage candle, PairState pair, decimal trend, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var volume = GetPositionValue(pair.Security, Portfolio) ?? 0m;
		var absVolume = Math.Abs(volume);
		var position = Positions.FirstOrDefault(p => p.Security == pair.Security);
		var profit = position?.PnL ?? 0m;

		UpdateDirectionState(pair, volume);

		// Manage profit-based exits before considering new entries.
		HandleProfitExit(candle, pair, profit, absVolume);

		// Manage loss-based exits.
		HandleLossExit(candle, pair, profit, absVolume);

		if (!IsTradingAllowedForNewOrders())
		return;

		// Evaluate scaling logic if we already have exposure.
		if (absVolume > 0m)
		{
			HandleScaling(pair, profit, absVolume, atr);
			return;
		}

		// No exposure -> consider opening a new basket according to the strength ranking.
		TryOpenInitialOrder(candle, pair, trend, atr);
	}

	private void TryOpenInitialOrder(ICandleMessage candle, PairState pair, decimal trend, decimal atr)
	{
		if (TypeOperation == Oper.StandByMode)
		return;

		var deviation = atr > 0m ? (candle.ClosePrice - trend) / atr : 0m;
		var desireLong = deviation >= SignalThreshold && (SideToOpenOrders == SideMode.TradePlusAndMinus || SideToOpenOrders == SideMode.TradeOnlyPlus);
		var desireShort = deviation <= -SignalThreshold && (SideToOpenOrders == SideMode.TradePlusAndMinus || SideToOpenOrders == SideMode.TradeOnlyMinus);

		if (!desireLong && !desireShort)
		return;

		var volume = pair.BaseVolume;
		if (volume <= 0m)
		return;

		if (desireLong && (!desireShort || deviation >= 0m))
		{
			BuyMarket(volume, pair.Security);
			pair.Direction = 1;
			pair.LevelsOpened = 1;
		}
		else if (desireShort)
		{
			SellMarket(volume, pair.Security);
			pair.Direction = -1;
			pair.LevelsOpened = 1;
		}
	}

	private void HandleScaling(PairState pair, decimal profit, decimal absVolume, decimal atr)
	{
		if (OpenOrdersInLoss == StepMode.NotOpenInLoss)
		return;

		var lotLoss = absVolume > 0m ? -profit / absVolume : 0m;
		if (lotLoss <= 0m)
		return;

		var threshold = CalculateStepThreshold(pair.LevelsOpened, atr);
		if (lotLoss < threshold)
		return;

		if (MaximumOrders > 0 && pair.LevelsOpened >= MaximumOrders)
		return;

		var additional = CalculateOrderVolume(pair, pair.LevelsOpened);
		if (additional <= 0m)
		return;

		if (pair.Direction > 0)
		{
			BuyMarket(additional, pair.Security);
		}
		else if (pair.Direction < 0)
		{
			SellMarket(additional, pair.Security);
		}

		pair.LevelsOpened++;
	}

	private void HandleProfitExit(ICandleMessage candle, PairState pair, decimal profit, decimal absVolume)
	{
		if (TargetCloseProfit <= 0m || absVolume <= 0m)
		{
			pair.ProfitDelayCounter = 0;
			return;
		}

		var targetPerLot = TargetCloseProfit * GetLotMultiplier(absVolume);
		var profitReached = profit >= targetPerLot;

		if (TypeCloseInProfit == CloseProfitMode.BasketOrders)
		{
			HandleBasketProfitExit();
			pair.ProfitDelayCounter = 0;
			return;
		}

		if (TypeCloseInProfit == CloseProfitMode.HybridMode)
		{
			HandleBasketProfitExit();
		}

		if (!profitReached)
		{
			pair.ProfitDelayCounter = 0;
			return;
		}

		if (pair.ProfitDelayCounter < DelayCloseProfit)
		{
			pair.ProfitDelayCounter++;
			return;
		}

		ClosePair(pair);

		if (TypeOperation == Oper.CloseInProfitAndStop)
		{
			Stop();
		}
	}

	private void HandleBasketProfitExit()
	{
		if (TargetCloseProfit <= 0m)
		return;

		var totalVolume = _pairs!.Sum(p => Math.Abs(GetPositionValue(p.Security, Portfolio) ?? 0m));
		if (totalVolume <= 0m)
		{
			_basketProfitDelay = 0;
			return;
		}

		var totalProfit = _pairs.Sum(p => Positions.FirstOrDefault(pos => pos.Security == p.Security)?.PnL ?? 0m);
		var target = TargetCloseProfit * GetLotMultiplier(totalVolume);

		if (totalProfit < target)
		{
			_basketProfitDelay = 0;
			return;
		}

		if (_basketProfitDelay < DelayCloseProfit)
		{
			_basketProfitDelay++;
			return;
		}

		CloseAllPairs(CurrentTime);

		if (TypeOperation == Oper.CloseInProfitAndStop)
		{
			Stop();
		}
	}

	private void HandleLossExit(ICandleMessage candle, PairState pair, decimal profit, decimal absVolume)
	{
		if (TargetCloseLoss <= 0m || absVolume <= 0m)
		{
			pair.LossDelayCounter = 0;
			return;
		}

		var lotLoss = absVolume > 0m ? -profit / absVolume : 0m;
		var threshold = TargetCloseLoss * GetLotMultiplier(1m);

		if (lotLoss < threshold)
		{
			pair.LossDelayCounter = 0;
			return;
		}

		if (pair.LossDelayCounter < DelayCloseLoss)
		{
			pair.LossDelayCounter++;
			return;
		}

		switch (TypeCloseInLoss)
		{
			case CloseLossMode.NotCloseInLoss:
			pair.LossDelayCounter = 0;
			return;
			case CloseLossMode.WholeTicket:
			ClosePair(pair);
			break;
			case CloseLossMode.OnlyFirstOrder:
			var baseVolume = pair.BaseVolume;
			if (baseVolume <= 0m)
			return;

			if (pair.Direction > 0)
			{
				SellMarket(Math.Min(absVolume, baseVolume), pair.Security);
			}
			else if (pair.Direction < 0)
			{
				BuyMarket(Math.Min(absVolume, baseVolume), pair.Security);
			}
			break;
		}

		pair.LossDelayCounter = 0;
	}

	private void ClosePair(PairState pair)
	{
		var volume = GetPositionValue(pair.Security, Portfolio) ?? 0m;
		if (volume == 0m)
		return;

		if (volume > 0m)
		{
			SellMarket(volume, pair.Security);
		}
		else
		{
			BuyMarket(Math.Abs(volume), pair.Security);
		}

		pair.LevelsOpened = 0;
		pair.Direction = 0;
		pair.ProfitDelayCounter = 0;
		pair.LossDelayCounter = 0;
	}

	private void CloseAllPairs(DateTimeOffset time)
	{
		if (_pairs == null)
		return;

		foreach (var pair in _pairs)
		{
			ClosePair(pair);
		}
	}

	private void UpdateDirectionState(PairState pair, decimal volume)
	{
		if (volume == 0m)
		{
			pair.Direction = 0;
			pair.LevelsOpened = 0;
			return;
		}

		pair.Direction = volume > 0m ? 1 : -1;
	}

	private bool IsTradingAllowedForNewOrders()
	{
		return TypeOperation == Oper.NormalOperation || TypeOperation == Oper.CloseInProfitAndStop;
	}

	private decimal CalculateStepThreshold(int levelIndex, decimal atr)
	{
		var baseThreshold = OpenOrdersInLoss switch
		{
			StepMode.OpenWithAutoStep => atr * StepOpenNextOrders,
			StepMode.OpenWithManualStep => StepOpenNextOrders,
			_ => 0m
		};

		if (baseThreshold <= 0m)
		return 0m;

		var multiplier = StepOrdersProgress switch
		{
			StepProgression.StaticalStep => 1m,
			StepProgression.GeometricalStep => 1m + levelIndex,
			StepProgression.ExponentialStep => (decimal)Math.Pow(2, levelIndex),
			_ => 1m
		};

		return baseThreshold * multiplier;
	}

	private decimal CalculateOrderVolume(PairState pair, int levelIndex)
	{
		var baseVolume = pair.BaseVolume;
		if (baseVolume <= 0m)
		return 0m;

		var multiplier = LotOrdersProgress switch
		{
			LotProgression.StaticalLot => 1m,
			LotProgression.GeometricalLot => 1m + levelIndex,
			LotProgression.ExponentialLot => (decimal)Math.Pow(2, levelIndex),
			_ => 1m
		};

		return NormalizeVolume(baseVolume * multiplier, pair.Security);
	}

	private decimal NormalizeVolume(decimal volume, Security security)
	{
		if (volume <= 0m)
		return 0m;

		var step = security.VolumeStep ?? 1m;
		var min = security.MinVolume ?? step;
		var max = security.MaxVolume;

		var normalized = Math.Max(min, Math.Round(volume / step) * step);
		if (max.HasValue)
		{
			normalized = Math.Min(normalized, max.Value);
		}

		return normalized;
	}

	private decimal CalculateBaseVolume(Security security)
	{
		if (AutoLotSize)
		{
			var equity = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;
			if (equity > 0m)
			{
				// Use a simple risk-based sizing approximation: allocate RiskFactor percent of equity per lot.
				var risk = RiskFactor / 100m;
				var lot = equity * risk;
				return Math.Max(lot, ManualLotSize);
			}
		}

		return ManualLotSize;
	}

	private decimal GetLotMultiplier(decimal volume)
	{
		return Math.Max(1m, volume);
	}

	private void EnsurePairsInitialized()
	{
		if (_pairs != null)
		return;

		_pairs = ResolvePairs().ToList();

		if (_pairs.Count == 0)
		{
			if (Security != null)
			{
				_pairs.Add(new PairState
				{
					Security = Security,
					Name = Security.Id
				});
			}
			else
			{
				throw new InvalidOperationException("No securities resolved for hedging.");
			}
		}
	}

	private IEnumerable<PairState> ResolvePairs()
	{
		var result = new List<PairState>();
		var tokens = (CurrencyTrade ?? string.Empty)
		.Split(new[] { '/', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
		.Select(t => t.Trim().ToUpperInvariant())
		.Where(t => t.Length > 0)
		.ToArray();

		var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		if (tokens.Length >= 2)
		{
			for (var i = 0; i < tokens.Length - 1; i++)
			{
				for (var j = i + 1; j < tokens.Length; j++)
				{
					var id = tokens[i] + tokens[j];
					if (!seen.Add(id))
					continue;

					var security = LookupSecurity(id) ?? LookupSecurity(tokens[j] + tokens[i]);
					if (security == null)
					continue;

					result.Add(new PairState
					{
						Security = security,
						Name = security.Id
					});
				}
			}
		}

		if (result.Count == 0 && Security != null)
		{
			result.Add(new PairState
			{
				Security = Security,
				Name = Security.Id
			});
		}

		return result;
	}

	private Security LookupSecurity(string id)
	{
		if (string.IsNullOrEmpty(id))
		return null;

		var provider = SecurityProvider;
		if (provider == null)
		{
			if (Security != null && string.Equals(Security.Id, id, StringComparison.OrdinalIgnoreCase))
			return Security;

			return null;
		}

		var security = provider.LookupById(id);
		if (security != null)
		return security;

		if (id.Length == 6)
		{
			var withSlash = id[..3] + "/" + id[3..];
			security = provider.LookupById(withSlash);
			if (security != null)
			return security;
		}

		return provider.LookupById(id.Replace("/", string.Empty, StringComparison.OrdinalIgnoreCase));
	}
}
