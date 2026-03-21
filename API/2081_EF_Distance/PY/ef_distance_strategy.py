import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class ef_distance_strategy(Strategy):
    def __init__(self):
        super(ef_distance_strategy, self).__init__()
        self._sma_period = self.Param("SmaPeriod", 10) \
            .SetDisplay("SMA Period", "Period for the smoothing moving average", "Indicator")
        self._atr_period = self.Param("AtrPeriod", 20) \
            .SetDisplay("ATR Period", "Period for the volatility filter", "Indicator")
        self._atr_multiplier = self.Param("AtrMultiplier", 0.5) \
            .SetDisplay("ATR Multiplier", "Minimum ATR relative to price", "Indicator")
        self._stop_loss_pct = self.Param("StopLossPct", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")
        self._take_profit_pct = self.Param("TakeProfitPct", 4.0) \
            .SetDisplay("Take Profit %", "Take profit percentage", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles for calculations", "General")
        self._prev = None
        self._prev2 = None

    @property
    def sma_period(self):
        return self._sma_period.Value

    @property
    def atr_period(self):
        return self._atr_period.Value

    @property
    def atr_multiplier(self):
        return self._atr_multiplier.Value

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
        super(ef_distance_strategy, self).OnReseted()
        self._prev = None
        self._prev2 = None

    def OnStarted(self, time):
        super(ef_distance_strategy, self).OnStarted(time)
        ema = ExponentialMovingAverage()
        ema.Length = self.sma_period
        atr = AverageTrueRange()
        atr.Length = self.atr_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(ema, atr, self.process_candle).Start()
        self.StartProtection(
            takeProfit=Unit(self.take_profit_pct, UnitTypes.Percent),
            stopLoss=Unit(self.stop_loss_pct, UnitTypes.Percent),
            useMarketOrders=True)
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, ema_val, atr_val):
        if candle.State != CandleStates.Finished:
            return
        if not ema_val.IsFormed or not atr_val.IsFormed:
            return

        sma_value = float(ema_val.ToDecimal())
        atr_value = float(atr_val.ToDecimal())

        if self._prev is None or self._prev2 is None:
            self._prev2 = self._prev
            self._prev = sma_value
            return

        atr_enough = atr_value >= float(self.atr_multiplier) / 100.0 * float(candle.ClosePrice)

        if atr_enough:
            if self._prev < self._prev2 and sma_value > self._prev and self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
            elif self._prev > self._prev2 and sma_value < self._prev and self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()

        self._prev2 = self._prev
        self._prev = sma_value

    def CreateClone(self):
        return ef_distance_strategy()
