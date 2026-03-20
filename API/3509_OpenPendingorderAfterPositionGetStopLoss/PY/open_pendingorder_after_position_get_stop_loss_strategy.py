import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class open_pendingorder_after_position_get_stop_loss_strategy(Strategy):
    def __init__(self):
        super(open_pendingorder_after_position_get_stop_loss_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60)))
        self._k_period = self.Param("KPeriod", 22)
        self._stop_loss_pct = self.Param("StopLossPct", 2.0)
        self._take_profit_pct = self.Param("TakeProfitPct", 3.0)
        self._signal_cooldown_candles = self.Param("SignalCooldownCandles", 4)

        self._last_k = None
        self._entry_price = 0.0
        self._candles_since_trade = 4

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def KPeriod(self):
        return self._k_period.Value

    @KPeriod.setter
    def KPeriod(self, value):
        self._k_period.Value = value

    @property
    def StopLossPct(self):
        return self._stop_loss_pct.Value

    @StopLossPct.setter
    def StopLossPct(self, value):
        self._stop_loss_pct.Value = value

    @property
    def TakeProfitPct(self):
        return self._take_profit_pct.Value

    @TakeProfitPct.setter
    def TakeProfitPct(self, value):
        self._take_profit_pct.Value = value

    @property
    def SignalCooldownCandles(self):
        return self._signal_cooldown_candles.Value

    @SignalCooldownCandles.setter
    def SignalCooldownCandles(self, value):
        self._signal_cooldown_candles.Value = value

    def OnReseted(self):
        super(open_pendingorder_after_position_get_stop_loss_strategy, self).OnReseted()
        self._last_k = None
        self._entry_price = 0.0
        self._candles_since_trade = self.SignalCooldownCandles

    def OnStarted(self, time):
        super(open_pendingorder_after_position_get_stop_loss_strategy, self).OnStarted(time)
        self._last_k = None
        self._entry_price = 0.0
        self._candles_since_trade = self.SignalCooldownCandles

        rsi = RelativeStrengthIndex()
        rsi.Length = self.KPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(rsi, self._process_candle).Start()

    def _process_candle(self, candle, current_k):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        k_val = float(current_k)

        if self._candles_since_trade < self.SignalCooldownCandles:
            self._candles_since_trade += 1

        # Check stop-loss / take-profit on existing position
        if self.Position != 0 and self._entry_price > 0:
            if self.Position > 0:
                pnl_pct = (close - self._entry_price) / self._entry_price * 100.0
                if pnl_pct <= -float(self.StopLossPct) or pnl_pct >= float(self.TakeProfitPct):
                    self.SellMarket()
                    self._entry_price = 0.0
                    self._last_k = k_val
                    return
            elif self.Position < 0:
                pnl_pct = (self._entry_price - close) / self._entry_price * 100.0
                if pnl_pct <= -float(self.StopLossPct) or pnl_pct >= float(self.TakeProfitPct):
                    self.BuyMarket()
                    self._entry_price = 0.0
                    self._last_k = k_val
                    return

        if self._last_k is None:
            self._last_k = k_val
            return

        prev_k = self._last_k
        self._last_k = k_val

        crossed_up = prev_k <= 45.0 and k_val > 45.0
        crossed_down = prev_k >= 55.0 and k_val < 55.0

        if crossed_up and self.Position <= 0 and self._candles_since_trade >= self.SignalCooldownCandles:
            self.BuyMarket()
            self._entry_price = close
            self._candles_since_trade = 0
        elif crossed_down and self.Position >= 0 and self._candles_since_trade >= self.SignalCooldownCandles:
            self.SellMarket()
            self._entry_price = close
            self._candles_since_trade = 0

    def CreateClone(self):
        return open_pendingorder_after_position_get_stop_loss_strategy()
