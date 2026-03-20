import clr
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy

class up3x1_krohabor_shift_strategy(Strategy):
    def __init__(self):
        super(up3x1_krohabor_shift_strategy, self).__init__()
        self._channel_period = self.Param("ChannelPeriod", 20).SetDisplay("Channel Period", "Channel lookback", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))).SetDisplay("Candle Type", "Candle timeframe", "General")
        self._prev_close = 0.0
        self._prev_mid = 0.0
        self._has_prev = False
        self._cooldown = 0

    @property
    def channel_period(self): return self._channel_period.Value
    @property
    def candle_type(self): return self._candle_type.Value

    def OnReseted(self):
        super(up3x1_krohabor_shift_strategy, self).OnReseted()
        self._prev_close = 0.0; self._prev_mid = 0.0; self._has_prev = False; self._cooldown = 0

    def OnStarted(self, time):
        super(up3x1_krohabor_shift_strategy, self).OnStarted(time)
        self._has_prev = False; self._cooldown = 0
        highest = Highest(); highest.Length = self.channel_period
        lowest = Lowest(); lowest.Length = self.channel_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(highest, lowest, self.process_candle).Start()

    def process_candle(self, candle, highest, lowest):
        if candle.State != CandleStates.Finished: return
        if not self.IsFormedAndOnlineAndAllowTrading(): return
        close = float(candle.ClosePrice)
        mid = (float(highest) + float(lowest)) / 2.0
        if not self._has_prev:
            self._prev_close = close; self._prev_mid = mid; self._has_prev = True; return
        if self._cooldown > 0:
            self._cooldown -= 1; self._prev_close = close; self._prev_mid = mid; return
        if self._prev_close <= self._prev_mid and close > mid and self.Position <= 0:
            volume = self.Volume + abs(self.Position)
            self.BuyMarket(volume); self._cooldown = 6
        elif self._prev_close >= self._prev_mid and close < mid and self.Position >= 0:
            volume = self.Volume + abs(self.Position)
            self.SellMarket(volume); self._cooldown = 6
        self._prev_close = close; self._prev_mid = mid

    def CreateClone(self): return up3x1_krohabor_shift_strategy()
