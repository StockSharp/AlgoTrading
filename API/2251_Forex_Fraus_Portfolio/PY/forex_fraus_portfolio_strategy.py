import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import WilliamsR
from StockSharp.Algo.Strategies import Strategy


class forex_fraus_portfolio_strategy(Strategy):
    def __init__(self):
        super(forex_fraus_portfolio_strategy, self).__init__()
        self._wpr_period = self.Param("WprPeriod", 60) \
            .SetDisplay("WPR Period", "Williams %R calculation period", "Parameters")
        self._buy_threshold = self.Param("BuyThreshold", -90.0) \
            .SetDisplay("Buy Threshold", "Trigger level for long entry", "Parameters")
        self._sell_threshold = self.Param("SellThreshold", -10.0) \
            .SetDisplay("Sell Threshold", "Trigger level for short entry", "Parameters")
        self._start_hour = self.Param("StartHour", 0) \
            .SetDisplay("Start Hour", "Trading start hour", "Time")
        self._stop_hour = self.Param("StopHour", 24) \
            .SetDisplay("Stop Hour", "Trading stop hour", "Time")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._ok_buy = False
        self._ok_sell = False

    @property
    def wpr_period(self):
        return self._wpr_period.Value

    @property
    def buy_threshold(self):
        return self._buy_threshold.Value

    @property
    def sell_threshold(self):
        return self._sell_threshold.Value

    @property
    def start_hour(self):
        return self._start_hour.Value

    @property
    def stop_hour(self):
        return self._stop_hour.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(forex_fraus_portfolio_strategy, self).OnReseted()
        self._ok_buy = False
        self._ok_sell = False

    def OnStarted(self, time):
        super(forex_fraus_portfolio_strategy, self).OnStarted(time)
        self._ok_buy = False
        self._ok_sell = False
        wpr = WilliamsR()
        wpr.Length = self.wpr_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(wpr, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, wpr)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, wpr_value):
        if candle.State != CandleStates.Finished:
            return
        wpr_value = float(wpr_value)
        hour = candle.OpenTime.Hour
        start_h = int(self.start_hour)
        stop_h = int(self.stop_hour)
        if start_h <= stop_h:
            in_time = hour >= start_h and hour < stop_h
        else:
            in_time = hour >= start_h or hour < stop_h
        if not in_time:
            if self.Position > 0:
                self.SellMarket()
            elif self.Position < 0:
                self.BuyMarket()
            return
        buy_thr = float(self.buy_threshold)
        sell_thr = float(self.sell_threshold)
        if wpr_value < buy_thr:
            self._ok_buy = True
        if wpr_value > buy_thr and self._ok_buy:
            self._ok_buy = False
            if self.Position <= 0:
                self.BuyMarket()
            return
        if wpr_value > sell_thr:
            self._ok_sell = True
        if wpr_value < sell_thr and self._ok_sell:
            self._ok_sell = False
            if self.Position >= 0:
                self.SellMarket()

    def CreateClone(self):
        return forex_fraus_portfolio_strategy()
