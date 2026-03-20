import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class eugene_strategy(Strategy):
    def __init__(self):
        super(eugene_strategy, self).__init__()
        self._sma_period = self.Param("SmaPeriod", 50) \
            .SetDisplay("SMA Period", "SMA lookback", "Indicators")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI lookback", "Indicators")
        self._cooldown_candles = self.Param("CooldownCandles", 200) \
            .SetDisplay("Cooldown", "Candles between signals", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._prev_close = 0.0
        self._prev_sma = 0.0
        self._has_prev = False
        self._cooldown_remaining = 0

    @property
    def sma_period(self):
        return self._sma_period.Value

    @property
    def rsi_period(self):
        return self._rsi_period.Value

    @property
    def cooldown_candles(self):
        return self._cooldown_candles.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(eugene_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._prev_sma = 0.0
        self._has_prev = False
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(eugene_strategy, self).OnStarted(time)
        self._prev_close = 0.0
        self._prev_sma = 0.0
        self._has_prev = False
        self._cooldown_remaining = 0

        sma = SimpleMovingAverage()
        sma.Length = self.sma_period
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, rsi, self.process_candle).Start()

    def process_candle(self, candle, sma, rsi):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        sma_val = float(sma)
        rsi_val = float(rsi)

        if not self._has_prev:
            self._prev_close = close
            self._prev_sma = sma_val
            self._has_prev = True
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._prev_close = close
            self._prev_sma = sma_val
            return

        if self._prev_close <= self._prev_sma and close > sma_val and rsi_val < 70 and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._cooldown_remaining = self.cooldown_candles
        elif self._prev_close >= self._prev_sma and close < sma_val and rsi_val > 30 and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._cooldown_remaining = self.cooldown_candles

        self._prev_close = close
        self._prev_sma = sma_val

    def CreateClone(self):
        return eugene_strategy()
