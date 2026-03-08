using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Directional Movement Index crossover strategy.
/// Buys when +DI crosses above -DI, sells on opposite.
/// </summary>
public class AdxDmiStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleTypeParam;
	private readonly StrategyParam<int> _dmiPeriod;

	private decimal? _prevPlus;
	private decimal? _prevMinus;

	public DataType CandleType
	{
		get => _candleTypeParam.Value;
		set => _candleTypeParam.Value = value;
	}

	public int DmiPeriod
	{
		get => _dmiPeriod.Value;
		set => _dmiPeriod.Value = value;
	}

	public AdxDmiStrategy()
	{
		_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for strategy calculation", "General");

		_dmiPeriod = Param(nameof(DmiPeriod), 14)
			.SetDisplay("DMI Period", "Directional Movement Index period", "Indicators")
			.SetGreaterThanZero();
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevPlus = null;
		_prevMinus = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevPlus = null;
		_prevMinus = null;

		var dmi = new DirectionalIndex { Length = DmiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(dmi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, dmi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue dmiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (dmiValue is not IDirectionalIndexValue dmi)
			return;

		if (dmi.Plus is not decimal currentPlus || dmi.Minus is not decimal currentMinus)
			return;

		if (_prevPlus is null || _prevMinus is null)
		{
			_prevPlus = currentPlus;
			_prevMinus = currentMinus;
			return;
		}

		var buySignal = _prevMinus > _prevPlus && currentMinus <= currentPlus;
		var sellSignal = _prevPlus > _prevMinus && currentPlus <= currentMinus;

		if (buySignal && Position <= 0)
			BuyMarket();

		if (sellSignal && Position >= 0)
			SellMarket();

		_prevPlus = currentPlus;
		_prevMinus = currentMinus;
	}
}
