import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class ict_ny_kill_zone_auto_trading_strategy(Strategy):
    def __init__(self):
        super(ict_ny_kill_zone_auto_trading_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev1 = None
        self._prev2 = None
        self._entry_price = 0.0
        self._stop_loss = 0.0
        self._take_profit = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(ict_ny_kill_zone_auto_trading_strategy, self).OnReseted()
        self._prev1 = None
        self._prev2 = None
        self._entry_price = 0.0
        self._stop_loss = 0.0
        self._take_profit = 0.0

    def OnStarted2(self, time):
        super(ict_ny_kill_zone_auto_trading_strategy, self).OnStarted2(time)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _is_kill_zone(self, t):
        hour = t.Hour
        return 9 <= hour < 16 or (hour == 16 and t.Minute == 0)

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return
        close = float(candle.ClosePrice)
        # Check stop/tp for existing position
        if self.Position > 0 and self._entry_price > 0:
            if close <= self._stop_loss or close >= self._take_profit:
                self.SellMarket()
                self._entry_price = 0.0
        elif self.Position < 0 and self._entry_price > 0:
            if close >= self._stop_loss or close <= self._take_profit:
                self.BuyMarket()
                self._entry_price = 0.0
        if self._prev1 is not None and self._prev2 is not None:
            is_kz = self._is_kill_zone(candle.OpenTime)
            is_fvg = float(self._prev2.HighPrice) < float(candle.LowPrice) and float(self._prev1.HighPrice) < float(candle.LowPrice)
            bullish_ob = float(self._prev2.ClosePrice) < float(self._prev2.OpenPrice) and float(self._prev1.ClosePrice) > float(self._prev1.OpenPrice) and close > float(candle.OpenPrice)
            bearish_ob = float(self._prev2.ClosePrice) > float(self._prev2.OpenPrice) and float(self._prev1.ClosePrice) < float(self._prev1.OpenPrice) and close < float(candle.OpenPrice)
            if is_kz and is_fvg and bullish_ob and self.Position <= 0:
                self.BuyMarket()
                self._entry_price = close
                self._stop_loss = close - 30.0
                self._take_profit = close + 60.0
            elif is_kz and is_fvg and bearish_ob and self.Position >= 0:
                self.SellMarket()
                self._entry_price = close
                self._stop_loss = close + 30.0
                self._take_profit = close - 60.0
        self._prev2 = self._prev1
        self._prev1 = candle

    def CreateClone(self):
        return ict_ny_kill_zone_auto_trading_strategy()
