namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Fundamental strategy based on financial ratios.
/// Opens long positions when key ratios improve.
/// </summary>
public class FinancialRatiosFundamentalStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevCurrentRatio;
	private decimal _prevInterestCoverage;
	private decimal _prevPayableTurnover;
	private decimal _prevGrossMargin;
	private bool _initialized;

	/// <summary>
	/// Candle type parameter.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="FinancialRatiosFundamentalStrategy"/>.
	/// </summary>
	public FinancialRatiosFundamentalStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
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

		_prevCurrentRatio = 0m;
		_prevInterestCoverage = 0m;
		_prevPayableTurnover = 0m;
		_prevGrossMargin = 0m;
		_initialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var currentRatio = GetFinancialValue("CURRENT_RATIO");
		var interestCoverage = GetFinancialValue("INTEREST_COVERAGE");
		var payableTurnover = GetFinancialValue("PAYABLE_TURNOVER");
		var grossMargin = GetFinancialValue("GROSS_MARGIN");

		if (!_initialized)
		{
			_prevCurrentRatio = currentRatio;
			_prevInterestCoverage = interestCoverage;
			_prevPayableTurnover = payableTurnover;
			_prevGrossMargin = grossMargin;
			_initialized = true;
			return;
		}

		var longTot =
			currentRatio > _prevCurrentRatio ||
			interestCoverage < _prevInterestCoverage ||
			payableTurnover > _prevPayableTurnover ||
			grossMargin > _prevGrossMargin;

		var exitLong =
			currentRatio < _prevCurrentRatio ||
			interestCoverage > _prevInterestCoverage ||
			payableTurnover < _prevPayableTurnover ||
			grossMargin < _prevGrossMargin;

		if (longTot && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (exitLong && Position > 0)
			SellMarket(Position);

		_prevCurrentRatio = currentRatio;
		_prevInterestCoverage = interestCoverage;
		_prevPayableTurnover = payableTurnover;
		_prevGrossMargin = grossMargin;
	}

	private decimal GetFinancialValue(string field)
	{
		// TODO: implement fundamental data retrieval
		return 0m;
	}
}
