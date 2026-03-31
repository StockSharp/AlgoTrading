import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class vidya_pro_trend_multi_tier_profit_strategy(Strategy):
    def __init__(self):
        super(vidya_pro_trend_multi_tier_profit_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 10) \
            .SetDisplay("Fast Length", "Fast adaptive MA period", "General")
        self._slow_length = self.Param("SlowLength", 30) \
            .SetDisplay("Slow Length", "Slow adaptive MA period", "General")
        self._tp1_pct = self.Param("Tp1Pct", 1.0) \
            .SetDisplay("TP1 %", "First take profit percent", "Risk")
        self._tp2_pct = self.Param("Tp2Pct", 5.0) \
            .SetDisplay("TP2 %", "Second take profit percent", "Risk")
        self._sl_pct = self.Param("SlPct", 3.0) \
            .SetDisplay("SL %", "Stop loss percent", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._entry_price = 0.0
        self._cooldown = 0

    @property
    def fast_length(self):
        return self._fast_length.Value

    @property
    def slow_length(self):
        return self._slow_length.Value

    @property
    def tp1_pct(self):
        return self._tp1_pct.Value

    @property
    def tp2_pct(self):
        return self._tp2_pct.Value

    @property
    def sl_pct(self):
        return self._sl_pct.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(vidya_pro_trend_multi_tier_profit_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._entry_price = 0.0
        self._cooldown = 0

    def OnStarted2(self, time):
        super(vidya_pro_trend_multi_tier_profit_strategy, self).OnStarted2(time)
        kama_fast = ExponentialMovingAverage()
        kama_fast.Length = self.fast_length
        kama_slow = ExponentialMovingAverage()
        kama_slow.Length = self.slow_length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(kama_fast, kama_slow, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, kama_fast)
            self.DrawIndicator(area, kama_slow)
            self.DrawOwnTrades(area)

    def on_process(self, candle, fast, slow):
        if candle.State != CandleStates.Finished:
            return
        fast = float(fast)
        slow = float(slow)
        close = float(candle.ClosePrice)
        if self._cooldown > 0:
            self._cooldown -= 1
        if self._prev_fast == 0:
            self._prev_fast = fast
            self._prev_slow = slow
            return
        # TP/SL management
        if self.Position > 0 and self._entry_price > 0:
            if close >= self._entry_price * (1 + float(self.tp2_pct) / 100) or \
               close <= self._entry_price * (1 - float(self.sl_pct) / 100):
                self.SellMarket()
                self._entry_price = 0
                self._cooldown = 60
                self._prev_fast = fast
                self._prev_slow = slow
                return
        elif self.Position < 0 and self._entry_price > 0:
            if close <= self._entry_price * (1 - float(self.tp2_pct) / 100) or \
               close >= self._entry_price * (1 + float(self.sl_pct) / 100):
                self.BuyMarket()
                self._entry_price = 0
                self._cooldown = 60
                self._prev_fast = fast
                self._prev_slow = slow
                return
        if self._cooldown > 0:
            self._prev_fast = fast
            self._prev_slow = slow
            return
        # Crossover entry
        long_cross = self._prev_fast <= self._prev_slow and fast > slow
        short_cross = self._prev_fast >= self._prev_slow and fast < slow
        if long_cross and self.Position <= 0:
            self.BuyMarket()
            self._entry_price = close
            self._cooldown = 60
        elif short_cross and self.Position >= 0:
            self.SellMarket()
            self._entry_price = close
            self._cooldown = 60
        self._prev_fast = fast
        self._prev_slow = slow

    def CreateClone(self):
        return vidya_pro_trend_multi_tier_profit_strategy()
