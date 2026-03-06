using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Mean-reversion strategy that enters only when Hurst indicates anti-persistent behavior and ATR confirms a quiet regime.
/// </summary>
public class HurstVolatilityFilterStrategy : Strategy
{
	private readonly StrategyParam<int> _hurstPeriod;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _hurstThreshold;
	private readonly StrategyParam<decimal> _deviationAtrMultiplier;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _sma;
	private AverageTrueRange _atr;
	private HurstExponent _hurstExponent;
	private SimpleMovingAverage _atrAverage;
	private int _cooldown;

	/// <summary>
	/// Period for Hurst exponent calculation.
	/// </summary>
	public int HurstPeriod
	{
		get => _hurstPeriod.Value;
		set => _hurstPeriod.Value = value;
	}

	/// <summary>
	/// Period for moving average calculation.
	/// </summary>
	public int MAPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Period for ATR calculation.
	/// </summary>
	public int ATRPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Maximum Hurst value allowed for entries.
	/// </summary>
	public decimal HurstThreshold
	{
		get => _hurstThreshold.Value;
		set => _hurstThreshold.Value = value;
	}

	/// <summary>
	/// ATR multiple required for deviation from the moving average.
	/// </summary>
	public decimal DeviationAtrMultiplier
	{
		get => _deviationAtrMultiplier.Value;
		set => _deviationAtrMultiplier.Value = value;
	}

	/// <summary>
	/// Stop loss percentage.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Bars to wait after each order.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Candle series used for calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public HurstVolatilityFilterStrategy()
	{
		_hurstPeriod = Param(nameof(HurstPeriod), 80)
			.SetRange(20, 200)
			.SetDisplay("Hurst Period", "Period for the Hurst exponent", "Indicators");

		_maPeriod = Param(nameof(MAPeriod), 20)
			.SetRange(5, 100)
			.SetDisplay("MA Period", "Period for the moving average", "Indicators");

		_atrPeriod = Param(nameof(ATRPeriod), 14)
			.SetRange(5, 50)
			.SetDisplay("ATR Period", "Period for the ATR", "Indicators");

		_hurstThreshold = Param(nameof(HurstThreshold), 0.7m)
			.SetRange(-1m, 1m)
			.SetDisplay("Hurst Threshold", "Maximum Hurst value allowed for entries", "Signals");

		_deviationAtrMultiplier = Param(nameof(DeviationAtrMultiplier), 0.5m)
			.SetRange(0.1m, 5m)
			.SetDisplay("Deviation ATR", "Minimum ATR multiple required for entry", "Signals");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetRange(0.5m, 10m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

		_cooldownBars = Param(nameof(CooldownBars), 90)
			.SetRange(1, 500)
			.SetDisplay("Cooldown Bars", "Bars to wait after each order", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security != null)
			yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_sma = null;
		_atr = null;
		_hurstExponent = null;
		_atrAverage = null;
		_cooldown = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		if (Security == null)
			throw new InvalidOperationException("Security is not specified.");

		_sma = new SimpleMovingAverage { Length = MAPeriod };
		_atr = new AverageTrueRange { Length = ATRPeriod };
		_hurstExponent = new HurstExponent { Length = HurstPeriod };
		_atrAverage = new SimpleMovingAverage { Length = Math.Max(ATRPeriod * 2, 10) };
		_cooldown = 0;

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(_sma, _atr, _hurstExponent, ProcessCandle)
			.Start();

		var area = CreateChartArea();

		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _sma);
			DrawIndicator(area, _atr);
			DrawIndicator(area, _hurstExponent);
			DrawOwnTrades(area);
		}

		StartProtection(new Unit(0, UnitTypes.Absolute), new Unit(StopLossPercent, UnitTypes.Percent), false);
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue, decimal atrValue, decimal hurstValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var atrAverageValue = _atrAverage.Process(atrValue, candle.OpenTime, true).ToDecimal();

		if (!_sma.IsFormed || !_atr.IsFormed || !_hurstExponent.IsFormed || !_atrAverage.IsFormed)
			return;

		if (ProcessState != ProcessStates.Started)
			return;

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		var price = candle.ClosePrice;
		var deviation = price - smaValue;
		var requiredDeviation = atrValue * DeviationAtrMultiplier;
		var isMeanReversionRegime = hurstValue <= HurstThreshold;
		var isQuietVolatility = atrValue <= atrAverageValue * 1.5m;

		if (Position == 0)
		{
			if (!isMeanReversionRegime || !isQuietVolatility)
				return;

			if (deviation <= -requiredDeviation)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
			else if (deviation >= requiredDeviation)
			{
				SellMarket();
				_cooldown = CooldownBars;
			}

			return;
		}

		if (Position > 0 && (price >= smaValue || deviation >= -atrValue * 0.2m || !isMeanReversionRegime))
		{
			SellMarket(Math.Abs(Position));
			_cooldown = CooldownBars;
		}
		else if (Position < 0 && (price <= smaValue || deviation <= atrValue * 0.2m || !isMeanReversionRegime))
		{
			BuyMarket(Math.Abs(Position));
			_cooldown = CooldownBars;
		}
	}
}
