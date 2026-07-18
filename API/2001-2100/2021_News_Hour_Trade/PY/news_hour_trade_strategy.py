import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import DateTime, TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class news_hour_trade_strategy(Strategy):

    def __init__(self):
        super(news_hour_trade_strategy, self).__init__()

        self._start_hour = self.Param("StartHour", 0) \
            .SetDisplay("Start Hour", "Hour to start", "Parameters")
        self._start_minute = self.Param("StartMinute", 0) \
            .SetDisplay("Start Minute", "Minute to start", "Parameters")
        self._stop_loss = self.Param("StopLoss", 500) \
            .SetDisplay("Stop Loss", "Stop distance in steps", "Risk")
        self._take_profit = self.Param("TakeProfit", 1000) \
            .SetDisplay("Take Profit", "Take profit distance in steps", "Risk")
        self._price_gap = self.Param("PriceGap", 10) \
            .SetDisplay("Price Gap", "Price offset in steps", "Parameters")
        self._trade_interval_days = self.Param("TradeIntervalDays", 365) \
            .SetDisplay("Trade Interval Days", "Minimum number of calendar days between setups", "Parameters")
        self._buy_trade = self.Param("BuyTrade", True) \
            .SetDisplay("Buy Trade", "Enable buys", "Parameters")
        self._sell_trade = self.Param("SellTrade", True) \
            .SetDisplay("Sell Trade", "Enable sells", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Working timeframe", "Parameters")

        self._last_trade_day = DateTime.MinValue
        self._tick_size = 0.0
        self._setup_consumed = False
        self._exit_submitted = False
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._take_price = 0.0

    @property
    def StartHour(self):
        return self._start_hour.Value

    @StartHour.setter
    def StartHour(self, value):
        self._start_hour.Value = value

    @property
    def StartMinute(self):
        return self._start_minute.Value

    @StartMinute.setter
    def StartMinute(self, value):
        self._start_minute.Value = value

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
    def PriceGap(self):
        return self._price_gap.Value

    @PriceGap.setter
    def PriceGap(self, value):
        self._price_gap.Value = value

    @property
    def TradeIntervalDays(self):
        return self._trade_interval_days.Value

    @TradeIntervalDays.setter
    def TradeIntervalDays(self, value):
        self._trade_interval_days.Value = value

    @property
    def BuyTrade(self):
        return self._buy_trade.Value

    @BuyTrade.setter
    def BuyTrade(self, value):
        self._buy_trade.Value = value

    @property
    def SellTrade(self):
        return self._sell_trade.Value

    @SellTrade.setter
    def SellTrade(self, value):
        self._sell_trade.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted2(self, time):
        super(news_hour_trade_strategy, self).OnStarted2(time)

        ps = self.Security.PriceStep
        self._tick_size = float(ps) if ps is not None else 1.0

        self.SubscribeCandles(self.CandleType) \
            .Bind(self.ProcessCandle) \
            .Start()

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        date = candle.OpenTime.Date
        sl = self.StopLoss
        tp = self.TakeProfit
        ts = self._tick_size

        enough_days = self._last_trade_day == DateTime.MinValue or (date - self._last_trade_day).TotalDays >= self.TradeIntervalDays

        if not self._setup_consumed and enough_days and candle.OpenTime.Hour == self.StartHour and candle.OpenTime.Minute >= self.StartMinute and self.Position == 0:
            self._last_trade_day = date
            self._setup_consumed = True
            self._exit_submitted = False

            close = float(candle.ClosePrice)
            open_p = float(candle.OpenPrice)
            long_bias = close >= open_p
            open_long = self.BuyTrade and (not self.SellTrade or long_bias)
            open_short = self.SellTrade and (not self.BuyTrade or not long_bias)

            if open_long:
                self.BuyMarket()
                self._entry_price = close
                self._stop_price = self._entry_price - sl * ts
                self._take_price = self._entry_price + tp * ts
            elif open_short:
                self.SellMarket()
                self._entry_price = close
                self._stop_price = self._entry_price + sl * ts
                self._take_price = self._entry_price - tp * ts

            return

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        if self.Position > 0:
            if not self._exit_submitted and (low <= self._stop_price or high >= self._take_price):
                self.SellMarket()
                self._exit_submitted = True
        elif self.Position < 0:
            if not self._exit_submitted and (high >= self._stop_price or low <= self._take_price):
                self.BuyMarket()
                self._exit_submitted = True

    def OnReseted(self):
        super(news_hour_trade_strategy, self).OnReseted()
        self._last_trade_day = DateTime.MinValue
        self._tick_size = 0.0
        self._setup_consumed = False
        self._exit_submitted = False
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._take_price = 0.0

    def CreateClone(self):
        return news_hour_trade_strategy()
