using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// FVG positioning average with 200 EMA strategy.
/// A bullish fair value gap is a candle whose low is above the high two candles back by more than AtrMultiplier ATRs, a bearish gap
/// the mirror. The levels of the last FvgLookback bullish and bearish gaps are averaged. A close crossing above the bearish average
/// while both averages are above the EMA goes long, a close crossing below the bullish average while both are below the EMA goes short.
/// The stop sits at the lowest low (highest high) of LookbackPeriod candles and the target at RiskReward times that risk.
/// </summary>
public class FvgPositioningAverageWith200EmaAutoTradingStrategy : Strategy
{
	private const int _atrLength = 200;

	private readonly StrategyParam<int> _fvgLookback;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<decimal> _riskReward;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _bullLevels = [];
	private readonly List<decimal> _bearLevels = [];
	private decimal? _high1, _high2, _low1, _low2;
	private decimal? _prevClose, _prevBullAvg, _prevBearAvg;
	private decimal _stopPrice;
	private decimal _takePrice;

	/// <summary>
	/// Number of recent gaps of each direction that are averaged.
	/// </summary>
	public int FvgLookback
	{
		get => _fvgLookback.Value;
		set => _fvgLookback.Value = value;
	}

	/// <summary>
	/// Minimum gap size in ATRs.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Candles used for the stop at the recent low or high.
	/// </summary>
	public int LookbackPeriod
	{
		get => _lookbackPeriod.Value;
		set => _lookbackPeriod.Value = value;
	}

	/// <summary>
	/// Trend EMA period.
	/// </summary>
	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	/// <summary>
	/// Target distance as a multiple of the stop distance.
	/// </summary>
	public decimal RiskReward
	{
		get => _riskReward.Value;
		set => _riskReward.Value = value;
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
	/// Constructor.
	/// </summary>
	public FvgPositioningAverageWith200EmaAutoTradingStrategy()
	{
		_fvgLookback = Param(nameof(FvgLookback), 30)
			.SetGreaterThanZero()
			.SetDisplay("FVG Lookback", "Number of recent gaps of each direction that are averaged", "FVG");

		_atrMultiplier = Param(nameof(AtrMultiplier), 0.25m)
			.SetNotNegative()
			.SetDisplay("ATR Multiplier", "Minimum gap size in ATRs", "FVG");

		_lookbackPeriod = Param(nameof(LookbackPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Lookback Period", "Candles used for the stop at the recent low or high", "Risk");

		_emaPeriod = Param(nameof(EmaPeriod), 200)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "Trend EMA period", "Indicators");

		_riskReward = Param(nameof(RiskReward), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Risk Reward", "Target distance as a multiple of the stop distance", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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
		ResetState();
	}

	private void ResetState()
	{
		_bullLevels.Clear();
		_bearLevels.Clear();
		_high1 = _high2 = _low1 = _low2 = null;
		_prevClose = _prevBullAvg = _prevBearAvg = null;
		_stopPrice = 0m;
		_takePrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var atr = new AverageTrueRange { Length = _atrLength };
		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var highest = new Highest { Length = LookbackPeriod };
		var lowest = new Lowest { Length = LookbackPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(atr, ema, highest, lowest, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atr, decimal ema, decimal highest, decimal lowest)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_high2 is decimal high2 && _low2 is decimal low2)
		{
			var minGap = atr * AtrMultiplier;

			if (candle.LowPrice > high2 && candle.LowPrice - high2 > minGap)
				AddLevel(_bullLevels, candle.LowPrice);
			else if (candle.HighPrice < low2 && low2 - candle.HighPrice > minGap)
				AddLevel(_bearLevels, candle.HighPrice);
		}

		_high2 = _high1;
		_low2 = _low1;
		_high1 = candle.HighPrice;
		_low1 = candle.LowPrice;

		decimal? bullAvg = _bullLevels.Count > 0 ? _bullLevels.Average() : null;
		decimal? bearAvg = _bearLevels.Count > 0 ? _bearLevels.Average() : null;

		var prevClose = _prevClose;
		var prevBullAvg = _prevBullAvg;
		var prevBearAvg = _prevBearAvg;
		_prevClose = candle.ClosePrice;
		_prevBullAvg = bullAvg;
		_prevBearAvg = bearAvg;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (ManagePosition(candle))
			return;

		if (bullAvg is not decimal bull || bearAvg is not decimal bear || prevClose is not decimal pc)
			return;

		var close = candle.ClosePrice;
		var crossUp = prevBearAvg is decimal pBear && pc <= pBear && close > bear;
		var crossDown = prevBullAvg is decimal pBull && pc >= pBull && close < bull;

		if (crossUp && bull > ema && bear > ema && Position <= 0 && lowest < close)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_stopPrice = lowest;
			_takePrice = close + (close - lowest) * RiskReward;
		}
		else if (crossDown && bull < ema && bear < ema && Position >= 0 && highest > close)
		{
			SellMarket(Volume + Math.Abs(Position));
			_stopPrice = highest;
			_takePrice = close - (highest - close) * RiskReward;
		}
	}

	private void AddLevel(List<decimal> levels, decimal level)
	{
		levels.Add(level);

		while (levels.Count > FvgLookback)
			levels.RemoveAt(0);
	}

	// Returns true when the position was closed on this candle.
	private bool ManagePosition(ICandleMessage candle)
	{
		if (Position > 0 && _stopPrice > 0)
		{
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takePrice)
			{
				SellMarket(Position);
				return true;
			}
		}
		else if (Position < 0 && _stopPrice > 0)
		{
			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takePrice)
			{
				BuyMarket(Math.Abs(Position));
				return true;
			}
		}

		return false;
	}
}
