import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

MOMENTUM_BARS = 6


class autonomous_5_minute_robot_strategy(Strategy):
    """
    Autonomous 5-minute robot strategy.
    Buy volume is the volume of up candles and sell volume the volume of down candles over the last VolumeLength candles. A close
    above the SMA and above the close 6 candles ago with buy volume above sell volume goes long; a close below the SMA and below
    the close 6 candles ago with sell volume above buy volume goes short, reversing an opposite position. Percent stop-loss and
    take-profit protect every position.
    """

    def __init__(self):
        super(autonomous_5_minute_robot_strategy, self).__init__()
        self._ma_length = self.Param("MaLength", 50) \
            .SetGreaterThanZero() \
            .SetDisplay("MA Length", "SMA period", "Trend")
        self._volume_length = self.Param("VolumeLength", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("Volume Length", "Candles over which buy and sell volume are summed", "Volume")
        self._stop_loss_percent = self.Param("StopLossPercent", 3.0) \
            .SetNotNegative() \
            .SetDisplay("Stop Loss %", "Stop-loss percentage", "Risk")
        self._take_profit_percent = self.Param("TakeProfitPercent", 29.0) \
            .SetNotNegative() \
            .SetDisplay("Take Profit %", "Take-profit percentage", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._closes = []
        self._volumes = []

    @property
    def CandleType(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnReseted(self):
        super(autonomous_5_minute_robot_strategy, self).OnReseted()
        self._closes = []
        self._volumes = []

    def OnStarted2(self, time):
        super(autonomous_5_minute_robot_strategy, self).OnStarted2(time)

        self._closes = []
        self._volumes = []

        sma = SimpleMovingAverage()
        sma.Length = self._ma_length.Value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(sma, self._process_candle).Start()

        take = float(self._take_profit_percent.Value)
        stop = float(self._stop_loss_percent.Value)
        self.StartProtection(
            Unit(Decimal(take), UnitTypes.Percent) if take > 0 else Unit(),
            Unit(Decimal(stop), UnitTypes.Percent) if stop > 0 else Unit(),
            useMarketOrders=True)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, sma_value):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        open_price = float(candle.OpenPrice)
        volume = float(candle.TotalVolume)

        self._closes.append(close)
        if len(self._closes) > MOMENTUM_BARS + 1:
            self._closes.pop(0)

        self._volumes.append((volume if close > open_price else 0.0, volume if close < open_price else 0.0))
        volume_length = self._volume_length.Value
        if len(self._volumes) > volume_length:
            self._volumes.pop(0)

        if len(self._closes) <= MOMENTUM_BARS or len(self._volumes) < volume_length:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        buy_volume = sum(v[0] for v in self._volumes)
        sell_volume = sum(v[1] for v in self._volumes)
        past_close = self._closes[0]
        sma = float(sma_value)

        if close > sma and close > past_close and buy_volume > sell_volume and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif close < sma and close < past_close and sell_volume > buy_volume and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return autonomous_5_minute_robot_strategy()
