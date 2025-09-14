namespace StockSharp.Samples.Strategies;


using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy based on the Loco indicator.
/// </summary>
public class LocoStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<LocoIndicator.AppliedPrice> _priceType;
	
	public LocoStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(8).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");
		
		_length = Param(nameof(Length), 1)
		.SetDisplay("Length", "Lookback length", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(1, 10, 1);
		
		_priceType = Param(nameof(PriceType), LocoIndicator.AppliedPrice.Close)
		.SetDisplay("Price Type", "Price used for calculations", "Indicator");
	}
	
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}
	
	public LocoIndicator.AppliedPrice PriceType
	{
		get => _priceType.Value;
		set => _priceType.Value = value;
	}
	
	private int _prevColor = -1;
	
	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}
	
	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		
		_prevColor = -1;
	}
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		var loco = new LocoIndicator
		{
			Length = Length,
			PriceType = PriceType,
		};
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(loco, ProcessCandle)
		.Start();
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal color)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		var current = (int)color;
		
		if (_prevColor == -1)
		{
			_prevColor = current;
			return;
		}
		
		if (current != _prevColor)
		{
			if (current == 1)
			{
				if (Position < 0)
				ClosePosition();
				
				if (Position <= 0)
				BuyMarket();
			}
			else
			{
				if (Position > 0)
				ClosePosition();
				
				if (Position >= 0)
				SellMarket();
			}
		}
		
		_prevColor = current;
	}
	
	private class LocoIndicator : Indicator<ICandleMessage>
	{
		public int Length { get; set; } = 1;
		public AppliedPrice PriceType { get; set; } = AppliedPrice.Close;
		
		private readonly Queue<decimal> _prices = new();
		private decimal? _prev;
		
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var candle = input.GetValue<ICandleMessage>();
			var price = GetPrice(candle);
			
			_prices.Enqueue(price);
			
			if (_prices.Count <= Length)
			{
				_prev = price;
				IsFormed = false;
				return new DecimalIndicatorValue(this, 0m, input.Time);
			}
			
			var series1 = _prices.Dequeue();
			var prev = _prev ?? price;
			decimal result;
			int color;
			
			if (price == prev)
			{
				result = prev;
				color = 0;
			}
			else if (series1 > prev && price > prev)
			{
				result = Math.Max(prev, price * 0.999m);
				color = 0;
			}
			else if (series1 < prev && price < prev)
			{
				result = Math.Min(prev, price * 1.001m);
				color = 1;
			}
			else
			{
				if (price > prev)
				{
					result = price * 0.999m;
					color = 0;
				}
				else
				{
					result = price * 1.001m;
					color = 1;
				}
			}
			
			_prev = result;
			IsFormed = true;
			return new DecimalIndicatorValue(this, color, input.Time);
		}
		
		public override void Reset()
		{
			base.Reset();
			_prices.Clear();
			_prev = null;
		}
		
		private decimal GetPrice(ICandleMessage candle)
		{
			var o = candle.OpenPrice;
			var h = candle.HighPrice;
			var l = candle.LowPrice;
			var c = candle.ClosePrice;
			
			return PriceType switch
			{
				AppliedPrice.Close => c,
				AppliedPrice.Open => o,
				AppliedPrice.High => h,
				AppliedPrice.Low => l,
				AppliedPrice.Median => (h + l) / 2m,
				AppliedPrice.Typical => (c + h + l) / 3m,
				AppliedPrice.Weighted => (2m * c + h + l) / 4m,
				AppliedPrice.Simple => (o + c) / 2m,
				AppliedPrice.Quarter => (o + c + h + l) / 4m,
				AppliedPrice.TrendFollow0 => c > o ? h : c < o ? l : c,
				AppliedPrice.TrendFollow1 => c > o ? (h + c) / 2m : c < o ? (l + c) / 2m : c,
				AppliedPrice.Demark =>
				{
					var res = h + l + c;
					if (c < o)
					res = (res + l) / 2m;
					else if (c > o)
					res = (res + h) / 2m;
					else
					res = (res + c) / 2m;
					return ((res - l) + (res - h)) / 2m;
				},
				_ => c
			};
		}
		
		public enum AppliedPrice
		{
			Close = 1,
			Open,
			High,
			Low,
			Median,
			Typical,
			Weighted,
			Simple,
			Quarter,
			TrendFollow0,
			TrendFollow1,
			Demark
		}
	}
}
