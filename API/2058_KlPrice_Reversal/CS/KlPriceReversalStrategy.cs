using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// KlPrice reversal strategy converted from the MQL i-KlPrice expert.
/// Calculates a normalized oscillator based on price and ATR.
/// </summary>
public class KlPriceReversalStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _priceMaLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _upLevel;
	private readonly StrategyParam<decimal> _downLevel;
	private readonly StrategyParam<bool> _enableBuy;
	private readonly StrategyParam<bool> _enableSell;

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Period for price smoothing.
	/// </summary>
	public int PriceMaLength
	{
		get => _priceMaLength.Value;
		set => _priceMaLength.Value = value;
	}

	/// <summary>
	/// Period for ATR calculation.
	/// </summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <summary>
	/// Upper threshold of the oscillator.
	/// </summary>
	public decimal UpLevel
	{
		get => _upLevel.Value;
		set => _upLevel.Value = value;
	}

	/// <summary>
	/// Lower threshold of the oscillator.
	/// </summary>
	public decimal DownLevel
	{
		get => _downLevel.Value;
		set => _downLevel.Value = value;
	}

	/// <summary>
	/// Enable opening long positions.
	/// </summary>
	public bool EnableBuy
	{
		get => _enableBuy.Value;
		set => _enableBuy.Value = value;
	}

	/// <summary>
	/// Enable opening short positions.
	/// </summary>
	public bool EnableSell
	{
		get => _enableSell.Value;
		set => _enableSell.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public KlPriceReversalStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for calculations", "General");

		_priceMaLength = Param(nameof(PriceMaLength), 100)
			.SetGreaterThanZero()
			.SetDisplay("Price MA Length", "SMA period for price smoothing", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(20, 200, 10);

		_atrLength = Param(nameof(AtrLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR period for range estimation", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(10, 100, 5);

		_upLevel = Param(nameof(UpLevel), 50m)
			.SetDisplay("Upper Level", "Upper threshold for signals", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(20, 100, 5);

		_downLevel = Param(nameof(DownLevel), -50m)
			.SetDisplay("Lower Level", "Lower threshold for signals", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(-100, -20, 5);

		_enableBuy = Param(nameof(EnableBuy), true)
			.SetDisplay("Enable Buy", "Allow opening long positions", "Parameters");

		_enableSell = Param(nameof(EnableSell), true)
			.SetDisplay("Enable Sell", "Allow opening short positions", "Parameters");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var priceMa = new SimpleMovingAverage
		{
			Length = PriceMaLength,
			CandlePrice = CandlePrice.Close
		};

		var atr = new AverageTrueRange
		{
			Length = AtrLength
		};

		var subscription = SubscribeCandles(CandleType);

		decimal prevColor = 2m;
		var isFirst = true;

		subscription
			.Bind(priceMa, atr, (candle, ma, tr) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!priceMa.IsFormed || !atr.IsFormed)
					return;

				if (tr == 0m)
					return;

				var dwband = ma - tr;
				var jres = 100m * (candle.ClosePrice - dwband) / (2m * tr) - 50m;

				var color = 2m;

				if (jres > UpLevel)
					color = 4m;
				else if (jres > 0m)
					color = 3m;

				if (jres < DownLevel)
					color = 0m;
				else if (jres < 0m)
					color = 1m;

				if (!isFirst)
				{
					if (EnableBuy && prevColor == 4m && color < 4m && Position <= 0)
						BuyMarket(Volume + Math.Abs(Position));

					if (EnableSell && prevColor == 0m && color > 0m && Position >= 0)
						SellMarket(Volume + Math.Abs(Position));

					if (prevColor < 2m && Position > 0)
						SellMarket(Position);

					if (prevColor > 2m && Position < 0)
						BuyMarket(-Position);
				}

				prevColor = color;
				isFirst = false;
			})
			.Start();

		StartProtection();
	}
}
