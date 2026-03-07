using System;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// CCI reversal strategy filtered by deterministic put/call ratio divergence.
/// </summary>
public class CciPutCallRatioDivergenceStrategy : Strategy
{
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private CommodityChannelIndex _cci = null!;
	private AverageTrueRange _atr = null!;
	private decimal _prevPcr;
	private decimal _currentPcr;
	private decimal _prevPrice;
	private decimal? _prevCci;
	private int _cooldownRemaining;

	/// <summary>
	/// CCI period.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier for stop loss.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Closed candles to wait between position changes.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize strategy.
	/// </summary>
	public CciPutCallRatioDivergenceStrategy()
	{
		_cciPeriod = Param(nameof(CciPeriod), 20)
			.SetRange(10, 50)
			.SetDisplay("CCI Period", "Period for CCI calculation", "Indicators");

		_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
			.SetRange(1m, 5m)
			.SetDisplay("ATR Multiplier", "Multiplier for ATR-based stop loss", "Risk Management");

		_cooldownBars = Param(nameof(CooldownBars), 24)
			.SetNotNegative()
			.SetDisplay("Cooldown Bars", "Closed candles to wait before another position change", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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

		_cci?.Reset();
		_atr?.Reset();

		_cci = null!;
		_atr = null!;
		_prevPcr = 0m;
		_currentPcr = 0m;
		_prevPrice = 0m;
		_prevCci = null;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_cci = new CommodityChannelIndex
		{
			Length = CciPeriod
		};

		_atr = new AverageTrueRange
		{
			Length = 14
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_cci, _atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _cci);
			DrawIndicator(area, _atr);
			DrawOwnTrades(area);
		}

		StartProtection(
			new Unit(2, UnitTypes.Percent),
			new Unit(2, UnitTypes.Percent)
		);
	}

	private void ProcessCandle(ICandleMessage candle, decimal cci, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var price = candle.ClosePrice;
		UpdatePutCallRatio(candle);

		if (_prevPrice == 0m)
		{
			_prevPrice = price;
			_prevPcr = _currentPcr;
			_prevCci = cci;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevPrice = price;
			_prevPcr = _currentPcr;
			_prevCci = cci;
			return;
		}

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		var bullishDivergence = price < _prevPrice && _currentPcr > _prevPcr;
		var bearishDivergence = price > _prevPrice && _currentPcr < _prevPcr;
		var oversoldCross = _prevCci is decimal previousCci && previousCci >= -100m && cci < -100m;
		var overboughtCross = _prevCci is decimal previousCci2 && previousCci2 <= 100m && cci > 100m;

		if (_cooldownRemaining == 0 && oversoldCross && bullishDivergence && Position <= 0)
		{
			BuyMarket(Volume + (Position < 0 ? Math.Abs(Position) : 0m));
			_cooldownRemaining = CooldownBars;
		}
		else if (_cooldownRemaining == 0 && overboughtCross && bearishDivergence && Position >= 0)
		{
			SellMarket(Volume + (Position > 0 ? Math.Abs(Position) : 0m));
			_cooldownRemaining = CooldownBars;
		}
		else if (Position > 0 && cci >= 20m)
		{
			SellMarket(Position);
			_cooldownRemaining = CooldownBars;
		}
		else if (Position < 0 && cci <= -20m)
		{
			BuyMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}

		_prevPrice = price;
		_prevPcr = _currentPcr;
		_prevCci = cci;
	}

	private void UpdatePutCallRatio(ICandleMessage candle)
	{
		var priceChange = (candle.ClosePrice - candle.OpenPrice) / Math.Max(candle.OpenPrice, 1m);
		var range = (candle.HighPrice - candle.LowPrice) / Math.Max(candle.OpenPrice, 1m);
		var skew = Math.Min(0.2m, range * 5m);

		if (priceChange >= 0)
			_currentPcr = 0.8m - priceChange + skew;
		else
			_currentPcr = 1.1m + Math.Abs(priceChange) + skew;

		_currentPcr = Math.Max(0.5m, Math.Min(2.0m, _currentPcr));
	}
}
