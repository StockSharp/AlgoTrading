
namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy based on the Range Expansion Index (REI).
/// Opens long positions when REI crosses above the down level (-60)
/// and short positions when it crosses below the up level (+60).
/// </summary>
public class RangeExpansionIndexStrategy : Strategy
{
	private readonly StrategyParam<int> _reiPeriod;
	private readonly StrategyParam<decimal> _upLevel;
	private readonly StrategyParam<decimal> _downLevel;
	private readonly StrategyParam<DataType> _candleType;

	private RangeExpansionIndex _rei;
	private decimal? _prevRei;

	/// <summary>
	/// REI calculation period.
	/// </summary>
	public int ReiPeriod
	{
		get => _reiPeriod.Value;
		set => _reiPeriod.Value = value;
	}

	/// <summary>
	/// Upper indicator level.
	/// </summary>
	public decimal UpLevel
	{
		get => _upLevel.Value;
		set => _upLevel.Value = value;
	}

	/// <summary>
	/// Lower indicator level.
	/// </summary>
	public decimal DownLevel
	{
		get => _downLevel.Value;
		set => _downLevel.Value = value;
	}

	/// <summary>
	/// Candle type used for analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="RangeExpansionIndexStrategy"/>.
	/// </summary>
	public RangeExpansionIndexStrategy()
	{
		_reiPeriod = Param(nameof(ReiPeriod), 8)
			.SetGreaterThanZero()
			.SetDisplay("REI Period", "Length of REI indicator", "Parameters")
			.SetCanOptimize(true);

		_upLevel = Param(nameof(UpLevel), 60m)
			.SetDisplay("Up Level", "Upper threshold", "Parameters")
			.SetCanOptimize(true);

		_downLevel = Param(nameof(DownLevel), -60m)
			.SetDisplay("Down Level", "Lower threshold", "Parameters")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(8).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
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
		_rei?.Reset();
		_prevRei = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_rei = new RangeExpansionIndex { Length = ReiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rei, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _rei);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal reiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_rei.IsFormed)
		{
			_prevRei = reiValue;
			return;
		}

		if (_prevRei is decimal prev)
		{
			if (prev < DownLevel && reiValue >= DownLevel && Position <= 0)
				BuyMarket();
			else if (prev > UpLevel && reiValue <= UpLevel && Position >= 0)
				SellMarket();
		}

		_prevRei = reiValue;
	}

	private class RangeExpansionIndex : Indicator<ICandleMessage>
	{
		public int Length { get; set; } = 8;

		private readonly List<ICandleMessage> _buffer = new();

		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var candle = input.GetValue<ICandleMessage>();
			_buffer.Add(candle);

			var need = Length + 8;
			if (_buffer.Count > need)
				_buffer.RemoveAt(0);

			if (_buffer.Count < need)
			{
				IsFormed = false;
				return new DecimalIndicatorValue(this, 0m, input.Time);
			}

			var last = _buffer.Count - 1;
			decimal subSum = 0m;
			decimal absSum = 0m;

			for (var i = last; i > last - Length; i--)
			{
				var hi = _buffer[i].HighPrice;
				var hi2 = _buffer[i - 2].HighPrice;
				var lo = _buffer[i].LowPrice;
				var lo2 = _buffer[i - 2].LowPrice;

				var diff1 = hi - hi2;
				var diff2 = lo - lo2;

				var num1 = (_buffer[i - 2].HighPrice < _buffer[i - 7].ClosePrice &&
					_buffer[i - 2].HighPrice < _buffer[i - 8].ClosePrice &&
					hi < _buffer[i - 5].HighPrice &&
					hi < _buffer[i - 6].HighPrice) ? 0m : 1m;

				var num2 = (_buffer[i - 2].LowPrice > _buffer[i - 7].ClosePrice &&
					_buffer[i - 2].LowPrice > _buffer[i - 8].ClosePrice &&
					lo > _buffer[i - 5].LowPrice &&
					lo > _buffer[i - 6].LowPrice) ? 0m : 1m;

				subSum += num1 * num2 * (diff1 + diff2);
				absSum += Math.Abs(diff1) + Math.Abs(diff2);
			}

			var rei = absSum == 0m ? 0m : subSum / absSum * 100m;
			IsFormed = true;
			return new DecimalIndicatorValue(this, rei, input.Time);
		}

		public override void Reset()
		{
			base.Reset();
			_buffer.Clear();
		}
	}
}
