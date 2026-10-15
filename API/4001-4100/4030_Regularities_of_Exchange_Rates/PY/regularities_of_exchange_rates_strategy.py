import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import SimpleMovingAverage

class regularities_of_exchange_rates_strategy(Strategy):
    def __init__(self):
        super(regularities_of_exchange_rates_strategy, self).__init__()

        self._opening_hour = self.Param("OpeningHour", 9) \
            .SetDisplay("Opening Hour", "Hour (0-23) when breakout levels are set", "Schedule")
        self._closing_hour = self.Param("ClosingHour", 2) \
            .SetDisplay("Closing Hour", "Hour (0-23) when the strategy exits", "Schedule")
        self._entry_offset_points = self.Param("EntryOffsetPoints", 20.0) \
            .SetDisplay("Entry Offset (points)", "Distance from reference price for breakout", "Orders")
        self._take_profit_points = self.Param("TakeProfitPoints", 20.0) \
            .SetDisplay("Take Profit (points)", "Profit target distance in points", "Risk")
        self._stop_loss_points = self.Param("StopLossPoints", 500.0) \
            .SetDisplay("Stop Loss (points)", "Stop-loss distance in points", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Timeframe used to evaluate trading hours", "General")

        self._point_size = 0.01
        self._last_entry_date = None
        self._reference_price = 0.0
        self._entry_price = 0.0
        self._waiting_for_breakout = False

    @property
    def OpeningHour(self):
        return self._opening_hour.Value

    @property
    def ClosingHour(self):
        return self._closing_hour.Value

    @property
    def EntryOffsetPoints(self):
        return self._entry_offset_points.Value

    @property
    def TakeProfitPoints(self):
        return self._take_profit_points.Value

    @property
    def StopLossPoints(self):
        return self._stop_loss_points.Value

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(regularities_of_exchange_rates_strategy, self).OnStarted2(time)

        self._point_size = 0.01
        if self.Security is not None and self.Security.PriceStep is not None:
            ps = float(self.Security.PriceStep)
            if ps > 0:
                self._point_size = ps

        self._dummy_sma = SimpleMovingAverage()
        self._dummy_sma.Length = 2

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._dummy_sma, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, sma_value):
        if candle.State != CandleStates.Finished:
            return

        hour = candle.OpenTime.Hour
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        # At closing hour: flatten position and cancel breakout watch
        if hour == self.ClosingHour:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            elif self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self._waiting_for_breakout = False
            self._entry_price = 0.0

        # Manage take-profit and stop-loss for existing position
        if self.Position != 0 and self._entry_price > 0:
            tp = float(self.TakeProfitPoints) * self._point_size
            sl = float(self.StopLossPoints) * self._point_size

            if self.Position > 0:
                if (tp > 0 and close - self._entry_price >= tp) or (sl > 0 and self._entry_price - close >= sl):
                    self.SellMarket(Math.Abs(self.Position))
                    self._entry_price = 0.0
                    self._waiting_for_breakout = False
            elif self.Position < 0:
                if (tp > 0 and self._entry_price - close >= tp) or (sl > 0 and close - self._entry_price >= sl):
                    self.BuyMarket(Math.Abs(self.Position))
                    self._entry_price = 0.0
                    self._waiting_for_breakout = False

        # At opening hour: set reference price for breakout
        if hour == self.OpeningHour and self.Position == 0:
            candle_date = candle.OpenTime.Date
            if self._last_entry_date is None or self._last_entry_date != candle_date:
                self._reference_price = close
                self._waiting_for_breakout = True
                self._last_entry_date = candle_date

        # Check for breakout entry
        if self._waiting_for_breakout and self.Position == 0 and self._reference_price > 0:

            offset = float(self.EntryOffsetPoints) * self._point_size
            buy_level = self._reference_price + offset
            sell_level = self._reference_price - offset

            if high >= buy_level:
                self.BuyMarket()
                self._entry_price = close
                self._waiting_for_breakout = False
            elif low <= sell_level:
                self.SellMarket()
                self._entry_price = close
                self._waiting_for_breakout = False

    def OnReseted(self):
        super(regularities_of_exchange_rates_strategy, self).OnReseted()
        self._point_size = 0.01
        self._last_entry_date = None
        self._reference_price = 0.0
        self._entry_price = 0.0
        self._waiting_for_breakout = False

    def CreateClone(self):
        return regularities_of_exchange_rates_strategy()
