import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class long_leg_doji_breakout_strategy(Strategy):
    """
    Long-Leg Doji breakout strategy.
    Detects doji patterns and trades breakouts with SMA confirmation.
    """

    def __init__(self):
        super(long_leg_doji_breakout_strategy, self).__init__()
        self._doji_threshold = self.Param("DojiBodyThreshold", 15.0) \
            .SetDisplay("Doji Body %", "Body size as % of range", "Pattern")
        self._wick_ratio = self.Param("MinWickRatio", 1.2) \
            .SetDisplay("Min Wick Ratio", "Min wick to body ratio", "Pattern")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "ATR period", "Filter")
        self._cooldown_bars = self.Param("CooldownBars", 70) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Candles", "General")

        self._doji_high = 0.0
        self._doji_low = 0.0
        self._waiting = False
        self._prev_close = 0.0
        self._prev_sma = 0.0
        self._cooldown = 0
        self._entry_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(long_leg_doji_breakout_strategy, self).OnReseted()
        self._doji_high = 0.0
        self._doji_low = 0.0
        self._waiting = False
        self._prev_close = 0.0
        self._prev_sma = 0.0
        self._cooldown = 0
        self._entry_price = 0.0

    def OnStarted2(self, time):
        super(long_leg_doji_breakout_strategy, self).OnStarted2(time)

        sma = SimpleMovingAverage()
        sma.Length = 10
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, sma_val, atr_val):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        open_p = float(candle.OpenPrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        sma = float(sma_val)
        atr = float(atr_val)

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_close = close
            self._prev_sma = sma
            return

        if atr <= 0 or self._prev_sma == 0:
            self._prev_close = close
            self._prev_sma = sma
            return

        body = abs(close - open_p)
        rng = high - low

        if rng > 0:
            upper_wick = high - max(open_p, close)
            lower_wick = min(open_p, close) - low
            threshold = self._doji_threshold.Value / 100.0
            small_body = body <= rng * threshold
            long_wicks = upper_wick >= body * self._wick_ratio.Value and lower_wick >= body * self._wick_ratio.Value
            if small_body and long_wicks:
                self._doji_high = high
                self._doji_low = low
                self._waiting = True

        if self.Position == 0 and self._prev_close > 0:
            doji_long = self._waiting and close > self._doji_high and close > sma
            doji_short = self._waiting and close < self._doji_low and close < sma
            sma_cross_up = self._prev_close < self._prev_sma and close > sma
            sma_cross_down = self._prev_close > self._prev_sma and close < sma

            if doji_long or sma_cross_up:
                self.BuyMarket()
                self._entry_price = close
                self._waiting = False
                self._cooldown = self._cooldown_bars.Value
            elif doji_short or sma_cross_down:
                self.SellMarket()
                self._entry_price = close
                self._waiting = False
                self._cooldown = self._cooldown_bars.Value

        if self.Position > 0:
            sma_cross = self._prev_close >= self._prev_sma and close < sma
            atr_stop = self._entry_price > 0 and close < self._entry_price - atr * 2
            if sma_cross or atr_stop:
                self.SellMarket(abs(self.Position))
                self._cooldown = self._cooldown_bars.Value
        elif self.Position < 0:
            sma_cross = self._prev_close <= self._prev_sma and close > sma
            atr_stop = self._entry_price > 0 and close > self._entry_price + atr * 2
            if sma_cross or atr_stop:
                self.BuyMarket(abs(self.Position))
                self._cooldown = self._cooldown_bars.Value

        self._prev_close = close
        self._prev_sma = sma

    def CreateClone(self):
        return long_leg_doji_breakout_strategy()
