import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy


class volume_profile_makit0_strategy(Strategy):
    def __init__(self):
        super(volume_profile_makit0_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(volume_profile_makit0_strategy, self).OnReseted()

    def OnStarted(self, time):
        super(volume_profile_makit0_strategy, self).OnStarted(time)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle):
        if candle.State != CandleStates.Finished:
            return
        session_start = GetSessionStart(candle.OpenTime.Date)
        # start new session on session boundary
        if self._current_session != session_start:
            if self.Position > 0:
                self.SellMarket()
            elif self.Position < 0:
                self.BuyMarket()
            self._current_session = session_start
            self._session_high = candle.HighPrice
            self._session_low = candle.LowPrice
            self._session_mid = candle.ClosePrice
            self._poc_price = candle.ClosePrice
            self._max_volume = candle.TotalVolume
            self._session_trade_done = False
            return
        self._session_high = max(self._session_high, candle.HighPrice)
        self._session_low = min(self._session_low, candle.LowPrice)
        self._session_mid = (self._session_high + self._session_low) / 2
        if candle.TotalVolume > self._max_volume:
            self._max_volume = candle.TotalVolume
            self._poc_price = candle.ClosePrice
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        bullish_profile = candle.ClosePrice > self._poc_price and candle.ClosePrice > self._session_mid
        bearish_profile = candle.ClosePrice < self._poc_price and candle.ClosePrice < self._session_mid
        if not self._session_trade_done and self.Position == 0 and bullish_profile:
            self.BuyMarket()
            self._session_trade_done = True
        elif not self._session_trade_done and self.Position == 0 and bearish_profile:
            self.SellMarket()
            self._session_trade_done = True
        if self.Position > 0 and candle.ClosePrice < self._poc_price and candle.ClosePrice < self._session_mid:
            self.SellMarket()
        elif self.Position < 0 and candle.ClosePrice > self._poc_price and candle.ClosePrice > self._session_mid:
            self.BuyMarket()

    def CreateClone(self):
        return volume_profile_makit0_strategy()
