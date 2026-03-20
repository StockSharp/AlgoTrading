import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class cdc_pl_rsi_strategy(Strategy):
    def __init__(self):
        super(cdc_pl_rsi_strategy, self).__init__()

        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI period", "Indicators")
        self._oversold_level = self.Param("OversoldLevel", 40.0) \
            .SetDisplay("Oversold Level", "RSI below this for long entry", "Signals")
        self._overbought_level = self.Param("OverboughtLevel", 60.0) \
            .SetDisplay("Overbought Level", "RSI above this for short entry", "Signals")
        self._signal_cooldown = self.Param("SignalCooldownCandles", 6) \
            .SetDisplay("Signal Cooldown", "Bars to wait between trades", "Trading")

        self._rsi = None
        self._candles = []
        self._has_prev_rsi = False
        self._candles_since_trade = 0

    @property
    def rsi_period(self):
        return self._rsi_period.Value

    @property
    def oversold_level(self):
        return self._oversold_level.Value

    @property
    def overbought_level(self):
        return self._overbought_level.Value

    @property
    def signal_cooldown(self):
        return self._signal_cooldown.Value

    def OnReseted(self):
        super(cdc_pl_rsi_strategy, self).OnReseted()
        self._rsi = None
        self._candles = []
        self._has_prev_rsi = False
        self._candles_since_trade = self.signal_cooldown

    def OnStarted(self, time):
        super(cdc_pl_rsi_strategy, self).OnStarted(time)

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.rsi_period
        self._candles = []
        self._has_prev_rsi = False
        self._candles_since_trade = self.signal_cooldown

        subscription = self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        subscription.Bind(self._rsi, self._process_candle)
        subscription.Start()

        self.StartProtection(takeProfit=Unit(2, UnitTypes.Percent), stopLoss=Unit(1, UnitTypes.Percent), useMarketOrders=True)

    def _process_candle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._rsi.IsFormed:
            return

        rsi_val = float(rsi_value)

        if self._candles_since_trade < self.signal_cooldown:
            self._candles_since_trade += 1

        self._candles.append(candle)
        if len(self._candles) > 5:
            self._candles.pop(0)

        if len(self._candles) >= 2 and self._has_prev_rsi:
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

            if is_piercing and rsi_val < self.oversold_level and self.Position == 0 and self._candles_since_trade >= self.signal_cooldown:
                self.BuyMarket()
                self._candles_since_trade = 0
            elif is_dark_cloud and rsi_val > self.overbought_level and self.Position == 0 and self._candles_since_trade >= self.signal_cooldown:
                self.SellMarket()
                self._candles_since_trade = 0

        self._has_prev_rsi = True

    def CreateClone(self):
        return cdc_pl_rsi_strategy()
