using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi EMA crossover strategy.
/// Opens up to four long positions when faster EMAs cross above slower ones and closes each when the faster EMA falls below the slower EMA.
/// </summary>
public class MultiEmaCrossoverStrategy : Strategy
{
	private readonly StrategyParam<int> _ema1Length;
	private readonly StrategyParam<int> _ema3Length;
	private readonly StrategyParam<int> _ema5Length;
	private readonly StrategyParam<int> _ema10Length;
	private readonly StrategyParam<int> _ema20Length;
	private readonly StrategyParam<int> _ema40Length;
	private readonly StrategyParam<DataType> _candleType;
	
	/// <summary>
	/// EMA length for the 1-period EMA.
	/// </summary>
	public int Ema1Length
	{
		get => _ema1Length.Value;
		set => _ema1Length.Value = value;
	}
	
	/// <summary>
	/// EMA length for the 3-period EMA.
	/// </summary>
	public int Ema3Length
	{
		get => _ema3Length.Value;
		set => _ema3Length.Value = value;
	}
	
	/// <summary>
	/// EMA length for the 5-period EMA.
	/// </summary>
	public int Ema5Length
	{
		get => _ema5Length.Value;
		set => _ema5Length.Value = value;
	}
	
	/// <summary>
	/// EMA length for the 10-period EMA.
	/// </summary>
	public int Ema10Length
	{
		get => _ema10Length.Value;
		set => _ema10Length.Value = value;
	}
	
	/// <summary>
	/// EMA length for the 20-period EMA.
	/// </summary>
	public int Ema20Length
	{
		get => _ema20Length.Value;
		set => _ema20Length.Value = value;
	}
	
	/// <summary>
	/// EMA length for the 40-period EMA.
	/// </summary>
	public int Ema40Length
	{
		get => _ema40Length.Value;
		set => _ema40Length.Value = value;
	}
	
	/// <summary>
	/// Candle type to use.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Initializes a new instance of the <see cref="MultiEmaCrossoverStrategy"/> class.
	/// </summary>
	public MultiEmaCrossoverStrategy()
	{
		_ema1Length = Param(nameof(Ema1Length), 1)
		.SetGreaterThanZero()
		.SetDisplay("EMA1", "Length of the first EMA", "Parameters");
		
		_ema3Length = Param(nameof(Ema3Length), 3)
		.SetGreaterThanZero()
		.SetDisplay("EMA3", "Length of the third EMA", "Parameters");
		
		_ema5Length = Param(nameof(Ema5Length), 5)
		.SetGreaterThanZero()
		.SetDisplay("EMA5", "Length of the fifth EMA", "Parameters");
		
		_ema10Length = Param(nameof(Ema10Length), 10)
		.SetGreaterThanZero()
		.SetDisplay("EMA10", "Length of the tenth EMA", "Parameters");
		
		_ema20Length = Param(nameof(Ema20Length), 20)
		.SetGreaterThanZero()
		.SetDisplay("EMA20", "Length of the twentieth EMA", "Parameters");
		
		_ema40Length = Param(nameof(Ema40Length), 40)
		.SetGreaterThanZero()
		.SetDisplay("EMA40", "Length of the fortieth EMA", "Parameters");
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		
		var ema1 = new EMA { Length = Ema1Length };
		var ema3 = new EMA { Length = Ema3Length };
		var ema5 = new EMA { Length = Ema5Length };
		var ema10 = new EMA { Length = Ema10Length };
		var ema20 = new EMA { Length = Ema20Length };
		var ema40 = new EMA { Length = Ema40Length };
		
		var subscription = SubscribeCandles(CandleType);
		
		var initialized = false;
		var prev1 = 0m;
		var prev3 = 0m;
		var prev5 = 0m;
		var prev10 = 0m;
		var prev20 = 0m;
		var prev40 = 0m;
		var long15 = false;
		var long310 = false;
		var long520 = false;
		var long1040 = false;
		
		subscription
		.Bind(ema1, ema3, ema5, ema10, ema20, ema40, (candle, v1, v3, v5, v10, v20, v40) =>
		{
			if (candle.State != CandleStates.Finished)
			return;
			
			if (!IsFormedAndOnlineAndAllowTrading())
			return;
			
			if (!initialized)
			{
				if (ema1.IsFormed && ema3.IsFormed && ema5.IsFormed && ema10.IsFormed && ema20.IsFormed && ema40.IsFormed)
				{
					prev1 = v1;
					prev3 = v3;
					prev5 = v5;
					prev10 = v10;
					prev20 = v20;
					prev40 = v40;
					initialized = true;
				}
				
				return;
			}
			
			var cross15 = prev1 <= prev5 && v1 > v5;
			var cross310 = prev3 <= prev10 && v3 > v10;
			var cross520 = prev5 <= prev20 && v5 > v20;
			var cross1040 = prev10 <= prev40 && v10 > v40;
			
			var short15 = v1 < v5;
			var short310 = v3 < v10;
			var short520 = v5 < v20;
			var short1040 = v10 < v40;
			
			if (cross15 && !long15)
			{
				BuyMarket();
				long15 = true;
			}
			
			if (cross310 && !long310)
			{
				BuyMarket();
				long310 = true;
			}
			
			if (cross520 && !long520)
			{
				BuyMarket();
				long520 = true;
			}
			
			if (cross1040 && !long1040)
			{
				BuyMarket();
				long1040 = true;
			}
			
			if (short15 && long15)
			{
				SellMarket();
				long15 = false;
			}
			
			if (short310 && long310)
			{
				SellMarket();
				long310 = false;
			}
			
			if (short520 && long520)
			{
				SellMarket();
				long520 = false;
			}
			
			if (short1040 && long1040)
			{
				SellMarket();
				long1040 = false;
			}
			
			prev1 = v1;
			prev3 = v3;
			prev5 = v5;
			prev10 = v10;
			prev20 = v20;
			prev40 = v40;
			})
			.Start();
			
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, ema1);
				DrawIndicator(area, ema3);
				DrawIndicator(area, ema5);
				DrawIndicator(area, ema10);
				DrawIndicator(area, ema20);
				DrawIndicator(area, ema40);
				DrawOwnTrades(area);
			}
		}
	}
