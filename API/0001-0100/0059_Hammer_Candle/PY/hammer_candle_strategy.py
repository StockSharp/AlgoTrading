import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class hammer_candle_strategy(Strategy):
    """
    Hammer Candle strategy.
    Buys after a hammer: a candle whose lower shadow is at least twice its body and whose upper shadow is under half of it.
    The stop is the hammer's low and the target lies RewardRiskRatio times that risk above the entry close.
    """

    def __init__(self):
        super(hammer_candle_strategy, self).__init__()
        self._reward_risk_ratio = self.Param("RewardRiskRatio", 2.0).SetGreaterThanZero().SetDisplay("Reward/Risk", "Target distance in multiples of the stop distance", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")

        self._stop_price = Decimal(0)
        self._target_price = Decimal(0)

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(hammer_candle_strategy, self).OnReseted()
        self._stop_price = Decimal(0)
        self._target_price = Decimal(0)

    def OnStarted2(self, time):
        super(hammer_candle_strategy, self).OnStarted2(time)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = candle.ClosePrice

        if self.Position > 0:
            if close <= self._stop_price or close >= self._target_price:
                self.SellMarket(self.Position)
            return

        if self.Position != 0:
            return

        body = Math.Abs(candle.OpenPrice - close)
        lower_shadow = Math.Min(candle.OpenPrice, close) - candle.LowPrice
        upper_shadow = candle.HighPrice - Math.Max(candle.OpenPrice, close)

        is_hammer = body > 0 and lower_shadow >= body * Decimal(2) and upper_shadow < body * Decimal(0.5)
        if not is_hammer or close <= candle.LowPrice:
            return

        self.BuyMarket(self.Volume)
        self._stop_price = candle.LowPrice
        self._target_price = close + Decimal(self._reward_risk_ratio.Value) * (close - candle.LowPrice)

    def CreateClone(self):
        return hammer_candle_strategy()
