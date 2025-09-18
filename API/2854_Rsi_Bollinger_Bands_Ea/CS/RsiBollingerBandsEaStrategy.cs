namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// RSI Bollinger Bands strategy converted from the MetaTrader 5 expert adviser.
/// Combines fixed RSI overbought/oversold thresholds and adaptive Bollinger-based RSI bands across M15, H1, and H4 timeframes.
/// </summary>
public class RsiBollingerBandsEaStrategy : Strategy
{
	private readonly StrategyParam<bool> _triggerOne;
	private readonly StrategyParam<bool> _triggerTwo;

	private readonly StrategyParam<decimal> _bbSpreadH4Min1;
	private readonly StrategyParam<decimal> _bbSpreadM15Max1;
	private readonly StrategyParam<int> _rsiPeriod1;
	private readonly StrategyParam<decimal> _rsiLoM15_1;
	private readonly StrategyParam<decimal> _rsiHiM15_1;
	private readonly StrategyParam<decimal> _rsiLoH1_1;
	private readonly StrategyParam<decimal> _rsiHiH1_1;
	private readonly StrategyParam<decimal> _rsiLoH4_1;
	private readonly StrategyParam<decimal> _rsiHiH4_1;
	private readonly StrategyParam<decimal> _rsiHiLimH4_1;
	private readonly StrategyParam<decimal> _rsiLoLimH4_1;
	private readonly StrategyParam<decimal> _rsiHiLimH1_1;
	private readonly StrategyParam<decimal> _rsiLoLimH1_1;
	private readonly StrategyParam<decimal> _rsiHiLimM15_1;
	private readonly StrategyParam<decimal> _rsiLoLimM15_1;
	private readonly StrategyParam<decimal> _rDeltaM15Lim1;
	private readonly StrategyParam<decimal> _stocLoM15_1;
	private readonly StrategyParam<decimal> _stocHiM15_1;

	private readonly StrategyParam<int> _rsiPeriod2;
	private readonly StrategyParam<decimal> _bbSpreadH4Min2;
	private readonly StrategyParam<decimal> _bbSpreadM15Max2;
	private readonly StrategyParam<int> _numRsi;
	private readonly StrategyParam<decimal> _rsiM15Sigma2;
	private readonly StrategyParam<decimal> _rsiH1Sigma2;
	private readonly StrategyParam<decimal> _rsiH4Sigma2;
	private readonly StrategyParam<decimal> _rsiM15SigmaLim2;
	private readonly StrategyParam<decimal> _rsiH1SigmaLim2;
	private readonly StrategyParam<decimal> _rsiH4SigmaLim2;
	private readonly StrategyParam<decimal> _rDeltaM15Lim2;
	private readonly StrategyParam<decimal> _stocLoM15_2;
	private readonly StrategyParam<decimal> _stocHiM15_2;

	private readonly StrategyParam<decimal> _takeProfitBuy1;
	private readonly StrategyParam<decimal> _stopLossBuy1;
	private readonly StrategyParam<decimal> _takeProfitSell1;
	private readonly StrategyParam<decimal> _stopLossSell1;

	private readonly StrategyParam<decimal> _takeProfitBuy2;
	private readonly StrategyParam<decimal> _stopLossBuy2;
	private readonly StrategyParam<decimal> _takeProfitSell2;
	private readonly StrategyParam<decimal> _stopLossSell2;

	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _atrLimit;
	private readonly StrategyParam<int> _entryHour;
	private readonly StrategyParam<int> _openHours;
	private readonly StrategyParam<int> _numPositions;
	private readonly StrategyParam<int> _fridayEndHour;
	private readonly StrategyParam<int> _stochasticK;
	private readonly StrategyParam<int> _stochasticD;
	private readonly StrategyParam<int> _stochasticSlowing;

	private readonly StrategyParam<DataType> _m15CandleType;
	private readonly StrategyParam<DataType> _h1CandleType;
	private readonly StrategyParam<DataType> _h4CandleType;

	private RelativeStrengthIndex _rsiM15Trigger1 = null!;
	private RelativeStrengthIndex _rsiH1Trigger1 = null!;
	private RelativeStrengthIndex _rsiH4Trigger1 = null!;

	private RelativeStrengthIndex _rsiM15Trigger2 = null!;
	private RelativeStrengthIndex _rsiH1Trigger2 = null!;
	private RelativeStrengthIndex _rsiH4Trigger2 = null!;

	private StochasticOscillator _stochastic = null!;
	private BollingerBands _bollingerM15 = null!;
	private BollingerBands _bollingerH4 = null!;
	private AverageTrueRange _atrH4 = null!;

	private decimal? _rsiM15Current1;
	private decimal? _rsiM15Previous1;
	private decimal? _rsiH1Current1;
	private decimal? _rsiH4Current1;

	private decimal? _rsiM15Current2;
	private decimal? _rsiM15Previous2;
	private decimal? _rsiH1Current2;
	private decimal? _rsiH4Current2;

	private readonly Queue<decimal> _rsiM15History2 = new();
	private readonly Queue<decimal> _rsiH1History2 = new();
	private readonly Queue<decimal> _rsiH4History2 = new();

	private decimal? _stochasticMain;
	private decimal? _bbSpreadM15Pips;
	private decimal? _bbSpreadH4Pips;
	private decimal? _atrH4Pips;

	private decimal? _longStopPrice;
	private decimal? _longTakePrice;
	private decimal? _shortStopPrice;
	private decimal? _shortTakePrice;

	private decimal _pipSize;

	public RsiBollingerBandsEaStrategy()
	{
		_triggerOne = Param(nameof(TriggerOne), true)
		.SetDisplay("Trigger One", "Enable fixed RSI overbought/oversold trigger", "General");

		_triggerTwo = Param(nameof(TriggerTwo), false)
		.SetDisplay("Trigger Two", "Enable Bollinger-based RSI trigger", "General");

		_bbSpreadH4Min1 = Param(nameof(BbSpreadH4Min1), 84m)
		.SetDisplay("H4 BB spread min (Trig1)", "Minimum H4 Bollinger spread in pips for trigger one", "Trigger One");

		_bbSpreadM15Max1 = Param(nameof(BbSpreadM15Max1), 64m)
		.SetDisplay("M15 BB spread max (Trig1)", "Maximum M15 Bollinger spread in pips for trigger one", "Trigger One");

		_rsiPeriod1 = Param(nameof(RsiPeriod1), 10)
		.SetDisplay("RSI Period 1", "RSI length for trigger one", "Trigger One");

		_rsiLoM15_1 = Param(nameof(RsiLoM15_1), 24m)
		.SetDisplay("RSI M15 Low", "Oversold threshold on M15 for trigger one", "Trigger One");

		_rsiHiM15_1 = Param(nameof(RsiHiM15_1), 66m)
		.SetDisplay("RSI M15 High", "Overbought threshold on M15 for trigger one", "Trigger One");

		_rsiLoH1_1 = Param(nameof(RsiLoH1_1), 34m)
		.SetDisplay("RSI H1 Low", "Oversold threshold on H1 for trigger one", "Trigger One");

		_rsiHiH1_1 = Param(nameof(RsiHiH1_1), 54m)
		.SetDisplay("RSI H1 High", "Overbought threshold on H1 for trigger one", "Trigger One");

		_rsiLoH4_1 = Param(nameof(RsiLoH4_1), 48m)
		.SetDisplay("RSI H4 Low", "Oversold threshold on H4 for trigger one", "Trigger One");

		_rsiHiH4_1 = Param(nameof(RsiHiH4_1), 56m)
		.SetDisplay("RSI H4 High", "Overbought threshold on H4 for trigger one", "Trigger One");

		_rsiHiLimH4_1 = Param(nameof(RsiHiLimH4_1), 85m)
		.SetDisplay("RSI H4 High Limit", "Maximum allowed RSI on H4 for trigger one", "Trigger One");

		_rsiLoLimH4_1 = Param(nameof(RsiLoLimH4_1), 35m)
		.SetDisplay("RSI H4 Low Limit", "Minimum allowed RSI on H4 for trigger one", "Trigger One");

		_rsiHiLimH1_1 = Param(nameof(RsiHiLimH1_1), 80m)
		.SetDisplay("RSI H1 High Limit", "Maximum allowed RSI on H1 for trigger one", "Trigger One");

		_rsiLoLimH1_1 = Param(nameof(RsiLoLimH1_1), 24m)
		.SetDisplay("RSI H1 Low Limit", "Minimum allowed RSI on H1 for trigger one", "Trigger One");

		_rsiHiLimM15_1 = Param(nameof(RsiHiLimM15_1), 92m)
		.SetDisplay("RSI M15 High Limit", "Maximum allowed RSI on M15 for trigger one", "Trigger One");

		_rsiLoLimM15_1 = Param(nameof(RsiLoLimM15_1), 20m)
		.SetDisplay("RSI M15 Low Limit", "Minimum allowed RSI on M15 for trigger one", "Trigger One");

		_rDeltaM15Lim1 = Param(nameof(RDeltaM15Lim1), -3.5m)
		.SetDisplay("RSI Delta Limit 1", "Minimum RSI slope on M15 for trigger one", "Trigger One");

		_stocLoM15_1 = Param(nameof(StocLoM15_1), 26m)
		.SetDisplay("Stochastic Low 1", "Maximum stochastic value to allow longs for trigger one", "Trigger One");

		_stocHiM15_1 = Param(nameof(StocHiM15_1), 64m)
		.SetDisplay("Stochastic High 1", "Minimum stochastic value to allow shorts for trigger one", "Trigger One");

		_rsiPeriod2 = Param(nameof(RsiPeriod2), 20)
		.SetDisplay("RSI Period 2", "RSI length for trigger two", "Trigger Two");

		_bbSpreadH4Min2 = Param(nameof(BbSpreadH4Min2), 65m)
		.SetDisplay("H4 BB spread min (Trig2)", "Minimum H4 Bollinger spread in pips for trigger two", "Trigger Two");

		_bbSpreadM15Max2 = Param(nameof(BbSpreadM15Max2), 75m)
		.SetDisplay("M15 BB spread max (Trig2)", "Maximum M15 Bollinger spread in pips for trigger two", "Trigger Two");

		_numRsi = Param(nameof(NumRsi), 60)
		.SetDisplay("RSI Samples", "Sample size for Bollinger RSI statistics", "Trigger Two");

		_rsiM15Sigma2 = Param(nameof(RsiM15Sigma2), 1.20m)
		.SetDisplay("RSI M15 Sigma", "Standard deviation multiplier on M15", "Trigger Two");

		_rsiH1Sigma2 = Param(nameof(RsiH1Sigma2), 0.95m)
		.SetDisplay("RSI H1 Sigma", "Standard deviation multiplier on H1", "Trigger Two");

		_rsiH4Sigma2 = Param(nameof(RsiH4Sigma2), 0.9m)
		.SetDisplay("RSI H4 Sigma", "Standard deviation multiplier on H4", "Trigger Two");

		_rsiM15SigmaLim2 = Param(nameof(RsiM15SigmaLim2), 1.85m)
		.SetDisplay("RSI M15 Sigma Limit", "Maximum band width on M15", "Trigger Two");

		_rsiH1SigmaLim2 = Param(nameof(RsiH1SigmaLim2), 2.55m)
		.SetDisplay("RSI H1 Sigma Limit", "Maximum band width on H1", "Trigger Two");

		_rsiH4SigmaLim2 = Param(nameof(RsiH4SigmaLim2), 2.7m)
		.SetDisplay("RSI H4 Sigma Limit", "Maximum band width on H4", "Trigger Two");

		_rDeltaM15Lim2 = Param(nameof(RDeltaM15Lim2), -5.5m)
		.SetDisplay("RSI Delta Limit 2", "Minimum RSI slope on M15 for trigger two", "Trigger Two");

		_stocLoM15_2 = Param(nameof(StocLoM15_2), 24m)
		.SetDisplay("Stochastic Low 2", "Maximum stochastic value to allow longs for trigger two", "Trigger Two");

		_stocHiM15_2 = Param(nameof(StocHiM15_2), 68m)
		.SetDisplay("Stochastic High 2", "Minimum stochastic value to allow shorts for trigger two", "Trigger Two");

		_takeProfitBuy1 = Param(nameof(TakeProfitBuy1), 150m)
		.SetDisplay("Take Profit Buy 1", "Take profit in pips for trigger-one buys", "Money Management");

		_stopLossBuy1 = Param(nameof(StopLossBuy1), 70m)
		.SetDisplay("Stop Loss Buy 1", "Stop loss in pips for trigger-one buys", "Money Management");

		_takeProfitSell1 = Param(nameof(TakeProfitSell1), 70m)
		.SetDisplay("Take Profit Sell 1", "Take profit in pips for trigger-one sells", "Money Management");

		_stopLossSell1 = Param(nameof(StopLossSell1), 35m)
		.SetDisplay("Stop Loss Sell 1", "Stop loss in pips for trigger-one sells", "Money Management");

		_takeProfitBuy2 = Param(nameof(TakeProfitBuy2), 140m)
		.SetDisplay("Take Profit Buy 2", "Take profit in pips for trigger-two buys", "Money Management");

		_stopLossBuy2 = Param(nameof(StopLossBuy2), 35m)
		.SetDisplay("Stop Loss Buy 2", "Stop loss in pips for trigger-two buys", "Money Management");

		_takeProfitSell2 = Param(nameof(TakeProfitSell2), 60m)
		.SetDisplay("Take Profit Sell 2", "Take profit in pips for trigger-two sells", "Money Management");

		_stopLossSell2 = Param(nameof(StopLossSell2), 30m)
		.SetDisplay("Stop Loss Sell 2", "Stop loss in pips for trigger-two sells", "Money Management");

		_atrPeriod = Param(nameof(AtrPeriod), 60)
		.SetDisplay("ATR Period", "ATR length calculated on H4 candles", "Filters");

		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
		.SetDisplay("Bollinger Period", "Bollinger Bands length for spread filters", "Filters");

		_atrLimit = Param(nameof(AtrLimit), 90m)
		.SetDisplay("ATR Limit", "Maximum ATR in pips to allow trading", "Filters");

		_entryHour = Param(nameof(EntryHour), 0)
		.SetDisplay("Entry Hour", "Trading window opening hour", "Session");

		_openHours = Param(nameof(OpenHours), 14)
		.SetDisplay("Open Hours", "Length of trading window in hours", "Session");

		_numPositions = Param(nameof(NumPositions), 1)
		.SetDisplay("Max Positions", "Maximum simultaneous positions", "Risk");

		_fridayEndHour = Param(nameof(FridayEndHour), 4)
		.SetDisplay("Friday End Hour", "Hour on Friday to stop trading", "Session");

		_stochasticK = Param(nameof(StochasticK), 12)
		.SetDisplay("Stochastic K", "Main period for stochastic oscillator", "Indicators");

		_stochasticD = Param(nameof(StochasticD), 5)
		.SetDisplay("Stochastic D", "Signal period for stochastic oscillator", "Indicators");

		_stochasticSlowing = Param(nameof(StochasticSlowing), 5)
		.SetDisplay("Stochastic Slowing", "Smoothing for stochastic oscillator", "Indicators");

		_m15CandleType = Param(nameof(M15CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("M15 Candle", "Primary trading timeframe", "General");

		_h1CandleType = Param(nameof(H1CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("H1 Candle", "Confirmation timeframe", "General");

		_h4CandleType = Param(nameof(H4CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("H4 Candle", "Higher timeframe used for filters", "General");

		Volume = 0.1m;
	}

	public bool TriggerOne { get => _triggerOne.Value; set => _triggerOne.Value = value; }
	public bool TriggerTwo { get => _triggerTwo.Value; set => _triggerTwo.Value = value; }

	public decimal BbSpreadH4Min1 { get => _bbSpreadH4Min1.Value; set => _bbSpreadH4Min1.Value = value; }
	public decimal BbSpreadM15Max1 { get => _bbSpreadM15Max1.Value; set => _bbSpreadM15Max1.Value = value; }
	public int RsiPeriod1 { get => _rsiPeriod1.Value; set => _rsiPeriod1.Value = value; }
	public decimal RsiLoM15_1 { get => _rsiLoM15_1.Value; set => _rsiLoM15_1.Value = value; }
	public decimal RsiHiM15_1 { get => _rsiHiM15_1.Value; set => _rsiHiM15_1.Value = value; }
	public decimal RsiLoH1_1 { get => _rsiLoH1_1.Value; set => _rsiLoH1_1.Value = value; }
	public decimal RsiHiH1_1 { get => _rsiHiH1_1.Value; set => _rsiHiH1_1.Value = value; }
	public decimal RsiLoH4_1 { get => _rsiLoH4_1.Value; set => _rsiLoH4_1.Value = value; }
	public decimal RsiHiH4_1 { get => _rsiHiH4_1.Value; set => _rsiHiH4_1.Value = value; }
	public decimal RsiHiLimH4_1 { get => _rsiHiLimH4_1.Value; set => _rsiHiLimH4_1.Value = value; }
	public decimal RsiLoLimH4_1 { get => _rsiLoLimH4_1.Value; set => _rsiLoLimH4_1.Value = value; }
	public decimal RsiHiLimH1_1 { get => _rsiHiLimH1_1.Value; set => _rsiHiLimH1_1.Value = value; }
	public decimal RsiLoLimH1_1 { get => _rsiLoLimH1_1.Value; set => _rsiLoLimH1_1.Value = value; }
	public decimal RsiHiLimM15_1 { get => _rsiHiLimM15_1.Value; set => _rsiHiLimM15_1.Value = value; }
	public decimal RsiLoLimM15_1 { get => _rsiLoLimM15_1.Value; set => _rsiLoLimM15_1.Value = value; }
	public decimal RDeltaM15Lim1 { get => _rDeltaM15Lim1.Value; set => _rDeltaM15Lim1.Value = value; }
	public decimal StocLoM15_1 { get => _stocLoM15_1.Value; set => _stocLoM15_1.Value = value; }
	public decimal StocHiM15_1 { get => _stocHiM15_1.Value; set => _stocHiM15_1.Value = value; }

	public int RsiPeriod2 { get => _rsiPeriod2.Value; set => _rsiPeriod2.Value = value; }
	public decimal BbSpreadH4Min2 { get => _bbSpreadH4Min2.Value; set => _bbSpreadH4Min2.Value = value; }
	public decimal BbSpreadM15Max2 { get => _bbSpreadM15Max2.Value; set => _bbSpreadM15Max2.Value = value; }
	public int NumRsi { get => _numRsi.Value; set => _numRsi.Value = value; }
	public decimal RsiM15Sigma2 { get => _rsiM15Sigma2.Value; set => _rsiM15Sigma2.Value = value; }
	public decimal RsiH1Sigma2 { get => _rsiH1Sigma2.Value; set => _rsiH1Sigma2.Value = value; }
	public decimal RsiH4Sigma2 { get => _rsiH4Sigma2.Value; set => _rsiH4Sigma2.Value = value; }
	public decimal RsiM15SigmaLim2 { get => _rsiM15SigmaLim2.Value; set => _rsiM15SigmaLim2.Value = value; }
	public decimal RsiH1SigmaLim2 { get => _rsiH1SigmaLim2.Value; set => _rsiH1SigmaLim2.Value = value; }
	public decimal RsiH4SigmaLim2 { get => _rsiH4SigmaLim2.Value; set => _rsiH4SigmaLim2.Value = value; }
	public decimal RDeltaM15Lim2 { get => _rDeltaM15Lim2.Value; set => _rDeltaM15Lim2.Value = value; }
	public decimal StocLoM15_2 { get => _stocLoM15_2.Value; set => _stocLoM15_2.Value = value; }
	public decimal StocHiM15_2 { get => _stocHiM15_2.Value; set => _stocHiM15_2.Value = value; }

	public decimal TakeProfitBuy1 { get => _takeProfitBuy1.Value; set => _takeProfitBuy1.Value = value; }
	public decimal StopLossBuy1 { get => _stopLossBuy1.Value; set => _stopLossBuy1.Value = value; }
	public decimal TakeProfitSell1 { get => _takeProfitSell1.Value; set => _takeProfitSell1.Value = value; }
	public decimal StopLossSell1 { get => _stopLossSell1.Value; set => _stopLossSell1.Value = value; }

	public decimal TakeProfitBuy2 { get => _takeProfitBuy2.Value; set => _takeProfitBuy2.Value = value; }
	public decimal StopLossBuy2 { get => _stopLossBuy2.Value; set => _stopLossBuy2.Value = value; }
	public decimal TakeProfitSell2 { get => _takeProfitSell2.Value; set => _takeProfitSell2.Value = value; }
	public decimal StopLossSell2 { get => _stopLossSell2.Value; set => _stopLossSell2.Value = value; }

	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public int BollingerPeriod { get => _bollingerPeriod.Value; set => _bollingerPeriod.Value = value; }
	public decimal AtrLimit { get => _atrLimit.Value; set => _atrLimit.Value = value; }
	public int EntryHour { get => _entryHour.Value; set => _entryHour.Value = value; }
	public int OpenHours { get => _openHours.Value; set => _openHours.Value = value; }
	public int NumPositions { get => _numPositions.Value; set => _numPositions.Value = value; }
	public int FridayEndHour { get => _fridayEndHour.Value; set => _fridayEndHour.Value = value; }
	public int StochasticK { get => _stochasticK.Value; set => _stochasticK.Value = value; }
	public int StochasticD { get => _stochasticD.Value; set => _stochasticD.Value = value; }
	public int StochasticSlowing { get => _stochasticSlowing.Value; set => _stochasticSlowing.Value = value; }

	public DataType M15CandleType { get => _m15CandleType.Value; set => _m15CandleType.Value = value; }
	public DataType H1CandleType { get => _h1CandleType.Value; set => _h1CandleType.Value = value; }
	public DataType H4CandleType { get => _h4CandleType.Value; set => _h4CandleType.Value = value; }

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		var security = Security;
		if (security == null)
		yield break;

		yield return (security, M15CandleType);
		yield return (security, H1CandleType);
		yield return (security, H4CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_rsiM15Current1 = null;
		_rsiM15Previous1 = null;
		_rsiH1Current1 = null;
		_rsiH4Current1 = null;

		_rsiM15Current2 = null;
		_rsiM15Previous2 = null;
		_rsiH1Current2 = null;
		_rsiH4Current2 = null;

		_rsiM15History2.Clear();
		_rsiH1History2.Clear();
		_rsiH4History2.Clear();

		_stochasticMain = null;
		_bbSpreadM15Pips = null;
		_bbSpreadH4Pips = null;
		_atrH4Pips = null;

		_longStopPrice = null;
		_longTakePrice = null;
		_shortStopPrice = null;
		_shortTakePrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (TriggerOne && TriggerTwo)
		{
			throw new InvalidOperationException("Trigger One and Trigger Two cannot be enabled simultaneously.");
		}

		_pipSize = CalculatePipSize();

		_rsiM15Trigger1 = new RelativeStrengthIndex { Length = RsiPeriod1 };
		_rsiH1Trigger1 = new RelativeStrengthIndex { Length = RsiPeriod1 };
		_rsiH4Trigger1 = new RelativeStrengthIndex { Length = RsiPeriod1 };

		_rsiM15Trigger2 = new RelativeStrengthIndex { Length = RsiPeriod2 };
		_rsiH1Trigger2 = new RelativeStrengthIndex { Length = RsiPeriod2 };
		_rsiH4Trigger2 = new RelativeStrengthIndex { Length = RsiPeriod2 };

		_stochastic = new StochasticOscillator
		{
			Length = StochasticK,
			K = { Length = StochasticSlowing },
			D = { Length = StochasticD }
		};

		_bollingerM15 = new BollingerBands
		{
			Length = BollingerPeriod,
			Width = 2m
		};

		_bollingerH4 = new BollingerBands
		{
			Length = BollingerPeriod,
			Width = 2m
		};

		_atrH4 = new AverageTrueRange { Length = AtrPeriod };

		var m15Subscription = SubscribeCandles(M15CandleType);
		m15Subscription
		.Bind(_rsiM15Trigger1, UpdateRsiM15Trigger1)
		.Bind(_rsiM15Trigger2, UpdateRsiM15Trigger2)
		.Bind(ProcessM15Candle)
		.Bind(_bollingerM15, UpdateM15Bollinger)
		.BindEx(_stochastic, UpdateStochastic)
		.Start();

		var h1Subscription = SubscribeCandles(H1CandleType);
		h1Subscription
		.Bind(_rsiH1Trigger1, UpdateRsiH1Trigger1)
		.Bind(_rsiH1Trigger2, UpdateRsiH1Trigger2)
		.Start();

		var h4Subscription = SubscribeCandles(H4CandleType);
		h4Subscription
		.Bind(_rsiH4Trigger1, UpdateRsiH4Trigger1)
		.Bind(_rsiH4Trigger2, UpdateRsiH4Trigger2)
		.Bind(_bollingerH4, UpdateH4Bollinger)
		.Bind(_atrH4, UpdateAtr)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, m15Subscription);
			DrawIndicator(area, _bollingerM15);
			DrawOwnTrades(area);
		}
	}

	private void UpdateRsiM15Trigger1(ICandleMessage candle, decimal value)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_rsiM15Previous1 = _rsiM15Current1;
		_rsiM15Current1 = value;
	}

	private void UpdateRsiM15Trigger2(ICandleMessage candle, decimal value)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_rsiM15Previous2 = _rsiM15Current2;
		_rsiM15Current2 = value;
		EnqueueLimited(_rsiM15History2, value);
	}

	private void UpdateRsiH1Trigger1(ICandleMessage candle, decimal value)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_rsiH1Current1 = value;
	}

	private void UpdateRsiH1Trigger2(ICandleMessage candle, decimal value)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_rsiH1Current2 = value;
		EnqueueLimited(_rsiH1History2, value);
	}

	private void UpdateRsiH4Trigger1(ICandleMessage candle, decimal value)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_rsiH4Current1 = value;
	}

	private void UpdateRsiH4Trigger2(ICandleMessage candle, decimal value)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_rsiH4Current2 = value;
		EnqueueLimited(_rsiH4History2, value);
	}

	private void UpdateM15Bollinger(ICandleMessage candle, decimal middle, decimal upper, decimal lower)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (_pipSize > 0m)
		{
			_bbSpreadM15Pips = (upper - lower) / _pipSize;
		}
	}

	private void UpdateH4Bollinger(ICandleMessage candle, decimal middle, decimal upper, decimal lower)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (_pipSize > 0m)
		{
			_bbSpreadH4Pips = (upper - lower) / _pipSize;
		}
	}

	private void UpdateAtr(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (_pipSize > 0m)
		{
			_atrH4Pips = atrValue / _pipSize;
		}
	}

	private void UpdateStochastic(ICandleMessage candle, IIndicatorValue value)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!value.IsFinal)
		return;

		var stochValue = (StochasticOscillatorValue)value;
		if (stochValue.Main is decimal mainValue)
		{
			_stochasticMain = mainValue;
		}
	}

	private void ProcessM15Candle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Evaluate whether the latest candle invalidates existing protective levels.
		CheckProtectiveLevels(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (NumPositions <= 0)
			return;

		if (Position != 0m)
			return;

		if (_pipSize <= 0m)
			_pipSize = CalculatePipSize();

		if (_pipSize <= 0m)
			return;

		var time = candle.OpenTime.LocalDateTime;

		if (time.DayOfWeek == DayOfWeek.Friday && time.Hour >= FridayEndHour)
			return;

		if (!IsWithinTradingWindow(time.Hour, EntryHour, OpenHours))
			return;

		// Flags for the final direction requested by the active trigger.
		bool buySignal = false;
		bool sellSignal = false;
		decimal stopPips = 0m;
		decimal takePips = 0m;

		if (TriggerOne && EvaluateTriggerOneFilters())
		{
			EvaluateTriggerOne(ref buySignal, ref sellSignal);

			// If trigger one fired, align risk parameters with its money-management settings.
			if (buySignal)
			{
				stopPips = StopLossBuy1;
				takePips = TakeProfitBuy1;
			}

			if (sellSignal)
			{
				stopPips = StopLossSell1;
				takePips = TakeProfitSell1;
			}
		}

		if (TriggerTwo && EvaluateTriggerTwoFilters())
		{
			bool buy2 = false;
			bool sell2 = false;
			EvaluateTriggerTwo(ref buy2, ref sell2);

			if (buy2 || sell2)
			{
				buySignal = buy2;
				sellSignal = sell2;
				stopPips = buy2 ? StopLossBuy2 : StopLossSell2;
				takePips = buy2 ? TakeProfitBuy2 : TakeProfitSell2;
			}
		}

		if (buySignal == sellSignal)
			return;

		if (Position != 0m)
			return;

		if (Volume <= 0m)
			return;

		ExecuteEntry(buySignal ? 1 : -1, candle, stopPips, takePips);
	}

	private bool EvaluateTriggerOneFilters()
	{
		return _bbSpreadH4Pips is decimal spreadH4 && spreadH4 > BbSpreadH4Min1
		&& _bbSpreadM15Pips is decimal spreadM15 && spreadM15 < BbSpreadM15Max1
		&& _atrH4Pips is decimal atr && atr < AtrLimit;
	}

	private void EvaluateTriggerOne(ref bool buySignal, ref bool sellSignal)
	{
		if (_rsiM15Current1 is not decimal rsiM15 || _rsiM15Previous1 is not decimal rsiM15Prev)
		return;

		if (_rsiH1Current1 is not decimal rsiH1 || _rsiH4Current1 is not decimal rsiH4)
		return;

		if (_stochasticMain is not decimal stoch)
			return;

		// RSI slope acts as the momentum confirmation filter for trigger one.
		var delta = rsiM15 - rsiM15Prev;

		if (rsiH1 < RsiLoH1_1 && rsiM15 < RsiLoM15_1 && rsiH4 < RsiLoH4_1
		&& rsiH4 > RsiLoLimH4_1 && rsiM15 > RsiLoLimM15_1 && rsiH1 > RsiLoLimH1_1
		&& delta > RDeltaM15Lim1 && stoch < StocLoM15_1)
		{
			buySignal = true;
		}

		if (rsiH1 > RsiHiH1_1 && rsiM15 > RsiHiM15_1 && rsiH4 > RsiHiH4_1
		&& rsiH4 < RsiHiLimH4_1 && rsiM15 < RsiHiLimM15_1 && rsiH1 < RsiHiLimH1_1
		&& delta < -RDeltaM15Lim1 && stoch > StocHiM15_1)
		{
			sellSignal = true;
		}
	}

	private bool EvaluateTriggerTwoFilters()
	{
		return _bbSpreadH4Pips is decimal spreadH4 && spreadH4 > BbSpreadH4Min2
		&& _bbSpreadM15Pips is decimal spreadM15 && spreadM15 < BbSpreadM15Max2
		&& _atrH4Pips is decimal atr && atr < AtrLimit;
	}

	private void EvaluateTriggerTwo(ref bool buySignal, ref bool sellSignal)
	{
		if (_rsiM15Current2 is not decimal rsiM15 || _rsiM15Previous2 is not decimal rsiM15Prev)
		return;

		if (_rsiH1Current2 is not decimal rsiH1 || _rsiH4Current2 is not decimal rsiH4)
		return;

		if (_stochasticMain is not decimal stoch)
		return;

		if (_rsiM15History2.Count == 0 || _rsiH1History2.Count == 0 || _rsiH4History2.Count == 0)
			return;

		// Build asymmetric standard deviation envelopes just like the MT5 implementation.
		var statsM15 = ComputeStatistics(_rsiM15History2);
		var statsH1 = ComputeStatistics(_rsiH1History2);
		var statsH4 = ComputeStatistics(_rsiH4History2);

		var m15Low = Clamp(statsM15.Mean - RsiM15Sigma2 * statsM15.SigmaMinus, 5m, 95m);
		var m15High = Clamp(statsM15.Mean + RsiM15Sigma2 * statsM15.SigmaPlus, 5m, 95m);
		var h1Low = Clamp(statsH1.Mean - RsiH1Sigma2 * statsH1.SigmaMinus, 5m, 95m);
		var h1High = Clamp(statsH1.Mean + RsiH1Sigma2 * statsH1.SigmaPlus, 5m, 95m);
		var h4Low = Clamp(statsH4.Mean - RsiH4Sigma2 * statsH4.SigmaMinus, 5m, 95m);
		var h4High = Clamp(statsH4.Mean + RsiH4Sigma2 * statsH4.SigmaPlus, 5m, 95m);

		var m15LowLimit = Clamp(statsM15.Mean - RsiM15SigmaLim2 * statsM15.SigmaMinus, 5m, 95m);
		var m15HighLimit = Clamp(statsM15.Mean + RsiM15SigmaLim2 * statsM15.SigmaPlus, 5m, 95m);
		var h1LowLimit = Clamp(statsH1.Mean - RsiH1SigmaLim2 * statsH1.SigmaMinus, 5m, 95m);
		var h1HighLimit = Clamp(statsH1.Mean + RsiH1SigmaLim2 * statsH1.SigmaPlus, 5m, 95m);
		var h4LowLimit = Clamp(statsH4.Mean - RsiH4SigmaLim2 * statsH4.SigmaMinus, 5m, 95m);
		var h4HighLimit = Clamp(statsH4.Mean + RsiH4SigmaLim2 * statsH4.SigmaPlus, 5m, 95m);

		// Positive deltas confirm that RSI is bouncing from oversold territory.
		var delta = rsiM15 - rsiM15Prev;

		if (rsiH1 < h1Low && rsiM15 < m15Low && rsiH4 < h4Low
		&& rsiH4 > h4LowLimit && rsiM15 > m15LowLimit && rsiH1 > h1LowLimit
		&& delta > RDeltaM15Lim2 && stoch < StocLoM15_2)
		{
			buySignal = true;
		}

		if (rsiH1 > h1High && rsiM15 > m15High && rsiH4 > h4High
		&& rsiH4 < h4HighLimit && rsiM15 < m15HighLimit && rsiH1 < h1HighLimit
		&& delta < -RDeltaM15Lim2 && stoch > StocHiM15_2)
		{
			sellSignal = true;
		}
	}

	private void ExecuteEntry(int direction, ICandleMessage candle, decimal stopPips, decimal takePips)
	{
		var price = candle.ClosePrice;
		var volume = Volume;

		decimal? stopPrice = null;
		decimal? takePrice = null;

		if (stopPips > 0m)
		{
			var offset = stopPips * _pipSize;
			stopPrice = direction > 0 ? price - offset : price + offset;
			stopPrice = RoundPrice(stopPrice.Value);
		}

		if (takePips > 0m)
		{
			var offset = takePips * _pipSize;
			takePrice = direction > 0 ? price + offset : price - offset;
			takePrice = RoundPrice(takePrice.Value);
		}

		if (direction > 0)
		{
			BuyMarket(volume);
			_longStopPrice = stopPrice;
			_longTakePrice = takePrice;
			_shortStopPrice = null;
			_shortTakePrice = null;
		}
		else
		{
			SellMarket(volume);
			_shortStopPrice = stopPrice;
			_shortTakePrice = takePrice;
			_longStopPrice = null;
			_longTakePrice = null;
		}
	}

	private void CheckProtectiveLevels(ICandleMessage candle)
	{
		if (Position > 0m)
		{
			if (_longStopPrice is decimal stop && candle.LowPrice <= stop)
			{
				SellMarket(Position);
				ResetProtection();
				return;
			}

			if (_longTakePrice is decimal take && candle.HighPrice >= take)
			{
				SellMarket(Position);
				ResetProtection();
			}
		}
		else if (Position < 0m)
		{
			var absPosition = Math.Abs(Position);
			if (_shortStopPrice is decimal stop && candle.HighPrice >= stop)
			{
				BuyMarket(absPosition);
				ResetProtection();
				return;
			}

			if (_shortTakePrice is decimal take && candle.LowPrice <= take)
			{
				BuyMarket(absPosition);
				ResetProtection();
			}
		}
	}

	private void ResetProtection()
	{
		_longStopPrice = null;
		_longTakePrice = null;
		_shortStopPrice = null;
		_shortTakePrice = null;
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
		{
			ResetProtection();
		}
	}

	private (decimal Mean, decimal SigmaPlus, decimal SigmaMinus) ComputeStatistics(Queue<decimal> values)
	{
		var count = values.Count;
		if (count == 0)
		return (0m, 0m, 0m);

		var array = values.ToArray();
		var reference = array[^1];

		if (count < 3)
		{
			var sigma = Math.Abs(reference) * 0.3m;
			return (reference, sigma, sigma);
		}

		decimal sum = 0m;
		for (var i = 0; i < count; i++)
		{
			sum += array[i];
		}

		var mean = sum / count;

		decimal variance = 0m;
		decimal variancePlus = 0m;
		decimal varianceMinus = 0m;
		var plusCount = 0;
		var minusCount = 0;

		for (var i = 0; i < count; i++)
		{
			var diff = array[i] - mean;
			var square = diff * diff;
			variance += square;

			if (diff >= 0m)
			{
				variancePlus += square;
				plusCount++;
			}
			else
			{
				varianceMinus += square;
				minusCount++;
			}
		}

		var sigma = (decimal)Math.Sqrt((double)(variance / count));
		var sigmaPlus = sigma;
		var sigmaMinus = sigma;

		if (plusCount > 0)
		{
			sigmaPlus = (decimal)Math.Sqrt((double)(variancePlus / plusCount));
		}

		if (minusCount > 0)
		{
			sigmaMinus = (decimal)Math.Sqrt((double)(varianceMinus / minusCount));
		}

		return (mean, sigmaPlus, sigmaMinus);
	}

	private void EnqueueLimited(Queue<decimal> queue, decimal value)
	{
		queue.Enqueue(value);
		var max = NumRsi;
		while (queue.Count > max && queue.Count > 0)
		{
			queue.Dequeue();
		}
	}

	private bool IsWithinTradingWindow(int currentHour, int startHour, int windowLength)
	{
		var closeHour = Mod24(startHour + windowLength);

		if (closeHour == startHour)
		{
			return currentHour == startHour;
		}

		if (closeHour > startHour)
		{
			return currentHour >= startHour && currentHour <= closeHour;
		}

		if (currentHour >= startHour && currentHour <= 23)
		{
			return true;
		}

		return currentHour >= 0 && currentHour <= closeHour;
	}

	private int Mod24(int value)
	{
		var result = value % 24;
		return result < 0 ? result + 24 : result;
	}

	private decimal Clamp(decimal value, decimal min, decimal max)
	{
		if (value < min)
		return min;
		if (value > max)
		return max;
		return value;
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 0m;

		if (step <= 0m)
		return 0.0001m;

		var digits = 0;
		var value = step;

		while (value < 1m && digits < 10)
		{
			value *= 10m;
			digits++;
		}

		if (digits == 3 || digits == 5)
		return step * 10m;

		return step;
	}

	private decimal RoundPrice(decimal price)
	{
		var step = Security?.PriceStep;
		if (step == null || step.Value <= 0m)
		return price;

		return Math.Round(price / step.Value, 0, MidpointRounding.AwayFromZero) * step.Value;
	}
}
