import clr
import random

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan

from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class coin_flipping_strategy(Strategy):
    def __init__(self):
        super(coin_flipping_strategy, self).__init__()

        self._risk_percent = self.Param("RiskPercent", 2.0)
        self._take_profit_pips = self.Param("TakeProfitPips", 5000)
        self._stop_loss_pips = self.Param("StopLossPips", 3000)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromDays(1)))

        self._rng = None
        self._price_step = 1.0
        self._tp_dist = 0.0
        self._sl_dist = 0.0
        self._entry_price = 0.0
        self._stop_price = None
        self._take_price = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def RiskPercent(self):
        return self._risk_percent.Value

    @property
    def TakeProfitPips(self):
        return self._take_profit_pips.Value

    @property
    def StopLossPips(self):
        return self._stop_loss_pips.Value

    def OnStarted2(self, time):
        super(coin_flipping_strategy, self).OnStarted2(time)

        self._rng = random.Random()

        sec = self.Security
        self._price_step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 1.0
        if self._price_step <= 0:
            self._price_step = 1.0

        self._tp_dist = self.TakeProfitPips * self._price_step
        self._sl_dist = self.StopLossPips * self._price_step

        self._entry_price = 0.0
        self._stop_price = None
        self._take_price = None

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)

        if self.Position > 0:
            if self._stop_price is not None and low <= self._stop_price:
                self.SellMarket()
                self._reset_targets()
            elif self._take_price is not None and high >= self._take_price:
                self.SellMarket()
                self._reset_targets()
        elif self.Position < 0:
            if self._stop_price is not None and high >= self._stop_price:
                self.BuyMarket()
                self._reset_targets()
            elif self._take_price is not None and low <= self._take_price:
                self.BuyMarket()
                self._reset_targets()

        if self.Position != 0:
            return

        if self._rng is None:
            return

        if close <= 0:
            return

        is_buy = self._rng.randint(0, 1) == 0

        if is_buy:
            self.BuyMarket()
            self._entry_price = close
            self._stop_price = close - self._sl_dist if self._sl_dist > 0 else None
            self._take_price = close + self._tp_dist if self._tp_dist > 0 else None
        else:
            self.SellMarket()
            self._entry_price = close
            self._stop_price = close + self._sl_dist if self._sl_dist > 0 else None
            self._take_price = close - self._tp_dist if self._tp_dist > 0 else None

    def _reset_targets(self):
        self._entry_price = 0.0
        self._stop_price = None
        self._take_price = None

    def OnReseted(self):
        super(coin_flipping_strategy, self).OnReseted()
        self._rng = None
        self._price_step = 1.0
        self._tp_dist = 0.0
        self._sl_dist = 0.0
        self._entry_price = 0.0
        self._stop_price = None
        self._take_price = None

    def CreateClone(self):
        return coin_flipping_strategy()
