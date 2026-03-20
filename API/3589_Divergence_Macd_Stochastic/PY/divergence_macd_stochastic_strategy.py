import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class divergence_macd_stochastic_strategy(Strategy):
    DIVERGENCE_LOOKBACK = 10

    def __init__(self):
        super(divergence_macd_stochastic_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60)))
        self._macd_fast = self.Param("MacdFast", 20)
        self._macd_slow = self.Param("MacdSlow", 50)

        self._fast_ema = 0.0
        self._slow_ema = 0.0
        self._ema_initialized = False
        self._bar_count = 0
        self._fast_multiplier = 0.0
        self._slow_multiplier = 0.0

        self._macd_window = []
        self._price_window = []

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def MacdFast(self):
        return self._macd_fast.Value

    @MacdFast.setter
    def MacdFast(self, value):
        self._macd_fast.Value = value

    @property
    def MacdSlow(self):
        return self._macd_slow.Value

    @MacdSlow.setter
    def MacdSlow(self, value):
        self._macd_slow.Value = value

    def OnReseted(self):
        super(divergence_macd_stochastic_strategy, self).OnReseted()
        self._fast_ema = 0.0
        self._slow_ema = 0.0
        self._ema_initialized = False
        self._bar_count = 0
        self._macd_window = []
        self._price_window = []

    def OnStarted(self, time):
        super(divergence_macd_stochastic_strategy, self).OnStarted(time)
        self._fast_ema = 0.0
        self._slow_ema = 0.0
        self._ema_initialized = False
        self._bar_count = 0
        self._fast_multiplier = 2.0 / (self.MacdFast + 1)
        self._slow_multiplier = 2.0 / (self.MacdSlow + 1)
        self._macd_window = []
        self._price_window = []

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        self._bar_count += 1

        if not self._ema_initialized:
            self._fast_ema = close
            self._slow_ema = close
            self._ema_initialized = True
        else:
            self._fast_ema = close * self._fast_multiplier + self._fast_ema * (1 - self._fast_multiplier)
            self._slow_ema = close * self._slow_multiplier + self._slow_ema * (1 - self._slow_multiplier)

        if self._bar_count < self.MacdSlow:
            return

        macd_line = self._fast_ema - self._slow_ema

        self._macd_window.append(macd_line)
        self._price_window.append(close)
        while len(self._macd_window) > self.DIVERGENCE_LOOKBACK:
            self._macd_window.pop(0)
            self._price_window.pop(0)

        if len(self._macd_window) < self.DIVERGENCE_LOOKBACK:
            return

        old_macd = self._macd_window[0]
        new_macd = self._macd_window[-1]
        old_price = self._price_window[0]
        new_price = self._price_window[-1]

        min_price_move = old_price * 0.005

        # Bullish divergence: price makes lower low but MACD makes higher low
        bullish_div = new_price < old_price - min_price_move and new_macd > old_macd
        # Bearish divergence: price makes higher high but MACD makes lower high
        bearish_div = new_price > old_price + min_price_move and new_macd < old_macd

        if bullish_div:
            if self.Position <= 0:
                self.BuyMarket()
        elif bearish_div:
            if self.Position >= 0:
                self.SellMarket()

    def CreateClone(self):
        return divergence_macd_stochastic_strategy()
