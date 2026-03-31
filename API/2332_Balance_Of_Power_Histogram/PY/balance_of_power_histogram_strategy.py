import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BalanceOfPower
from StockSharp.Algo.Strategies import Strategy


class balance_of_power_histogram_strategy(Strategy):
    def __init__(self):
        super(balance_of_power_histogram_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._signal_level = self.Param("SignalLevel", 0.30) \
            .SetDisplay("Signal Level", "Minimum BOP value for confirmed reversals", "Signal")
        self._cooldown_candles = self.Param("CooldownCandles", 3) \
            .SetDisplay("Cooldown Candles", "Minimum finished candles between entries", "Signal")
        self._bars_since_signal = 0
        self._prev_bop = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def signal_level(self):
        return self._signal_level.Value

    @property
    def cooldown_candles(self):
        return self._cooldown_candles.Value

    def OnReseted(self):
        super(balance_of_power_histogram_strategy, self).OnReseted()
        self._bars_since_signal = int(self.cooldown_candles)
        self._prev_bop = None

    def OnStarted2(self, time):
        super(balance_of_power_histogram_strategy, self).OnStarted2(time)
        self._bars_since_signal = int(self.cooldown_candles)
        self._prev_bop = None
        bop = BalanceOfPower()
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(bop, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, bop):
        if candle.State != CandleStates.Finished:
            return
        bop = float(bop)
        prev_bop = self._prev_bop
        self._prev_bop = bop
        self._bars_since_signal += 1
        if prev_bop is None:
            return
        if self._bars_since_signal < int(self.cooldown_candles):
            return
        sl = float(self.signal_level)
        turned_up = prev_bop <= -sl and bop >= sl
        turned_down = prev_bop >= sl and bop <= -sl
        if turned_up and self.Position <= 0:
            self.BuyMarket()
            self._bars_since_signal = 0
        elif turned_down and self.Position >= 0:
            self.SellMarket()
            self._bars_since_signal = 0

    def CreateClone(self):
        return balance_of_power_histogram_strategy()
