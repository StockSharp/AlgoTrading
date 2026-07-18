import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import StochasticOscillator


class multi_stochastic_strategy(Strategy):
    """Multi stochastic crossover strategy: oversold/overbought K/D cross with SL/TP on single security."""

    def __init__(self):
        super(multi_stochastic_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Time frame applied to the symbol", "Data")
        self._stochastic_length = self.Param("StochasticLength", 5) \
            .SetGreaterThanZero() \
            .SetDisplay("Stochastic Length", "Base period for Stochastic", "Indicators")
        self._stochastic_k_period = self.Param("StochasticKPeriod", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("%K Period", "Smoothing period for %K", "Indicators")
        self._stochastic_d_period = self.Param("StochasticDPeriod", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("%D Period", "Smoothing period for %D", "Indicators")
        self._oversold_level = self.Param("OversoldLevel", 20.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Oversold Level", "Threshold for long entries", "Signals")
        self._overbought_level = self.Param("OverboughtLevel", 80.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Overbought Level", "Threshold for short entries", "Signals")
        self._stop_loss_pips = self.Param("StopLossPips", 50.0) \
            .SetDisplay("Stop Loss (pips)", "Stop-loss distance expressed in pips", "Risk")
        self._take_profit_pips = self.Param("TakeProfitPips", 10.0) \
            .SetDisplay("Take Profit (pips)", "Take-profit distance expressed in pips", "Risk")

        self._prev_k = None
        self._prev_d = None
        self._stop_price = None
        self._take_price = None
        self._pip_value = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value
    @property
    def StochasticLength(self):
        return int(self._stochastic_length.Value)
    @property
    def StochasticKPeriod(self):
        return int(self._stochastic_k_period.Value)
    @property
    def StochasticDPeriod(self):
        return int(self._stochastic_d_period.Value)
    @property
    def OversoldLevel(self):
        return float(self._oversold_level.Value)
    @property
    def OverboughtLevel(self):
        return float(self._overbought_level.Value)
    @property
    def StopLossPips(self):
        return float(self._stop_loss_pips.Value)
    @property
    def TakeProfitPips(self):
        return float(self._take_profit_pips.Value)

    def _calc_pip_value(self):
        sec = self.Security
        step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None and float(sec.PriceStep) > 0 else 0.0
        if step <= 0:
            return 0.0
        decimals = int(sec.Decimals) if sec is not None and sec.Decimals is not None else 0
        multiplier = 10.0 if (decimals == 3 or decimals == 5) else 1.0
        return step * multiplier

    def OnStarted2(self, time):
        super(multi_stochastic_strategy, self).OnStarted2(time)

        self._prev_k = None
        self._prev_d = None
        self._stop_price = None
        self._take_price = None
        self._pip_value = self._calc_pip_value()

        self._stoch = StochasticOscillator()
        self._stoch.K.Length = self.StochasticKPeriod
        self._stoch.D.Length = self.StochasticDPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(self._stoch, self.process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._stoch)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, stoch_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._stoch.IsFormed:
            return

        current_k = stoch_value.K
        current_d = stoch_value.D
        if current_k is None or current_d is None:
            return

        k = float(current_k)
        d = float(current_d)

        # Manage risk
        if self._manage_risk(candle):
            self._prev_k = k
            self._prev_d = d
            return

        if self._prev_k is None or self._prev_d is None:
            self._prev_k = k
            self._prev_d = d
            return

        prev_k = self._prev_k
        prev_d = self._prev_d

        long_signal = k < self.OversoldLevel and prev_k < prev_d and k > d
        short_signal = k > self.OverboughtLevel and prev_k > prev_d and k < d

        close = float(candle.ClosePrice)

        if self.Position == 0:
            if long_signal:
                self.BuyMarket()
                self._stop_price = close - self.StopLossPips * self._pip_value if self.StopLossPips > 0 and self._pip_value > 0 else None
                self._take_price = close + self.TakeProfitPips * self._pip_value if self.TakeProfitPips > 0 and self._pip_value > 0 else None
            elif short_signal:
                self.SellMarket()
                self._stop_price = close + self.StopLossPips * self._pip_value if self.StopLossPips > 0 and self._pip_value > 0 else None
                self._take_price = close - self.TakeProfitPips * self._pip_value if self.TakeProfitPips > 0 and self._pip_value > 0 else None

        self._prev_k = k
        self._prev_d = d

    def _manage_risk(self, candle):
        h = float(candle.HighPrice)
        lo = float(candle.LowPrice)

        if self.Position > 0:
            if self._stop_price is not None and lo <= self._stop_price:
                self.SellMarket()
                self._stop_price = None
                self._take_price = None
                return True
            if self._take_price is not None and h >= self._take_price:
                self.SellMarket()
                self._stop_price = None
                self._take_price = None
                return True
        elif self.Position < 0:
            if self._stop_price is not None and h >= self._stop_price:
                self.BuyMarket()
                self._stop_price = None
                self._take_price = None
                return True
            if self._take_price is not None and lo <= self._take_price:
                self.BuyMarket()
                self._stop_price = None
                self._take_price = None
                return True
        else:
            self._stop_price = None
            self._take_price = None

        return False

    def OnReseted(self):
        super(multi_stochastic_strategy, self).OnReseted()
        self._prev_k = None
        self._prev_d = None
        self._stop_price = None
        self._take_price = None
        self._pip_value = 0.0

    def CreateClone(self):
        return multi_stochastic_strategy()
