import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage, WeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy


class laguerre_filter_strategy(Strategy):
    def __init__(self):
        super(laguerre_filter_strategy, self).__init__()
        self._stop_loss_pct = self.Param("StopLossPct", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")
        self._take_profit_pct = self.Param("TakeProfitPct", 3.0) \
            .SetDisplay("Take Profit %", "Take profit percentage", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._prev_fir = None
        self._prev_laguerre = None

    @property
    def stop_loss_pct(self):
        return self._stop_loss_pct.Value
    @property
    def take_profit_pct(self):
        return self._take_profit_pct.Value
    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(laguerre_filter_strategy, self).OnReseted()
        self._prev_fir = None
        self._prev_laguerre = None

    def OnStarted(self, time):
        super(laguerre_filter_strategy, self).OnStarted(time)
        laguerre = ExponentialMovingAverage()
        laguerre.Length = 10
        fir = WeightedMovingAverage()
        fir.Length = 4
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(laguerre, fir, self.process_candle).Start()
        self.StartProtection(
            takeProfit=Unit(self.take_profit_pct, UnitTypes.Percent),
            stopLoss=Unit(self.stop_loss_pct, UnitTypes.Percent),
            useMarketOrders=True)
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, laguerre)
            self.DrawIndicator(area, fir)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, laguerre_value, fir_value):
        if candle.State != CandleStates.Finished:
            return
        laguerre_value = float(laguerre_value)
        fir_value = float(fir_value)
        if self._prev_fir is None or self._prev_laguerre is None:
            self._prev_fir = fir_value
            self._prev_laguerre = laguerre_value
            return
        fir_was_above = self._prev_fir > self._prev_laguerre
        fir_is_above = fir_value > laguerre_value
        if not fir_was_above and fir_is_above and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif fir_was_above and not fir_is_above and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        self._prev_fir = fir_value
        self._prev_laguerre = laguerre_value

    def CreateClone(self):
        return laguerre_filter_strategy()
