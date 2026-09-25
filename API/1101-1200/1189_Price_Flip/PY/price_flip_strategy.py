import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class price_flip_strategy(Strategy):
    def __init__(self):
        super(price_flip_strategy, self).__init__()
        self._ticker_max_lookback = self.Param("TickerMaxLookback", 100).SetGreaterThanZero()
        self._ticker_min_lookback = self.Param("TickerMinLookback", 100).SetGreaterThanZero()
        self._fast_ma_length = self.Param("FastMaLength", 12).SetGreaterThanZero()
        self._slow_ma_length = self.Param("SlowMaLength", 14).SetGreaterThanZero()
        self._use_trend_filter = self.Param("UseTrendFilter", True)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))

        self._candles = []
        self._previous_close = None
        self._previous_inverted = None
        self._previous_fast = None
        self._previous_slow = None

    def GetWorkingSecurities(self):
        return [(self.Security, self._candle_type.Value)]

    def OnReseted(self):
        super(price_flip_strategy, self).OnReseted()
        self._candles = []
        self._previous_close = None
        self._previous_inverted = None
        self._previous_fast = None
        self._previous_slow = None

    def OnStarted2(self, time):
        super(price_flip_strategy, self).OnStarted2(time)
        self.SubscribeCandles(self._candle_type.Value).Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._candles.append(candle)
        max_lookback = int(self._ticker_max_lookback.Value)
        min_lookback = int(self._ticker_min_lookback.Value)
        fast_len = int(self._fast_ma_length.Value)
        slow_len = int(self._slow_ma_length.Value)
        keep = max(max_lookback, min_lookback, slow_len)

        if len(self._candles) > keep:
            del self._candles[:-keep]
        if len(self._candles) < keep:
            return

        fast = sum(float(c.ClosePrice) for c in self._candles[-fast_len:]) / fast_len
        slow = sum(float(c.ClosePrice) for c in self._candles[-slow_len:]) / slow_len

        if all(v is not None for v in [
                self._previous_close, self._previous_inverted,
                self._previous_fast, self._previous_slow]):
            signal = self.get_signal(
                self._previous_close,
                self._previous_inverted,
                self._previous_fast,
                self._previous_slow,
                fast,
                slow,
                float(candle.ClosePrice),
                bool(self._use_trend_filter.Value))

            if signal > 0 and self.Position <= 0:
                self.BuyMarket(self.Volume + Math.Abs(self.Position))
            elif signal < 0 and self.Position >= 0:
                self.SellMarket(self.Volume + Math.Abs(self.Position))

        recent_high = max(float(c.HighPrice) for c in self._candles[-max_lookback:])
        recent_low = min(float(c.LowPrice) for c in self._candles[-min_lookback:])

        self._previous_close = float(candle.ClosePrice)
        self._previous_inverted = self.calculate_inverted_price(
            recent_high, recent_low, self._previous_close)
        self._previous_fast = fast
        self._previous_slow = slow

    @staticmethod
    def calculate_inverted_price(recent_high, recent_low, price):
        return recent_high + recent_low - price

    @staticmethod
    def get_signal(previous_close, previous_inverted,
                   previous_fast, previous_slow,
                   fast, slow, current_close, use_trend_filter):
        cross_up = previous_fast <= previous_slow and fast > slow
        cross_down = previous_fast >= previous_slow and fast < slow

        if previous_close > previous_inverted and cross_up and (not use_trend_filter or current_close > slow):
            return 1
        if previous_close < previous_inverted and cross_down and (not use_trend_filter or current_close < slow):
            return -1
        return 0

    def CreateClone(self):
        return price_flip_strategy()
