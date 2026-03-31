import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import StochasticOscillator
from StockSharp.Algo.Strategies import Strategy

class kdj_expert_advisor_strategy(Strategy):
    """
    KDJ Expert Advisor: uses Stochastic Oscillator K/D crossover
    for momentum reversals with pip-based SL/TP via StartProtection.
    """

    def __init__(self):
        super(kdj_expert_advisor_strategy, self).__init__()
        self._kdj_period = self.Param("KdjPeriod", 30) \
            .SetDisplay("KDJ Length", "Lookback period for KDJ", "KDJ")
        self._smooth_d = self.Param("SmoothD", 6) \
            .SetDisplay("Smooth %D", "Smoothing for %D", "KDJ")
        self._stop_loss_pips = self.Param("StopLossPips", 250) \
            .SetDisplay("Stop Loss (pips)", "Stop distance in pips", "Risk")
        self._take_profit_pips = self.Param("TakeProfitPips", 450) \
            .SetDisplay("Take Profit (pips)", "Profit target in pips", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for KDJ", "Data")

        self._prev_k = None
        self._prev_kdc = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(kdj_expert_advisor_strategy, self).OnReseted()
        self._prev_k = None
        self._prev_kdc = None

    def _calculate_pip_size(self):
        step = 1.0
        if self.Security is not None and self.Security.PriceStep is not None:
            step = float(self.Security.PriceStep)
        if step <= 0:
            step = 1.0
        decimals = 0
        if self.Security is not None and self.Security.Decimals is not None:
            decimals = int(self.Security.Decimals)
        multiplier = 10.0 if decimals in (3, 5) else 1.0
        return step * multiplier

    def OnStarted2(self, time):
        super(kdj_expert_advisor_strategy, self).OnStarted2(time)

        pip_size = self._calculate_pip_size()
        sl_pips = self._stop_loss_pips.Value
        tp_pips = self._take_profit_pips.Value

        tp_val = Decimal(float(tp_pips) * pip_size) if tp_pips > 0 else Decimal(0)
        sl_val = Decimal(float(sl_pips) * pip_size) if sl_pips > 0 else Decimal(0)
        tp_unit = Unit(tp_val, UnitTypes.Absolute) if tp_pips > 0 else Unit()
        sl_unit = Unit(sl_val, UnitTypes.Absolute) if sl_pips > 0 else Unit()
        self.StartProtection(tp_unit, sl_unit)

        kdj = StochasticOscillator()
        kdj.K.Length = self._kdj_period.Value
        kdj.D.Length = self._smooth_d.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(kdj, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, kdj)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, kdj_value):
        if candle.State != CandleStates.Finished:
            return

        k = kdj_value.K
        d = kdj_value.D
        if k is None or d is None:
            return

        k = float(k)
        d = float(d)
        kdc = k - d

        buy_signal = False
        sell_signal = False

        if self._prev_kdc is not None:
            if self._prev_kdc < 0 and kdc > 0:
                buy_signal = True
            if self._prev_kdc > 0 and kdc < 0:
                sell_signal = True

        if self._prev_k is not None:
            if kdc > 0 and self._prev_k < k:
                buy_signal = True
            if kdc < 0 and self._prev_k > k:
                sell_signal = True

        pos = float(self.Position)
        if abs(pos) < 0.0001:
            if buy_signal:
                self.BuyMarket()
            elif sell_signal:
                self.SellMarket()

        self._prev_k = k
        self._prev_kdc = kdc

    def CreateClone(self):
        return kdj_expert_advisor_strategy()
