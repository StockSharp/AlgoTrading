import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import RelativeStrengthIndex, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class euro_surge_simplified_strategy(Strategy):
    def __init__(self):
        super(euro_surge_simplified_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))
        self._ma_period = self.Param("MaPeriod", 52)
        self._rsi_period = self.Param("RsiPeriod", 13)
        self._rsi_buy_level = self.Param("RsiBuyLevel", 50.0)
        self._rsi_sell_level = self.Param("RsiSellLevel", 50.0)
        self._min_trade_interval_minutes = self.Param("MinTradeIntervalMinutes", 600)

        self._last_trade_time = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def MaPeriod(self):
        return self._ma_period.Value

    @MaPeriod.setter
    def MaPeriod(self, value):
        self._ma_period.Value = value

    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    @RsiPeriod.setter
    def RsiPeriod(self, value):
        self._rsi_period.Value = value

    @property
    def RsiBuyLevel(self):
        return self._rsi_buy_level.Value

    @RsiBuyLevel.setter
    def RsiBuyLevel(self, value):
        self._rsi_buy_level.Value = value

    @property
    def RsiSellLevel(self):
        return self._rsi_sell_level.Value

    @RsiSellLevel.setter
    def RsiSellLevel(self, value):
        self._rsi_sell_level.Value = value

    @property
    def MinTradeIntervalMinutes(self):
        return self._min_trade_interval_minutes.Value

    @MinTradeIntervalMinutes.setter
    def MinTradeIntervalMinutes(self, value):
        self._min_trade_interval_minutes.Value = value

    def OnReseted(self):
        super(euro_surge_simplified_strategy, self).OnReseted()
        self._last_trade_time = None

    def OnStarted(self, time):
        super(euro_surge_simplified_strategy, self).OnStarted(time)
        self._last_trade_time = None

        fast_ma = SimpleMovingAverage()
        fast_ma.Length = 20
        slow_ma = SimpleMovingAverage()
        slow_ma.Length = self.MaPeriod
        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(fast_ma, slow_ma, rsi, self._process_candle).Start()

        self.StartProtection(
            takeProfit=Unit(2, UnitTypes.Percent),
            stopLoss=Unit(1, UnitTypes.Percent))

    def _process_candle(self, candle, fast_value, slow_value, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        fast_val = float(fast_value)
        slow_val = float(slow_value)
        rsi_val = float(rsi_value)

        ma_buy = fast_val > slow_val
        ma_sell = fast_val < slow_val
        rsi_buy = rsi_val <= float(self.RsiBuyLevel)
        rsi_sell = rsi_val >= float(self.RsiSellLevel)

        is_buy = ma_buy and rsi_buy
        is_sell = ma_sell and rsi_sell

        if not is_buy and not is_sell:
            return

        now = candle.CloseTime
        min_interval = self.MinTradeIntervalMinutes
        if self._last_trade_time is not None:
            elapsed = (now - self._last_trade_time).TotalMinutes
            if elapsed < min_interval:
                return

        if is_buy and self.Position <= 0:
            self.BuyMarket()
            self._last_trade_time = now
        elif is_sell and self.Position >= 0:
            self.SellMarket()
            self._last_trade_time = now

    def CreateClone(self):
        return euro_surge_simplified_strategy()
