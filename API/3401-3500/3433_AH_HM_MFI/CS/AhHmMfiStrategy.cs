namespace StockSharp.Samples.Strategies;

using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Hammer/hanging-man lower-shadow patterns confirmed by previous SMA trend and MFI.
/// </summary>
public class AhHmMfiStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _mfiPeriod;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<decimal> _hammerEntryThreshold;
	private readonly StrategyParam<decimal> _hangingEntryThreshold;
	private readonly StrategyParam<decimal> _mfiUpperExitLevel;
	private readonly StrategyParam<decimal> _mfiLowerExitLevel;

	private MoneyFlowIndex _mfi;
	private SimpleMovingAverage _ma;
	private ICandleMessage _previousCandle;
	private decimal? _previousMa;
	private decimal? _previousMfi;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int MfiPeriod { get => _mfiPeriod.Value; set => _mfiPeriod.Value = value; }
	public int MaPeriod { get => _maPeriod.Value; set => _maPeriod.Value = value; }
	public decimal HammerEntryThreshold { get => _hammerEntryThreshold.Value; set => _hammerEntryThreshold.Value = value; }
	public decimal HangingEntryThreshold { get => _hangingEntryThreshold.Value; set => _hangingEntryThreshold.Value = value; }
	public decimal MfiUpperExitLevel { get => _mfiUpperExitLevel.Value; set => _mfiUpperExitLevel.Value = value; }
	public decimal MfiLowerExitLevel { get => _mfiLowerExitLevel.Value; set => _mfiLowerExitLevel.Value = value; }

	public AhHmMfiStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame());
		_mfiPeriod = Param(nameof(MfiPeriod), 47).SetGreaterThanZero();
		_maPeriod = Param(nameof(MaPeriod), 5).SetGreaterThanZero();
		_hammerEntryThreshold = Param(nameof(HammerEntryThreshold), 40m);
		_hangingEntryThreshold = Param(nameof(HangingEntryThreshold), 60m);
		_mfiUpperExitLevel = Param(nameof(MfiUpperExitLevel), 70m);
		_mfiLowerExitLevel = Param(nameof(MfiLowerExitLevel), 30m);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		ResetRuntime();
	}

	private void ResetRuntime()
	{
		_previousCandle = null;
		_previousMa = null;
		_previousMfi = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		ResetRuntime();
		_mfi = new MoneyFlowIndex { Length = MfiPeriod };
		_ma = new SimpleMovingAverage { Length = MaPeriod };
		SubscribeCandles(CandleType).Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var mfiValue = _mfi.Process(candle);
		var maValue = _ma.Process(candle.ClosePrice, candle.OpenTime, true);

		if (!mfiValue.IsFormed || !maValue.IsFormed)
		{
			Remember(candle, maValue.IsFormed ? maValue.ToDecimal() : null,
				mfiValue.IsFormed ? mfiValue.ToDecimal() : null);
			return;
		}

		var mfi = mfiValue.ToDecimal();
		var ma = maValue.ToDecimal();
		if (_previousMfi is decimal previousMfi)
		{
			if (Position > 0m &&
				((previousMfi <= MfiUpperExitLevel && mfi > MfiUpperExitLevel) ||
				 (previousMfi >= MfiLowerExitLevel && mfi < MfiLowerExitLevel)))
			{
				ClosePosition();
				Remember(candle, ma, mfi);
				return;
			}

			if (Position < 0m &&
				((previousMfi <= MfiLowerExitLevel && mfi > MfiLowerExitLevel) ||
				 (previousMfi <= MfiUpperExitLevel && mfi > MfiUpperExitLevel)))
			{
				ClosePosition();
				Remember(candle, ma, mfi);
				return;
			}
		}

		if (Position == 0m && _previousCandle is { } previous && _previousMa is decimal previousMa)
		{
			var body = Math.Abs(candle.ClosePrice - candle.OpenPrice);
			var range = candle.HighPrice - candle.LowPrice;
			if (body > 0m && range > 0m)
			{
				var upperShadow = candle.HighPrice - Math.Max(candle.OpenPrice, candle.ClosePrice);
				var lowerShadow = Math.Min(candle.OpenPrice, candle.ClosePrice) - candle.LowPrice;
				var bodyInUpperThird = Math.Min(candle.OpenPrice, candle.ClosePrice) >= candle.HighPrice - range / 3m;
				var lowerTailPattern = bodyInUpperThird && lowerShadow >= body * 2.5m && upperShadow <= body * 0.5m;
				var previousMidpoint = (previous.OpenPrice + previous.ClosePrice) / 2m;

				if (lowerTailPattern && candle.OpenPrice < previous.ClosePrice &&
					previousMidpoint < previousMa && mfi <= HammerEntryThreshold)
					BuyMarket();
				else if (lowerTailPattern && candle.OpenPrice > previous.ClosePrice &&
					previousMidpoint > previousMa && mfi >= HangingEntryThreshold)
					SellMarket();
			}
		}

		Remember(candle, ma, mfi);
	}

	private void Remember(ICandleMessage candle, decimal? ma, decimal? mfi)
	{
		_previousCandle = candle;
		_previousMa = ma;
		_previousMfi = mfi;
	}
}
