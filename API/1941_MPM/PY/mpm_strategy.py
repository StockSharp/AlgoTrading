import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class mpm_strategy(Strategy):

    def __init__(self):
        super(mpm_strategy, self).__init__()

        self._progressive_candles = self.Param("ProgressiveCandles", 3) \
            .SetDisplay("Progressive Candles", "Number of consecutive candles", "Signal")
        self._progressive_size = self.Param("ProgressiveSize", 0.9) \
            .SetDisplay("Progressive Size", "Minimal body size relative to ATR", "Signal")
        self._stop_ratio = self.Param("StopRatio", 1.5) \
            .SetDisplay("Stop Ratio", "Trailing stop ratio", "Risk")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "Average True Range period", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._profit_per_lot = self.Param("ProfitPerLot", 2000.0) \
            .SetDisplay("Profit Per Lot", "Profit target per lot", "Risk")
        self._break_even_per_lot = self.Param("BreakEvenPerLot", 800.0) \
            .SetDisplay("BreakEven Per Lot", "Break even profit per lot", "Risk")
        self._loss_per_lot = self.Param("LossPerLot", 1200.0) \
            .SetDisplay("Loss Per Lot", "Maximum loss per lot", "Risk")

        self._bull_count = 0
        self._bear_count = 0
        self._entry_price = 0.0
        self._stop_price = 0.0

    @property
    def ProgressiveCandles(self):
        return self._progressive_candles.Value

    @ProgressiveCandles.setter
    def ProgressiveCandles(self, value):
        self._progressive_candles.Value = value

    @property
    def ProgressiveSize(self):
        return self._progressive_size.Value

    @ProgressiveSize.setter
    def ProgressiveSize(self, value):
        self._progressive_size.Value = value

    @property
    def StopRatio(self):
        return self._stop_ratio.Value

    @StopRatio.setter
    def StopRatio(self, value):
        self._stop_ratio.Value = value

    @property
    def AtrPeriod(self):
        return self._atr_period.Value

    @AtrPeriod.setter
    def AtrPeriod(self, value):
        self._atr_period.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def ProfitPerLot(self):
        return self._profit_per_lot.Value

    @ProfitPerLot.setter
    def ProfitPerLot(self, value):
        self._profit_per_lot.Value = value

    @property
    def BreakEvenPerLot(self):
        return self._break_even_per_lot.Value

    @BreakEvenPerLot.setter
    def BreakEvenPerLot(self, value):
        self._break_even_per_lot.Value = value

    @property
    def LossPerLot(self):
        return self._loss_per_lot.Value

    @LossPerLot.setter
    def LossPerLot(self, value):
        self._loss_per_lot.Value = value

    def OnStarted(self, time):
        super(mpm_strategy, self).OnStarted(time)

        atr = AverageTrueRange()
        atr.Length = self.AtrPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(atr, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, atr)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, atr_value):
        if candle.State != CandleStates.Finished:
            return

        atr = float(atr_value)
        body = abs(float(candle.ClosePrice) - float(candle.OpenPrice))

        if candle.ClosePrice > candle.OpenPrice and body >= atr * float(self.ProgressiveSize):
            self._bull_count += 1
            self._bear_count = 0
        elif candle.ClosePrice < candle.OpenPrice and body >= atr * float(self.ProgressiveSize):
            self._bear_count += 1
            self._bull_count = 0
        else:
            self._bull_count = 0
            self._bear_count = 0

        price = float(candle.ClosePrice)

        if self.Position <= 0 and self._bull_count >= self.ProgressiveCandles:
            self._entry_price = price
            self._stop_price = self._entry_price - atr * float(self.StopRatio)
            self.BuyMarket()
            return

        if self.Position >= 0 and self._bear_count >= self.ProgressiveCandles:
            self._entry_price = price
            self._stop_price = self._entry_price + atr * float(self.StopRatio)
            self.SellMarket()
            return

        if self.Position > 0:
            profit_per_lot = price - self._entry_price
            if (profit_per_lot >= float(self.ProfitPerLot)
                    or profit_per_lot >= float(self.BreakEvenPerLot)
                    or profit_per_lot <= -float(self.LossPerLot)):
                self.SellMarket()
                return

            new_stop = price - atr * float(self.StopRatio)
            if new_stop > self._stop_price:
                self._stop_price = new_stop

            if price <= self._stop_price:
                self.SellMarket()

        elif self.Position < 0:
            profit_per_lot = self._entry_price - price
            if (profit_per_lot >= float(self.ProfitPerLot)
                    or profit_per_lot >= float(self.BreakEvenPerLot)
                    or profit_per_lot <= -float(self.LossPerLot)):
                self.BuyMarket()
                return

            new_stop = price + atr * float(self.StopRatio)
            if new_stop < self._stop_price:
                self._stop_price = new_stop

            if price >= self._stop_price:
                self.BuyMarket()

    def OnReseted(self):
        super(mpm_strategy, self).OnReseted()
        self._bull_count = 0
        self._bear_count = 0
        self._entry_price = 0.0
        self._stop_price = 0.0

    def CreateClone(self):
        return mpm_strategy()
