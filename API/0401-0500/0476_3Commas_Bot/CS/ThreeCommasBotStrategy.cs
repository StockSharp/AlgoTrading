namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// 3Commas Bot Strategy (simplified).
/// A long opens when the MaLength1 EMA crosses above the MaLength2 EMA and a short on the opposite cross, reversing an open
/// position. The stop is RiskM ATRs from the entry and the reward threshold RnR times that risk. With UseTakeProfit the
/// position closes at the threshold; with UseTrailingStop reaching it instead starts an ATR trailing stop that follows
/// the best price at RiskM ATRs.
/// </summary>
public class ThreeCommasBotStrategy : Strategy
{
	private readonly StrategyParam<int> _maLength1;
	private readonly StrategyParam<int> _maLength2;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _rnR;
	private readonly StrategyParam<decimal> _riskM;
	private readonly StrategyParam<bool> _useTakeProfit;
	private readonly StrategyParam<bool> _useTrailingStop;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevFast;
	private decimal? _prevSlow;
	private decimal? _stopPrice;
	private decimal? _targetPrice;
	private bool _trailing;

	/// <summary>
	/// Fast EMA period.
	/// </summary>
	public int MaLength1
	{
		get => _maLength1.Value;
		set => _maLength1.Value = value;
	}

	/// <summary>
	/// Slow EMA period.
	/// </summary>
	public int MaLength2
	{
		get => _maLength2.Value;
		set => _maLength2.Value = value;
	}

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <summary>
	/// Reward to risk ratio of the target.
	/// </summary>
	public decimal RnR
	{
		get => _rnR.Value;
		set => _rnR.Value = value;
	}

	/// <summary>
	/// ATR multiplier of the risk.
	/// </summary>
	public decimal RiskM
	{
		get => _riskM.Value;
		set => _riskM.Value = value;
	}

	/// <summary>
	/// Close the position at the reward target.
	/// </summary>
	public bool UseTakeProfit
	{
		get => _useTakeProfit.Value;
		set => _useTakeProfit.Value = value;
	}

	/// <summary>
	/// Trail the stop by ATR once the reward target is reached.
	/// </summary>
	public bool UseTrailingStop
	{
		get => _useTrailingStop.Value;
		set => _useTrailingStop.Value = value;
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
	public ThreeCommasBotStrategy()
	{
		_maLength1 = Param(nameof(MaLength1), 21)
			.SetGreaterThanZero()
			.SetDisplay("MA Length 1", "Fast EMA period", "Indicators");

		_maLength2 = Param(nameof(MaLength2), 50)
			.SetGreaterThanZero()
			.SetDisplay("MA Length 2", "Slow EMA period", "Indicators");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR period", "Risk");

		_rnR = Param(nameof(RnR), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Reward/Risk", "Reward to risk ratio of the target", "Risk");

		_riskM = Param(nameof(RiskM), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Risk Multiplier", "ATR multiplier of the risk", "Risk");

		_useTakeProfit = Param(nameof(UseTakeProfit), true)
			.SetDisplay("Use Take Profit", "Close the position at the reward target", "Risk");

		_useTrailingStop = Param(nameof(UseTrailingStop), false)
			.SetDisplay("Use Trailing Stop", "Trail the stop by ATR once the reward target is reached", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		ResetState();
	}

	private void ResetState()
	{
		_prevFast = null;
		_prevSlow = null;
		ClearProtection();
	}

	private void ClearProtection()
	{
		_stopPrice = null;
		_targetPrice = null;
		_trailing = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var fastEma = new ExponentialMovingAverage { Length = MaLength1 };
		var slowEma = new ExponentialMovingAverage { Length = MaLength2 };
		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(fastEma, slowEma, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastEma);
			DrawIndicator(area, slowEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue fastValue, IIndicatorValue slowValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!fastValue.IsFormed || !slowValue.IsFormed || !atrValue.IsFormed)
			return;

		var fast = fastValue.GetValue<decimal>();
		var slow = slowValue.GetValue<decimal>();
		var atr = atrValue.GetValue<decimal>();
		var prevFast = _prevFast;
		var prevSlow = _prevSlow;
		_prevFast = fast;
		_prevSlow = slow;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var risk = atr * RiskM;

		if (Position > 0 && ManageLong(candle, risk))
			return;

		if (Position < 0 && ManageShort(candle, risk))
			return;

		if (prevFast is not decimal pf || prevSlow is not decimal ps)
			return;

		var close = candle.ClosePrice;

		if (pf <= ps && fast > slow && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			ClearProtection();
			_stopPrice = close - risk;
			_targetPrice = close + risk * RnR;
		}
		else if (pf >= ps && fast < slow && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			ClearProtection();
			_stopPrice = close + risk;
			_targetPrice = close - risk * RnR;
		}
	}

	private bool ManageLong(ICandleMessage candle, decimal risk)
	{
		if (_targetPrice is decimal target && !_trailing && candle.HighPrice >= target)
		{
			if (UseTrailingStop)
				_trailing = true;
			else if (UseTakeProfit)
			{
				SellMarket(Position);
				ClearProtection();
				return true;
			}
		}

		if (_trailing)
		{
			var trailed = candle.HighPrice - risk;
			if (_stopPrice is not decimal current || trailed > current)
				_stopPrice = trailed;
		}

		if (_stopPrice is decimal stop && candle.LowPrice <= stop)
		{
			SellMarket(Position);
			ClearProtection();
			return true;
		}

		return false;
	}

	private bool ManageShort(ICandleMessage candle, decimal risk)
	{
		if (_targetPrice is decimal target && !_trailing && candle.LowPrice <= target)
		{
			if (UseTrailingStop)
				_trailing = true;
			else if (UseTakeProfit)
			{
				BuyMarket(-Position);
				ClearProtection();
				return true;
			}
		}

		if (_trailing)
		{
			var trailed = candle.LowPrice + risk;
			if (_stopPrice is not decimal current || trailed < current)
				_stopPrice = trailed;
		}

		if (_stopPrice is decimal stop && candle.HighPrice >= stop)
		{
			BuyMarket(-Position);
			ClearProtection();
			return true;
		}

		return false;
	}
}
