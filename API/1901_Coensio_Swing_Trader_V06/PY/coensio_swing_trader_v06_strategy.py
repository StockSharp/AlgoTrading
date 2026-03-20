import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy


class coensio_swing_trader_v06_strategy(Strategy):
    def __init__(self):
        super(coensio_swing_trader_v06_strategy, self).__init__()
        self._channel_period = self.Param("ChannelPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Channel Period", "Period for Donchian Channel", "Indicators")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percent", "Risk")
        self._take_profit_percent = self.Param("TakeProfitPercent", 4.0) \
            .SetDisplay("Take Profit %", "Take profit percent", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._highs = []
        self._lows = []
        self._entry_price = 0.0

    @property
    def channel_period(self):
        return self._channel_period.Value
    @property
    def stop_loss_percent(self):
        return self._stop_loss_percent.Value
    @property
    def take_profit_percent(self):
        return self._take_profit_percent.Value
    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(coensio_swing_trader_v06_strategy, self).OnReseted()
        self._highs = []
        self._lows = []
        self._entry_price = 0.0

    def OnStarted(self, time):
        super(coensio_swing_trader_v06_strategy, self).OnStarted(time)
        self.StartProtection(
            Unit(float(self.stop_loss_percent), UnitTypes.Percent),
            Unit(float(self.take_profit_percent), UnitTypes.Percent))
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        self._highs.append(float(candle.HighPrice))
        self._lows.append(float(candle.LowPrice))
        cp = self.channel_period
        if len(self._highs) > cp:
            self._highs.pop(0)
        if len(self._lows) > cp:
            self._lows.pop(0)
        if len(self._highs) < cp:
            return
        upper = max(self._highs[:-1])
        lower = min(self._lows[:-1])
        price = float(candle.ClosePrice)
        if price > upper and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._entry_price = price
        elif price < lower and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._entry_price = price

    def CreateClone(self):
        return coensio_swing_trader_v06_strategy()
