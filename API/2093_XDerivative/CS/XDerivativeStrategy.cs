using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// XDerivative strategy based on smoothed rate of change.
/// Enters long when the smoothed derivative forms a trough and short when it forms a peak.
/// </summary>
public class XDerivativeStrategy : Strategy
{
	private readonly StrategyParam<int> _rocPeriod;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<decimal> _takeProfitPct;
	private readonly StrategyParam<decimal> _stopLossPct;
	private readonly StrategyParam<DataType> _candleType;

	private JurikMovingAverage _jma;
	private decimal? _prevValue;
	private decimal? _prevPrevValue;

	public int RocPeriod
	{
		get => _rocPeriod.Value;
		set => _rocPeriod.Value = value;
	}

	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	public decimal TakeProfitPct
	{
		get => _takeProfitPct.Value;
		set => _takeProfitPct.Value = value;
	}

	public decimal StopLossPct
	{
		get => _stopLossPct.Value;
		set => _stopLossPct.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public XDerivativeStrategy()
	{
		_rocPeriod = Param(nameof(RocPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ROC Period", "Period for rate of change", "Parameters");

		_maLength = Param(nameof(MaLength), 7)
			.SetGreaterThanZero()
			.SetDisplay("JMA Length", "Period for Jurik MA smoothing", "Parameters");

		_takeProfitPct = Param(nameof(TakeProfitPct), 3m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit %", "Take profit percentage", "Risk");

		_stopLossPct = Param(nameof(StopLossPct), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "Parameters");
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
		_prevValue = null;
		_prevPrevValue = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_jma = new JurikMovingAverage { Length = MaLength };
		var roc = new RateOfChange { Length = RocPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(roc, (candle, rocValue) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				var jmaResult = _jma.Process(rocValue, candle.OpenTime, true);
				if (!jmaResult.IsFormed)
					return;

				var value = jmaResult.ToDecimal();

				if (_prevValue is decimal prev && _prevPrevValue is decimal prev2)
				{
					var turnUp = prev < prev2 && value > prev;
					var turnDown = prev > prev2 && value < prev;

					if (turnUp && Position <= 0)
					{
						if (Position < 0) BuyMarket();
						BuyMarket();
					}
					else if (turnDown && Position >= 0)
					{
						if (Position > 0) SellMarket();
						SellMarket();
					}
				}

				_prevPrevValue = _prevValue;
				_prevValue = value;
			})
			.Start();

		StartProtection(
			takeProfit: new Unit(TakeProfitPct, UnitTypes.Percent),
			stopLoss: new Unit(StopLossPct, UnitTypes.Percent),
			useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, roc);
			DrawOwnTrades(area);
		}
	}
}
