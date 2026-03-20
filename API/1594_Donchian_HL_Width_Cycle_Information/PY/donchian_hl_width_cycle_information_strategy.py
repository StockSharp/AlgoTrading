import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy


class donchian_hl_width_cycle_information_strategy(Strategy):
    def __init__(self):
        super(donchian_hl_width_cycle_information_strategy, self).__init__()
        self._length = self.Param("Length", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Donchian Length", "Lookback for Donchian channel", "Donchian")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cycle_trend = 0

    @property
    def length(self):
        return self._length.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(donchian_hl_width_cycle_information_strategy, self).OnReseted()
        self._cycle_trend = 0

    def OnStarted(self, time):
        super(donchian_hl_width_cycle_information_strategy, self).OnStarted(time)
        highest = Highest()
        highest.Length = self.length
        lowest = Lowest()
        lowest.Length = self.length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(highest, lowest, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, upper, lower):
        if candle.State != CandleStates.Finished:
            return
        if upper == lower:
            return
        # Cycle detection: breakout above upper band or below lower band
        if candle.ClosePrice >= upper and self._cycle_trend != 1:
            self._cycle_trend = 1
            if self.Position == 0:
                self.BuyMarket()
        elif candle.ClosePrice <= lower and self._cycle_trend != -1:
            self._cycle_trend = -1
            if self.Position == 0:
                self.SellMarket()

    def CreateClone(self):
        return donchian_hl_width_cycle_information_strategy()
