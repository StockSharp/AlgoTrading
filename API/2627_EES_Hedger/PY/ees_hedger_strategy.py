import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class ees_hedger_strategy(Strategy):
    """Hedge strategy with trailing stop management."""

    def __init__(self):
        super(ees_hedger_strategy, self).__init__()

        self._hedge_volume = self.Param("HedgeVolume", 0.1) \
            .SetDisplay("Hedge Volume", "Volume used for hedge orders", "General")
        self._stop_loss_pips = self.Param("StopLossPips", 50) \
            .SetDisplay("Stop Loss (pips)", "Stop-loss distance per hedge", "Risk Management")
        self._take_profit_pips = self.Param("TakeProfitPips", 50) \
            .SetDisplay("Take Profit (pips)", "Take-profit distance per hedge", "Risk Management")
        self._trailing_stop_pips = self.Param("TrailingStopPips", 25) \
            .SetDisplay("Trailing Stop (pips)", "Trailing stop distance", "Risk Management")
        self._trailing_step_pips = self.Param("TrailingStepPips", 5) \
            .SetDisplay("Trailing Step (pips)", "Minimum trailing stop increment", "Risk Management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Time frame for processing", "General")

        self._entry_price = 0.0
        self._stop_price = None
        self._pip_size = 1.0

    @property
    def HedgeVolume(self):
        return self._hedge_volume.Value
    @property
    def StopLossPips(self):
        return self._stop_loss_pips.Value
    @property
    def TakeProfitPips(self):
        return self._take_profit_pips.Value
    @property
    def TrailingStopPips(self):
        return self._trailing_stop_pips.Value
    @property
    def TrailingStepPips(self):
        return self._trailing_step_pips.Value
    @property
    def CandleType(self):
        return self._candle_type.Value

    def _calc_pip_size(self):
        sec = self.Security
        if sec is None or sec.PriceStep is None or float(sec.PriceStep) <= 0:
            return 1.0
        step = float(sec.PriceStep)
        decimals = sec.Decimals if sec.Decimals is not None else 0
        if decimals == 3 or decimals == 5:
            return step * 10.0
        return step

    def OnStarted(self, time):
        super(ees_hedger_strategy, self).OnStarted(time)
        self._pip_size = self._calc_pip_size()

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.process_candle).Start()

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        price = float(candle.ClosePrice)
        if price <= 0:
            return

        # Entry: if no position, open buy
        if self.Position == 0 and self._entry_price == 0:
            self.BuyMarket()
            self._entry_price = price
            self._stop_price = None
            return

        if self.Position != 0 and self._entry_price == 0:
            self._entry_price = price

        # Stop loss check
        if self.Position != 0 and self.StopLossPips > 0 and self._pip_size > 0:
            stop_dist = self.StopLossPips * self._pip_size
            if self.Position > 0 and price <= self._entry_price - stop_dist:
                self.SellMarket()
                self._entry_price = 0.0
                self._stop_price = None
                return
            elif self.Position < 0 and price >= self._entry_price + stop_dist:
                self.BuyMarket()
                self._entry_price = 0.0
                self._stop_price = None
                return

        # Take profit check
        if self.Position != 0 and self.TakeProfitPips > 0 and self._pip_size > 0:
            take_dist = self.TakeProfitPips * self._pip_size
            if self.Position > 0 and price >= self._entry_price + take_dist:
                self.SellMarket()
                self._entry_price = 0.0
                self._stop_price = None
                return
            elif self.Position < 0 and price <= self._entry_price - take_dist:
                self.BuyMarket()
                self._entry_price = 0.0
                self._stop_price = None
                return

        # Trailing stop
        if self.Position != 0 and self.TrailingStopPips > 0 and self._pip_size > 0:
            trail_dist = self.TrailingStopPips * self._pip_size
            trail_step = self.TrailingStepPips * self._pip_size

            if self.Position > 0:
                new_stop = price - trail_dist
                if new_stop > self._entry_price and (self._stop_price is None or new_stop > self._stop_price + trail_step):
                    self._stop_price = new_stop
                if self._stop_price is not None and price <= self._stop_price:
                    self.SellMarket()
                    self._entry_price = 0.0
                    self._stop_price = None

            elif self.Position < 0:
                new_stop = price + trail_dist
                if new_stop < self._entry_price and (self._stop_price is None or new_stop < self._stop_price - trail_step):
                    self._stop_price = new_stop
                if self._stop_price is not None and price >= self._stop_price:
                    self.BuyMarket()
                    self._entry_price = 0.0
                    self._stop_price = None

    def OnReseted(self):
        super(ees_hedger_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._stop_price = None
        self._pip_size = 1.0

    def CreateClone(self):
        return ees_hedger_strategy()
