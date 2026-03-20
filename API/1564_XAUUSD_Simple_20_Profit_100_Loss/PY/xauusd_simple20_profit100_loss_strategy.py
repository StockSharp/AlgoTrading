import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class xauusd_simple20_profit100_loss_strategy(Strategy):
    def __init__(self):
        super(xauusd_simple20_profit100_loss_strategy, self).__init__()
        self._tp_pct = self.Param("TpPct", 0.3) \
            .SetDisplay("TP %", "Take profit percent", "Risk")
        self._sl_pct = self.Param("SlPct", 1.5) \
            .SetDisplay("SL %", "Stop loss percent", "Risk")
        self._cooldown_bars = self.Param("CooldownBars", TimeSpan.FromHours(1)) \
            .SetDisplay("Cooldown", "Bars between trades", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._bars_since_exit = 0
        self._entry_price = 0.0

    @property
    def tp_pct(self):
        return self._tp_pct.Value

    @property
    def sl_pct(self):
        return self._sl_pct.Value

    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(xauusd_simple20_profit100_loss_strategy, self).OnReseted()
        self._bars_since_exit = 0
        self._entry_price = 0.0

    def OnStarted(self, time):
        super(xauusd_simple20_profit100_loss_strategy, self).OnStarted(time)
        sma = SimpleMovingAverage()
        sma.Length = 10
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, _dummy):
        if candle.State != CandleStates.Finished:
            return
        self._bars_since_exit += 1
        if self.Position == 0 and self._bars_since_exit >= self.cooldown_bars:
            self.BuyMarket()
            self._entry_price = candle.ClosePrice
            return
        if self.Position > 0 and self._entry_price > 0:
            tp = self._entry_price * (1 + self.tp_pct / 100)
            sl = self._entry_price * (1 - self.sl_pct / 100)
            if candle.ClosePrice >= tp or candle.ClosePrice <= sl:
                self.SellMarket()
                self._bars_since_exit = 0
                self._entry_price = 0

    def CreateClone(self):
        return xauusd_simple20_profit100_loss_strategy()
