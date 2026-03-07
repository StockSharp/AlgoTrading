using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Configuration;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Paired switching strategy that rotates between the primary instrument and a benchmark instrument based on the prior quarter's return.
/// </summary>
public class PairedSwitchingStrategy : Strategy
{
	private readonly StrategyParam<string> _security2Id;
	private readonly StrategyParam<DataType> _candleType;

	private readonly RollingWin _primaryPrices = new(21);
	private readonly RollingWin _benchmarkPrices = new(21);
	private Security _benchmark = null!;
	private int _lastProcessedMonthKey;

	/// <summary>
	/// Benchmark instrument identifier.
	/// </summary>
	public string Security2Id
	{
		get => _security2Id.Value;
		set => _security2Id.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public PairedSwitchingStrategy()
	{
		_security2Id = Param(nameof(Security2Id), Paths.HistoryDefaultSecurity2)
			.SetDisplay("Benchmark Security Id", "Identifier of the benchmark instrument", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candles time frame", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security != null)
			yield return (Security, CandleType);

		if (!Security2Id.IsEmpty())
			yield return (new Security { Id = Security2Id }, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_primaryPrices.Clear();
		_benchmarkPrices.Clear();
		_benchmark = null!;
		_lastProcessedMonthKey = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		if (Security == null)
			throw new InvalidOperationException("Primary security is not specified.");

		if (Security2Id.IsEmpty())
			throw new InvalidOperationException("Benchmark security identifier is not specified.");

		_benchmark = this.LookupById(Security2Id) ?? new Security { Id = Security2Id };

		SubscribeCandles(CandleType, security: Security)
			.Bind(candle => ProcessCandle(candle, true))
			.Start();

		SubscribeCandles(CandleType, security: _benchmark)
			.Bind(candle => ProcessCandle(candle, false))
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, bool isPrimary)
	{
		if (candle.State != CandleStates.Finished)
			return;

		(isPrimary ? _primaryPrices : _benchmarkPrices).Add(candle.ClosePrice);

		var day = candle.OpenTime.Date;
		var monthKey = (day.Year * 100) + day.Month;
		if (monthKey == _lastProcessedMonthKey)
			return;

		_lastProcessedMonthKey = monthKey;

		Rebalance();
	}

	private void Rebalance()
	{
		if (!_primaryPrices.Full || !_benchmarkPrices.Full)
			return;

		var primaryReturn = CalculateQuarterReturn(_primaryPrices);
		var benchmarkReturn = CalculateQuarterReturn(_benchmarkPrices);

		if (primaryReturn >= benchmarkReturn)
		{
			if (Position <= 0)
			{
				if (Position < 0)
					BuyMarket(Math.Abs(Position));

				BuyMarket();
			}
		}
		else if (Position >= 0)
		{
			if (Position > 0)
				SellMarket(Position);

			SellMarket();
		}
	}

	private static decimal CalculateQuarterReturn(RollingWin window)
	{
		var prices = window.Data;
		var start = prices[^21];
		var finish = prices[^1];
		return (finish - start) / Math.Max(start, 1m);
	}

	private sealed class RollingWin
	{
		private readonly int _capacity;
		private readonly Queue<decimal> _values = [];

		public RollingWin(int capacity)
		{
			_capacity = capacity;
		}

		public bool Full => _values.Count == _capacity;

		public decimal[] Data => [.. _values];

		public void Add(decimal value)
		{
			if (_values.Count == _capacity)
				_values.Dequeue();

			_values.Enqueue(value);
		}

		public void Clear()
		{
			_values.Clear();
		}

		public override bool Equals(object obj)
		{
			if (obj is not RollingWin other || other._values.Count != _values.Count)
				return false;

			var left = Data;
			var right = other.Data;
			for (var i = 0; i < left.Length; i++)
			{
				if (left[i] != right[i])
					return false;
			}

			return true;
		}

		public override int GetHashCode()
		{
			var hash = _capacity;
			foreach (var value in _values)
				hash = (hash * 397) ^ value.GetHashCode();

			return hash;
		}
	}
}
