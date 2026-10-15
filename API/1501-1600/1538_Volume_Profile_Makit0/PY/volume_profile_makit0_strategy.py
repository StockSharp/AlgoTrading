import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, DateTime
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy


class volume_profile_makit0_strategy(Strategy):
    def __init__(self):
        super(volume_profile_makit0_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._current_session = None
        self._session_high = 0.0
        self._session_low = 0.0
        self._session_mid = 0.0
        self._poc_price = 0.0
        self._max_volume = 0.0
        self._session_trade_done = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(volume_profile_makit0_strategy, self).OnReseted()
        self._current_session = None
        self._session_high = 0.0
        self._session_low = 0.0
        self._session_mid = 0.0
        self._poc_price = 0.0
        self._max_volume = 0.0
        self._session_trade_done = False

    def OnStarted2(self, time):
        super(volume_profile_makit0_strategy, self).OnStarted2(time)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.on_process).Start()
        self.StartProtection(Unit(2, UnitTypes.Percent), Unit(1, UnitTypes.Percent))
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    @staticmethod
    def _get_session_start(date):
        return DateTime(date.Year, date.Month, 1)

    def on_process(self, candle):
        if candle.State != CandleStates.Finished:
            return
        session_start = self._get_session_start(candle.OpenTime.Date)
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        vol = float(candle.TotalVolume)
        # start new session on session boundary
        if self._current_session is None or self._current_session != session_start:
            if self.Position > 0:
                self.SellMarket()
            elif self.Position < 0:
                self.BuyMarket()
            self._current_session = session_start
            self._session_high = high
            self._session_low = low
            self._session_mid = close
            self._poc_price = close
            self._max_volume = vol
            self._session_trade_done = False
            return
        self._session_high = max(self._session_high, high)
        self._session_low = min(self._session_low, low)
        self._session_mid = (self._session_high + self._session_low) / 2.0
        if vol > self._max_volume:
            self._max_volume = vol
            self._poc_price = close
        bullish_profile = close > self._poc_price and close > self._session_mid
        bearish_profile = close < self._poc_price and close < self._session_mid
        if not self._session_trade_done and self.Position == 0 and bullish_profile:
            self.BuyMarket()
            self._session_trade_done = True
        elif not self._session_trade_done and self.Position == 0 and bearish_profile:
            self.SellMarket()
            self._session_trade_done = True
        if self.Position > 0 and close < self._poc_price and close < self._session_mid:
            self.SellMarket()
        elif self.Position < 0 and close > self._poc_price and close > self._session_mid:
            self.BuyMarket()

    def CreateClone(self):
        return volume_profile_makit0_strategy()
