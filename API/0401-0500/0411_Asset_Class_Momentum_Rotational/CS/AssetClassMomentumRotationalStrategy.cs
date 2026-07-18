// AssetClassMomentumRotationalStrategy.cs
// -----------------------------------------------------------------------------
// Momentum rotation strategy using rate of change indicator.
// Enters long when ROC is positive and above threshold.
// Exits when ROC turns negative. Uses SMA filter for trend confirmation.
// Cooldown prevents excessive trading.
// -----------------------------------------------------------------------------
// Date: 2 Aug 2025
// -----------------------------------------------------------------------------
using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Momentum rotation strategy using ROC and SMA trend filter.
/// </summary>
public class AssetClassMomentumRotationalStrategy : Strategy
{
	private readonly StrategyParam<int> _rocLength;
	private readonly StrategyParam<int> _smaPeriod;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Rate of change lookback length.
	/// </summary>
	public int RocLength
	{
		get => _rocLength.Value;
		set => _rocLength.Value = value;
	}

	/// <summary>
	/// SMA period for trend filter.
	/// </summary>
	public int SmaPeriod
	{
		get => _smaPeriod.Value;
		set => _smaPeriod.Value = value;
	}

	/// <summary>
	/// Cooldown bars between trades.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Candle type used to compute momentum.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	private RateOfChange _roc;
	private SimpleMovingAverage _sma;
	private int _cooldownRemaining;

	public AssetClassMomentumRotationalStrategy()
	{
		_rocLength = Param(nameof(RocLength), 14)
			.SetDisplay("ROC Length", "Rate of change lookback", "Parameters");

		_smaPeriod = Param(nameof(SmaPeriod), 30)
			.SetDisplay("SMA Period", "SMA period for trend filter", "Parameters");

		_cooldownBars = Param(nameof(CooldownBars), 20)
			.SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Candle type used for momentum", "General");
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

		_roc = null;
		_sma = null;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_roc = new RateOfChange { Length = RocLength };
		_sma = new SimpleMovingAverage { Length = SmaPeriod };

		SubscribeCandles(CandleType)
			.Bind(_roc, _sma, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rocValue, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_roc.IsFormed || !_sma.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			return;
		}

		var close = candle.ClosePrice;

		// Strong positive momentum + price above SMA -> long
		if (rocValue > 0 && close > smaValue && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));

			BuyMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Negative momentum or price below SMA -> exit/short
		else if (rocValue < 0 && close < smaValue && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));

			SellMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
	}
}
