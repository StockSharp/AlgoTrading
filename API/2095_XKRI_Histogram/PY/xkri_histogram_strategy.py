import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class xkri_histogram_strategy(Strategy):
    def __init__(self):
        super(xkri_histogram_strategy, self).__init__()
        self._kri_period = self.Param("KriPeriod", 20) \
            .SetDisplay("KRI Period", "Base moving average period", "Indicators")
        self._smooth_period = self.Param("SmoothPeriod", 7) \
            .SetDisplay("Smooth Period", "EMA smoothing period", "Indicators")
        self._take_profit_pct = self.Param("TakeProfitPct", 3.0) \
            .SetDisplay("Take Profit %", "Take profit percentage", "Protection")
        self._stop_loss_pct = self.Param("StopLossPct", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Protection")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Time frame for candles", "General")
        self._smooth = None
        self._last = 0.0
        self._prev = 0.0
        self._prev2 = 0.0
        self._value_count = 0

    @property
    def kri_period(self):
        return self._kri_period.Value
    @property
    def smooth_period(self):
        return self._smooth_period.Value
    @property
    def take_profit_pct(self):
        return self._take_profit_pct.Value
    @property
    def stop_loss_pct(self):
        return self._stop_loss_pct.Value
    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(xkri_histogram_strategy, self).OnReseted()
        self._smooth = None
        self._last = 0.0
        self._prev = 0.0
        self._prev2 = 0.0
        self._value_count = 0

    def OnStarted(self, time):
        super(xkri_histogram_strategy, self).OnStarted(time)
        ma = ExponentialMovingAverage()
        ma.Length = self.kri_period
        self._smooth = ExponentialMovingAverage()
        self._smooth.Length = self.smooth_period
        self.Indicators.Add(self._smooth)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ma, self.on_candle).Start()
        self.StartProtection(
            takeProfit=Unit(self.take_profit_pct, UnitTypes.Percent),
            stopLoss=Unit(self.stop_loss_pct, UnitTypes.Percent),
            useMarketOrders=True)
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._smooth)
            self.DrawOwnTrades(area)

    def on_candle(self, candle, ma_value):
        if candle.State != CandleStates.Finished:
            return
        ma_value = float(ma_value)
        if ma_value == 0:
            return
        kri = 100.0 * (float(candle.ClosePrice) - ma_value) / ma_value
        smooth_result = self._smooth.Process(kri, candle.OpenTime, True)
        if not smooth_result.IsFormed:
            return
        smooth = float(smooth_result.ToDecimal())

        self._prev2 = self._prev
        self._prev = self._last
        self._last = smooth
        self._value_count += 1

        if self._value_count < 3:
            return

        long_signal = self._prev < self._prev2 and self._last > self._prev and self.Position <= 0
        short_signal = self._prev > self._prev2 and self._last < self._prev and self.Position >= 0

        if long_signal:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif short_signal:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

    def CreateClone(self):
        return xkri_histogram_strategy()
