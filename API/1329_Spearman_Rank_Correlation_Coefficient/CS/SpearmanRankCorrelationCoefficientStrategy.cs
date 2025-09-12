using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Spearman Rank Correlation Coefficient strategy.
/// Trades a pair of securities based on Spearman correlation.
/// </summary>
public class SpearmanRankCorrelationCoefficientStrategy : Strategy
{
	private readonly StrategyParam<Security> _security2;
	private readonly StrategyParam<int> _correlationPeriod;
	private readonly StrategyParam<decimal> _threshold;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<decimal> _security1Prices = new();
	private readonly Queue<decimal> _security2Prices = new();

	private decimal _security1LastPrice;
	private decimal _security2LastPrice;
	private bool _security1Updated;
	private bool _security2Updated;
	private decimal _currentCorrelation;

	/// <summary>
	/// First security for correlation calculation.
	/// </summary>
	public Security Security1
	{
		get => Security;
		set => Security = value;
	}

	/// <summary>
	/// Second security for correlation calculation.
	/// </summary>
	public Security Security2
	{
		get => _security2.Value;
		set => _security2.Value = value;
	}

	/// <summary>
	/// Period for Spearman correlation calculation.
	/// </summary>
	public int CorrelationPeriod
	{
		get => _correlationPeriod.Value;
		set => _correlationPeriod.Value = value;
	}

	/// <summary>
	/// Correlation threshold for entries.
	/// </summary>
	public decimal Threshold
	{
		get => _threshold.Value;
		set => _threshold.Value = value;
	}

	/// <summary>
	/// Candle type for data subscription.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SpearmanRankCorrelationCoefficientStrategy"/> class.
	/// </summary>
	public SpearmanRankCorrelationCoefficientStrategy()
	{
		_security2 = Param(nameof(Security2), default(Security))
			.SetDisplay("Second Security", "Secondary symbol for correlation", "Parameters");
		_correlationPeriod = Param(nameof(CorrelationPeriod), 10)
			.SetDisplay("Correlation Period", "Spearman correlation period", "Parameters");
		_threshold = Param(nameof(Threshold), 0.8m)
			.SetDisplay("Threshold", "Correlation threshold for entries", "Parameters");
		_candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(1)))
			.SetDisplay("Candle Type", "Candle type", "Parameters");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (Security1 == null)
			throw new InvalidOperationException("First security is not specified.");

		if (Security2 == null)
			throw new InvalidOperationException("Second security is not specified.");

		var subscription1 = SubscribeCandles(CandleType, false, Security1);
		var subscription2 = SubscribeCandles(CandleType, false, Security2);

		subscription1
			.Bind(ProcessSecurity1Candle)
			.Start();

		subscription2
			.Bind(ProcessSecurity2Candle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription1);
			DrawCandles(area, subscription2);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessSecurity1Candle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_security1LastPrice = candle.ClosePrice;
		_security1Updated = true;
		TryCalculate(candle.ServerTime);
	}

	private void ProcessSecurity2Candle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_security2LastPrice = candle.ClosePrice;
		_security2Updated = true;
		TryCalculate(candle.ServerTime);
	}

	private void TryCalculate(DateTimeOffset time)
	{
		if (!_security1Updated || !_security2Updated)
			return;

		_security1Updated = false;
		_security2Updated = false;

		_security1Prices.Enqueue(_security1LastPrice);
		_security2Prices.Enqueue(_security2LastPrice);

		while (_security1Prices.Count > CorrelationPeriod)
		{
			_security1Prices.Dequeue();
			_security2Prices.Dequeue();
		}

		if (_security1Prices.Count < CorrelationPeriod)
			return;

		_currentCorrelation = CalculateSpearman([.. _security1Prices], [.. _security2Prices]);
		CheckSignal();
	}

	private static decimal CalculateSpearman(decimal[] series1, decimal[] series2)
	{
		if (series1.Length < 2 || series1.Length != series2.Length)
			return 0;

		var ranks1 = GetRanks(series1);
		var ranks2 = GetRanks(series2);

		decimal mean1 = ranks1.Average();
		decimal mean2 = ranks2.Average();

		decimal sum1 = 0;
		decimal sum2 = 0;
		decimal sum12 = 0;

		for (int i = 0; i < ranks1.Length; i++)
		{
			decimal diff1 = ranks1[i] - mean1;
			decimal diff2 = ranks2[i] - mean2;

			sum1 += diff1 * diff1;
			sum2 += diff2 * diff2;
			sum12 += diff1 * diff2;
		}

		var denom = (decimal)Math.Sqrt((double)(sum1 * sum2));
		return denom == 0 ? 0 : sum12 / denom;
	}

	private static decimal[] GetRanks(decimal[] series)
	{
		var sorted = series.Select((v, i) => new { v, i }).OrderBy(t => t.v).ToArray();
		var ranks = new decimal[series.Length];
		int i = 0;
		while (i < sorted.Length)
		{
			int j = i;
			while (j + 1 < sorted.Length && sorted[j + 1].v == sorted[i].v)
				j++;

			var rank = (i + j) / 2m;

			for (int k = i; k <= j; k++)
				ranks[sorted[k].i] = rank;

			i = j + 1;
		}
		return ranks;
	}

	private void CheckSignal()
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (GetPositionValue(Security1) <= 0 && GetPositionValue(Security2) >= 0 && _currentCorrelation > Threshold)
		{
			SellMarket(Security1);
			BuyMarket(Security2);
			LogInfo($"SHORT {Security1.Code}, LONG {Security2.Code}: correlation {_currentCorrelation:F2}");
		}
		else if (GetPositionValue(Security1) >= 0 && GetPositionValue(Security2) <= 0 && _currentCorrelation < -Threshold)
		{
			BuyMarket(Security1);
			SellMarket(Security2);
			LogInfo($"LONG {Security1.Code}, SHORT {Security2.Code}: correlation {_currentCorrelation:F2}");
		}
		else if (Math.Abs(_currentCorrelation) < Threshold / 2)
		{
			ClosePosition(Security1);
			ClosePosition(Security2);
			LogInfo($"CLOSE PAIR: correlation {_currentCorrelation:F2}");
		}
	}
}

