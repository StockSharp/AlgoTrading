using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// OBV (On-Balance Volume) Divergence strategy.
/// Tracks OBV direction vs price direction over a lookback window.
/// Bullish divergence: price trending down but OBV trending up.
/// Bearish divergence: price trending up but OBV trending down.
/// Uses SMA for exit signals.
/// </summary>
public class ObvDivergenceStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _lookback;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _cumulativeObv;
	private decimal _prevClosePrice;
	private readonly List<decimal> _priceHistory = new();
	private readonly List<decimal> _obvHistory = new();
	private int _cooldown;

	/// <summary>
	/// MA Period.
	/// </summary>
	public int MAPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Lookback period for divergence.
	/// </summary>
	public int Lookback
	{
		get => _lookback.Value;
		set => _lookback.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Cooldown bars.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public ObvDivergenceStrategy()
	{
		_maPeriod = Param(nameof(MAPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Period for SMA exit signal", "Indicators");

		_lookback = Param(nameof(Lookback), 10)
			.SetGreaterThanZero()
			.SetDisplay("Lookback", "Lookback period for divergence detection", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_cooldownBars = Param(nameof(CooldownBars), 500)
			.SetRange(1, 1000)
			.SetDisplay("Cooldown Bars", "Bars to wait between trades", "General");
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
		_cumulativeObv = default;
		_prevClosePrice = default;
		_priceHistory.Clear();
		_obvHistory.Clear();
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_cumulativeObv = 0;
		_prevClosePrice = 0;
		_priceHistory.Clear();
		_obvHistory.Clear();
		_cooldown = 0;

		var sma = new SimpleMovingAverage { Length = MAPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Calculate OBV manually
		if (_prevClosePrice > 0)
		{
			if (candle.ClosePrice > _prevClosePrice)
				_cumulativeObv += candle.TotalVolume;
			else if (candle.ClosePrice < _prevClosePrice)
				_cumulativeObv -= candle.TotalVolume;
		}
		_prevClosePrice = candle.ClosePrice;

		// Store history
		_priceHistory.Add(candle.ClosePrice);
		_obvHistory.Add(_cumulativeObv);

		// Keep only what we need
		if (_priceHistory.Count > Lookback + 1)
		{
			_priceHistory.RemoveAt(0);
			_obvHistory.RemoveAt(0);
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_priceHistory.Count <= Lookback)
			return;

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		// Compare current values to lookback-period-ago values
		var priceChange = _priceHistory[_priceHistory.Count - 1] - _priceHistory[0];
		var obvChange = _obvHistory[_obvHistory.Count - 1] - _obvHistory[0];

		// Bullish divergence: price down but OBV up
		var bullishDiv = priceChange < 0 && obvChange > 0;
		// Bearish divergence: price up but OBV down
		var bearishDiv = priceChange > 0 && obvChange < 0;

		if (Position == 0 && bullishDiv)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
		else if (Position == 0 && bearishDiv)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position > 0 && candle.ClosePrice < smaValue)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position < 0 && candle.ClosePrice > smaValue)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
	}
}
