using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy combining Laguerre filter, CCI and moving average.
/// </summary>
public class LaguerreCciMaStrategy : Strategy
{
	private readonly StrategyParam<decimal> _lagGamma;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<decimal> _cciLevel;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;

	private LaguerreFilter _laguerre;
	private CommodityChannelIndex _cci;
	private ExponentialMovingAverage _ma;
	private decimal _prevMa;

	/// <summary>
	/// Laguerre filter gamma.
	/// </summary>
	public decimal LagGamma
	{
		get => _lagGamma.Value;
		set => _lagGamma.Value = value;
	}

	/// <summary>
	/// CCI period.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// Absolute CCI level for entries.
	/// </summary>
	public decimal CciLevel
	{
		get => _cciLevel.Value;
		set => _cciLevel.Value = value;
	}

	/// <summary>
	/// Moving average period.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Candle type to use.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Take profit in absolute price units.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Stop loss in absolute price units.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref=\"LaguerreCciMaStrategy\"/>.
	/// </summary>
	public LaguerreCciMaStrategy()
	{
		_lagGamma = Param(nameof(LagGamma), 0.7m)
			.SetRange(0.1m, 0.9m)
			.SetDisplay(\"Laguerre Gamma\", \"Gamma parameter for Laguerre filter\", \"Indicators\")
			.SetCanOptimize(true);

		_cciPeriod = Param(nameof(CciPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay(\"CCI Period\", \"Period for CCI indicator\", \"Indicators\")
			.SetCanOptimize(true);

		_cciLevel = Param(nameof(CciLevel), 5m)
			.SetGreaterThanZero()
			.SetDisplay(\"CCI Level\", \"Threshold for CCI\", \"Indicators\")
			.SetCanOptimize(true);

		_maPeriod = Param(nameof(MaPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay(\"MA Period\", \"Period for moving average\", \"Indicators\")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay(\"Candle Type\", \"Type of candles\", \"General\");

		_takeProfit = Param(nameof(TakeProfit), 0m)
			.SetDisplay(\"Take Profit\", \"Take profit in absolute price\", \"Risk Management\")
			.SetCanOptimize(true);

		_stopLoss = Param(nameof(StopLoss), 0m)
			.SetDisplay(\"Stop Loss\", \"Stop loss in absolute price\", \"Risk Management\")
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

		_laguerre = default;
		_cci = default;
		_ma = default;
		_prevMa = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_laguerre = new LaguerreFilter { Gamma = LagGamma };
		_cci = new CommodityChannelIndex { Length = CciPeriod };
		_ma = new ExponentialMovingAverage { Length = MaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_laguerre, _cci, _ma, ProcessCandle)
			.Start();

		StartProtection(
				takeProfit: TakeProfit > 0 ? new Unit(TakeProfit, UnitTypes.Absolute) : null,
				stopLoss: StopLoss > 0 ? new Unit(StopLoss, UnitTypes.Absolute) : null
		);

		var area = CreateChartArea();
		if (area != null)
		{
				DrawCandles(area, subscription);
				DrawIndicator(area, _laguerre);
				DrawIndicator(area, _cci);
				DrawIndicator(area, _ma);
				DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal lagValue, decimal cciValue, decimal maValue)
	{
		// Use only finished candles
		if (candle.State != CandleStates.Finished)
				return;

		// Ensure indicators are ready
		if (!_laguerre.IsFormed || !_cci.IsFormed || !_ma.IsFormed)
		{
				_prevMa = maValue;
				return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
				_prevMa = maValue;
				return;
		}

		var isMaRising = maValue > _prevMa;
		var isMaFalling = maValue < _prevMa;

		// Entry signals
		if (lagValue <= 0m && isMaRising && cciValue < -CciLevel && Position <= 0)
		{
				var volume = Volume + Math.Abs(Position);
				BuyMarket(volume);
		}
		else if (lagValue >= 1m && isMaFalling && cciValue > CciLevel && Position >= 0)
		{
				var volume = Volume + Math.Abs(Position);
				SellMarket(volume);
		}

		// Exit signals
		if (Position > 0 && lagValue > 0.9m)
		{
				SellMarket(Math.Abs(Position));
		}
		else if (Position < 0 && lagValue < 0.1m)
		{
				BuyMarket(Math.Abs(Position));
		}

		_prevMa = maValue;
	}
}
