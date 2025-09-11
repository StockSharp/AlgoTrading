using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy detecting price divergence across multiple indicators.
/// </summary>
public class DivergenceForManyIndicatorsV4Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _minConfirmations;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;

	private MovingAverageConvergenceDivergenceSignal _macd;
	private RelativeStrengthIndex _rsi;
	private StochasticOscillator _stochastic;
	private CommodityChannelIndex _cci;
	private Momentum _momentum;
	private OnBalanceVolume _obv;
	private MoneyFlowIndex _mfi;

	private decimal _prevPrice;
	private decimal _prevMacd;
	private decimal _prevRsi;
	private decimal _prevStoch;
	private decimal _prevCci;
	private decimal _prevMomentum;
	private decimal _prevObv;
	private decimal _prevMfi;
	private bool _isFirstValue;

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Minimum number of indicators confirming divergence.
	/// </summary>
	public int MinConfirmations
	{
		get => _minConfirmations.Value;
		set => _minConfirmations.Value = value;
	}

	/// <summary>
	/// Take profit in percent.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Stop loss in percent.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="DivergenceForManyIndicatorsV4Strategy"/>.
	/// </summary>
	public DivergenceForManyIndicatorsV4Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle type", "Candle type for strategy calculation.", "General");

		_minConfirmations = Param(nameof(MinConfirmations), 2)
			.SetDisplay("Min divergences", "Minimum number of indicators confirming divergence.", "Parameters")
			.SetRange(1, 7)
			.SetCanOptimize(true);

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 4m)
			.SetDisplay("Take profit (%)", "Take profit percentage.", "Risk")
			.SetRange(1m, 10m)
			.SetCanOptimize(true);

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetDisplay("Stop loss (%)", "Stop loss percentage.", "Risk")
			.SetRange(1m, 5m)
			.SetCanOptimize(true);
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

		_prevPrice = 0;
		_prevMacd = 0;
		_prevRsi = 0;
		_prevStoch = 0;
		_prevCci = 0;
		_prevMomentum = 0;
		_prevObv = 0;
		_prevMfi = 0;
		_isFirstValue = true;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = 12 },
				LongMa = { Length = 26 },
			},
			SignalMa = { Length = 9 }
		};

		_rsi = new RelativeStrengthIndex { Length = 14 };
		_stochastic = new StochasticOscillator { K = { Length = 14 }, D = { Length = 3 } };
		_cci = new CommodityChannelIndex { Length = 20 };
		_momentum = new Momentum { Length = 10 };
		_obv = new OnBalanceVolume();
		_mfi = new MoneyFlowIndex { Length = 14 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_macd, _rsi, _stochastic, _cci, _momentum, _obv, _mfi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _macd);
			DrawIndicator(area, _rsi);
			DrawIndicator(area, _stochastic);
			DrawIndicator(area, _cci);
			DrawIndicator(area, _momentum);
			DrawIndicator(area, _obv);
			DrawIndicator(area, _mfi);
			DrawOwnTrades(area);
		}

		StartProtection(
			new Unit(TakeProfitPercent, UnitTypes.Percent),
			new Unit(StopLossPercent, UnitTypes.Percent)
		);
	}

	private void ProcessCandle(
		ICandleMessage candle,
		IIndicatorValue macdValue,
		IIndicatorValue rsiValue,
		IIndicatorValue stochValue,
		IIndicatorValue cciValue,
		IIndicatorValue momentumValue,
		IIndicatorValue obvValue,
		IIndicatorValue mfiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!macdValue.IsFinal || !rsiValue.IsFinal || !stochValue.IsFinal || !cciValue.IsFinal ||
			!momentumValue.IsFinal || !obvValue.IsFinal || !mfiValue.IsFinal)
			return;

		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		var macd = macdTyped.Macd;

		var rsi = rsiValue.GetValue<decimal>();
		var stochTyped = (StochasticOscillatorValue)stochValue;
		if (stochTyped.K is not decimal stoch)
			return;
		var cci = cciValue.GetValue<decimal>();
		var momentum = momentumValue.GetValue<decimal>();
		var obv = obvValue.GetValue<decimal>();
		var mfi = mfiValue.GetValue<decimal>();

		if (_isFirstValue)
		{
			_prevPrice = candle.ClosePrice;
			_prevMacd = macd;
			_prevRsi = rsi;
			_prevStoch = stoch;
			_prevCci = cci;
			_prevMomentum = momentum;
			_prevObv = obv;
			_prevMfi = mfi;
			_isFirstValue = false;
			return;
		}

		var bullish = 0;
		var bearish = 0;

		var priceUp = candle.ClosePrice > _prevPrice;
		var priceDown = candle.ClosePrice < _prevPrice;

		CheckDivergence(priceUp, priceDown, macd, _prevMacd, ref bullish, ref bearish);
		CheckDivergence(priceUp, priceDown, rsi, _prevRsi, ref bullish, ref bearish);
		CheckDivergence(priceUp, priceDown, stoch, _prevStoch, ref bullish, ref bearish);
		CheckDivergence(priceUp, priceDown, cci, _prevCci, ref bullish, ref bearish);
		CheckDivergence(priceUp, priceDown, momentum, _prevMomentum, ref bullish, ref bearish);
		CheckDivergence(priceUp, priceDown, obv, _prevObv, ref bullish, ref bearish);
		CheckDivergence(priceUp, priceDown, mfi, _prevMfi, ref bullish, ref bearish);

		_prevPrice = candle.ClosePrice;
		_prevMacd = macd;
		_prevRsi = rsi;
		_prevStoch = stoch;
		_prevCci = cci;
		_prevMomentum = momentum;
		_prevObv = obv;
		_prevMfi = mfi;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (bullish >= MinConfirmations)
		{
			if (Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
			else if (Position < 0)
				BuyMarket(Math.Abs(Position));
		}
		else if (bearish >= MinConfirmations)
		{
			if (Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
			else if (Position > 0)
				SellMarket(Position);
		}
	}

	private static void CheckDivergence(bool priceUp, bool priceDown, decimal current, decimal previous, ref int bullish, ref int bearish)
	{
		var indicatorUp = current > previous;
		var indicatorDown = current < previous;

		if (priceUp && indicatorDown)
			bearish++;
		else if (priceDown && indicatorUp)
			bullish++;
	}
}
