using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades on the crossing of two rate of change lines.
/// </summary>
public class Roc2VgStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rocPeriod1;
	private readonly StrategyParam<RocType> _rocType1;
	private readonly StrategyParam<int> _rocPeriod2;
	private readonly StrategyParam<RocType> _rocType2;
	private readonly StrategyParam<bool> _invert;

	private decimal? _prevUp;
	private decimal? _prevDn;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int RocPeriod1 { get => _rocPeriod1.Value; set => _rocPeriod1.Value = value; }
	public RocType RocType1 { get => _rocType1.Value; set => _rocType1.Value = value; }
	public int RocPeriod2 { get => _rocPeriod2.Value; set => _rocPeriod2.Value = value; }
	public RocType RocType2 { get => _rocType2.Value; set => _rocType2.Value = value; }
	public bool Invert { get => _invert.Value; set => _invert.Value = value; }

	public Roc2VgStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candles", "General");
		_rocPeriod1 = Param(nameof(RocPeriod1), 8).SetGreaterThanZero()
			.SetDisplay("ROC Period 1", "Length of first ROC", "Indicator");
		_rocType1 = Param(nameof(RocType1), RocType.Momentum)
			.SetDisplay("ROC Type 1", "Type of first ROC", "Indicator");
		_rocPeriod2 = Param(nameof(RocPeriod2), 14).SetGreaterThanZero()
			.SetDisplay("ROC Period 2", "Length of second ROC", "Indicator");
		_rocType2 = Param(nameof(RocType2), RocType.Momentum)
			.SetDisplay("ROC Type 2", "Type of second ROC", "Indicator");
		_invert = Param(nameof(Invert), false).SetDisplay("Invert", "Swap ROC lines", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var ind1 = CreateIndicator(RocType1, RocPeriod1);
		var ind2 = CreateIndicator(RocType2, RocPeriod2);

		var sub = SubscribeCandles(CandleType);
		sub.Bind(ind1, ind2, Process).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, sub);
			DrawIndicator(area, ind1);
			DrawIndicator(area, ind2);
			DrawOwnTrades(area);
		}
	}

	private static IIndicator CreateIndicator(RocType type, int period)
	{
		return type == RocType.Momentum
			? new Momentum { Length = period }
			: new ROC { Length = period };
	}

	private decimal Transform(RocType type, decimal value)
	{
		return type switch
		{
			RocType.Momentum => value,
			RocType.Roc => value * 100m,
			RocType.RocP => value,
			RocType.RocR => value + 1m,
			RocType.RocR100 => (value + 1m) * 100m,
			_ => value
		};
	}

	private void Process(ICandleMessage candle, decimal v1, decimal v2)
	{
		if (candle.State != CandleStates.Finished || !IsFormedAndOnlineAndAllowTrading())
			return;

		var up = Transform(RocType1, Invert ? v2 : v1);
		var dn = Transform(RocType2, Invert ? v1 : v2);

		if (_prevUp is decimal pUp && _prevDn is decimal pDn)
		{
			if (pUp > pDn && up <= dn && Position <= 0)
			{
				var vol = Position < 0 ? 2m : 1m;
				BuyMarket(volume: vol);
			}
			else if (pUp < pDn && up >= dn && Position >= 0)
			{
				var vol = Position > 0 ? 2m : 1m;
				SellMarket(volume: vol);
			}
		}

		_prevUp = up;
		_prevDn = dn;
	}
}

/// <summary>
/// Types of rate of change calculation.
/// </summary>
public enum RocType
{
	/// <summary>Price - previous price.</summary>
	Momentum,

	/// <summary>((price / previous) - 1) * 100.</summary>
	Roc,

	/// <summary>(price - previous) / previous.</summary>
	RocP,

	/// <summary>price / previous.</summary>
	RocR,

	/// <summary>(price / previous) * 100.</summary>
	RocR100
}
