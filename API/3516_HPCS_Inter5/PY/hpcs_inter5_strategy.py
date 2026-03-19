import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy

class hpcs_inter5_strategy(Strategy):
    """
    HPCS Inter5: momentum strategy comparing close from 5 bars ago
    vs current close. Buys when older close exceeds recent by threshold.
    Sells on reverse. Uses StartProtection for SL/TP.
    """

    def __init__(self):
        super(hpcs_inter5_strategy, self).__init__()
        self._stop_loss_pips = self.Param("StopLossPips", 20) \
            .SetDisplay("Stop Loss (pips)", "Stop-loss distance in pips", "Risk Management")
        self._take_profit_pips = self.Param("TakeProfitPips", 20) \
            .SetDisplay("Take Profit (pips)", "Take-profit distance in pips", "Risk Management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(120))) \
            .SetDisplay("Candle Type", "Candle type for close comparison", "General")

        self._recent_closes = [None] * 6
        self._pip_size = 0.0
        self._was_long_signal = False
        self._has_signal = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(hpcs_inter5_strategy, self).OnReseted()
        self._recent_closes = [None] * 6
        self._pip_size = 0.0
        self._was_long_signal = False
        self._has_signal = False

    def OnStarted(self, time):
        super(hpcs_inter5_strategy, self).OnStarted(time)

        step = 0.01
        if self.Security is not None and self.Security.PriceStep is not None:
            step = float(self.Security.PriceStep)
        if step <= 0:
            step = 0.01

        decimals = 0
        if self.Security is not None and self.Security.Decimals is not None:
            decimals = int(self.Security.Decimals)
        pip_factor = 10.0 if decimals in (3, 5) else 1.0
        self._pip_size = step * pip_factor

        sl_pips = self._stop_loss_pips.Value
        tp_pips = self._take_profit_pips.Value
        sl = None
        tp = None
        if sl_pips > 0 and self._pip_size > 0:
            sl = Unit(sl_pips * self._pip_size, UnitTypes.Absolute)
        if tp_pips > 0 and self._pip_size > 0:
            tp = Unit(tp_pips * self._pip_size, UnitTypes.Absolute)
        if sl is not None or tp is not None:
            self.StartProtection(tp, sl)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)

        for i in range(len(self._recent_closes) - 1, 0, -1):
            self._recent_closes[i] = self._recent_closes[i - 1]
        self._recent_closes[0] = close

        last_close = self._recent_closes[1]
        older_close = self._recent_closes[5]
        if last_close is None or older_close is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        threshold = last_close * 0.005
        long_signal = older_close - last_close > threshold
        short_signal = last_close - older_close > threshold
        crossed_long = long_signal and (not self._has_signal or not self._was_long_signal)
        crossed_short = short_signal and (not self._has_signal or self._was_long_signal)

        if crossed_long and self.Position <= 0:
            self.BuyMarket()
        elif crossed_short and self.Position >= 0:
            self.SellMarket()

        if long_signal or short_signal:
            self._was_long_signal = long_signal
            self._has_signal = True

    def CreateClone(self):
        return hpcs_inter5_strategy()
