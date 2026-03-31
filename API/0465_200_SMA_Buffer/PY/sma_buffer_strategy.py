import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class sma_buffer_strategy(Strategy):
    """200 SMA Buffer Strategy."""

    def __init__(self):
        super(sma_buffer_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._sma_length = self.Param("SmaLength", 100) \
            .SetDisplay("SMA Length", "Period of the moving average", "Parameters")
        self._entry_percent = self.Param("EntryPercent", 2.0) \
            .SetDisplay("Entry %", "Percent above/below SMA to enter", "Parameters")
        self._exit_percent = self.Param("ExitPercent", 1.0) \
            .SetDisplay("Exit %", "Percent toward SMA to exit", "Parameters")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")

        self._sma = None
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(sma_buffer_strategy, self).OnReseted()
        self._sma = None
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(sma_buffer_strategy, self).OnStarted2(time)

        self._sma = SimpleMovingAverage()
        self._sma.Length = int(self._sma_length.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._sma, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._sma)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, sma_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._sma.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return

        price = float(candle.ClosePrice)
        sma_v = float(sma_value)
        entry_pct = float(self._entry_percent.Value)
        exit_pct = float(self._exit_percent.Value)
        cooldown = int(self._cooldown_bars.Value)

        upper_entry = sma_v * (1.0 + entry_pct / 100.0)
        lower_entry = sma_v * (1.0 - entry_pct / 100.0)
        upper_exit = sma_v * (1.0 + exit_pct / 100.0)
        lower_exit = sma_v * (1.0 - exit_pct / 100.0)

        if price > upper_entry and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif price < lower_entry and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            self.SellMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif self.Position > 0 and price < lower_exit:
            self.SellMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown
        elif self.Position < 0 and price > upper_exit:
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown

    def CreateClone(self):
        return sma_buffer_strategy()
