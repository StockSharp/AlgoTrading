import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from collections import deque
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Strategies import Strategy


class dss_bressert_strategy(Strategy):

    def __init__(self):
        super(dss_bressert_strategy, self).__init__()

        self._ema_period = self.Param("EmaPeriod", 8) \
            .SetDisplay("EMA Period", "EMA smoothing period", "Indicators")
        self._sto_period = self.Param("StoPeriod", 13) \
            .SetDisplay("Stochastic Period", "Stochastic calculation period", "Indicators")
        self._take_profit_percent = self.Param("TakeProfitPercent", 2.0) \
            .SetDisplay("Take Profit %", "Take profit level in percent", "Risk")
        self._stop_loss_percent = self.Param("StopLossPercent", 1.0) \
            .SetDisplay("Stop Loss %", "Stop loss level in percent", "Risk")
        self._cooldown_bars = self.Param("CooldownBars", 1) \
            .SetDisplay("Cooldown Bars", "Bars to wait after a completed trade", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._prev_dss = 0.0
        self._prev_mit = 0.0
        self._bars_since_trade = 0

        self._high_buf = deque()
        self._low_buf = deque()
        self._mit_buf = deque()
        self._prev_mit_ema = 50.0
        self._prev_dss_ema = 50.0
        self._last_mit = 50.0
        self._dss_formed = False

    @property
    def EmaPeriod(self):
        return self._ema_period.Value

    @EmaPeriod.setter
    def EmaPeriod(self, value):
        self._ema_period.Value = value

    @property
    def StoPeriod(self):
        return self._sto_period.Value

    @StoPeriod.setter
    def StoPeriod(self, value):
        self._sto_period.Value = value

    @property
    def TakeProfitPercent(self):
        return self._take_profit_percent.Value

    @TakeProfitPercent.setter
    def TakeProfitPercent(self, value):
        self._take_profit_percent.Value = value

    @property
    def StopLossPercent(self):
        return self._stop_loss_percent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stop_loss_percent.Value = value

    @property
    def CooldownBars(self):
        return self._cooldown_bars.Value

    @CooldownBars.setter
    def CooldownBars(self, value):
        self._cooldown_bars.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def _compute_dss(self, candle):
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)
        sto_period = self.StoPeriod
        ema_period = self.EmaPeriod

        self._high_buf.append(high)
        self._low_buf.append(low)

        if len(self._high_buf) > sto_period:
            self._high_buf.popleft()
            self._low_buf.popleft()

        if len(self._high_buf) >= sto_period:
            self._dss_formed = True

        high_range = max(self._high_buf)
        low_range = min(self._low_buf)
        if high_range == low_range:
            return self._prev_dss_ema

        delta = close - low_range
        mit_raw = delta / (high_range - low_range) * 100.0
        coeff = 2.0 / (1.0 + ema_period)
        mit_value = self._prev_mit_ema + coeff * (mit_raw - self._prev_mit_ema)
        self._prev_mit_ema = mit_value
        self._last_mit = mit_value

        self._mit_buf.append(mit_value)
        if len(self._mit_buf) > sto_period:
            self._mit_buf.popleft()

        high_mit = max(self._mit_buf)
        low_mit = min(self._mit_buf)
        if high_mit == low_mit:
            return self._prev_dss_ema

        delta_mit = mit_value - low_mit
        dss_raw = delta_mit / (high_mit - low_mit) * 100.0
        dss_value = self._prev_dss_ema + coeff * (dss_raw - self._prev_dss_ema)
        self._prev_dss_ema = dss_value

        return dss_value

    def OnStarted(self, time):
        super(dss_bressert_strategy, self).OnStarted(time)

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

        self.StartProtection(
            takeProfit=Unit(self.TakeProfitPercent, UnitTypes.Percent),
            stopLoss=Unit(self.StopLossPercent, UnitTypes.Percent))

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        dss_value = self._compute_dss(candle)

        if not self._dss_formed:
            return

        if self._bars_since_trade < self.CooldownBars:
            self._bars_since_trade += 1

        current_mit = self._last_mit

        if (self._bars_since_trade >= self.CooldownBars
                and self._prev_dss <= self._prev_mit
                and dss_value > current_mit
                and self.Position <= 0):
            self.BuyMarket(self.Volume + abs(self.Position))
            self._bars_since_trade = 0
        elif (self._bars_since_trade >= self.CooldownBars
              and self._prev_dss >= self._prev_mit
              and dss_value < current_mit
              and self.Position >= 0):
            self.SellMarket(self.Volume + abs(self.Position))
            self._bars_since_trade = 0

        self._prev_dss = dss_value
        self._prev_mit = current_mit

    def OnReseted(self):
        super(dss_bressert_strategy, self).OnReseted()
        self._prev_dss = 0.0
        self._prev_mit = 0.0
        self._bars_since_trade = self.CooldownBars
        self._high_buf = deque()
        self._low_buf = deque()
        self._mit_buf = deque()
        self._prev_mit_ema = 50.0
        self._prev_dss_ema = 50.0
        self._last_mit = 50.0
        self._dss_formed = False

    def CreateClone(self):
        return dss_bressert_strategy()
