import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy


class dark_cloud_piercing_cci_strategy(Strategy):
    def __init__(self):
        super(dark_cloud_piercing_cci_strategy, self).__init__()

        self._cci_period = self.Param("CciPeriod", 14) \
            .SetDisplay("CCI Period", "CCI period", "Indicators")
        self._entry_level = self.Param("EntryLevel", 50.0) \
            .SetDisplay("Entry Level", "CCI level for confirmation", "Signals")
        self._signal_cooldown = self.Param("SignalCooldownCandles", 6) \
            .SetDisplay("Signal Cooldown", "Bars to wait between trades", "Trading")

        self._cci = None
        self._candles = []
        self._has_prev_cci = False
        self._candles_since_trade = 0

    @property
    def cci_period(self):
        return self._cci_period.Value

    @property
    def entry_level(self):
        return self._entry_level.Value

    @property
    def signal_cooldown(self):
        return self._signal_cooldown.Value

    def OnReseted(self):
        super(dark_cloud_piercing_cci_strategy, self).OnReseted()
        self._cci = None
        self._candles = []
        self._has_prev_cci = False
        self._candles_since_trade = self.signal_cooldown

    def OnStarted2(self, time):
        super(dark_cloud_piercing_cci_strategy, self).OnStarted2(time)

        self._cci = CommodityChannelIndex()
        self._cci.Length = self.cci_period
        self._candles = []
        self._has_prev_cci = False
        self._candles_since_trade = self.signal_cooldown

        subscription = self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        subscription.Bind(self._cci, self._process_candle)
        subscription.Start()

        self.StartProtection(takeProfit=Unit(2, UnitTypes.Percent), stopLoss=Unit(1, UnitTypes.Percent))

    def _process_candle(self, candle, cci_value):
        if candle.State != CandleStates.Finished:
            return

        cci_val = float(cci_value)

        if self._candles_since_trade < self.signal_cooldown:
            self._candles_since_trade += 1

        self._candles.append(candle)
        if len(self._candles) > 5:
            self._candles.pop(0)

        if len(self._candles) >= 2 and self._has_prev_cci:
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

            if is_piercing and cci_val < -self.entry_level and self.Position == 0 and self._candles_since_trade >= self.signal_cooldown:
                self.BuyMarket()
                self._candles_since_trade = 0
            elif is_dark_cloud and cci_val > self.entry_level and self.Position == 0 and self._candles_since_trade >= self.signal_cooldown:
                self.SellMarket()
                self._candles_since_trade = 0

        self._has_prev_cci = True

    def CreateClone(self):
        return dark_cloud_piercing_cci_strategy()
