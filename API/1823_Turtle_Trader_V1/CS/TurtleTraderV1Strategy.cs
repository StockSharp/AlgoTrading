using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class TurtleTraderV1Strategy : Strategy
{
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _stochPeriod;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<int> _choFastPeriod;
	private readonly StrategyParam<int> _choSlowPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _fastMa = null!;
	private ExponentialMovingAverage _slowMa = null!;
	private RelativeStrengthIndex _rsi = null!;
	private StochasticOscillator _stochastic = null!;
	private CommodityChannelIndex _cci = null!;
	private Momentum _momentum = null!;
	private AccumulationDistributionLine _ad = null!;
	private ExponentialMovingAverage _choFastEma = null!;
	private ExponentialMovingAverage _choSlowEma = null!;

	private decimal _prevRsi;
	private decimal _prevCci;
	private decimal _prevMomentum;
	private decimal _prevCho;
	private decimal _prevFastMa;

	public int FastMaPeriod { get => _fastMaPeriod.Value; set => _fastMaPeriod.Value = value; }
	public int SlowMaPeriod { get => _slowMaPeriod.Value; set => _slowMaPeriod.Value = value; }
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public int StochPeriod { get => _stochPeriod.Value; set => _stochPeriod.Value = value; }
	public int CciPeriod { get => _cciPeriod.Value; set => _cciPeriod.Value = value; }
	public int MomentumPeriod { get => _momentumPeriod.Value; set => _momentumPeriod.Value = value; }
	public int ChoFastPeriod { get => _choFastPeriod.Value; set => _choFastPeriod.Value = value; }
	public int ChoSlowPeriod { get => _choSlowPeriod.Value; set => _choSlowPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TurtleTraderV1Strategy()
	{
		_fastMaPeriod = Param(nameof(FastMaPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA", "Fast moving average period", "General");

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA", "Slow moving average period", "General");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI length", "Oscillators");

		_stochPeriod = Param(nameof(StochPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic Period", "Stochastic length", "Oscillators");

		_cciPeriod = Param(nameof(CciPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "CCI length", "Oscillators");

		_momentumPeriod = Param(nameof(MomentumPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Period", "Momentum length", "Oscillators");

		_choFastPeriod = Param(nameof(ChoFastPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("Chaikin Fast", "Chaikin fast EMA", "Chaikin");

		_choSlowPeriod = Param(nameof(ChoSlowPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Chaikin Slow", "Chaikin slow EMA", "Chaikin");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for candles", "Data");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevRsi = _prevCci = _prevMomentum = _prevCho = _prevFastMa = 0m;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastMa = new ExponentialMovingAverage { Length = FastMaPeriod };
		_slowMa = new ExponentialMovingAverage { Length = SlowMaPeriod };
		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		_stochastic = new StochasticOscillator { Length = StochPeriod };
		_cci = new CommodityChannelIndex { Length = CciPeriod };
		_momentum = new Momentum { Length = MomentumPeriod };
		_ad = new AccumulationDistributionLine();
		_choFastEma = new ExponentialMovingAverage { Length = ChoFastPeriod };
		_choSlowEma = new ExponentialMovingAverage { Length = ChoSlowPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_fastMa, _slowMa, _rsi, _stochastic, _cci, _momentum, _ad, ProcessCandle)
			.Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(
		ICandleMessage candle,
		IIndicatorValue fastMaValue,
		IIndicatorValue slowMaValue,
		IIndicatorValue rsiValue,
		IIndicatorValue stochValue,
		IIndicatorValue cciValue,
		IIndicatorValue momentumValue,
		IIndicatorValue adValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!fastMaValue.IsFinal || !slowMaValue.IsFinal || !rsiValue.IsFinal ||
			!stochValue.IsFinal || !cciValue.IsFinal || !momentumValue.IsFinal || !adValue.IsFinal)
			return;

		var fastMa = fastMaValue.GetValue<decimal>();
		var slowMa = slowMaValue.GetValue<decimal>();
		var rsi = rsiValue.GetValue<decimal>();
		var stoch = (StochasticOscillatorValue)stochValue;
		if (stoch.K is not decimal stochK)
			return;
		var cci = cciValue.GetValue<decimal>();
		var momentum = momentumValue.GetValue<decimal>();
		var ad = adValue.GetValue<decimal>();

		var fastCho = _choFastEma.Process(new DecimalIndicatorValue(_choFastEma, ad, candle.Time));
		var slowCho = _choSlowEma.Process(new DecimalIndicatorValue(_choSlowEma, ad, candle.Time));
		if (!fastCho.IsFinal || !slowCho.IsFinal)
		{
			_prevCho = fastCho.ToDecimal() - slowCho.ToDecimal();
			_prevRsi = rsi;
			_prevCci = cci;
			_prevMomentum = momentum;
			_prevFastMa = fastMa;
			return;
		}

		var cho = fastCho.ToDecimal() - slowCho.ToDecimal();

		var bullish = fastMa > slowMa && fastMa > _prevFastMa &&
			rsi < 70m && rsi > _prevRsi &&
			stochK < 88m &&
			cci > _prevCci &&
			momentum > _prevMomentum &&
			cho > _prevCho;

		var bearish = fastMa < slowMa && fastMa < _prevFastMa &&
			rsi > 30m && rsi < _prevRsi &&
			stochK > 12m &&
			cci < _prevCci &&
			momentum < _prevMomentum &&
			cho < _prevCho;

		if (bullish && Position <= 0)
			BuyMarket();
		else if (bearish && Position >= 0)
			SellMarket();

		_prevRsi = rsi;
		_prevCci = cci;
		_prevMomentum = momentum;
		_prevCho = cho;
		_prevFastMa = fastMa;
	}
}