import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import AverageDirectionalIndex
from StockSharp.Algo.Strategies import Strategy


class color_xadx_strategy(Strategy):
    def __init__(self):
        super(color_xadx_strategy, self).__init__()
        self._adx_period = self.Param("AdxPeriod", 14) \
            .SetDisplay("ADX Period", "Period for ADX calculation", "Indicators")
        self._adx_threshold = self.Param("AdxThreshold", 20.0) \
            .SetDisplay("ADX Threshold", "Minimum ADX level for trades", "Indicators")
        self._stop_loss_pct = self.Param("StopLossPct", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")
        self._take_profit_pct = self.Param("TakeProfitPct", 3.0) \
            .SetDisplay("Take Profit %", "Take profit percentage", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._prev_plus_di = None
        self._prev_minus_di = None

    @property
    def adx_period(self):
        return self._adx_period.Value

    @property
    def adx_threshold(self):
        return self._adx_threshold.Value

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
        super(color_xadx_strategy, self).OnReseted()
        self._prev_plus_di = None
        self._prev_minus_di = None

    def OnStarted2(self, time):
        super(color_xadx_strategy, self).OnStarted2(time)
        adx = AverageDirectionalIndex()
        adx.Length = self.adx_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(adx, self.process_candle).Start()
        self.StartProtection(
            takeProfit=Unit(self.take_profit_pct, UnitTypes.Percent),
            stopLoss=Unit(self.stop_loss_pct, UnitTypes.Percent),
            useMarketOrders=True)
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, adx)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, adx_value):
        if candle.State != CandleStates.Finished:
            return
        if not adx_value.IsFormed:
            return
        plus_di = adx_value.Dx.Plus
        minus_di = adx_value.Dx.Minus
        adx_main = adx_value.MovingAverage
        if plus_di is None or minus_di is None or adx_main is None:
            return
        plus_di = float(plus_di)
        minus_di = float(minus_di)
        adx_main = float(adx_main)
        if self._prev_plus_di is None or self._prev_minus_di is None:
            self._prev_plus_di = plus_di
            self._prev_minus_di = minus_di
            return

        if (plus_di > minus_di and self._prev_plus_di <= self._prev_minus_di and
                adx_main > float(self.adx_threshold) and self.Position <= 0):
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif (minus_di > plus_di and self._prev_minus_di <= self._prev_plus_di and
                adx_main > float(self.adx_threshold) and self.Position >= 0):
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_plus_di = plus_di
        self._prev_minus_di = minus_di

    def CreateClone(self):
        return color_xadx_strategy()
