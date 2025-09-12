using System;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class ProxyFinancialStressIndexStrategy : Strategy
{
	private readonly StrategyParam<int> _smaLength;
	private readonly StrategyParam<int> _stdDevLength;
	private readonly StrategyParam<decimal> _threshold;
	private readonly StrategyParam<int> _holdingPeriod;

	private readonly StrategyParam<decimal> _vixWeight;
	private readonly StrategyParam<decimal> _us10yWeight;
	private readonly StrategyParam<decimal> _dxyWeight;
	private readonly StrategyParam<decimal> _sp500Weight;
	private readonly StrategyParam<decimal> _eurusdWeight;
	private readonly StrategyParam<decimal> _hygWeight;

	private readonly StrategyParam<Security> _vixSecurity;
	private readonly StrategyParam<Security> _us10ySecurity;
	private readonly StrategyParam<Security> _dxySecurity;
	private readonly StrategyParam<Security> _eurusdSecurity;
	private readonly StrategyParam<Security> _hygSecurity;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _smaMain;
	private SimpleMovingAverage _smaVix;
	private SimpleMovingAverage _smaUs10y;
	private SimpleMovingAverage _smaDxy;
	private SimpleMovingAverage _smaEurusd;
	private SimpleMovingAverage _smaHyg;

	private StandardDeviation _stdMain;
	private StandardDeviation _stdVix;
	private StandardDeviation _stdUs10y;
	private StandardDeviation _stdDxy;
	private StandardDeviation _stdEurusd;
	private StandardDeviation _stdHyg;

	private decimal _vixNorm;
	private decimal _us10yNorm;
	private decimal _dxyNorm;
	private decimal _sp500Norm;
	private decimal _eurusdNorm;
	private decimal _hygNorm;

	private bool _vixReady;
	private bool _us10yReady;
	private bool _dxyReady;
	private bool _sp500Ready;
	private bool _eurusdReady;
	private bool _hygReady;

	private decimal _prevStressIndex;
	private int _barsInPosition;

	public int SmaLength { get => _smaLength.Value; set => _smaLength.Value = value; }
	public int StdDevLength { get => _stdDevLength.Value; set => _stdDevLength.Value = value; }
	public decimal Threshold { get => _threshold.Value; set => _threshold.Value = value; }
	public int HoldingPeriod { get => _holdingPeriod.Value; set => _holdingPeriod.Value = value; }

	public decimal VixWeight { get => _vixWeight.Value; set => _vixWeight.Value = value; }
	public decimal Us10yWeight { get => _us10yWeight.Value; set => _us10yWeight.Value = value; }
	public decimal DxyWeight { get => _dxyWeight.Value; set => _dxyWeight.Value = value; }
	public decimal Sp500Weight { get => _sp500Weight.Value; set => _sp500Weight.Value = value; }
	public decimal EurusdWeight { get => _eurusdWeight.Value; set => _eurusdWeight.Value = value; }
	public decimal HygWeight { get => _hygWeight.Value; set => _hygWeight.Value = value; }

	public Security VixSecurity { get => _vixSecurity.Value; set => _vixSecurity.Value = value; }
	public Security Us10ySecurity { get => _us10ySecurity.Value; set => _us10ySecurity.Value = value; }
	public Security DxySecurity { get => _dxySecurity.Value; set => _dxySecurity.Value = value; }
	public Security EurusdSecurity { get => _eurusdSecurity.Value; set => _eurusdSecurity.Value = value; }
	public Security HygSecurity { get => _hygSecurity.Value; set => _hygSecurity.Value = value; }

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ProxyFinancialStressIndexStrategy()
	{
		_smaLength = Param(nameof(SmaLength), 41)
		.SetGreaterThanZero()
		.SetDisplay("SMA Length", "Window for moving average", "Parameters")
		.SetCanOptimize(true);

		_stdDevLength = Param(nameof(StdDevLength), 20)
		.SetGreaterThanZero()
		.SetDisplay("StdDev Length", "Window for standard deviation", "Parameters")
		.SetCanOptimize(true);

		_threshold = Param(nameof(Threshold), -0.8m)
		.SetDisplay("Threshold", "Stress index entry level", "Parameters")
		.SetCanOptimize(true);

		_holdingPeriod = Param(nameof(HoldingPeriod), 28)
		.SetGreaterThanZero()
		.SetDisplay("Holding Period", "Bars to hold position", "Parameters")
		.SetCanOptimize(true);

		_vixWeight = Param(nameof(VixWeight), 0.4m)
		.SetDisplay("VIX Weight", "Weight for VIX component", "Weights");

		_us10yWeight = Param(nameof(Us10yWeight), 0.2m)
		.SetDisplay("US10Y Weight", "Weight for US 10Y component", "Weights");

		_dxyWeight = Param(nameof(DxyWeight), 0.12m)
		.SetDisplay("DXY Weight", "Weight for DXY component", "Weights");

		_sp500Weight = Param(nameof(Sp500Weight), 0.06m)
		.SetDisplay("SP500 Weight", "Weight for S&P 500 component", "Weights");

		_eurusdWeight = Param(nameof(EurusdWeight), 0.1m)
		.SetDisplay("EURUSD Weight", "Weight for EUR/USD component", "Weights");

		_hygWeight = Param(nameof(HygWeight), 0.18m)
		.SetDisplay("HYG Weight", "Weight for HYG component", "Weights");

		_vixSecurity = Param(nameof(VixSecurity), new Security())
		.SetDisplay("VIX Security", "Security for VIX index", "Securities");

		_us10ySecurity = Param(nameof(Us10ySecurity), new Security())
		.SetDisplay("US10Y Security", "Security for US 10Y yield", "Securities");

		_dxySecurity = Param(nameof(DxySecurity), new Security())
		.SetDisplay("DXY Security", "Security for Dollar Index", "Securities");

		_eurusdSecurity = Param(nameof(EurusdSecurity), new Security())
		.SetDisplay("EURUSD Security", "Security for EUR/USD", "Securities");

		_hygSecurity = Param(nameof(HygSecurity), new Security())
		.SetDisplay("HYG Security", "Security for HYG ETF", "Securities");

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe for all securities", "Parameters");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (Security == null)
		throw new InvalidOperationException("Main security is not set.");

		if (VixSecurity == null || Us10ySecurity == null || DxySecurity == null || EurusdSecurity == null || HygSecurity == null)
		throw new InvalidOperationException("Component securities are not set.");

		_smaMain = new SimpleMovingAverage { Length = SmaLength };
		_stdMain = new StandardDeviation { Length = StdDevLength };
		_smaVix = new SimpleMovingAverage { Length = SmaLength };
		_stdVix = new StandardDeviation { Length = StdDevLength };
		_smaUs10y = new SimpleMovingAverage { Length = SmaLength };
		_stdUs10y = new StandardDeviation { Length = StdDevLength };
		_smaDxy = new SimpleMovingAverage { Length = SmaLength };
		_stdDxy = new StandardDeviation { Length = StdDevLength };
		_smaEurusd = new SimpleMovingAverage { Length = SmaLength };
		_stdEurusd = new StandardDeviation { Length = StdDevLength };
		_smaHyg = new SimpleMovingAverage { Length = SmaLength };
		_stdHyg = new StandardDeviation { Length = StdDevLength };

		SubscribeComponent(VixSecurity, _smaVix, _stdVix, v => { _vixNorm = v; _vixReady = true; });
		SubscribeComponent(Us10ySecurity, _smaUs10y, _stdUs10y, v => { _us10yNorm = v; _us10yReady = true; });
		SubscribeComponent(DxySecurity, _smaDxy, _stdDxy, v => { _dxyNorm = v; _dxyReady = true; });
		SubscribeComponent(EurusdSecurity, _smaEurusd, _stdEurusd, v => { _eurusdNorm = v; _eurusdReady = true; });
		SubscribeComponent(HygSecurity, _smaHyg, _stdHyg, v => { _hygNorm = v; _hygReady = true; });

		var mainSubscription = SubscribeCandles(CandleType);
		mainSubscription.Bind(_smaMain, _stdMain, ProcessMain).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, mainSubscription);
			DrawOwnTrades(area);
		}
	}

	private void SubscribeComponent(Security security, SimpleMovingAverage sma, StandardDeviation std, Action<decimal> setter)
	{
		var subscription = SubscribeCandles(CandleType, false, security);
		subscription.Bind(sma, std, (candle, smaValue, stdValue) =>
		{
			if (candle.State != CandleStates.Finished || stdValue == 0m)
			return;

			var norm = (candle.ClosePrice - smaValue) / stdValue;
			setter(norm);
		}).Start();
	}

	private void ProcessMain(ICandleMessage candle, decimal smaValue, decimal stdValue)
	{
		if (candle.State != CandleStates.Finished || stdValue == 0m)
		return;

		_sp500Norm = (candle.ClosePrice - smaValue) / stdValue;
		_sp500Ready = true;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (Position != 0m)
		{
			_barsInPosition++;

			if (_barsInPosition >= HoldingPeriod)
			{
				ClosePosition();
				_barsInPosition = 0;
			}
		}

		if (!(_vixReady && _us10yReady && _dxyReady && _sp500Ready && _eurusdReady && _hygReady))
		return;

		var stressIndex = (_vixNorm * VixWeight) +
		(_us10yNorm * Us10yWeight) +
		(_dxyNorm * DxyWeight) +
		(_sp500Norm * Sp500Weight) +
		(_eurusdNorm * EurusdWeight) +
		(_hygNorm * HygWeight);

		if (Position == 0m && _prevStressIndex > Threshold && stressIndex <= Threshold)
		{
			BuyMarket();
			_barsInPosition = 0;
		}

		_prevStressIndex = stressIndex;
	}
}
