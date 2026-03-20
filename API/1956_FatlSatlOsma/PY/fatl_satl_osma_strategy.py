import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy


class fatl_satl_osma_strategy(Strategy):

    def __init__(self):
        super(fatl_satl_osma_strategy, self).__init__()

        self._fast = self.Param("Fast", 39) \
            .SetDisplay("Fast", "Fast MA period", "Indicator")
        self._slow = self.Param("Slow", 65) \
            .SetDisplay("Slow", "Slow MA period", "Indicator")
        self._buy_open = self.Param("BuyOpen", True) \
            .SetDisplay("Buy Open", "Allow opening long positions", "Trading")
        self._sell_open = self.Param("SellOpen", True) \
            .SetDisplay("Sell Open", "Allow opening short positions", "Trading")
        self._buy_close = self.Param("BuyClose", True) \
            .SetDisplay("Buy Close", "Allow closing long positions", "Trading")
        self._sell_close = self.Param("SellClose", True) \
            .SetDisplay("Sell Close", "Allow closing short positions", "Trading")
        self._cooldown_bars = self.Param("CooldownBars", 1) \
            .SetDisplay("Cooldown Bars", "Bars to wait after a completed trade", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

        self._prev1 = 0.0
        self._prev2 = 0.0
        self._init = False
        self._bars_since_trade = 0

    @property
    def Fast(self):
        return self._fast.Value

    @Fast.setter
    def Fast(self, value):
        self._fast.Value = value

    @property
    def Slow(self):
        return self._slow.Value

    @Slow.setter
    def Slow(self, value):
        self._slow.Value = value

    @property
    def BuyOpen(self):
        return self._buy_open.Value

    @BuyOpen.setter
    def BuyOpen(self, value):
        self._buy_open.Value = value

    @property
    def SellOpen(self):
        return self._sell_open.Value

    @SellOpen.setter
    def SellOpen(self, value):
        self._sell_open.Value = value

    @property
    def BuyClose(self):
        return self._buy_close.Value

    @BuyClose.setter
    def BuyClose(self, value):
        self._buy_close.Value = value

    @property
    def SellClose(self):
        return self._sell_close.Value

    @SellClose.setter
    def SellClose(self, value):
        self._sell_close.Value = value

    @property
    def CooldownBars(self):
        return self._cooldown_bars.Value

    @CooldownBars.setter
    def CooldownBars(self, value):
        self._cooldown_bars.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(fatl_satl_osma_strategy, self).OnStarted(time)

        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self.Fast
        macd.Macd.LongMa.Length = self.Slow
        macd.SignalMa.Length = 9

        subscription = self.SubscribeCandles(self.CandleType)
        subscription \
            .BindEx(macd, self.ProcessCandle) \
            .Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, macd)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, macd_val):
        if candle.State != CandleStates.Finished:
            return

        macd_raw = macd_val.Macd
        if macd_raw is None:
            return

        val = float(macd_raw)

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._bars_since_trade < self.CooldownBars:
            self._bars_since_trade += 1

        if not self._init:
            self._prev1 = val
            self._prev2 = val
            self._init = True
            return

        if self._prev1 < self._prev2:
            if (self._bars_since_trade >= self.CooldownBars
                    and self.BuyOpen and val > self._prev1
                    and self.Position <= 0):
                self.BuyMarket(self.Volume + abs(self.Position))
                self._bars_since_trade = 0
        elif self._prev1 > self._prev2:
            if (self._bars_since_trade >= self.CooldownBars
                    and self.SellOpen and val < self._prev1
                    and self.Position >= 0):
                self.SellMarket(self.Volume + abs(self.Position))
                self._bars_since_trade = 0

        self._prev2 = self._prev1
        self._prev1 = val

    def OnReseted(self):
        super(fatl_satl_osma_strategy, self).OnReseted()
        self._prev1 = 0.0
        self._prev2 = 0.0
        self._init = False
        self._bars_since_trade = self.CooldownBars

    def CreateClone(self):
        return fatl_satl_osma_strategy()
