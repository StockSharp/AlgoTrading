import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class martingale_breakout_strategy(Strategy):
    def __init__(self):
        super(martingale_breakout_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60)))
        self._lookback = self.Param("Lookback", 10)
        self._breakout_multiplier = self.Param("BreakoutMultiplier", 3.0)
        self._take_profit_pct = self.Param("TakeProfitPct", 1.0)
        self._stop_loss_pct = self.Param("StopLossPct", 0.5)

        self._range_buffer = [0.0] * 10
        self._range_buffer_count = 0
        self._range_buffer_index = 0
        self._range_buffer_sum = 0.0
        self._entry_price = 0.0
        self._entry_side = 0
        self._last_was_loss = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def Lookback(self):
        return self._lookback.Value

    @Lookback.setter
    def Lookback(self, value):
        self._lookback.Value = value

    @property
    def BreakoutMultiplier(self):
        return self._breakout_multiplier.Value

    @BreakoutMultiplier.setter
    def BreakoutMultiplier(self, value):
        self._breakout_multiplier.Value = value

    @property
    def TakeProfitPct(self):
        return self._take_profit_pct.Value

    @TakeProfitPct.setter
    def TakeProfitPct(self, value):
        self._take_profit_pct.Value = value

    @property
    def StopLossPct(self):
        return self._stop_loss_pct.Value

    @StopLossPct.setter
    def StopLossPct(self, value):
        self._stop_loss_pct.Value = value

    def OnReseted(self):
        super(martingale_breakout_strategy, self).OnReseted()
        self._range_buffer = [0.0] * 10
        self._range_buffer_count = 0
        self._range_buffer_index = 0
        self._range_buffer_sum = 0.0
        self._entry_price = 0.0
        self._entry_side = 0
        self._last_was_loss = False

    def OnStarted2(self, time):
        super(martingale_breakout_strategy, self).OnStarted2(time)
        self._range_buffer = [0.0] * 10
        self._range_buffer_count = 0
        self._range_buffer_index = 0
        self._range_buffer_sum = 0.0
        self._entry_price = 0.0
        self._entry_side = 0
        self._last_was_loss = False

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

    def _update_range_statistics(self, candle):
        r = float(candle.HighPrice) - float(candle.LowPrice)
        buf_len = len(self._range_buffer)

        if self._range_buffer_count < buf_len:
            self._range_buffer[self._range_buffer_index] = r
            self._range_buffer_sum += r
            self._range_buffer_count += 1
            self._range_buffer_index = (self._range_buffer_index + 1) % buf_len
            return

        self._range_buffer_sum -= self._range_buffer[self._range_buffer_index]
        self._range_buffer[self._range_buffer_index] = r
        self._range_buffer_sum += r
        self._range_buffer_index = (self._range_buffer_index + 1) % buf_len

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        buf_len = len(self._range_buffer)

        # Check exit conditions first
        if self.Position != 0 and self._entry_price > 0:
            tp = float(self.TakeProfitPct) * 1.5 if self._last_was_loss else float(self.TakeProfitPct)
            sl = float(self.StopLossPct)

            if self._entry_side == 1:
                pnl_pct = (close - self._entry_price) / self._entry_price * 100.0
                if pnl_pct >= tp or pnl_pct <= -sl:
                    self._last_was_loss = pnl_pct < 0
                    self.SellMarket()
                    self._entry_price = 0.0
                    self._entry_side = 0
                    self._update_range_statistics(candle)
                    return
            elif self._entry_side == -1:
                pnl_pct = (self._entry_price - close) / self._entry_price * 100.0
                if pnl_pct >= tp or pnl_pct <= -sl:
                    self._last_was_loss = pnl_pct < 0
                    self.BuyMarket()
                    self._entry_price = 0.0
                    self._entry_side = 0
                    self._update_range_statistics(candle)
                    return

        # Entry logic - only when flat
        if self.Position == 0:
            r = float(candle.HighPrice) - float(candle.LowPrice)

            if self._range_buffer_count >= buf_len:
                avg_range = self._range_buffer_sum / buf_len

                if r > avg_range * float(self.BreakoutMultiplier):
                    body = float(candle.ClosePrice) - float(candle.OpenPrice)

                    if body > 0 and body > r * 0.4:
                        self.BuyMarket()
                        self._entry_price = close
                        self._entry_side = 1
                    elif body < 0 and abs(body) > r * 0.4:
                        self.SellMarket()
                        self._entry_price = close
                        self._entry_side = -1

        self._update_range_statistics(candle)

    def CreateClone(self):
        return martingale_breakout_strategy()
