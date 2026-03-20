import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math, DateTimeOffset
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class vwap_stdev_bands_long_strategy(Strategy):
    def __init__(self):
        super(vwap_stdev_bands_long_strategy, self).__init__()
        self._dev_down = self.Param("DevDown", 1.28) \
            .SetDisplay("Stdev Down", "Std dev below VWAP", "Parameters")
        self._profit_pct = self.Param("ProfitPct", 0.3) \
            .SetDisplay("Profit %", "Profit target percent", "Parameters")
        self._gap_minutes = self.Param("GapMinutes", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Gap Minutes", "Gap before new order", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._session_date = None
        self._vwap_sum = 0.0
        self._vol_sum = 0.0
        self._v2_sum = 0.0
        self._prev_close = 0.0
        self._prev_lower = 0.0
        self._has_prev = False
        self._last_entry_price = 0.0
        self._last_entry_time = None

    @property
    def dev_down(self):
        return self._dev_down.Value

    @property
    def profit_pct(self):
        return self._profit_pct.Value

    @property
    def gap_minutes(self):
        return self._gap_minutes.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(vwap_stdev_bands_long_strategy, self).OnReseted()
        self._session_date = None
        self._vwap_sum = 0.0
        self._vol_sum = 0.0
        self._v2_sum = 0.0
        self._prev_close = 0.0
        self._prev_lower = 0.0
        self._has_prev = False
        self._last_entry_price = 0.0
        self._last_entry_time = None

    def OnStarted(self, time):
        super(vwap_stdev_bands_long_strategy, self).OnStarted(time)
        sma = SimpleMovingAverage()
        sma.Length = 2
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, _dummy):
        if candle.State != CandleStates.Finished:
            return
        date = candle.OpenTime.Date
        volume = candle.TotalVolume
        price = (candle.HighPrice + candle.LowPrice) / 2
        if date != self._session_date:
            self._session_date = date
            self._vwap_sum = price * volume
            self._vol_sum = volume
            self._v2_sum = volume * price * price
            self._has_prev = False
        else:
            self._vwap_sum += price * volume
            self._vol_sum += volume
            self._v2_sum += volume * price * price
        if self._vol_sum == 0:
            return
        vwap = self._vwap_sum / self._vol_sum
        variance = self._v2_sum / self._vol_sum - vwap * vwap
        dev = float(Math.Sqrt((double)max(variance, 0)))
        lower = vwap - self.dev_down * dev
        can_enter = not self._last_entry_time is not None or candle.OpenTime - self._last_entry_time >= TimeSpan.FromMinutes(self.gap_minutes)
        crossed_lower = self._has_prev and self._prev_close >= self._prev_lower and candle.ClosePrice < lower
        if crossed_lower and can_enter and self.Position <= 0:
            self.BuyMarket()
            self._last_entry_price = candle.ClosePrice
            self._last_entry_time = candle.OpenTime
        # Profit target exit
        if self.Position > 0 and self._last_entry_price > 0:
            target = self._last_entry_price * (1 + self.profit_pct / 100)
            if candle.ClosePrice >= target:
                self.SellMarket()
                self._last_entry_time = None
                self._last_entry_price = 0
        self._prev_close = candle.ClosePrice
        self._prev_lower = lower
        self._has_prev = True

    def CreateClone(self):
        return vwap_stdev_bands_long_strategy()
