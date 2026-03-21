import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class smoothing_average_strategy(Strategy):

    def __init__(self):
        super(smoothing_average_strategy, self).__init__()

        self._ma_period = self.Param("MaPeriod", 200) \
            .SetDisplay("MA Period", "Moving average period", "MA")
        self._smoothing = self.Param("Smoothing", 1400.0) \
            .SetDisplay("Smoothing", "Price offset from moving average", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 36) \
            .SetDisplay("Cooldown Bars", "Bars to wait between new signals", "General")

        self._cooldown = 0

    @property
    def MaPeriod(self):
        return self._ma_period.Value

    @MaPeriod.setter
    def MaPeriod(self, value):
        self._ma_period.Value = value

    @property
    def Smoothing(self):
        return self._smoothing.Value

    @Smoothing.setter
    def Smoothing(self, value):
        self._smoothing.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def CooldownBars(self):
        return self._cooldown_bars.Value

    @CooldownBars.setter
    def CooldownBars(self, value):
        self._cooldown_bars.Value = value

    def OnStarted(self, time):
        super(smoothing_average_strategy, self).OnStarted(time)

        sma = SimpleMovingAverage()
        sma.Length = self.MaPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription \
            .Bind(sma, self.ProcessCandle) \
            .Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, ma_value):
        if candle.State != CandleStates.Finished:
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        price = float(candle.ClosePrice)
        ma_val = float(ma_value)
        step_raw = self.Security.PriceStep
        step = float(step_raw) if step_raw is not None else 1.0
        offset = step * float(self.Smoothing)

        if self.Position == 0:
            if price >= ma_val + offset:
                self.SellMarket()
                self._cooldown = self.CooldownBars
            elif price <= ma_val - offset:
                self.BuyMarket()
                self._cooldown = self.CooldownBars
        elif self.Position < 0 and price <= ma_val:
            self.BuyMarket()
            self._cooldown = self.CooldownBars
        elif self.Position > 0 and price >= ma_val:
            self.SellMarket()
            self._cooldown = self.CooldownBars

    def OnReseted(self):
        super(smoothing_average_strategy, self).OnReseted()
        self._cooldown = 0

    def CreateClone(self):
        return smoothing_average_strategy()
