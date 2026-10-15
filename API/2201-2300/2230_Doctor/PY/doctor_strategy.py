import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import WeightedMovingAverage, RelativeStrengthIndex, ParabolicSar
from StockSharp.Algo.Strategies import Strategy

class doctor_strategy(Strategy):
    """
    Doctor strategy ported from MQL. Combines WMA slope, MA position, RSI and PSAR.
    """

    def __init__(self):
        super(doctor_strategy, self).__init__()
        self._stop_loss_ticks = self.Param("StopLossTicks", 70) \
            .SetDisplay("Stop Loss", "Stop-loss distance in ticks", "Risk")
        self._take_profit_ticks = self.Param("TakeProfitTicks", 40) \
            .SetDisplay("Take Profit", "Take-profit distance in ticks", "Risk")
        self._trailing_stop = self.Param("TrailingStop", True) \
            .SetDisplay("Trailing Stop", "Use trailing stop", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for candles", "General")

        self._wma40 = [0.0, 0.0]
        self._wma400 = [0.0, 0.0, 0.0, 0.0]
        self._high = [0.0, 0.0, 0.0, 0.0]
        self._low = [0.0, 0.0, 0.0, 0.0]
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._take_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(doctor_strategy, self).OnReseted()
        self._wma40 = [0.0, 0.0]
        self._wma400 = [0.0, 0.0, 0.0, 0.0]
        self._high = [0.0, 0.0, 0.0, 0.0]
        self._low = [0.0, 0.0, 0.0, 0.0]
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._take_price = 0.0

    def OnStarted2(self, time):
        super(doctor_strategy, self).OnStarted2(time)

        wma_slope = WeightedMovingAverage()
        wma_slope.Length = 10
        wma_trend = WeightedMovingAverage()
        wma_trend.Length = 50
        rsi14 = RelativeStrengthIndex()
        rsi14.Length = 14
        rsi5 = RelativeStrengthIndex()
        rsi5.Length = 5
        psar = ParabolicSar()

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(wma_slope, wma_trend, rsi14, rsi5, psar, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, wma_slope)
            self.DrawIndicator(area, wma_trend)
            self.DrawIndicator(area, psar)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, wma40, wma400, rsi14, rsi5, psar):
        if candle.State != CandleStates.Finished:
            return

        wma40 = float(wma40)
        wma400 = float(wma400)
        rsi14 = float(rsi14)
        rsi5 = float(rsi5)
        psar = float(psar)

        self._wma40[1] = self._wma40[0]
        self._wma40[0] = wma40

        for i in range(3, 0, -1):
            self._wma400[i] = self._wma400[i - 1]
            self._high[i] = self._high[i - 1]
            self._low[i] = self._low[i - 1]

        self._wma400[0] = wma400
        self._high[0] = float(candle.HighPrice)
        self._low[0] = float(candle.LowPrice)

        if self._wma40[1] == 0.0 or self._wma400[3] == 0.0:
            return

        slope = 2 if self._wma40[0] > self._wma40[1] else 1

        ma_below = (self._wma400[1] < self._low[1] and
                    self._wma400[2] < self._low[2] and
                    self._wma400[3] < self._low[3])
        ma_above = (self._wma400[1] > self._high[1] and
                    self._wma400[2] > self._high[2] and
                    self._wma400[3] > self._high[3])
        ma_linear = 2 if ma_above else (1 if ma_below else 0)

        rsi_state = 0
        if rsi14 < 50 and rsi5 > rsi14:
            rsi_state = 1
        elif rsi14 > 50 and rsi5 < rsi14:
            rsi_state = 2

        psar_state = 0
        if psar <= float(candle.LowPrice):
            psar_state = 1
        elif psar >= float(candle.HighPrice):
            psar_state = 2

        step = 1.0
        if self.Security is not None and self.Security.PriceStep is not None:
            step = float(self.Security.PriceStep)
        if step <= 0:
            step = 1.0
        stop_distance = self._stop_loss_ticks.Value * step
        take_distance = self._take_profit_ticks.Value * step

        if self.Position > 0 and slope == 1 and (ma_linear == 1 or rsi_state == 1 or psar_state == 2):
            self.SellMarket()
        elif self.Position < 0 and slope == 2 and (ma_linear == 2 or rsi_state == 2 or psar_state == 1):
            self.BuyMarket()

        close = float(candle.ClosePrice)
        if self.Position > 0:
            if self._trailing_stop.Value and close - self._entry_price > stop_distance / 2.0:
                self._stop_price = max(self._stop_price, close - stop_distance)
            if float(candle.LowPrice) <= self._stop_price or float(candle.HighPrice) >= self._take_price:
                self.SellMarket()
        elif self.Position < 0:
            if self._trailing_stop.Value and self._entry_price - close > stop_distance / 2.0:
                self._stop_price = min(self._stop_price, close + stop_distance)
            if float(candle.HighPrice) >= self._stop_price or float(candle.LowPrice) <= self._take_price:
                self.BuyMarket()

        if slope == 2 and (ma_linear == 2 or rsi_state == 2) and self.Position <= 0:
            self._entry_price = close
            self._stop_price = self._entry_price - stop_distance
            self._take_price = self._entry_price + take_distance
            self.BuyMarket()
        elif slope == 1 and (ma_linear == 1 or rsi_state == 1) and self.Position >= 0:
            self._entry_price = close
            self._stop_price = self._entry_price + stop_distance
            self._take_price = self._entry_price - take_distance
            self.SellMarket()

    def CreateClone(self):
        return doctor_strategy()
