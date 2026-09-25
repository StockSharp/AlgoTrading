using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// GO strategy: EMA-smoothed OHLC composite multiplied by candle volume.
/// </summary>
public class GoStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<decimal> _openLevel;
	private readonly StrategyParam<decimal> _closeLevelDiff;
	private readonly StrategyParam<bool> _showGo;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _openEma;
	private decimal? _highEma;
	private decimal? _lowEma;
	private decimal? _closeEma;
	private int _samples;

	public int MaPeriod { get => _maPeriod.Value; set => _maPeriod.Value = value; }
	public decimal OpenLevel { get => _openLevel.Value; set => _openLevel.Value = value; }
	public decimal CloseLevelDiff { get => _closeLevelDiff.Value; set => _closeLevelDiff.Value = value; }
	public bool ShowGo { get => _showGo.Value; set => _showGo.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public GoStrategy()
	{
		_maPeriod = Param(nameof(MaPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "EMA period for O/H/L/C smoothing.", "GO");
		_openLevel = Param(nameof(OpenLevel), 0m)
			.SetNotNegative()
			.SetDisplay("Open Level", "Absolute GO threshold required for entry.", "GO");
		_closeLevelDiff = Param(nameof(CloseLevelDiff), 0m)
			.SetNotNegative()
			.SetDisplay("Close Level Diff", "Distance between entry and exit GO thresholds.", "GO");
		_showGo = Param(nameof(ShowGo), false)
			.SetDisplay("Show GO", "Log computed GO values.", "GO");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle type.", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_openEma = null;
		_highEma = null;
		_lowEma = null;
		_closeEma = null;
		_samples = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		SubscribeCandles(CandleType).Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_samples++;
		_openEma = Ema(_openEma, candle.OpenPrice, MaPeriod);
		_highEma = Ema(_highEma, candle.HighPrice, MaPeriod);
		_lowEma = Ema(_lowEma, candle.LowPrice, MaPeriod);
		_closeEma = Ema(_closeEma, candle.ClosePrice, MaPeriod);

		if (_samples < MaPeriod)
			return;

		var go = CalculateGo(_openEma.Value, _highEma.Value, _lowEma.Value, _closeEma.Value, candle.TotalVolume);

		if (ShowGo)
			LogInfo("GO={0}", go);

		var closeLevel = OpenLevel - CloseLevelDiff;

		if (Position > 0m)
		{
			if (go < closeLevel)
				SellMarket(Math.Abs(Position));
			return;
		}

		if (Position < 0m)
		{
			if (go > -closeLevel)
				BuyMarket(Math.Abs(Position));
			return;
		}

		if (go > OpenLevel)
			BuyMarket();
		else if (go < -OpenLevel)
			SellMarket();
	}

	internal static decimal CalculateGo(decimal openEma, decimal highEma, decimal lowEma, decimal closeEma, decimal volume)
		=> ((closeEma - openEma) +
			(highEma - openEma) +
			(lowEma - openEma) +
			(closeEma - lowEma) +
			(closeEma - highEma)) * volume;

	private static decimal Ema(decimal? previous, decimal value, int period)
	{
		if (previous is null)
			return value;

		var alpha = 2m / (period + 1m);
		return previous.Value + alpha * (value - previous.Value);
	}
}
