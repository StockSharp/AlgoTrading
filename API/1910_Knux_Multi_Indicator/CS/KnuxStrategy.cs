using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Knux multi-indicator strategy.
/// Combines ADX, RVI, CCI, Williams %R and a moving average crossover.
/// </summary>
public class KnuxStrategy : Strategy
{
	private readonly StrategyParam<int> _fastMaLength;
	private readonly StrategyParam<int> _slowMaLength;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _adxLevel;
	private readonly StrategyParam<int> _rviPeriod;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<decimal> _cciLevel;
	private readonly StrategyParam<int> _wprPeriod;
	private readonly StrategyParam<decimal> _wprBuyRange;
	private readonly StrategyParam<decimal> _wprSellRange;
	private readonly StrategyParam<DataType> _candleType;

	private SMA _fastMa;
	private SMA _slowMa;
	private ADX _adx;
	private RVI _rvi;
	private CCI _cci;
	private WilliamsR _wpr;

	private decimal _prevFast;
	private decimal _prevSlow;
	private decimal _prevRvi;
	private bool _wasFastBelow;
	private bool _isInitialized;

	/// <summary>
	/// Fast MA period.
	/// </summary>
	public int FastMaLength
	{
		get => _fastMaLength.Value;
		set => _fastMaLength.Value = value;
	}

	/// <summary>
	/// Slow MA period.
	/// </summary>
	public int SlowMaLength
	{
		get => _slowMaLength.Value;
		set => _slowMaLength.Value = value;
	}

	/// <summary>
	/// ADX calculation period.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// Minimum ADX level required for signals.
	/// </summary>
	public decimal AdxLevel
	{
		get => _adxLevel.Value;
		set => _adxLevel.Value = value;
	}

	/// <summary>
	/// RVI calculation period.
	/// </summary>
	public int RviPeriod
	{
		get => _rviPeriod.Value;
		set => _rviPeriod.Value = value;
	}

	/// <summary>
	/// CCI calculation period.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// Absolute CCI level for signals.
	/// </summary>
	public decimal CciLevel
	{
		get => _cciLevel.Value;
		set => _cciLevel.Value = value;
	}

	/// <summary>
	/// Williams %R period.
	/// </summary>
	public int WprPeriod
	{
		get => _wprPeriod.Value;
		set => _wprPeriod.Value = value;
	}

	/// <summary>
	/// Oversold threshold for Williams %R.
	/// </summary>
	public decimal WprBuyRange
	{
		get => _wprBuyRange.Value;
		set => _wprBuyRange.Value = value;
	}

	/// <summary>
	/// Overbought threshold for Williams %R.
	/// </summary>
	public decimal WprSellRange
	{
		get => _wprSellRange.Value;
		set => _wprSellRange.Value = value;
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
	/// Initializes parameters.
	/// </summary>
	public KnuxStrategy()
	{
		_fastMaLength = Param(nameof(FastMaLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA Length", "Period of fast moving average", "General");

		_slowMaLength = Param(nameof(SlowMaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA Length", "Period of slow moving average", "General");

		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ADX Period", "ADX calculation period", "General");

		_adxLevel = Param(nameof(AdxLevel), 15m)
			.SetGreaterThanZero()
			.SetDisplay("ADX Level", "Minimum ADX level", "General");

		_rviPeriod = Param(nameof(RviPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("RVI Period", "RVI calculation period", "General");

		_cciPeriod = Param(nameof(CciPeriod), 40)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "CCI calculation period", "General");

		_cciLevel = Param(nameof(CciLevel), 150m)
			.SetGreaterThanZero()
			.SetDisplay("CCI Level", "Absolute CCI threshold", "General");

		_wprPeriod = Param(nameof(WprPeriod), 60)
			.SetGreaterThanZero()
			.SetDisplay("WPR Period", "Williams %R period", "General");

		_wprBuyRange = Param(nameof(WprBuyRange), 15m)
			.SetGreaterThanZero()
			.SetDisplay("WPR Buy Range", "Oversold threshold", "General");

		_wprSellRange = Param(nameof(WprSellRange), 15m)
			.SetGreaterThanZero()
			.SetDisplay("WPR Sell Range", "Overbought threshold", "General");

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
		_prevFast = _prevSlow = _prevRvi = 0m;
		_wasFastBelow = false;
		_isInitialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastMa = new SMA { Length = FastMaLength };
		_slowMa = new SMA { Length = SlowMaLength };
		_adx = new ADX { Length = AdxPeriod };
		_rvi = new RVI { Length = RviPeriod };
		_cci = new CCI { Length = CciPeriod };
		_wpr = new WilliamsR { Length = WprPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_fastMa, _slowMa, _adx, _rvi, _cci, _wpr, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawIndicator(area, _adx);
			DrawIndicator(area, _rvi);
			DrawIndicator(area, _cci);
			DrawIndicator(area, _wpr);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal adxValue,
		decimal rviValue, decimal cciValue, decimal wprValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!_fastMa.IsFormed || !_slowMa.IsFormed || !_adx.IsFormed || !_rvi.IsFormed || !_cci.IsFormed || !_wpr.IsFormed)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!_isInitialized)
		{
		_prevFast = fast;
		_prevSlow = slow;
		_prevRvi = rviValue;
		_wasFastBelow = fast < slow;
		_isInitialized = true;
		return;
		}

		var crossUp = _wasFastBelow && fast > slow;
		var crossDown = !_wasFastBelow && fast < slow;
		var rviRising = rviValue > _prevRvi;
		var rviFalling = rviValue < _prevRvi;

		if (adxValue > AdxLevel)
		{
		if (crossUp && rviRising && cciValue < -CciLevel && wprValue <= -100m + WprBuyRange && Position <= 0)
		{
		BuyMarket(Volume + Math.Abs(Position));
		}
		else if (crossDown && rviFalling && cciValue > CciLevel && wprValue >= -WprSellRange && Position >= 0)
		{
		SellMarket(Volume + Math.Abs(Position));
		}
		}

		_wasFastBelow = fast < slow;
		_prevFast = fast;
		_prevSlow = slow;
		_prevRvi = rviValue;
	}
}
