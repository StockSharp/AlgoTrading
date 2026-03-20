import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class xrp_ai15m_adaptive_v31_strategy(Strategy):
    def __init__(self):
        super(xrp_ai15m_adaptive_v31_strategy, self).__init__()
        self._stop_pct = self.Param("StopPct", 1.5) \
            .SetDisplay("Stop %", "Stop loss percent", "Risk")
        self._tp_small = self.Param("TpSmall", 2) \
            .SetDisplay("Small TP %", "Take profit for small setups", "Risk")
        self._tp_large = self.Param("TpLarge", 4) \
            .SetDisplay("Large TP %", "Take profit for large setups", "Risk")
        self._trail_pct = self.Param("TrailPct", 1) \
            .SetDisplay("Trail %", "Trailing stop percent", "Risk")
        self._max_bars = self.Param("MaxBars", TimeSpan.FromMinutes(5)) \
            .SetDisplay("Max Bars", "Maximum bars to hold", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Main candle type", "Parameters")
        self._trend_up = False
        self._bar_index = 0
        self._entry_bar = 0
        self._entry_price = 0.0
        self._high_water = 0.0
        self._trail_live = False
        self._stop_price = 0.0
        self._take_price = 0.0

    @property
    def stop_pct(self):
        return self._stop_pct.Value

    @property
    def tp_small(self):
        return self._tp_small.Value

    @property
    def tp_large(self):
        return self._tp_large.Value

    @property
    def trail_pct(self):
        return self._trail_pct.Value

    @property
    def max_bars(self):
        return self._max_bars.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(xrp_ai15m_adaptive_v31_strategy, self).OnReseted()
        self._trend_up = False
        self._bar_index = 0
        self._entry_bar = 0
        self._entry_price = 0.0
        self._high_water = 0.0
        self._trail_live = False
        self._stop_price = 0.0
        self._take_price = 0.0

    def OnStarted(self, time):
        super(xrp_ai15m_adaptive_v31_strategy, self).OnStarted(time)
        ema13 = ExponentialMovingAverage()
        ema13.Length = 13
        ema34 = ExponentialMovingAverage()
        ema34.Length = 34
        rsi = RelativeStrengthIndex()
        rsi.Length = 14
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema13, ema34, rsi, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema13)
            self.DrawIndicator(area, ema34)
            self.DrawOwnTrades(area)

    def on_process(self, candle, ema13, ema34, rsi):
        if candle.State != CandleStates.Finished:
            return
        self._bar_index += 1
        # Use EMA cross for trend
        self._trend_up = ema13 > ema34
        # Exit management
        if self.Position > 0 and self._entry_price > 0:
            if candle.HighPrice > self._high_water:
                self._high_water = candle.HighPrice
            # Stop loss
            if candle.ClosePrice <= self._stop_price:
                self.SellMarket()
                # ResetTrade()
                return
            # Take profit
            if candle.ClosePrice >= self._take_price:
                self.SellMarket()
                # ResetTrade()
                return
            # Trailing stop
            if not self._trail_live and self._high_water >= self._entry_price * (1 + self.trail_pct / 100):
                self._trail_live = True
            if self._trail_live:
                trail_stop = self._high_water * (1 - self.trail_pct / 200)
                if candle.ClosePrice <= trail_stop:
                    self.SellMarket()
                    # ResetTrade()
                    return
            # Time-based exit
            if self._bar_index - self._entry_bar >= self.max_bars:
                self.SellMarket()
                # ResetTrade()
                return
        # Entry conditions - long only
        if self.Position == 0:
            # Large setup: extreme oversold + trend up
            large_ok = rsi < 25 and candle.ClosePrice > ema34 and self._trend_up
            # Small setup: pullback to EMA in uptrend
            small_ok = candle.ClosePrice <= ema13 * 0.998 and
            rsi < 45 and
            candle.ClosePrice > candle.OpenPrice and
            self._trend_up
            if large_ok:
                self.BuyMarket()
                self._entry_price = candle.ClosePrice
                self._entry_bar = self._bar_index
                self._high_water = candle.ClosePrice
                self._trail_live = False
                self._stop_price = self._entry_price * (1 - self.stop_pct / 100)
                self._take_price = self._entry_price * (1 + self.tp_large / 100)
            elif small_ok:
                self.BuyMarket()
                self._entry_price = candle.ClosePrice
                self._entry_bar = self._bar_index
                self._high_water = candle.ClosePrice
                self._trail_live = False
                self._stop_price = self._entry_price * (1 - self.stop_pct / 100)
                self._take_price = self._entry_price * (1 + self.tp_small / 100)

    def reset_trade(self):
        self._entry_bar = -1
        self._entry_price = 0
        self._high_water = 0
        self._trail_live = False
        self._stop_price = 0
        self._take_price = 0

    def CreateClone(self):
        return xrp_ai15m_adaptive_v31_strategy()
