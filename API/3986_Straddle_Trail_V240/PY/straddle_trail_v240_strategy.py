import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, UnitTypes, Unit
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import ExponentialMovingAverage

class straddle_trail_v240_strategy(Strategy):
    def __init__(self):
        super(straddle_trail_v240_strategy, self).__init__()

        self._channel_period = self.Param("ChannelPeriod", 20) \
            .SetDisplay("Channel Period", "Lookback period for breakout levels", "Parameters")
        self._stop_loss = self.Param("StopLoss", 500.0) \
            .SetDisplay("Stop Loss", "Stop loss distance", "Risk")
        self._take_profit = self.Param("TakeProfit", 500.0) \
            .SetDisplay("Take Profit", "Take profit distance", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(2))) \
            .SetDisplay("Candle Type", "Candle subscription used", "General")

        self._highs = []
        self._lows = []

    @property
    def ChannelPeriod(self):
        return self._channel_period.Value

    @property
    def StopLoss(self):
        return self._stop_loss.Value

    @property
    def TakeProfit(self):
        return self._take_profit.Value

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(straddle_trail_v240_strategy, self).OnStarted(time)

        ema = ExponentialMovingAverage()
        ema.Length = 10

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(ema, self.ProcessCandle).Start()

        tp = Unit(float(self.TakeProfit), UnitTypes.Absolute) if float(self.TakeProfit) > 0 else None
        sl = Unit(float(self.StopLoss), UnitTypes.Absolute) if float(self.StopLoss) > 0 else None
        self.StartProtection(tp, sl)

    def ProcessCandle(self, candle, ema_value):
        if candle.State != CandleStates.Finished:
            return

        self._highs.append(float(candle.HighPrice))
        self._lows.append(float(candle.LowPrice))

        cp = self.ChannelPeriod
        while len(self._highs) > cp:
            self._highs.pop(0)
        while len(self._lows) > cp:
            self._lows.pop(0)

        if len(self._highs) < cp:
            return

        upper = max(self._highs[:-1])
        lower = min(self._lows[:-1])

        close = float(candle.ClosePrice)

        if close > upper and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(abs(self.Position))
            self.BuyMarket(self.Volume)
        elif close < lower and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(self.Position)
            self.SellMarket(self.Volume)

    def OnReseted(self):
        super(straddle_trail_v240_strategy, self).OnReseted()
        self._highs = []
        self._lows = []

    def CreateClone(self):
        return straddle_trail_v240_strategy()
