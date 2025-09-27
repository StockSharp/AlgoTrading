using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Ultimate Balance Strategy combines multiple indicators into a weighted oscillator.
/// Opens long when the oscillator MA crosses above the oversold level and exits or reverses on overbought.
/// </summary>
public class UltimateBalanceStrategy : Strategy
{
	private readonly StrategyParam<decimal> _weightRoc;
	private readonly StrategyParam<decimal> _weightRsi;
	private readonly StrategyParam<decimal> _weightCci;
	private readonly StrategyParam<decimal> _weightWilliams;
	private readonly StrategyParam<decimal> _weightAdx;
	private readonly StrategyParam<bool> _enableShort;
	private readonly StrategyParam<decimal> _overboughtLevel;
	private readonly StrategyParam<decimal> _oversoldLevel;
	private readonly StrategyParam<string> _maType;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<DataType> _candleType;

	private RateOfChange _roc;
	private Highest _rocMax;
	private Lowest _rocMin;
	private RSI _rsi;
	private CCI _cci;
	private Highest _cciMax;
	private Lowest _cciMin;
	private WilliamsR _williams;
	private Highest _williamsMax;
	private Lowest _williamsMin;
	private ADX _adx;
	private IIndicator _ma;

	private decimal _prevMa;

	/// <summary>
	/// ROC weight.
	/// </summary>
	public decimal WeightRoc
	{
		get => _weightRoc.Value;
		set => _weightRoc.Value = value;
	}

	/// <summary>
	/// RSI weight.
	/// </summary>
	public decimal WeightRsi
	{
		get => _weightRsi.Value;
		set => _weightRsi.Value = value;
	}

	/// <summary>
	/// CCI weight.
	/// </summary>
	public decimal WeightCci
	{
		get => _weightCci.Value;
		set => _weightCci.Value = value;
	}

	/// <summary>
	/// Williams %R weight.
	/// </summary>
	public decimal WeightWilliams
	{
		get => _weightWilliams.Value;
		set => _weightWilliams.Value = value;
	}

	/// <summary>
	/// ADX weight.
	/// </summary>
	public decimal WeightAdx
	{
		get => _weightAdx.Value;
		set => _weightAdx.Value = value;
	}

	/// <summary>
	/// Enable short positions.
	/// </summary>
	public bool EnableShort
	{
		get => _enableShort.Value;
		set => _enableShort.Value = value;
	}

	/// <summary>
	/// Overbought level.
	/// </summary>
	public decimal OverboughtLevel
	{
		get => _overboughtLevel.Value;
		set => _overboughtLevel.Value = value;
	}

	/// <summary>
	/// Oversold level.
	/// </summary>
	public decimal OversoldLevel
	{
		get => _oversoldLevel.Value;
		set => _oversoldLevel.Value = value;
	}

	/// <summary>
	/// Moving average type.
	/// </summary>
	public string MaType
	{
		get => _maType.Value;
		set => _maType.Value = value;
	}

	/// <summary>
	/// Moving average length.
	/// </summary>
	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="UltimateBalanceStrategy"/> class.
	/// </summary>
	public UltimateBalanceStrategy()
	{
		_weightRoc = Param(nameof(WeightRoc), 2m)
			.SetDisplay("ROC Weight", "Weight of ROC", "Weightings")
			.SetCanOptimize(true)
			.SetOptimize(0m, 5m, 0.5m);

		_weightRsi = Param(nameof(WeightRsi), 0.5m)
			.SetDisplay("RSI Weight", "Weight of RSI", "Weightings")
			.SetCanOptimize(true)
			.SetOptimize(0m, 5m, 0.5m);

		_weightCci = Param(nameof(WeightCci), 2m)
			.SetDisplay("CCI Weight", "Weight of CCI", "Weightings")
			.SetCanOptimize(true)
			.SetOptimize(0m, 5m, 0.5m);

		_weightWilliams = Param(nameof(WeightWilliams), 0.5m)
			.SetDisplay("Williams %R Weight", "Weight of Williams %R", "Weightings")
			.SetCanOptimize(true)
			.SetOptimize(0m, 5m, 0.5m);

		_weightAdx = Param(nameof(WeightAdx), 0.5m)
			.SetDisplay("ADX Weight", "Weight of ADX", "Weightings")
			.SetCanOptimize(true)
			.SetOptimize(0m, 5m, 0.5m);

		_enableShort = Param(nameof(EnableShort), false)
			.SetDisplay("Enable Short", "Allow short positions", "General");

		_overboughtLevel = Param(nameof(OverboughtLevel), 0.75m)
			.SetDisplay("Overbought Level", "Overbought threshold", "General")
			.SetCanOptimize(true)
			.SetOptimize(0.6m, 0.9m, 0.05m);

		_oversoldLevel = Param(nameof(OversoldLevel), 0.25m)
			.SetDisplay("Oversold Level", "Oversold threshold", "General")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 0.4m, 0.05m);

		_maType = Param(nameof(MaType), "SMA")
			.SetDisplay("MA Type", "Moving average type", "MA");

		_maLength = Param(nameof(MaLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "Length of MA", "MA")
			.SetCanOptimize(true)
			.SetOptimize(5, 50, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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
		_prevMa = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_roc = new RateOfChange { Length = 20 };
		_rocMax = new Highest { Length = 20 };
		_rocMin = new Lowest { Length = 20 };
		_rsi = new RSI { Length = 14 };
		_cci = new CCI { Length = 20 };
		_cciMax = new Highest { Length = 20 };
		_cciMin = new Lowest { Length = 20 };
		_williams = new WilliamsR { Length = 14 };
		_williamsMax = new Highest { Length = 14 };
		_williamsMin = new Lowest { Length = 14 };
		_adx = new ADX { Length = 14 };
		_ma = CreateMa();
		_prevMa = 0m;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_roc, _rocMax, _rocMin, _rsi, _cci, _cciMax, _cciMin, _williams, _williamsMax, _williamsMin, _adx, ProcessCandle)
			.Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rocValue, decimal rocMax, decimal rocMin, decimal rsiValue, decimal cciValue, decimal cciMax, decimal cciMin, decimal wrValue, decimal wrMax, decimal wrMin, decimal adxValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_roc.IsFormed || !_rsi.IsFormed || !_cci.IsFormed || !_williams.IsFormed || !_adx.IsFormed)
			return;

		var normRoc = rocMax != rocMin ? (rocValue - rocMin) / (rocMax - rocMin) : 0m;
		var normRsi = rsiValue / 100m;
		var normCci = cciMax != cciMin ? (cciValue - cciMin) / (cciMax - cciMin) : 0m;
		var normWr = wrMax != wrMin ? (wrValue - wrMin) / (wrMax - wrMin) : 0m;
		var normAdx = adxValue / 50m;

		var sum = WeightRoc + WeightRsi + WeightCci + WeightWilliams + WeightAdx;
		var oscillator = sum == 0m ? 0m : (normRoc * WeightRoc + normRsi * WeightRsi + normCci * WeightCci + normWr * WeightWilliams + normAdx * WeightAdx) / sum;

		var maValue = _ma.Process(new DecimalIndicatorValue(_ma, oscillator, candle.OpenTime)).ToDecimal();

		if (!_ma.IsFormed)
		{
			_prevMa = maValue;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevMa = maValue;
			return;
		}

		var buySignal = _prevMa <= OversoldLevel && maValue > OversoldLevel;
		var sellSignal = _prevMa >= OverboughtLevel && maValue < OverboughtLevel;

		if (buySignal && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));

		if (sellSignal && Position >= 0)
		{
			if (EnableShort)
				SellMarket(Volume + Math.Max(Position, 0m));
			else if (Position > 0)
				SellMarket(Position);
		}

		_prevMa = maValue;
	}

	private IIndicator CreateMa()
	{
		return MaType switch
		{
			"EMA" => new ExponentialMovingAverage { Length = MaLength },
			"WMA" => new WeightedMovingAverage { Length = MaLength },
			"DEMA" => new DoubleExponentialMovingAverage { Length = MaLength },
			_ => new SimpleMovingAverage { Length = MaLength },
		};
	}
}

