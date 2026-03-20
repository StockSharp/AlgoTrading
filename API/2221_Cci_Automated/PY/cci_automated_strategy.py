import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy


class cci_automated_strategy(Strategy):
    def __init__(self):
        super(cci_automated_strategy, self).__init__()
        self._cci_period = self.Param("CciPeriod", 9) \
            .SetDisplay("CCI Period", "CCI indicator length", "Indicators")
        self._trades_duplicator = self.Param("TradesDuplicator", 3) \
            .SetDisplay("Trades Duplicator", "Maximum number of concurrent trades", "General")
        self._stop_loss = self.Param("StopLoss", 50.0) \
            .SetDisplay("Stop Loss", "Stop loss in price units", "Risk")
        self._take_profit = self.Param("TakeProfit", 200.0) \
            .SetDisplay("Take Profit", "Take profit in price units", "Risk")
        self._trailing_stop = self.Param("TrailingStop", 50.0) \
            .SetDisplay("Trailing Stop", "Trailing stop in price units", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._prev_cci = None
        self._trail_price = None

    @property
    def cci_period(self):
        return self._cci_period.Value

    @property
    def trades_duplicator(self):
        return self._trades_duplicator.Value

    @property
    def stop_loss(self):
        return self._stop_loss.Value

    @property
    def take_profit(self):
        return self._take_profit.Value

    @property
    def trailing_stop(self):
        return self._trailing_stop.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(cci_automated_strategy, self).OnReseted()
        self._prev_cci = None
        self._trail_price = None

    def OnStarted(self, time):
        super(cci_automated_strategy, self).OnStarted(time)
        cci = CommodityChannelIndex()
        cci.Length = self.cci_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(cci, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, cci)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, cci_value):
        if candle.State != CandleStates.Finished:
            return
        cci_value = float(cci_value)
        close = float(candle.ClosePrice)
        trail = float(self.trailing_stop)
        max_vol = self.trades_duplicator * self.Volume
        if self._prev_cci is not None:
            if self._prev_cci < -90.0 and cci_value > -80.0 and self.Position + self.Volume <= max_vol:
                self.BuyMarket()
                self._trail_price = close - trail
            elif self._prev_cci > 90.0 and cci_value < 80.0 and self.Position - self.Volume >= -max_vol:
                self.SellMarket()
                self._trail_price = close + trail
        if self.Position > 0:
            candidate = close - trail
            if self._trail_price is None or candidate > self._trail_price:
                self._trail_price = candidate
            if self._trail_price is not None and close <= self._trail_price:
                self.SellMarket()
                self._trail_price = None
        elif self.Position < 0:
            candidate = close + trail
            if self._trail_price is None or candidate < self._trail_price:
                self._trail_price = candidate
            if self._trail_price is not None and close >= self._trail_price:
                self.BuyMarket()
                self._trail_price = None
        self._prev_cci = cci_value

    def CreateClone(self):
        return cci_automated_strategy()
