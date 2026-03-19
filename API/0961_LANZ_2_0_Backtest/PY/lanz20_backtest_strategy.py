import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class lanz20_backtest_strategy(Strategy):
    """
    LANZ strategy: swing structure BOS with session time entry and SL/TP.
    """

    def __init__(self):
        super(lanz20_backtest_strategy, self).__init__()
        self._rr_multiplier = self.Param("RrMultiplier", 5.5).SetDisplay("RR Multiplier", "Risk reward multiplier", "Risk")
        self._cooldown_days = self.Param("CooldownDays", 1).SetDisplay("Cooldown Days", "Min days between entries", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Timeframe", "General")

        self._highs = []
        self._lows = []
        self._last_swing_high = 0.0
        self._last_swing_low = 0.0
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._bos_dir = 0
        self._stop_price = 0.0
        self._tp_price = 0.0
        self._last_trade_day = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(lanz20_backtest_strategy, self).OnReseted()
        self._highs = []
        self._lows = []
        self._last_swing_high = 0.0
        self._last_swing_low = 0.0
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._bos_dir = 0
        self._stop_price = 0.0
        self._tp_price = 0.0
        self._last_trade_day = None

    def OnStarted(self, time):
        super(lanz20_backtest_strategy, self).OnStarted(time)
        ema1 = ExponentialMovingAverage()
        ema1.Length = 10
        ema2 = ExponentialMovingAverage()
        ema2.Length = 20
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema1, ema2, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, d1, d2):
        if candle.State != CandleStates.Finished:
            return

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)
        sec = self.Security
        step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 1.0
        pip_size = step * 10

        self._highs.append(high)
        self._lows.append(low)
        if len(self._highs) > 10:
            self._highs.pop(0)
            self._lows.pop(0)

        if len(self._highs) >= 5:
            h2, h3, h4 = self._highs[-3], self._highs[-4], self._highs[-5]
            l2, l3, l4 = self._lows[-3], self._lows[-4], self._lows[-5]
            if h2 > h3 and h3 > h4:
                self._prev_high = self._last_swing_high
                self._last_swing_high = h2
            if l2 < l3 and l3 < l4:
                self._prev_low = self._last_swing_low
                self._last_swing_low = l2

        if self._last_swing_high > 0 and close > self._last_swing_high + 0.5 * pip_size:
            self._bos_dir = 1
        elif self._last_swing_low > 0 and close < self._last_swing_low - 0.5 * pip_size:
            self._bos_dir = -1

        hour = candle.OpenTime.Hour

        if self.Position != 0:
            if self.Position > 0:
                if low <= self._stop_price:
                    self.SellMarket()
                    return
                if high >= self._tp_price:
                    self.SellMarket()
                    return
            else:
                if high >= self._stop_price:
                    self.BuyMarket()
                    return
                if low <= self._tp_price:
                    self.BuyMarket()
                    return
            if hour == 15:
                if self.Position > 0:
                    self.SellMarket()
                elif self.Position < 0:
                    self.BuyMarket()
            return

        today = candle.OpenTime.Date
        if self._last_trade_day is not None:
            diff = (today - self._last_trade_day).Days
            if diff < self._cooldown_days.Value:
                return

        if hour != 10:
            return

        if self._bos_dir == 1:
            sl = self._last_swing_low if self._last_swing_low > 0 else close - 5 * pip_size
            if close - sl < 10 * pip_size:
                sl = close - 10 * pip_size
            tp = close + self._rr_multiplier.Value * (close - sl)
            self.BuyMarket()
            self._stop_price = sl
            self._tp_price = tp
            self._last_trade_day = today
        elif self._bos_dir == -1:
            sl = self._last_swing_high if self._last_swing_high > 0 else close + 5 * pip_size
            if sl - close < 10 * pip_size:
                sl = close + 10 * pip_size
            tp = close - self._rr_multiplier.Value * (sl - close)
            self.SellMarket()
            self._stop_price = sl
            self._tp_price = tp
            self._last_trade_day = today

    def CreateClone(self):
        return lanz20_backtest_strategy()
