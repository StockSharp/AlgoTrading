using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simple pair trading strategy.
/// </summary>
public class DoubleTradingStrategy : Strategy
{
	public enum TradeDirection { Auto, Buy, Sell }

	private readonly StrategyParam<decimal> _volume1;
	private readonly StrategyParam<decimal> _volume2;
	private readonly StrategyParam<decimal> _profitTarget;
	private readonly StrategyParam<Security> _secondSecurity;
	private readonly StrategyParam<TradeDirection> _direction1;
	private readonly StrategyParam<TradeDirection> _direction2;
	private readonly StrategyParam<DataType> _candleType;

	private Sides _side1;
	private Sides _side2;
	private decimal? _entry1;
	private decimal? _entry2;
	private decimal _last1;
	private decimal _last2;

	public decimal Volume1 { get => _volume1.Value; set => _volume1.Value = value; }
	public decimal Volume2 { get => _volume2.Value; set => _volume2.Value = value; }
	public decimal ProfitTarget { get => _profitTarget.Value; set => _profitTarget.Value = value; }
	public Security SecondSecurity { get => _secondSecurity.Value; set => _secondSecurity.Value = value; }
	public TradeDirection Direction1 { get => _direction1.Value; set => _direction1.Value = value; }
	public TradeDirection Direction2 { get => _direction2.Value; set => _direction2.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public DoubleTradingStrategy()
	{
		_volume1 = Param(nameof(Volume1), 1m).SetDisplay("Volume1", "First volume", "Parameters");
		_volume2 = Param(nameof(Volume2), 1.3m).SetDisplay("Volume2", "Second volume", "Parameters");
		_profitTarget = Param(nameof(ProfitTarget), 20m).SetDisplay("Profit Target", "Exit profit", "Risk");
		_secondSecurity = Param<Security>(nameof(SecondSecurity)).SetDisplay("Second Security", "Hedged instrument", "Parameters").SetRequired();
		_direction1 = Param(nameof(Direction1), TradeDirection.Auto).SetDisplay("Direction1", "First side", "Parameters");
		_direction2 = Param(nameof(Direction2), TradeDirection.Auto).SetDisplay("Direction2", "Second side", "Parameters");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame()).SetDisplay("Candle Type", "Candles", "Data");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (SecondSecurity, CandleType)];
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		SubscribeCandles(CandleType).Bind(ProcessFirst).Start();
		SubscribeCandles(CandleType, security: SecondSecurity).Bind(ProcessSecond).Start();

		_side1 = Direction1 == TradeDirection.Sell ? Sides.Sell : Sides.Buy;
		_side2 = Direction2 == TradeDirection.Buy ? Sides.Buy : Sides.Sell;

		if (_side1 == Sides.Buy)
			BuyMarket(Volume1);
		else
			SellMarket(Volume1);

		if (_side2 == Sides.Buy)
			BuyMarket(SecondSecurity, Volume2);
		else
			SellMarket(SecondSecurity, Volume2);
	}

	private void ProcessFirst(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_last1 = candle.ClosePrice;
		_entry1 ??= candle.ClosePrice;
		CheckExit();
	}

	private void ProcessSecond(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_last2 = candle.ClosePrice;
		_entry2 ??= candle.ClosePrice;
		CheckExit();
	}

	private void CheckExit()
	{
		if (_entry1 is null || _entry2 is null)
			return;

		var pnl1 = (_side1 == Sides.Buy ? _last1 - _entry1.Value : _entry1.Value - _last1) * Volume1;
		var pnl2 = (_side2 == Sides.Buy ? _last2 - _entry2.Value : _entry2.Value - _last2) * Volume2;

		if (pnl1 + pnl2 >= ProfitTarget)
			ExitPositions();
	}

	private void ExitPositions()
	{
		if (_side1 == Sides.Buy)
			SellMarket(Volume1);
		else
			BuyMarket(Volume1);

		if (_side2 == Sides.Buy)
			SellMarket(SecondSecurity, Volume2);
		else
			BuyMarket(SecondSecurity, Volume2);

		_entry1 = null;
		_entry2 = null;
	}
}
