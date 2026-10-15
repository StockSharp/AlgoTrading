import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import (MovingAverageConvergenceDivergenceSignal,
    AwesomeOscillator, WilliamsR, StochasticOscillator)
from StockSharp.Algo.Strategies import Strategy

class multi_indicator_optimizer_strategy(Strategy):
    """
    Multi-indicator voting: MACD, AO, OsMA, Williams %R, Stochastic weighted scoring.
    """

    def __init__(self):
        super(multi_indicator_optimizer_strategy, self).__init__()
        self._macd_weight = self.Param("MacdWeight", 1.0).SetDisplay("MACD Weight", "MACD vote weight", "Weights")
        self._ao_weight = self.Param("AoWeight", 1.0).SetDisplay("AO Weight", "AO vote weight", "Weights")
        self._osma_weight = self.Param("OsmaWeight", 1.0).SetDisplay("OsMA Weight", "OsMA vote weight", "Weights")
        self._williams_weight = self.Param("WilliamsWeight", 1.0).SetDisplay("WPR Weight", "Williams vote weight", "Weights")
        self._stoch_weight = self.Param("StochasticWeight", 1.0).SetDisplay("Stoch Weight", "Stoch vote weight", "Weights")
        self._entry_threshold = self.Param("EntryThreshold", 0.5).SetDisplay("Entry Threshold", "Min score to enter", "Trading")
        self._exit_threshold = self.Param("ExitThreshold", 0.1).SetDisplay("Exit Threshold", "Score to exit", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Candles", "General")

        self._prev_macd_main = None
        self._prev_macd_signal = None
        self._prev_osma = None
        self._prev_ao = None
        self._prev_prev_ao = None
        self._prev_williams = None
        self._prev_prev_williams = None
        self._prev_stoch_k = None
        self._prev_prev_stoch_k = None
        self._prev_stoch_signal = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(multi_indicator_optimizer_strategy, self).OnReseted()
        self._prev_macd_main = None
        self._prev_macd_signal = None
        self._prev_osma = None
        self._prev_ao = None
        self._prev_prev_ao = None
        self._prev_williams = None
        self._prev_prev_williams = None
        self._prev_stoch_k = None
        self._prev_prev_stoch_k = None
        self._prev_stoch_signal = None

    def OnStarted2(self, time):
        super(multi_indicator_optimizer_strategy, self).OnStarted2(time)
        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = 12
        macd.Macd.LongMa.Length = 26
        macd.SignalMa.Length = 9
        osma = MovingAverageConvergenceDivergenceSignal()
        osma.Macd.ShortMa.Length = 12
        osma.Macd.LongMa.Length = 26
        osma.SignalMa.Length = 9
        ao = AwesomeOscillator()
        ao.ShortMa.Length = 5
        ao.LongMa.Length = 34
        williams = WilliamsR()
        williams.Length = 14
        stoch = StochasticOscillator()
        stoch.K.Length = 5
        stoch.D.Length = 3
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(macd, osma, ao, williams, stoch, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, macd_value, osma_value, ao_value, williams_value, stoch_value):
        if candle.State != CandleStates.Finished:
            return
        macd_typed = macd_value
        cur_macd = macd_typed.Macd
        cur_macd_sig = macd_typed.Signal
        if cur_macd is None or cur_macd_sig is None:
            return
        cur_macd = float(cur_macd)
        cur_macd_sig = float(cur_macd_sig)
        osma_typed = osma_value
        osma_m = osma_typed.Macd
        osma_s = osma_typed.Signal
        cur_osma = float(osma_m) - float(osma_s) if osma_m is not None and osma_s is not None else None
        if cur_osma is None:
            return
        cur_ao = float(ao_value)
        cur_williams = float(williams_value)
        stoch_typed = stoch_value
        cur_k = stoch_typed.K
        cur_d = stoch_typed.D
        if cur_k is None or cur_d is None:
            return
        cur_k = float(cur_k)
        cur_d = float(cur_d)
        signal = 0.0
        macd_w = float(self._macd_weight.Value)
        ao_w = float(self._ao_weight.Value)
        osma_w = float(self._osma_weight.Value)
        wpr_w = float(self._williams_weight.Value)
        stoch_w = float(self._stoch_weight.Value)
        if self._prev_macd_main is not None and self._prev_macd_signal is not None:
            ms = 1.0 if self._prev_macd_main > 0 else (-1.0 if self._prev_macd_main < 0 else 0.0)
            cs = 1.0 if self._prev_macd_main > self._prev_macd_signal else (-1.0 if self._prev_macd_main < self._prev_macd_signal else 0.0)
            signal += (ms + cs) / 2.0 * macd_w
        if self._prev_ao is not None:
            ds = 1.0 if self._prev_ao > 0 else (-1.0 if self._prev_ao < 0 else 0.0)
            mom = 0.0
            if self._prev_prev_ao is not None:
                mom = 1.0 if self._prev_ao > self._prev_prev_ao else (-1.0 if self._prev_ao < self._prev_prev_ao else 0.0)
            signal += (ds + mom) / 2.0 * ao_w
        if self._prev_osma is not None:
            os = 1.0 if self._prev_osma > 0 else (-1.0 if self._prev_osma < 0 else 0.0)
            signal += os * osma_w
        if self._prev_williams is not None and self._prev_prev_williams is not None:
            ws = 0.0
            if self._prev_williams > -80 and self._prev_prev_williams <= -80:
                ws = 1.0
            elif self._prev_williams < -20 and self._prev_prev_williams >= -20:
                ws = -1.0
            signal += ws * wpr_w
        if self._prev_stoch_k is not None and self._prev_stoch_signal is not None:
            ss1 = 0.0
            if self._prev_prev_stoch_k is not None:
                if self._prev_stoch_k > 20 and self._prev_prev_stoch_k <= 20:
                    ss1 = 1.0
                elif self._prev_stoch_k < 80 and self._prev_prev_stoch_k >= 80:
                    ss1 = -1.0
            ss2 = 1.0 if self._prev_stoch_k > self._prev_stoch_signal else (-1.0 if self._prev_stoch_k < self._prev_stoch_signal else 0.0)
            signal += (ss1 + ss2) / 2.0 * stoch_w
        entry_th = float(self._entry_threshold.Value)
        exit_th = float(self._exit_threshold.Value)
        if signal >= entry_th and self.Position <= 0:
            self.BuyMarket()
        elif signal <= -entry_th and self.Position >= 0:
            self.SellMarket()
        elif abs(signal) <= exit_th and self.Position != 0:
            if self.Position > 0:
                self.SellMarket()
            else:
                self.BuyMarket()
        self._prev_macd_main = cur_macd
        self._prev_macd_signal = cur_macd_sig
        self._prev_osma = cur_osma
        self._prev_prev_ao = self._prev_ao
        self._prev_ao = cur_ao
        self._prev_prev_williams = self._prev_williams
        self._prev_williams = cur_williams
        self._prev_prev_stoch_k = self._prev_stoch_k
        self._prev_stoch_k = cur_k
        self._prev_stoch_signal = cur_d

    def CreateClone(self):
        return multi_indicator_optimizer_strategy()
