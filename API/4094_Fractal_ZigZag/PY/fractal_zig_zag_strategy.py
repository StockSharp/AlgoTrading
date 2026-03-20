import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import AverageTrueRange

class fractal_zig_zag_strategy(Strategy):
    def __init__(self):
        super(fractal_zig_zag_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(8))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._level = self.Param("Level", 2) \
            .SetDisplay("Fractal Depth", "Candles on each side to confirm fractal", "Signals")
        self._atr_length = self.Param("AtrLength", 14) \
            .SetDisplay("ATR Length", "ATR period for stops", "Indicators")

        self._window = []
        self._trend = 0  # 1=bearish (last was high), 2=bullish (last was low)
        self._prev_trend = 0
        self._entry_price = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def Level(self):
        return self._level.Value

    @property
    def AtrLength(self):
        return self._atr_length.Value

    def OnStarted(self, time):
        super(fractal_zig_zag_strategy, self).OnStarted(time)

        self._window = []
        self._trend = 0
        self._prev_trend = 0
        self._entry_price = 0.0

        self._atr = AverageTrueRange()
        self._atr.Length = self.AtrLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._atr, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, atr_val):
        if candle.State != CandleStates.Finished:
            return

        av = float(atr_val)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)

        depth = max(1, self.Level)
        window_size = depth * 2 + 1

        self._window.append((high, low))
        while len(self._window) > window_size:
            self._window.pop(0)

        # Evaluate fractals
        if len(self._window) >= window_size:
            center_index = len(self._window) - 1 - depth
            center = self._window[center_index]
            is_high = True
            is_low = True

            for i in range(len(self._window)):
                if i == center_index:
                    continue
                if self._window[i][0] >= center[0]:
                    is_high = False
                if self._window[i][1] <= center[1]:
                    is_low = False
                if not is_high and not is_low:
                    break

            if is_high:
                self._trend = 1
            if is_low:
                self._trend = 2

        if av <= 0 or self._trend == 0:
            self._prev_trend = self._trend
            return

        # Exit management
        if self.Position > 0:
            if close <= self._entry_price - av * 2.0 or close >= self._entry_price + av * 3.0 or self._trend == 1:
                self.SellMarket()
                self._entry_price = 0.0
        elif self.Position < 0:
            if close >= self._entry_price + av * 2.0 or close <= self._entry_price - av * 3.0 or self._trend == 2:
                self.BuyMarket()
                self._entry_price = 0.0

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_trend = self._trend
            return

        # Entry on trend change
        if self.Position == 0 and self._prev_trend != 0 and self._trend != self._prev_trend:
            if self._trend == 2:
                self._entry_price = close
                self.BuyMarket()
            elif self._trend == 1:
                self._entry_price = close
                self.SellMarket()

        self._prev_trend = self._trend

    def OnReseted(self):
        super(fractal_zig_zag_strategy, self).OnReseted()
        self._window = []
        self._trend = 0
        self._prev_trend = 0
        self._entry_price = 0.0

    def CreateClone(self):
        return fractal_zig_zag_strategy()
