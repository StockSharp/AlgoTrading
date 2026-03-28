import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, UnitTypes, Unit
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal

class macd_pattern_trader_double_top_strategy(Strategy):
    def __init__(self):
        super(macd_pattern_trader_double_top_strategy, self).__init__()

        self._fast_period = self.Param("FastPeriod", 5) \
            .SetDisplay("Fast EMA", "Fast moving average length used by MACD", "MACD")
        self._slow_period = self.Param("SlowPeriod", 13) \
            .SetDisplay("Slow EMA", "Slow moving average length used by MACD", "MACD")
        self._signal_period = self.Param("SignalPeriod", 1) \
            .SetDisplay("Signal EMA", "Signal smoothing period for MACD", "MACD")
        self._trigger_level = self.Param("TriggerLevel", 50.0) \
            .SetDisplay("Trigger Level", "Absolute MACD level that arms the pattern logic", "MACD")
        self._stop_loss_pips = self.Param("StopLossPips", 100.0) \
            .SetDisplay("Stop-Loss (pips)", "Stop-loss distance expressed in pips", "Risk Management")
        self._take_profit_pips = self.Param("TakeProfitPips", 300.0) \
            .SetDisplay("Take-Profit (pips)", "Take-profit distance expressed in pips", "Risk Management")
        self._trade_volume = self.Param("TradeVolume", 0.1) \
            .SetDisplay("Trade Volume", "Order volume used for new entries", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Timeframe used for MACD calculations", "General")

        self._previous_macd = None
        self._previous_macd2 = None
        self._first_peak = None
        self._first_trough = None
        self._sell_pattern_armed = False
        self._buy_pattern_armed = False

    @property
    def FastPeriod(self):
        return self._fast_period.Value

    @property
    def SlowPeriod(self):
        return self._slow_period.Value

    @property
    def SignalPeriod(self):
        return self._signal_period.Value

    @property
    def TriggerLevel(self):
        return self._trigger_level.Value

    @property
    def StopLossPips(self):
        return self._stop_loss_pips.Value

    @property
    def TakeProfitPips(self):
        return self._take_profit_pips.Value

    @property
    def TradeVolume(self):
        return self._trade_volume.Value

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(macd_pattern_trader_double_top_strategy, self).OnStarted(time)

        self.Volume = float(self.TradeVolume)

        self._macd = MovingAverageConvergenceDivergenceSignal()
        self._macd.Macd.ShortMa.Length = self.FastPeriod
        self._macd.Macd.LongMa.Length = self.SlowPeriod
        self._macd.SignalMa.Length = self.SignalPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(self._macd, self.ProcessCandle).Start()

        ps = self.Security.PriceStep if self.Security is not None else None
        pip_size = float(ps) if ps is not None else 0.01
        if pip_size <= 0:
            pip_size = 0.01

        tp = float(self.TakeProfitPips)
        sl = float(self.StopLossPips)
        take_profit = Unit(tp * pip_size, UnitTypes.Absolute) if tp > 0 else None
        stop_loss = Unit(sl * pip_size, UnitTypes.Absolute) if sl > 0 else None
        self.StartProtection(take_profit, stop_loss, useMarketOrders=True)

    def ProcessCandle(self, candle, macd_value):
        if candle.State != CandleStates.Finished:
            return

        if not macd_value.IsFinal:
            return

        macd_line = macd_value.Macd
        if macd_line is None:
            return

        macd_line = float(macd_line)

        previous = self._previous_macd
        previous2 = self._previous_macd2

        if previous is not None and previous2 is not None:
            self._process_sell_pattern(macd_line, previous, previous2)
            self._process_buy_pattern(macd_line, previous, previous2)

        self._previous_macd2 = previous
        self._previous_macd = macd_line

    def _process_sell_pattern(self, current, previous, previous2):
        trigger = float(self.TriggerLevel)
        if current > trigger and current < previous and previous > previous2:
            if not self._sell_pattern_armed:
                self._first_peak = previous
                self._sell_pattern_armed = True
            elif self._first_peak is not None and previous < self._first_peak:
                self._enter_short()
                self._reset_sell_pattern()
        elif current < trigger:
            self._reset_sell_pattern()

    def _process_buy_pattern(self, current, previous, previous2):
        negative_trigger = -float(self.TriggerLevel)
        if current < negative_trigger and current > previous and previous < previous2:
            if not self._buy_pattern_armed:
                self._first_trough = previous
                self._buy_pattern_armed = True
            elif self._first_trough is not None and previous > self._first_trough:
                self._enter_long()
                self._reset_buy_pattern()
        elif current > negative_trigger:
            self._reset_buy_pattern()

    def _enter_short(self):
        volume = self.Volume + max(0.0, float(self.Position))
        if volume <= 0:
            return
        self.SellMarket(volume)

    def _enter_long(self):
        volume = self.Volume + max(0.0, -float(self.Position))
        if volume <= 0:
            return
        self.BuyMarket(volume)

    def _reset_sell_pattern(self):
        self._sell_pattern_armed = False
        self._first_peak = None

    def _reset_buy_pattern(self):
        self._buy_pattern_armed = False
        self._first_trough = None

    def OnReseted(self):
        super(macd_pattern_trader_double_top_strategy, self).OnReseted()
        self._previous_macd = None
        self._previous_macd2 = None
        self._first_peak = None
        self._first_trough = None
        self._sell_pattern_armed = False
        self._buy_pattern_armed = False

    def CreateClone(self):
        return macd_pattern_trader_double_top_strategy()
