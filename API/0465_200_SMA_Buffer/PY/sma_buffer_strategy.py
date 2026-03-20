import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class sma_buffer_strategy(Strategy):
    def __init__(self):
        super(sma_buffer_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._sma_length = self.Param("SmaLength", 100) \
            .SetGreaterThanZero() \
            .SetDisplay("SMA Length", "Period of the moving average", "Parameters")
        self._entry_percent = self.Param("EntryPercent", 2.0) \
            .SetDisplay("Entry %", "Percent above/below SMA to enter", "Parameters")
        self._exit_percent = self.Param("ExitPercent", 1.0) \
            .SetDisplay("Exit %", "Percent toward SMA to exit", "Parameters")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value
    @property
    def sma_length(self):
        return self._sma_length.Value
    @property
    def entry_percent(self):
        return self._entry_percent.Value
    @property
    def exit_percent(self):
        return self._exit_percent.Value
    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    def OnReseted(self):
        super(sma_buffer_strategy, self).OnReseted()
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(sma_buffer_strategy, self).OnStarted(time)
        self._sma = SimpleMovingAverage()
        self._sma.Length = self.sma_length

        subscription = self.SubscribeCandles(self.candle_type)
        subscription \
            .Bind(self._sma, self.OnProcess) \
            .Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._sma)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, sma_value):
        if candle.State != CandleStates.Finished:
            return
        if not self._sma.IsFormed:
            return
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return

        price = float(candle.ClosePrice)
        sma_v = float(sma_value)
        upper_entry = sma_v * (1.0 + float(self.entry_percent) / 100.0)
        lower_entry = sma_v * (1.0 - float(self.entry_percent) / 100.0)
        upper_exit = sma_v * (1.0 + float(self.exit_percent) / 100.0)
        lower_exit = sma_v * (1.0 - float(self.exit_percent) / 100.0)

        if price > upper_entry and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(abs(self.Position))
            self.BuyMarket()
            self._cooldown_remaining = self.cooldown_bars
        elif price < lower_entry and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(abs(self.Position))
            self.SellMarket()
            self._cooldown_remaining = self.cooldown_bars
        elif self.Position > 0 and price < lower_exit:
            self.SellMarket(abs(self.Position))
            self._cooldown_remaining = self.cooldown_bars
        elif self.Position < 0 and price > upper_exit:
            self.BuyMarket(abs(self.Position))
            self._cooldown_remaining = self.cooldown_bars

    def CreateClone(self):
        return sma_buffer_strategy()
