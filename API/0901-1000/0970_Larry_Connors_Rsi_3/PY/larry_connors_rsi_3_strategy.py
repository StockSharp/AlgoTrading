import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class larry_connors_rsi_3_strategy(Strategy):
    def __init__(self):
        super(larry_connors_rsi_3_strategy, self).__init__()
        self._rsi_period = self.Param("RsiPeriod", 2) \
            .SetDisplay("RSI Period", "Period for RSI calculation", "Indicators")
        self._sma_period = self.Param("SmaPeriod", 50) \
            .SetDisplay("SMA Period", "Period for trend SMA", "Indicators")
        self._oversold_level = self.Param("OversoldLevel", 50.0) \
            .SetDisplay("Oversold Level", "RSI oversold threshold", "Strategy")
        self._overbought_level = self.Param("OverboughtLevel", 70.0) \
            .SetDisplay("Overbought Level", "RSI exit threshold", "Strategy")
        self._max_entries = self.Param("MaxEntries", 45) \
            .SetDisplay("Max Entries", "Maximum entries per run", "Risk")
        self._cooldown_bars = self.Param("CooldownBars", 20) \
            .SetDisplay("Cooldown Bars", "Minimum bars between orders", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._entries_executed = 0
        self._bars_since_signal = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(larry_connors_rsi_3_strategy, self).OnReseted()
        self._entries_executed = 0
        self._bars_since_signal = 0

    def OnStarted2(self, time):
        super(larry_connors_rsi_3_strategy, self).OnStarted2(time)
        self._entries_executed = 0
        self._bars_since_signal = self._cooldown_bars.Value
        sma = SimpleMovingAverage()
        sma.Length = self._sma_period.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, rsi, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawIndicator(area, rsi)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, sma_val, rsi_val):
        if candle.State != CandleStates.Finished:
            return
        self._bars_since_signal += 1
        rv = float(rsi_val)
        oversold = float(self._oversold_level.Value)
        overbought = float(self._overbought_level.Value)
        cond = rv > 0.0 and rv < oversold
        if cond and self.Position <= 0 and self._entries_executed < self._max_entries.Value and self._bars_since_signal >= self._cooldown_bars.Value:
            self.BuyMarket()
            self._entries_executed += 1
            self._bars_since_signal = 0
        elif rv > overbought and self.Position > 0:
            self.SellMarket()
            self._bars_since_signal = 0

    def CreateClone(self):
        return larry_connors_rsi_3_strategy()
