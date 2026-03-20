import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, RelativeStrengthIndex, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class nse_index_with_entry_exit_markers_strategy(Strategy):
    def __init__(self):
        super(nse_index_with_entry_exit_markers_strategy, self).__init__()
        self._sma_period = self.Param("SmaPeriod", 200)
        self._rsi_period = self.Param("RsiPeriod", 14)
        self._rsi_oversold = self.Param("RsiOversold", 25.0)
        self._atr_period = self.Param("AtrPeriod", 14)
        self._atr_multiplier = self.Param("AtrMultiplier", 4.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._stop_loss = 0.0
        self._take_profit = 0.0
        self._prev_rsi = 0.0
        self._is_rsi_initialized = False
        self._last_signal_ticks = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(nse_index_with_entry_exit_markers_strategy, self).OnReseted()
        self._stop_loss = 0.0
        self._take_profit = 0.0
        self._prev_rsi = 0.0
        self._is_rsi_initialized = False
        self._last_signal_ticks = 0

    def OnStarted(self, time):
        super(nse_index_with_entry_exit_markers_strategy, self).OnStarted(time)
        self._stop_loss = 0.0
        self._take_profit = 0.0
        self._prev_rsi = 0.0
        self._is_rsi_initialized = False
        self._last_signal_ticks = 0
        self._sma = SimpleMovingAverage()
        self._sma.Length = self._sma_period.Value
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self._rsi_period.Value
        self._atr = AverageTrueRange()
        self._atr.Length = self._atr_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._sma, self._rsi, self._atr, self.OnProcess).Start()

    def OnProcess(self, candle, sma_value, rsi_value, atr_value):
        if candle.State != CandleStates.Finished:
            return
        sv = float(sma_value)
        rv = float(rsi_value)
        av = float(atr_value)
        close = float(candle.ClosePrice)
        low = float(candle.LowPrice)
        high = float(candle.HighPrice)
        cooldown_ticks = TimeSpan.FromMinutes(480).Ticks
        current_ticks = candle.OpenTime.Ticks
        if self.Position > 0 and current_ticks - self._last_signal_ticks >= cooldown_ticks:
            if low <= self._stop_loss or high >= self._take_profit:
                self.SellMarket()
                self._stop_loss = 0.0
                self._take_profit = 0.0
                self._last_signal_ticks = current_ticks
        if not self._is_rsi_initialized:
            self._prev_rsi = rv
            self._is_rsi_initialized = True
            return
        os_level = float(self._rsi_oversold.Value)
        in_uptrend = close > sv
        cross_up = self._prev_rsi <= os_level and rv > os_level
        am = float(self._atr_multiplier.Value)
        if in_uptrend and cross_up and self.Position <= 0 and current_ticks - self._last_signal_ticks >= cooldown_ticks:
            self.BuyMarket()
            self._stop_loss = close - am * av
            self._take_profit = close + am * av
            self._last_signal_ticks = current_ticks
        self._prev_rsi = rv

    def CreateClone(self):
        return nse_index_with_entry_exit_markers_strategy()
