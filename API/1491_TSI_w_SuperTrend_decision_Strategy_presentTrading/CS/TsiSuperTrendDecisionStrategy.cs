namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Correlation-based TSI with SuperTrend direction.
/// </summary>
public class TsiSuperTrendDecisionStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _tsiLength;
	private readonly StrategyParam<int> _stLength;
	private readonly StrategyParam<decimal> _stMultiplier;
	private readonly StrategyParam<decimal> _threshold;
	private readonly StrategyParam<TradeDirection> _direction;
	private readonly StrategyParam<ProtectionType> _tpsl;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;

	private SuperTrend _superTrend;
	private decimal[] _prices;
	private int _index;

	public enum ProtectionType
	{
		None,
		TP,
		SL,
		Both
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int TsiLength
	{
		get => _tsiLength.Value;
		set
		{
			_tsiLength.Value = value;
			_prices = new decimal[value];
		}
	}

	public int StLength { get => _stLength.Value; set => _stLength.Value = value; }
	public decimal StMultiplier { get => _stMultiplier.Value; set => _stMultiplier.Value = value; }
	public decimal Threshold { get => _threshold.Value; set => _threshold.Value = value; }
	public TradeDirection Direction { get => _direction.Value; set => _direction.Value = value; }
	public ProtectionType Tpsl { get => _tpsl.Value; set => _tpsl.Value = value; }
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }

	public TsiSuperTrendDecisionStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");
		_tsiLength = Param(nameof(TsiLength), 64)
		.SetDisplay("TSI Length", "Correlation period", "Indicators");
		_stLength = Param(nameof(StLength), 10)
		.SetDisplay("ST Length", "SuperTrend length", "Indicators");
		_stMultiplier = Param(nameof(StMultiplier), 3m)
		.SetDisplay("ST Mult", "SuperTrend factor", "Indicators");
		_threshold = Param(nameof(Threshold), 0.241m)
		.SetDisplay("TSI Threshold", "Entry threshold", "Trading");
		_direction = Param(nameof(Direction), TradeDirection.Both)
		.SetDisplay("Direction", "Trade direction", "Trading");
		_tpsl = Param(nameof(Tpsl), ProtectionType.None)
		.SetDisplay("TPSL", "Protection type", "Risk");
		_takeProfit = Param(nameof(TakeProfit), 30m)
		.SetDisplay("Take Profit %", "Take profit", "Risk");
		_stopLoss = Param(nameof(StopLoss), 20m)
		.SetDisplay("Stop Loss %", "Stop loss", "Risk");

		_prices = new decimal[TsiLength];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		switch (Tpsl)
		{
			case ProtectionType.TP:
				StartProtection(new Unit(TakeProfit, UnitTypes.Percent), new Unit(0));
				break;
			case ProtectionType.SL:
				StartProtection(new Unit(0), new Unit(StopLoss, UnitTypes.Percent));
				break;
			case ProtectionType.Both:
				StartProtection(new Unit(TakeProfit, UnitTypes.Percent), new Unit(StopLoss, UnitTypes.Percent));
				break;
			default:
				StartProtection();
				break;
		}

		_superTrend = new SuperTrend { Length = StLength, Multiplier = StMultiplier };

		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(_superTrend, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _superTrend);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var st = (SuperTrendIndicatorValue)stValue;
		var isUp = st.IsUpTrend;

		_prices[_index % TsiLength] = candle.ClosePrice;
		_index++;

		if (_index < TsiLength)
			return;

		var tsi = CalculateCorrelation();

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if ((Direction == TradeDirection.Both || Direction == TradeDirection.Long) && isUp && tsi > -Threshold && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if ((Direction == TradeDirection.Both || Direction == TradeDirection.Short) && !isUp && tsi < Threshold && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));

		if (Position > 0 && (!isUp || tsi < Threshold))
			SellMarket(Math.Abs(Position));
		else if (Position < 0 && (isUp || tsi > -Threshold))
			BuyMarket(Math.Abs(Position));
	}

	private decimal CalculateCorrelation()
	{
		var n = TsiLength;
		decimal sumX = 0m, sumY = 0m, sumX2 = 0m, sumY2 = 0m, sumXY = 0m;
		for (var i = 0; i < n; i++)
		{
			var x = _prices[(_index - n + i) % n];
			var y = i;
			sumX += x;
			sumY += y;
			sumX2 += x * x;
			sumY2 += y * y;
			sumXY += x * y;
		}

		var num = n * sumXY - sumX * sumY;
		var den = Math.Sqrt((double)(n * sumX2 - sumX * sumX) * (double)(n * sumY2 - sumY * sumY));
		if (den == 0.0)
			return 0m;
		return (decimal)(num / (decimal)den);
	}
}