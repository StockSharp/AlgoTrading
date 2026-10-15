import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class i4_drf_v2_strategy(Strategy):
    """
    I4 DRF v2: custom indicator counting up/down close directions.
    Trades color (direction) flips with SL/TP management.
    TrendMode 0 = contrarian, 1 = trend following.
    """

    def __init__(self):
        super(i4_drf_v2_strategy, self).__init__()
        self._period = self.Param("Period", 11) \
            .SetDisplay("Period", "Indicator period", "Indicator")
        self._buy_pos_open = self.Param("BuyPosOpen", True) \
            .SetDisplay("Buy Open", "Allow opening longs", "Trading")
        self._sell_pos_open = self.Param("SellPosOpen", True) \
            .SetDisplay("Sell Open", "Allow opening shorts", "Trading")
        self._buy_pos_close = self.Param("BuyPosClose", True) \
            .SetDisplay("Buy Close", "Allow closing longs", "Trading")
        self._sell_pos_close = self.Param("SellPosClose", True) \
            .SetDisplay("Sell Close", "Allow closing shorts", "Trading")
        self._trend_mode = self.Param("TrendMode", 0) \
            .SetDisplay("Trend Mode", "0=contrarian, 1=trend following", "General")
        self._stop_loss = self.Param("StopLoss", 1000) \
            .SetDisplay("Stop Loss", "Stop loss in price steps", "Risk")
        self._take_profit = self.Param("TakeProfit", 2000) \
            .SetDisplay("Take Profit", "Take profit in price steps", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe of candles", "General")

        self._signs = []
        self._sign_sum = 0
        self._prev_price = None
        self._prev_color = None
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._take_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(i4_drf_v2_strategy, self).OnReseted()
        self._signs = []
        self._sign_sum = 0
        self._prev_price = None
        self._prev_color = None
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._take_price = 0.0

    def OnStarted2(self, time):
        super(i4_drf_v2_strategy, self).OnStarted2(time)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        period = self._period.Value

        if self._prev_price is None:
            self._prev_price = close
            return

        sign = 1 if close > self._prev_price else -1
        self._prev_price = close

        self._signs.append(sign)
        self._sign_sum += sign

        if len(self._signs) > period:
            self._sign_sum -= self._signs.pop(0)

        if len(self._signs) < period:
            current_color = 1 if self._sign_sum > 0 else 0
            self._prev_color = current_color
            return

        is_formed = True
        drf_value = float(self._sign_sum) / period * 100.0
        current_color = 1 if drf_value > 0 else 0

        step = 1.0
        if self.Security is not None and self.Security.PriceStep is not None:
            step = float(self.Security.PriceStep)
        if step <= 0:
            step = 1.0

        sl = self._stop_loss.Value
        tp = self._take_profit.Value

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_color = current_color
            return

        # Handle protective stops
        if self.Position > 0:
            if (sl > 0 and close <= self._stop_price) or (tp > 0 and close >= self._take_price):
                self.SellMarket()
                self._prev_color = current_color
                return
        elif self.Position < 0:
            if (sl > 0 and close >= self._stop_price) or (tp > 0 and close <= self._take_price):
                self.BuyMarket()
                self._prev_color = current_color
                return

        if self._prev_color is None:
            self._prev_color = current_color
            return

        trend_mode = self._trend_mode.Value

        if trend_mode == 0:
            # Direct (contrarian)
            if self._prev_color == 1 and current_color == 0:
                if self._sell_pos_close.Value and self.Position < 0:
                    self.BuyMarket()
                if self._buy_pos_open.Value and self.Position <= 0:
                    self.BuyMarket()
                    self._entry_price = close
                    self._stop_price = close - sl * step
                    self._take_price = close + tp * step
            elif self._prev_color == 0 and current_color == 1:
                if self._buy_pos_close.Value and self.Position > 0:
                    self.SellMarket()
                if self._sell_pos_open.Value and self.Position >= 0:
                    self.SellMarket()
                    self._entry_price = close
                    self._stop_price = close + sl * step
                    self._take_price = close - tp * step
        else:
            # NotDirect (trend following)
            if self._prev_color == 0 and current_color == 1:
                if self._sell_pos_close.Value and self.Position < 0:
                    self.BuyMarket()
                if self._buy_pos_open.Value and self.Position <= 0:
                    self.BuyMarket()
                    self._entry_price = close
                    self._stop_price = close - sl * step
                    self._take_price = close + tp * step
            elif self._prev_color == 1 and current_color == 0:
                if self._buy_pos_close.Value and self.Position > 0:
                    self.SellMarket()
                if self._sell_pos_open.Value and self.Position >= 0:
                    self.SellMarket()
                    self._entry_price = close
                    self._stop_price = close + sl * step
                    self._take_price = close - tp * step

        self._prev_color = current_color

    def CreateClone(self):
        return i4_drf_v2_strategy()
