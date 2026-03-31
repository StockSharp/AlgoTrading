import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import MoneyFlowIndex
from StockSharp.Algo.Strategies import Strategy


class cdc_pl_mfi_strategy(Strategy):
    def __init__(self):
        super(cdc_pl_mfi_strategy, self).__init__()

        self._mfi_period = self.Param("MfiPeriod", 14) \
            .SetDisplay("MFI Period", "Money Flow Index period", "Indicators")
        self._long_level = self.Param("LongLevel", 40.0) \
            .SetDisplay("Long Level", "MFI below this for long entry", "Signals")
        self._short_level = self.Param("ShortLevel", 60.0) \
            .SetDisplay("Short Level", "MFI above this for short entry", "Signals")
        self._signal_cooldown = self.Param("SignalCooldownCandles", 6) \
            .SetDisplay("Signal Cooldown", "Bars to wait between trades", "Trading")

        self._mfi = None
        self._candles = []
        self._has_prev_mfi = False
        self._candles_since_trade = 0

    @property
    def mfi_period(self):
        return self._mfi_period.Value

    @property
    def long_level(self):
        return self._long_level.Value

    @property
    def short_level(self):
        return self._short_level.Value

    @property
    def signal_cooldown(self):
        return self._signal_cooldown.Value

    def OnReseted(self):
        super(cdc_pl_mfi_strategy, self).OnReseted()
        self._mfi = None
        self._candles = []
        self._has_prev_mfi = False
        self._candles_since_trade = self.signal_cooldown

    def OnStarted2(self, time):
        super(cdc_pl_mfi_strategy, self).OnStarted2(time)

        self._mfi = MoneyFlowIndex()
        self._mfi.Length = self.mfi_period
        self._candles = []
        self._has_prev_mfi = False
        self._candles_since_trade = self.signal_cooldown

        subscription = self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        subscription.Bind(self._mfi, self._process_candle)
        subscription.Start()

        self.StartProtection(takeProfit=Unit(2, UnitTypes.Percent), stopLoss=Unit(1, UnitTypes.Percent))

    def _process_candle(self, candle, mfi_value):
        if candle.State != CandleStates.Finished:
            return

        mfi_val = float(mfi_value)

        if self._candles_since_trade < self.signal_cooldown:
            self._candles_since_trade += 1

        self._candles.append(candle)
        if len(self._candles) > 5:
            self._candles.pop(0)

        if len(self._candles) >= 2 and self._has_prev_mfi:
            curr = self._candles[-1]
            prev = self._candles[-2]

            is_piercing = (float(prev.OpenPrice) > float(prev.ClosePrice)
                and float(curr.ClosePrice) > float(curr.OpenPrice)
                and float(curr.OpenPrice) < float(prev.LowPrice)
                and float(curr.ClosePrice) > (float(prev.OpenPrice) + float(prev.ClosePrice)) / 2.0)

            is_dark_cloud = (float(prev.ClosePrice) > float(prev.OpenPrice)
                and float(curr.OpenPrice) > float(curr.ClosePrice)
                and float(curr.OpenPrice) > float(prev.HighPrice)
                and float(curr.ClosePrice) < (float(prev.OpenPrice) + float(prev.ClosePrice)) / 2.0)

            if is_piercing and mfi_val < self.long_level and self.Position == 0 and self._candles_since_trade >= self.signal_cooldown:
                self.BuyMarket()
                self._candles_since_trade = 0
            elif is_dark_cloud and mfi_val > self.short_level and self.Position == 0 and self._candles_since_trade >= self.signal_cooldown:
                self.SellMarket()
                self._candles_since_trade = 0

        self._has_prev_mfi = True

    def CreateClone(self):
        return cdc_pl_mfi_strategy()
