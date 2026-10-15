import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class bnb_strategy(Strategy):
    def __init__(self):
        super(bnb_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Candles used for calculations", "General")
        self._length = self.Param("Length", 14) \
            .SetDisplay("EMA Length", "Length of smoothing for bulls and bears", "Parameters")
        self._min_net_power = self.Param("MinNetPower", 20.0) \
            .SetDisplay("Minimum Net Power", "Minimum absolute net bull/bear power for entries", "Filters")
        self._cooldown_bars = self.Param("CooldownBars", 4) \
            .SetDisplay("Cooldown Bars", "Completed candles to wait after a position change", "Trading")
        self._prev_bull = 0.0
        self._prev_bear = 0.0
        self._initialized = False
        self._bull_ema = 0.0
        self._bear_ema = 0.0
        self._k = 0.0
        self._count = 0
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value
    @property
    def length(self):
        return self._length.Value
    @property
    def min_net_power(self):
        return self._min_net_power.Value
    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    def OnReseted(self):
        super(bnb_strategy, self).OnReseted()
        self._prev_bull = 0.0
        self._prev_bear = 0.0
        self._initialized = False
        self._bull_ema = 0.0
        self._bear_ema = 0.0
        self._k = 0.0
        self._count = 0
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(bnb_strategy, self).OnStarted2(time)
        self._k = 2.0 / (self.length + 1.0)
        self._count = 0
        sma = SimpleMovingAverage()
        sma.Length = self.length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, sma_value):
        if candle.State != CandleStates.Finished:
            return
        sma_value = float(sma_value)
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
        bull_power = float(candle.HighPrice) - sma_value
        bear_power = float(candle.LowPrice) - sma_value
        self._count += 1
        if self._count == 1:
            self._bull_ema = bull_power
            self._bear_ema = bear_power
        else:
            self._bull_ema = bull_power * self._k + self._bull_ema * (1.0 - self._k)
            self._bear_ema = bear_power * self._k + self._bear_ema * (1.0 - self._k)
        if self._count < self.length:
            return
        if not self._initialized:
            self._prev_bull = self._bull_ema
            self._prev_bear = self._bear_ema
            self._initialized = True
            return
        net_power = self._bull_ema + self._bear_ema
        prev_net = self._prev_bull + self._prev_bear
        min_np = float(self.min_net_power)
        cross_up = prev_net <= 0 and net_power > 0 and abs(net_power) >= min_np
        cross_down = prev_net >= 0 and net_power < 0 and abs(net_power) >= min_np
        if self._cooldown_remaining == 0:
            if cross_up and self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
                self._cooldown_remaining = self.cooldown_bars
            elif cross_down and self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()
                self._cooldown_remaining = self.cooldown_bars
        self._prev_bull = self._bull_ema
        self._prev_bear = self._bear_ema

    def CreateClone(self):
        return bnb_strategy()
