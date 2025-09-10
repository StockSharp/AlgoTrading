using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// BTC Future Gamma-Weighted Momentum Model strategy.
/// </summary>
public class BtcFutureGammaWeightedMomentumModelStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _gammaFactor;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _weightedSum;
	private decimal _weightTotal;
	private decimal _prevClose1;
	private decimal _prevClose2;
	private decimal _prevClose3;
	private int _barCount;

	/// <summary>
	/// Length for GWAP calculation.
	/// </summary>
	public int Length { get => _length.Value; set => _length.Value = value; }

	/// <summary>
	/// Gamma weight factor.
	/// </summary>
	public decimal GammaFactor { get => _gammaFactor.Value; set => _gammaFactor.Value = value; }

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="BtcFutureGammaWeightedMomentumModelStrategy"/>.
	/// </summary>
	public BtcFutureGammaWeightedMomentumModelStrategy()
	{
		_length = Param(nameof(Length), 60)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Length for GWAP calculation", "Parameters")
			.SetCanOptimize(true);

		_gammaFactor = Param(nameof(GammaFactor), 0.75m)
			.SetDisplay("Gamma Factor", "Gamma weight factor", "Parameters")
			.SetCanOptimize(true);

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

		_weightedSum = 0m;
		_weightTotal = 0m;
		_prevClose1 = 0m;
		_prevClose2 = 0m;
		_prevClose3 = 0m;
		_barCount = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_weightedSum = candle.ClosePrice + GammaFactor * _weightedSum;
		_weightTotal = 1m + GammaFactor * _weightTotal;

		var gwap = _weightTotal != 0m ? _weightedSum / _weightTotal : 0m;

		_barCount++;

		if (_barCount <= Math.Max(Length, 3))
		{
			_prevClose3 = _prevClose2;
			_prevClose2 = _prevClose1;
			_prevClose1 = candle.ClosePrice;
			return;
		}

		var longCondition = candle.ClosePrice > gwap && _prevClose1 > _prevClose2 && _prevClose2 > _prevClose3;
		var shortCondition = candle.ClosePrice < gwap && _prevClose1 < _prevClose2 && _prevClose2 < _prevClose3;

		if (longCondition && Position <= 0)
			RegisterBuy();
		else if (shortCondition && Position >= 0)
			RegisterSell();

		_prevClose3 = _prevClose2;
		_prevClose2 = _prevClose1;
		_prevClose1 = candle.ClosePrice;
	}
}
