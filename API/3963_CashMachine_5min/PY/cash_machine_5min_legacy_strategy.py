import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import DeMarker, StochasticOscillator
from StockSharp.Algo.Strategies import Strategy

class cash_machine_5min_legacy_strategy(Strategy):
    """
    Cash Machine strategy using DeMarker and Stochastic oscillator crossovers on 5 minute candles.
    Gradually tightens a hidden stop when profit targets are reached.
    """

    def __init__(self):
        super(cash_machine_5min_legacy_strategy, self).__init__()

        self._hidden_take_profit = self.Param("HiddenTakeProfit", 60.0) \
            .SetDisplay("Hidden Take Profit", "Hidden take profit distance in pips", "Risk")
        self._hidden_stop_loss = self.Param("HiddenStopLoss", 30.0) \
            .SetDisplay("Hidden Stop Loss", "Hidden stop loss distance in pips", "Risk")
        self._target_tp1 = self.Param("TargetTp1", 20.0) \
            .SetDisplay("Target TP1", "First profit threshold", "Risk")
        self._target_tp2 = self.Param("TargetTp2", 35.0) \
            .SetDisplay("Target TP2", "Second profit threshold", "Risk")
        self._target_tp3 = self.Param("TargetTp3", 50.0) \
            .SetDisplay("Target TP3", "Third profit threshold", "Risk")
        self._order_volume = self.Param("OrderVolume", 0.2) \
            .SetDisplay("Order Volume", "Order volume for new trades", "Trading")
        self._demarker_length = self.Param("DeMarkerLength", 14) \
            .SetDisplay("DeMarker Length", "DeMarker averaging period", "Indicators")
        self._stochastic_length = self.Param("StochasticLength", 5) \
            .SetDisplay("Stochastic Length", "Base Stochastic length", "Indicators")
        self._stochastic_k = self.Param("StochasticK", 3) \
            .SetDisplay("Stochastic %K", "%K smoothing length", "Indicators")
        self._stochastic_d = self.Param("StochasticD", 3) \
            .SetDisplay("Stochastic %D", "%D smoothing length", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Primary timeframe", "General")

        self._previous_demarker = None
        self._previous_stochastic_k = None
        self._long_trailing_stop = None
        self._short_trailing_stop = None
        self._long_stage = 0
        self._short_stage = 0
        self._pip_size = 0.0001
        self._entry_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(cash_machine_5min_legacy_strategy, self).OnReseted()
        self._previous_demarker = None
        self._previous_stochastic_k = None
        self._long_trailing_stop = None
        self._short_trailing_stop = None
        self._long_stage = 0
        self._short_stage = 0
        self._pip_size = 0.0001
        self._entry_price = 0.0

    def _calculate_pip_size(self):
        step = self.Security.PriceStep if self.Security.PriceStep is not None else 0.0
        if step <= 0:
            return 0.0001
        inverse = 1.0 / step
        digits = int(round(math.log10(inverse)))
        adjust = 10.0 if digits in (3, 5) else 1.0
        return step * adjust

    def OnStarted(self, time):
        super(cash_machine_5min_legacy_strategy, self).OnStarted(time)

        self._pip_size = self._calculate_pip_size()

        demarker = DeMarker()
        demarker.Length = self._demarker_length.Value

        stochastic = StochasticOscillator()
        stochastic.K.Length = self._stochastic_length.Value
        stochastic.D.Length = self._stochastic_d.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(demarker, stochastic, self.on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, demarker)
            oscillator_area = self.CreateChartArea()
            if oscillator_area is not None:
                self.DrawIndicator(oscillator_area, stochastic)
            self.DrawOwnTrades(area)

    def on_process(self, candle, demarker_value, stochastic_value):
        if candle.State != CandleStates.Finished:
            return

        if not stochastic_value.IsFinal or not self.IsFormedAndOnlineAndAllowTrading():
            return

        demarker = float(demarker_value)
        current_k = stochastic_value.K
        if current_k is None:
            return

        if self.Position == 0:
            self._long_stage = 0
            self._short_stage = 0
            self._long_trailing_stop = None
            self._short_trailing_stop = None

        if (self.Position == 0 and self._previous_demarker is not None
                and self._previous_stochastic_k is not None):
            long_signal = (self._previous_demarker < 0.30 and demarker >= 0.30
                           and self._previous_stochastic_k < 20 and current_k >= 20)
            short_signal = (self._previous_demarker > 0.70 and demarker <= 0.70
                            and self._previous_stochastic_k > 80 and current_k <= 80)

            if long_signal and self._order_volume.Value > 0:
                self._entry_price = float(candle.ClosePrice)
                self.BuyMarket()
            elif short_signal and self._order_volume.Value > 0:
                self._entry_price = float(candle.ClosePrice)
                self.SellMarket()
        elif self.Position > 0:
            self._manage_long(candle)
        elif self.Position < 0:
            self._manage_short(candle)

        self._previous_demarker = demarker
        self._previous_stochastic_k = current_k

    def _manage_long(self, candle):
        if self._entry_price <= 0 or self._pip_size <= 0:
            return

        sl_price = self._entry_price - self._hidden_stop_loss.Value * self._pip_size
        tp_price = self._entry_price + self._hidden_take_profit.Value * self._pip_size

        if float(candle.LowPrice) <= sl_price or float(candle.HighPrice) >= tp_price:
            self.SellMarket()
            return

        t1 = self._entry_price + self._target_tp1.Value * self._pip_size
        t2 = self._entry_price + self._target_tp2.Value * self._pip_size
        t3 = self._entry_price + self._target_tp3.Value * self._pip_size

        if self._long_stage < 3 and float(candle.HighPrice) >= t3:
            new_stop = float(candle.HighPrice) - max(self._target_tp3.Value - 13, 0) * self._pip_size
            self._long_trailing_stop = max(self._long_trailing_stop, new_stop) if self._long_trailing_stop is not None else new_stop
            self._long_stage = 3
            return

        if self._long_stage < 2 and float(candle.HighPrice) >= t2:
            new_stop = float(candle.HighPrice) - max(self._target_tp2.Value - 13, 0) * self._pip_size
            self._long_trailing_stop = max(self._long_trailing_stop, new_stop) if self._long_trailing_stop is not None else new_stop
            self._long_stage = 2
            return

        if self._long_stage < 1 and float(candle.HighPrice) >= t1:
            new_stop = float(candle.HighPrice) - max(self._target_tp1.Value - 13, 0) * self._pip_size
            self._long_trailing_stop = max(self._long_trailing_stop, new_stop) if self._long_trailing_stop is not None else new_stop
            self._long_stage = 1
            return

        if self._long_trailing_stop is not None and float(candle.LowPrice) <= self._long_trailing_stop:
            self.SellMarket()

    def _manage_short(self, candle):
        if self._entry_price <= 0 or self._pip_size <= 0:
            return

        sl_price = self._entry_price + self._hidden_stop_loss.Value * self._pip_size
        tp_price = self._entry_price - self._hidden_take_profit.Value * self._pip_size

        if float(candle.HighPrice) >= sl_price or float(candle.LowPrice) <= tp_price:
            self.BuyMarket()
            return

        t1 = self._entry_price - self._target_tp1.Value * self._pip_size
        t2 = self._entry_price - self._target_tp2.Value * self._pip_size
        t3 = self._entry_price - self._target_tp3.Value * self._pip_size

        if self._short_stage < 3 and float(candle.LowPrice) <= t3:
            new_stop = float(candle.LowPrice) + (self._target_tp3.Value + 13) * self._pip_size
            self._short_trailing_stop = min(self._short_trailing_stop, new_stop) if self._short_trailing_stop is not None else new_stop
            self._short_stage = 3
            return

        if self._short_stage < 2 and float(candle.LowPrice) <= t2:
            new_stop = float(candle.LowPrice) + (self._target_tp2.Value + 13) * self._pip_size
            self._short_trailing_stop = min(self._short_trailing_stop, new_stop) if self._short_trailing_stop is not None else new_stop
            self._short_stage = 2
            return

        if self._short_stage < 1 and float(candle.LowPrice) <= t1:
            new_stop = float(candle.LowPrice) + (self._target_tp1.Value + 13) * self._pip_size
            self._short_trailing_stop = min(self._short_trailing_stop, new_stop) if self._short_trailing_stop is not None else new_stop
            self._short_stage = 1
            return

        if self._short_trailing_stop is not None and float(candle.HighPrice) >= self._short_trailing_stop:
            self.BuyMarket()

    def CreateClone(self):
        return cash_machine_5min_legacy_strategy()
