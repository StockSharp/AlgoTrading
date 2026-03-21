import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import AverageTrueRange

class ft_time_bigdog_strategy(Strategy):
    def __init__(self):
        super(ft_time_bigdog_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe.", "General")
        self._channel_length = self.Param("ChannelLength", 20) \
            .SetDisplay("Channel Length", "Lookback for high/low channel.", "Indicators")
        self._atr_length = self.Param("AtrLength", 14) \
            .SetDisplay("ATR Length", "ATR period.", "Indicators")

        self._entry_price = 0.0
        self._highest = 0.0
        self._lowest = 0.0
        self._bar_count = 0
        self._highs = []
        self._lows = []

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def ChannelLength(self):
        return self._channel_length.Value

    @property
    def AtrLength(self):
        return self._atr_length.Value

    def OnStarted(self, time):
        super(ft_time_bigdog_strategy, self).OnStarted(time)

        self._entry_price = 0.0
        self._bar_count = 0
        self._highest = 0.0
        self._lowest = 0.0
        self._highs = [0.0] * 20
        self._lows = [0.0] * 20

        self._atr = AverageTrueRange()
        self._atr.Length = self.AtrLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._atr, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, atr_val):
        if candle.State != CandleStates.Finished:
            return

        av = float(atr_val)

        length = min(self.ChannelLength, 20)
        idx = self._bar_count % length
        self._highs[idx] = float(candle.HighPrice)
        self._lows[idx] = float(candle.LowPrice)
        self._bar_count += 1

        if self._bar_count < length or av <= 0:
            return

        high = max(self._highs[i] for i in range(length))
        low = min(self._lows[i] for i in range(length))

        prev_high = self._highest
        prev_low = self._lowest
        self._highest = high
        self._lowest = low

        if prev_high == 0 or prev_low == 0:
            return

        close = float(candle.ClosePrice)

        if self.Position > 0:
            if close >= self._entry_price + av * 3.0 or close <= self._entry_price - av * 2.0:
                self.SellMarket()
                self._entry_price = 0.0
        elif self.Position < 0:
            if close <= self._entry_price - av * 3.0 or close >= self._entry_price + av * 2.0:
                self.BuyMarket()
                self._entry_price = 0.0

        if self.Position == 0:
            if close > prev_high:
                self._entry_price = close
                self.BuyMarket()
            elif close < prev_low:
                self._entry_price = close
                self.SellMarket()

    def OnReseted(self):
        super(ft_time_bigdog_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._bar_count = 0
        self._highest = 0.0
        self._lowest = 0.0
        self._highs = []
        self._lows = []

    def CreateClone(self):
        return ft_time_bigdog_strategy()
