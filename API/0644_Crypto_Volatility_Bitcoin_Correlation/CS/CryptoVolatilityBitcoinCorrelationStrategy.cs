using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class CryptoVolatilityBitcoinCorrelationStrategy : Strategy
{
	private readonly StrategyParam<int> _vixFixLength;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<Security> _volatilitySecurity;

	private decimal _prevVixFix;
	private decimal _currentBvol;
	private decimal _prevBvol;

	public int VixFixLength { get => _vixFixLength.Value; set => _vixFixLength.Value = value; }
	public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public Security VolatilitySecurity { get => _volatilitySecurity.Value; set => _volatilitySecurity.Value = value; }

	public CryptoVolatilityBitcoinCorrelationStrategy()
	{
		_vixFixLength = Param(nameof(VixFixLength), 22)
		.SetDisplay("VIX Fix Length", "Length for VIX Fix calculation", "Parameters");

		_emaLength = Param(nameof(EmaLength), 50)
		.SetDisplay("EMA Length", "EMA period", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe for candles", "General");

		_volatilitySecurity = Param<Security>(nameof(VolatilitySecurity))
		.SetDisplay("Volatility Security", "Security for volatility index", "Data")
		.SetRequired();
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return
		[
		(Security, CandleType),
		(VolatilitySecurity, CandleType)
		];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevVixFix = 0m;
		_currentBvol = 0m;
		_prevBvol = 0m;
	}
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var highestClose = new Highest { Length = VixFixLength };
		var ema = new ExponentialMovingAverage { Length = EmaLength };

		var mainSubscription = SubscribeCandles(CandleType);
		mainSubscription
		.Bind(highestClose, ema, ProcessMainCandle)
		.Start();

		var bvolSubscription = SubscribeCandles(CandleType, security: VolatilitySecurity);
		bvolSubscription
		.Bind(ProcessVolatilityCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, mainSubscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessVolatilityCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (_currentBvol == 0m)
		{
			_currentBvol = candle.ClosePrice;
			return;
		}

		_prevBvol = _currentBvol;
		_currentBvol = candle.ClosePrice;
	}

	private void ProcessMainCandle(ICandleMessage candle, decimal highestClose, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (_prevBvol == 0m || highestClose == 0m)
		return;

		var vixFix = (highestClose - candle.LowPrice) / highestClose * 100m;

		if (_prevVixFix == 0m)
		{
			_prevVixFix = vixFix;
			return;
		}

		var longCondition = vixFix > _prevVixFix && _currentBvol > _prevBvol && candle.ClosePrice > emaValue;
		var exitCondition = candle.ClosePrice < emaValue;

		if (Position <= 0 && longCondition)
		{
			CancelActiveOrders();
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (Position > 0 && exitCondition)
		{
			SellMarket(Position);
		}

		_prevVixFix = vixFix;
	}
}
