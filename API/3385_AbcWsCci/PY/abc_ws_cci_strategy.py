import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy


class abc_ws_cci_strategy(Strategy):
    def __init__(self):
        super(abc_ws_cci_strategy, self).__init__()

        self._cci_period = self.Param("CciPeriod", 14) \
            .SetDisplay("CCI Period", "CCI period for confirmation", "Indicators")
        self._signal_cooldown = self.Param("SignalCooldownCandles", 6) \
            .SetDisplay("Signal Cooldown", "Bars to wait between trades", "Trading")

        self._cci = None
        self._bull_count = 0
        self._bear_count = 0
        self._candles_since_trade = 0

    @property
    def cci_period(self):
        return self._cci_period.Value

    @property
    def signal_cooldown(self):
        return self._signal_cooldown.Value

    def OnReseted(self):
        super(abc_ws_cci_strategy, self).OnReseted()
        self._cci = None
        self._bull_count = 0
        self._bear_count = 0
        self._candles_since_trade = self.signal_cooldown

    def OnStarted2(self, time):
        super(abc_ws_cci_strategy, self).OnStarted2(time)

        self._cci = CommodityChannelIndex()
        self._cci.Length = self.cci_period
        self._bull_count = 0
        self._bear_count = 0
        self._candles_since_trade = self.signal_cooldown

        subscription = self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromMinutes(60)))
        subscription.Bind(self._cci, self._process_candle)
        subscription.Start()

    def _process_candle(self, candle, cci_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._cci.IsFormed:
            return

        cci_val = float(cci_value)

        if self._candles_since_trade < self.signal_cooldown:
            self._candles_since_trade += 1

        close = float(candle.ClosePrice)
        open_p = float(candle.OpenPrice)

        if close > open_p:
            self._bull_count += 1
            self._bear_count = 0
        elif close < open_p:
            self._bear_count += 1
            self._bull_count = 0
        else:
            self._bull_count = 0
            self._bear_count = 0

        if self.Position > 0 and cci_val > 200.0 and self._candles_since_trade >= self.signal_cooldown:
            self.SellMarket()
            self._candles_since_trade = 0
        elif self.Position < 0 and cci_val < -200.0 and self._candles_since_trade >= self.signal_cooldown:
            self.BuyMarket()
            self._candles_since_trade = 0

        if self._bull_count >= 3 and cci_val < 100.0 and self.Position <= 0 and self._candles_since_trade >= self.signal_cooldown:
            self.BuyMarket()
            self._bull_count = 0
            self._candles_since_trade = 0
        elif self._bear_count >= 3 and cci_val > -100.0 and self.Position >= 0 and self._candles_since_trade >= self.signal_cooldown:
            self.SellMarket()
            self._bear_count = 0
            self._candles_since_trade = 0

    def CreateClone(self):
        return abc_ws_cci_strategy()
