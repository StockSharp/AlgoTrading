import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class vlt_trader_filter_strategy(Strategy):
    """Volatility contraction breakout strategy: enters when narrowest range breaks previous candle high/low."""

    def __init__(self):
        super(vlt_trader_filter_strategy, self).__init__()

        self._candle_count = self.Param("CandleCount", 6) \
            .SetGreaterThanZero() \
            .SetDisplay("Candle Count", "Number of historical candles for volatility filter", "Signals")
        self._tp_mult = self.Param("TakeProfitMultiplier", 3.0) \
            .SetGreaterThanZero() \
            .SetDisplay("TP Multiplier", "Take profit as multiplier of narrow range", "Risk")
        self._sl_mult = self.Param("StopLossMultiplier", 1.5) \
            .SetGreaterThanZero() \
            .SetDisplay("SL Multiplier", "Stop loss as multiplier of narrow range", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Time frame used for signal candles", "General")

        self._range_history = []
        self._prev_high = None
        self._prev_low = None
        self._prev_range = None
        self._entry_price = 0.0
        self._is_long = False

    @property
    def CandleCount(self):
        return self._candle_count.Value
    @property
    def TakeProfitMultiplier(self):
        return self._tp_mult.Value
    @property
    def StopLossMultiplier(self):
        return self._sl_mult.Value
    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(vlt_trader_filter_strategy, self).OnStarted(time)

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)
        rng = high - low

        # Check exit conditions
        if self.Position != 0 and self._entry_price != 0 and self._prev_range is not None and self._prev_range > 0:
            tp = self._prev_range * float(self.TakeProfitMultiplier)
            sl = self._prev_range * float(self.StopLossMultiplier)

            if self._is_long and self.Position > 0:
                if close >= self._entry_price + tp or close <= self._entry_price - sl:
                    self.SellMarket()
                    self._update_history(rng, high, low)
                    return
            elif not self._is_long and self.Position < 0:
                if close <= self._entry_price - tp or close >= self._entry_price + sl:
                    self.BuyMarket()
                    self._update_history(rng, high, low)
                    return

        # Check entry conditions when flat
        if self.Position == 0 and self._prev_high is not None and self._prev_low is not None and self._prev_range is not None:
            prev_h = self._prev_high
            prev_l = self._prev_low
            prev_r = self._prev_range

            if prev_r > 0 and len(self._range_history) >= self.CandleCount:
                is_narrowest = True
                for hist_range in self._range_history:
                    if hist_range > 0 and hist_range <= prev_r:
                        is_narrowest = False
                        break

                if is_narrowest:
                    if close > prev_h:
                        self.BuyMarket()
                        self._entry_price = close
                        self._is_long = True
                    elif close < prev_l:
                        self.SellMarket()
                        self._entry_price = close
                        self._is_long = False

        self._update_history(rng, high, low)

    def _update_history(self, rng, high, low):
        if self._prev_range is not None:
            self._range_history.append(self._prev_range)
            while len(self._range_history) > self.CandleCount:
                self._range_history.pop(0)

        self._prev_range = rng
        self._prev_high = high
        self._prev_low = low

    def OnReseted(self):
        super(vlt_trader_filter_strategy, self).OnReseted()
        self._range_history = []
        self._prev_high = None
        self._prev_low = None
        self._prev_range = None
        self._entry_price = 0.0
        self._is_long = False

    def CreateClone(self):
        return vlt_trader_filter_strategy()
