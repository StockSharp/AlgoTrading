using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// JB: Bollinger breakout with long-term SMA, Force Index and martingale sizing.
/// </summary>
public class JbStrategy : Strategy
{
	private readonly StrategyParam<int> _smaPeriod;
	private readonly StrategyParam<int> _forcePeriod;
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<decimal> _lossMultiplier;
	private readonly StrategyParam<decimal> _averageProfitTarget;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _closes = [];
	private decimal? _forceEma;
	private decimal? _lastClose;

	private decimal? _prevClose;
	private decimal? _prevSma;
	private decimal? _prevForce;
	private decimal? _prevLower;
	private decimal? _prevUpper;

	private decimal _nextVolume;
	private decimal _cycleRealizedStart;
	private bool _cycleActive;

	public int SmaPeriod { get => _smaPeriod.Value; set => _smaPeriod.Value = value; }
	public int ForcePeriod { get => _forcePeriod.Value; set => _forcePeriod.Value = value; }
	public int BollingerPeriod { get => _bollingerPeriod.Value; set => _bollingerPeriod.Value = value; }
	public decimal BollingerDeviation { get => _bollingerDeviation.Value; set => _bollingerDeviation.Value = value; }
	public decimal BaseVolume { get => _baseVolume.Value; set => _baseVolume.Value = value; }
	public decimal LossMultiplier { get => _lossMultiplier.Value; set => _lossMultiplier.Value = value; }
	public decimal AverageProfitTarget { get => _averageProfitTarget.Value; set => _averageProfitTarget.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public JbStrategy()
	{
		_smaPeriod = Param(nameof(SmaPeriod), 100).SetGreaterThanZero();
		_forcePeriod = Param(nameof(ForcePeriod), 100).SetGreaterThanZero();
		_bollingerPeriod = Param(nameof(BollingerPeriod), 20).SetGreaterThanZero();
		_bollingerDeviation = Param(nameof(BollingerDeviation), 2m).SetGreaterThanZero();
		_baseVolume = Param(nameof(BaseVolume), 0.1m).SetGreaterThanZero();
		_lossMultiplier = Param(nameof(LossMultiplier), 1.55m).SetGreaterThanZero();
		_averageProfitTarget = Param(nameof(AverageProfitTarget), 2.8m).SetNotNegative();
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame());
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_closes.Clear();
		_forceEma = null;
		_lastClose = null;
		_prevClose = null;
		_prevSma = null;
		_prevForce = null;
		_prevLower = null;
		_prevUpper = null;
		_nextVolume = BaseVolume;
		_cycleRealizedStart = 0m;
		_cycleActive = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_nextVolume = NormalizeVolume(BaseVolume);
		SubscribeCandles(CandleType).Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (Position != 0m)
		{
			var perContract = Math.Abs(Position) > 0m
				? PnLManager.UnrealizedPnL / Math.Abs(Position)
				: 0m;

			if (perContract >= AverageProfitTarget)
			{
				if (Position > 0m)
					SellMarket(Math.Abs(Position));
				else
					BuyMarket(Math.Abs(Position));

				UpdateIndicators(candle);
				return;
			}
		}
		else if (_prevClose is decimal prevClose &&
			_prevSma is decimal prevSma &&
			_prevForce is decimal prevForce &&
			_prevLower is decimal prevLower &&
			_prevUpper is decimal prevUpper)
		{
			var signal = GetSignal(prevClose, prevSma, prevForce, prevLower, prevUpper);

			if (signal > 0)
				Enter(Sides.Buy);
			else if (signal < 0)
				Enter(Sides.Sell);
		}

		UpdateIndicators(candle);
	}

	private void UpdateIndicators(ICandleMessage candle)
	{
		if (_lastClose is decimal last)
		{
			var rawForce = (candle.ClosePrice - last) * candle.TotalVolume;
			_forceEma = Ema(_forceEma, rawForce, ForcePeriod);
		}

		_lastClose = candle.ClosePrice;
		_closes.Add(candle.ClosePrice);

		var keep = Math.Max(SmaPeriod, BollingerPeriod);
		if (_closes.Count > keep)
			_closes.RemoveRange(0, _closes.Count - keep);

		if (_closes.Count < SmaPeriod || _closes.Count < BollingerPeriod || _forceEma is null)
			return;

		var sma = _closes.Skip(_closes.Count - SmaPeriod).Average();
		var bb = _closes.Skip(_closes.Count - BollingerPeriod).ToArray();
		var mean = bb.Average();
		var variance = bb.Select(v => (v - mean) * (v - mean)).Average();
		var std = (decimal)Math.Sqrt((double)variance);

		_prevClose = candle.ClosePrice;
		_prevSma = sma;
		_prevForce = _forceEma.Value;
		_prevLower = mean - BollingerDeviation * std;
		_prevUpper = mean + BollingerDeviation * std;
	}

	private void Enter(Sides side)
	{
		var volume = NormalizeVolume(_nextVolume);
		if (volume <= 0m)
			return;

		_cycleRealizedStart = PnLManager.RealizedPnL;
		_cycleActive = true;

		if (side == Sides.Buy)
			BuyMarket(volume);
		else
			SellMarket(volume);
	}

	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (!_cycleActive || Position != 0m)
			return;

		var cyclePnL = PnLManager.RealizedPnL - _cycleRealizedStart;
		_nextVolume = cyclePnL < 0m
			? NormalizeVolume(_nextVolume * LossMultiplier)
			: NormalizeVolume(BaseVolume);
		_cycleActive = false;
	}

	internal static int GetSignal(decimal previousClose, decimal sma, decimal force, decimal lowerBand, decimal upperBand)
	{
		if (previousClose <= lowerBand && previousClose > sma && force > 0m)
			return 1;

		if (previousClose >= upperBand && previousClose < sma && force < 0m)
			return -1;

		return 0;
	}

	private decimal NormalizeVolume(decimal volume)
	{
		if (Security?.MaxVolume is decimal max && max > 0m)
			volume = Math.Min(volume, max);
		if (Security?.MinVolume is decimal min && min > 0m)
			volume = Math.Max(volume, min);
		if (Security?.VolumeStep is decimal step && step > 0m)
			volume = Math.Floor(volume / step) * step;

		return volume;
	}

	private static decimal Ema(decimal? previous, decimal value, int period)
	{
		if (previous is null)
			return value;

		var alpha = 2m / (period + 1m);
		return previous.Value + alpha * (value - previous.Value);
	}
}
