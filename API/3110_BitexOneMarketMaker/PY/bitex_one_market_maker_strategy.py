import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class bitex_one_market_maker_strategy(Strategy):
    def __init__(self):
        super(bitex_one_market_maker_strategy, self).__init__()

        self._sma_period = self.Param("SmaPeriod", 100) \
            .SetDisplay("SMA Period", "SMA period for mean reversion", "Indicator")
        self._stop_loss_points = self.Param("StopLossPoints", 200) \
            .SetDisplay("Stop Loss", "Stop-loss in price steps", "Risk")
        self._take_profit_points = self.Param("TakeProfitPoints", 400) \
            .SetDisplay("Take Profit", "Take-profit in price steps", "Risk")

        self._sma = None
        self._entry_price = 0.0
        self._cooldown = 0

    @property
    def sma_period(self):
        return self._sma_period.Value

    @property
    def stop_loss_points(self):
        return self._stop_loss_points.Value

    @property
    def take_profit_points(self):
        return self._take_profit_points.Value

    def OnReseted(self):
        super(bitex_one_market_maker_strategy, self).OnReseted()
        self._sma = None
        self._entry_price = 0.0
        self._cooldown = 0

    def OnStarted2(self, time):
        super(bitex_one_market_maker_strategy, self).OnStarted2(time)

        self._sma = SimpleMovingAverage()
        self._sma.Length = self.sma_period

        subscription = self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        subscription.Bind(self._sma, self._process_candle)
        subscription.Start()

    def _process_candle(self, candle, sma_value):
        if candle.State != CandleStates.Finished:
            return

        sma_val = float(sma_value)

        if not self._sma.IsFormed:
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        close = float(candle.ClosePrice)
        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0

        # Check SL/TP
        if self.Position > 0 and self._entry_price > 0:
            if self.stop_loss_points > 0 and close <= self._entry_price - self.stop_loss_points * step:
                self.SellMarket()
                self._entry_price = 0.0
                self._cooldown = 100
                return
            if self.take_profit_points > 0 and close >= self._entry_price + self.take_profit_points * step:
                self.SellMarket()
                self._entry_price = 0.0
                self._cooldown = 100
                return
        elif self.Position < 0 and self._entry_price > 0:
            if self.stop_loss_points > 0 and close >= self._entry_price + self.stop_loss_points * step:
                self.BuyMarket()
                self._entry_price = 0.0
                self._cooldown = 100
                return
            if self.take_profit_points > 0 and close <= self._entry_price - self.take_profit_points * step:
                self.BuyMarket()
                self._entry_price = 0.0
                self._cooldown = 100
                return

        # Mean reversion around SMA
        deviation = sma_val * 0.008

        if close < sma_val - deviation and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._entry_price = close
            self._cooldown = 100
        elif close > sma_val + deviation and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._entry_price = close
            self._cooldown = 100

    def CreateClone(self):
        return bitex_one_market_maker_strategy()
