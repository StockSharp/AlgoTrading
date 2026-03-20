import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math, DateTime
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import KeltnerChannels, KeltnerChannelMiddle, KeltnerChannelBand, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class exp_xma_range_bands_strategy(Strategy):

    def __init__(self):
        super(exp_xma_range_bands_strategy, self).__init__()

        self._ma_length = self.Param("MaLength", 100) \
            .SetDisplay("MA Length", "EMA period for channel center", "Indicator")
        self._range_length = self.Param("RangeLength", 20) \
            .SetDisplay("ATR Length", "ATR period for channel width", "Indicator")
        self._deviation = self.Param("Deviation", 2.0) \
            .SetDisplay("Deviation", "ATR multiplier for channel width", "Indicator")
        self._stop_loss = self.Param("StopLoss", 1000.0) \
            .SetDisplay("Stop Loss", "Stop loss in points", "Risk")
        self._take_profit = self.Param("TakeProfit", 2000.0) \
            .SetDisplay("Take Profit", "Take profit in points", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._prev_upper = 0.0
        self._prev_lower = 0.0
        self._prev_close = 0.0
        self._entry_price = 0.0
        self._is_first = True
        self._last_signal_time = DateTime.MinValue
        self._signal_cooldown = TimeSpan.FromHours(8)

    @property
    def MaLength(self):
        return self._ma_length.Value

    @MaLength.setter
    def MaLength(self, value):
        self._ma_length.Value = value

    @property
    def RangeLength(self):
        return self._range_length.Value

    @RangeLength.setter
    def RangeLength(self, value):
        self._range_length.Value = value

    @property
    def Deviation(self):
        return self._deviation.Value

    @Deviation.setter
    def Deviation(self, value):
        self._deviation.Value = value

    @property
    def StopLoss(self):
        return self._stop_loss.Value

    @StopLoss.setter
    def StopLoss(self, value):
        self._stop_loss.Value = value

    @property
    def TakeProfit(self):
        return self._take_profit.Value

    @TakeProfit.setter
    def TakeProfit(self, value):
        self._take_profit.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def _can_trade(self, candle):
        return (self._last_signal_time == DateTime.MinValue
                or candle.CloseTime >= self._last_signal_time + self._signal_cooldown)

    def OnStarted(self, time):
        super(exp_xma_range_bands_strategy, self).OnStarted(time)

        keltner = KeltnerChannels(
            KeltnerChannelMiddle(self.MaLength),
            AverageTrueRange(self.RangeLength),
            KeltnerChannelBand(self.MaLength),
            KeltnerChannelBand(self.MaLength))
        keltner.Multiplier = self.Deviation

        subscription = self.SubscribeCandles(self.CandleType)
        subscription \
            .BindEx(keltner, self.ProcessCandle) \
            .Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, keltner)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, keltner_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        upper_raw = keltner_value.Upper
        lower_raw = keltner_value.Lower
        if upper_raw is None or lower_raw is None:
            return

        upper = float(upper_raw)
        lower = float(lower_raw)
        close = float(candle.ClosePrice)

        if self._is_first:
            self._prev_upper = upper
            self._prev_lower = lower
            self._prev_close = close
            self._is_first = False
            return

        if self._prev_close > self._prev_upper:
            if close <= upper and self.Position <= 0 and self._can_trade(candle):
                volume = self.Volume + abs(self.Position) if self.Position < 0 else self.Volume
                self.BuyMarket(volume)
                self._entry_price = close
                self._last_signal_time = candle.CloseTime
        elif self._prev_close < self._prev_lower:
            if close >= lower and self.Position >= 0 and self._can_trade(candle):
                volume = self.Volume + self.Position if self.Position > 0 else self.Volume
                self.SellMarket(volume)
                self._entry_price = close
                self._last_signal_time = candle.CloseTime

        step_raw = self.Security.PriceStep
        step = float(step_raw) if step_raw is not None else 1.0
        sl = step * float(self.StopLoss)
        tp = step * float(self.TakeProfit)

        if self.Position > 0:
            if close <= self._entry_price - sl or close >= self._entry_price + tp:
                self.SellMarket(self.Position)
                self._entry_price = 0.0
        elif self.Position < 0:
            if close >= self._entry_price + sl or close <= self._entry_price - tp:
                self.BuyMarket(abs(self.Position))
                self._entry_price = 0.0

        self._prev_upper = upper
        self._prev_lower = lower
        self._prev_close = close

    def OnReseted(self):
        super(exp_xma_range_bands_strategy, self).OnReseted()
        self._prev_upper = 0.0
        self._prev_lower = 0.0
        self._prev_close = 0.0
        self._entry_price = 0.0
        self._is_first = True
        self._last_signal_time = DateTime.MinValue

    def CreateClone(self):
        return exp_xma_range_bands_strategy()
