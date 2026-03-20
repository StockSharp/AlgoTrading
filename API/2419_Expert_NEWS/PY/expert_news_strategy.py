import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy


class expert_news_strategy(Strategy):
    def __init__(self):
        super(expert_news_strategy, self).__init__()

        self._entry_offset = self.Param("EntryOffset", 200.0)
        self._stop_loss = self.Param("StopLoss", 1000.0)
        self._take_profit = self.Param("TakeProfit", 2000.0)
        self._lookback_period = self.Param("LookbackPeriod", 20)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))

        self._highs = []
        self._lows = []
        self._entry_price = 0.0
        self._last_signal = 0

    @property
    def EntryOffset(self):
        return self._entry_offset.Value

    @EntryOffset.setter
    def EntryOffset(self, value):
        self._entry_offset.Value = value

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
    def LookbackPeriod(self):
        return self._lookback_period.Value

    @LookbackPeriod.setter
    def LookbackPeriod(self, value):
        self._lookback_period.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(expert_news_strategy, self).OnStarted(time)

        self._highs = []
        self._lows = []
        self._entry_price = 0.0
        self._last_signal = 0

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

        self.StartProtection(
            Unit(self.TakeProfit, UnitTypes.Absolute),
            Unit(self.StopLoss, UnitTypes.Absolute))

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        h = float(candle.HighPrice)
        l = float(candle.LowPrice)
        close = float(candle.ClosePrice)

        self._highs.append(h)
        self._lows.append(l)

        period = int(self.LookbackPeriod)

        if len(self._highs) > period + 1:
            self._highs.pop(0)
        if len(self._lows) > period + 1:
            self._lows.pop(0)

        if len(self._highs) <= period:
            return

        range_high = -1e18
        range_low = 1e18
        for i in range(len(self._highs) - 1):
            if self._highs[i] > range_high:
                range_high = self._highs[i]
            if self._lows[i] < range_low:
                range_low = self._lows[i]

        offset = float(self.EntryOffset)
        breakout_up = close > range_high + offset
        breakout_down = close < range_low - offset

        if breakout_up and self._last_signal != 1 and self.Position <= 0:
            self.BuyMarket()
            self._entry_price = close
            self._last_signal = 1
        elif breakout_down and self._last_signal != -1 and self.Position >= 0:
            self.SellMarket()
            self._entry_price = close
            self._last_signal = -1
        elif not breakout_up and not breakout_down:
            self._last_signal = 0

    def OnReseted(self):
        super(expert_news_strategy, self).OnReseted()
        self._highs = []
        self._lows = []
        self._entry_price = 0.0
        self._last_signal = 0

    def CreateClone(self):
        return expert_news_strategy()
