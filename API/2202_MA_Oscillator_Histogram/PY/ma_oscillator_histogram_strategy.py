import clr
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class ma_oscillator_histogram_strategy(Strategy):
    def __init__(self):
        super(ma_oscillator_histogram_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 13).SetDisplay("Fast Period", "Period of fast moving average", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 24).SetDisplay("Slow Period", "Period of slow moving average", "Indicators")
        self._enable_buy_open = self.Param("EnableBuyOpen", True).SetDisplay("Enable Buy Open", "Allow opening long positions", "Signals")
        self._enable_sell_open = self.Param("EnableSellOpen", True).SetDisplay("Enable Sell Open", "Allow opening short positions", "Signals")
        self._enable_buy_close = self.Param("EnableBuyClose", True).SetDisplay("Enable Buy Close", "Allow closing long positions", "Signals")
        self._enable_sell_close = self.Param("EnableSellClose", True).SetDisplay("Enable Sell Close", "Allow closing short positions", "Signals")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._prev_osc1 = 0.0
        self._prev_osc2 = 0.0
        self._is_warmup = True
    @property
    def fast_period(self): return self._fast_period.Value
    @property
    def slow_period(self): return self._slow_period.Value
    @property
    def enable_buy_open(self): return self._enable_buy_open.Value
    @property
    def enable_sell_open(self): return self._enable_sell_open.Value
    @property
    def enable_buy_close(self): return self._enable_buy_close.Value
    @property
    def enable_sell_close(self): return self._enable_sell_close.Value
    @property
    def candle_type(self): return self._candle_type.Value
    def OnReseted(self):
        super(ma_oscillator_histogram_strategy, self).OnReseted()
        self._prev_osc1 = 0.0
        self._prev_osc2 = 0.0
        self._is_warmup = True
    def OnStarted(self, time):
        super(ma_oscillator_histogram_strategy, self).OnStarted(time)
        fast_ma = ExponentialMovingAverage()
        fast_ma.Length = self.fast_period
        slow_ma = ExponentialMovingAverage()
        slow_ma.Length = self.slow_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_ma, slow_ma, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ma)
            self.DrawIndicator(area, slow_ma)
            self.DrawOwnTrades(area)
    def process_candle(self, candle, fast_value, slow_value):
        if candle.State != CandleStates.Finished: return
        if not self.IsFormedAndOnlineAndAllowTrading(): return
        osc = float(fast_value) - float(slow_value)
        if self._is_warmup:
            self._prev_osc1 = osc
            self._prev_osc2 = osc
            self._is_warmup = False
            return
        buy_signal = self._prev_osc2 > self._prev_osc1 and self._prev_osc1 < osc
        sell_signal = self._prev_osc2 < self._prev_osc1 and self._prev_osc1 > osc
        if buy_signal:
            if self.enable_sell_close and self.Position < 0:
                self.BuyMarket()
            if self.enable_buy_open and self.Position <= 0:
                self.BuyMarket()
        elif sell_signal:
            if self.enable_buy_close and self.Position > 0:
                self.SellMarket()
            if self.enable_sell_open and self.Position >= 0:
                self.SellMarket()
        self._prev_osc2 = self._prev_osc1
        self._prev_osc1 = osc
    def CreateClone(self): return ma_oscillator_histogram_strategy()
