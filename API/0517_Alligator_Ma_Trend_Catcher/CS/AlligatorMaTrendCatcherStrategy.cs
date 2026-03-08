using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Alligator + MA Trend Catcher strategy.
/// Buy when price is above the EMA trendline and Alligator lines are aligned up.
/// Sell short when price is below the trendline and Alligator lines are aligned down.
/// </summary>
public class AlligatorMaTrendCatcherStrategy : Strategy
{
	private readonly StrategyParam<int> _jawLength;
	private readonly StrategyParam<int> _teethLength;
	private readonly StrategyParam<int> _lipsLength;
	private readonly StrategyParam<int> _trendlineLength;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private int _cooldownRemaining;

	public int JawLength { get => _jawLength.Value; set => _jawLength.Value = value; }
	public int TeethLength { get => _teethLength.Value; set => _teethLength.Value = value; }
	public int LipsLength { get => _lipsLength.Value; set => _lipsLength.Value = value; }
	public int TrendlineLength { get => _trendlineLength.Value; set => _trendlineLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }

	public AlligatorMaTrendCatcherStrategy()
	{
		_jawLength = Param(nameof(JawLength), 13)
			.SetGreaterThanZero()
			.SetDisplay("Jaw Length", "Length of jaw SMA", "Alligator");

		_teethLength = Param(nameof(TeethLength), 8)
			.SetGreaterThanZero()
			.SetDisplay("Teeth Length", "Length of teeth SMA", "Alligator");

		_lipsLength = Param(nameof(LipsLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Lips Length", "Length of lips SMA", "Alligator");

		_trendlineLength = Param(nameof(TrendlineLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "Length of the EMA trendline", "Trendline");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_cooldownBars = Param(nameof(CooldownBars), 30)
			.SetDisplay("Cooldown Bars", "Bars between trades", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var jaw = new SmoothedMovingAverage { Length = JawLength };
		var teeth = new SmoothedMovingAverage { Length = TeethLength };
		var lips = new SmoothedMovingAverage { Length = LipsLength };
		var trend = new ExponentialMovingAverage { Length = TrendlineLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(jaw, teeth, lips, trend, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, trend);
			DrawIndicator(area, jaw);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal jawMa, decimal teethMa, decimal lipsMa, decimal trendline)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			return;
		}

		var alligatorUp = lipsMa > teethMa && teethMa > jawMa;
		var alligatorDown = lipsMa < teethMa && teethMa < jawMa;

		if (alligatorUp && candle.ClosePrice > trendline && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		else if (alligatorDown && candle.ClosePrice < trendline && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
	}
}
