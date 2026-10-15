import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy


class vegas_tunnel_strategy(Strategy):
    def __init__(self):
        super(vegas_tunnel_strategy, self).__init__()
        self._risk_reward_ratio = self.Param("RiskRewardRatio", 2) \
            .SetDisplay("Risk/Reward", "Risk to reward ratio", "General")
        self._stop_mult = self.Param("StopMult", 3) \
            .SetDisplay("Stop Mult", "StdDev multiplier for stop", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._stop_price = 0.0
        self._take_price = 0.0
        self._std_val = 0.0
        self._prev_slow = 0.0
        self._prev_tunnel = 0.0
        self._cooldown = 0

    @property
    def risk_reward_ratio(self):
        return self._risk_reward_ratio.Value

    @property
    def stop_mult(self):
        return self._stop_mult.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(vegas_tunnel_strategy, self).OnReseted()
        self._stop_price = 0.0
        self._take_price = 0.0
        self._std_val = 0.0
        self._prev_slow = 0.0
        self._prev_tunnel = 0.0
        self._cooldown = 0

    def OnStarted2(self, time):
        super(vegas_tunnel_strategy, self).OnStarted2(time)
        ema_slow = ExponentialMovingAverage()
        ema_slow.Length = 144
        ema_tunnel = ExponentialMovingAverage()
        ema_tunnel.Length = 169
        std_dev = StandardDeviation()
        std_dev.Length = 20
        subscription = self.SubscribeCandles(self.candle_type)
        subscription \
            .Bind(std_dev, self._on_std) \
            .Bind(ema_slow, ema_tunnel, self.on_process) \
            .Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema_slow)
            self.DrawIndicator(area, ema_tunnel)
            self.DrawOwnTrades(area)

    def _on_std(self, candle, val):
        self._std_val = float(val)

    def on_process(self, candle, slow, tunnel):
        if candle.State != CandleStates.Finished:
            return
        slow = float(slow)
        tunnel = float(tunnel)
        if self._cooldown > 0:
            self._cooldown -= 1
        if self._std_val <= 0:
            self._prev_slow = slow
            self._prev_tunnel = tunnel
            return
        # Exit management
        if self.Position > 0 and self._stop_price > 0:
            if float(candle.LowPrice) <= self._stop_price or float(candle.HighPrice) >= self._take_price:
                self.SellMarket()
                self._stop_price = 0
                self._take_price = 0
                self._cooldown = 80
        elif self.Position < 0 and self._stop_price > 0:
            if float(candle.HighPrice) >= self._stop_price or float(candle.LowPrice) <= self._take_price:
                self.BuyMarket()
                self._stop_price = 0
                self._take_price = 0
                self._cooldown = 80
        if self._cooldown > 0 or self._prev_slow == 0:
            self._prev_slow = slow
            self._prev_tunnel = tunnel
            return
        # Entry: slow EMA (144) crosses tunnel EMA (169)
        slow_cross_above_tunnel = self._prev_slow <= self._prev_tunnel and slow > tunnel
        slow_cross_below_tunnel = self._prev_slow >= self._prev_tunnel and slow < tunnel
        if slow_cross_above_tunnel and float(candle.ClosePrice) > tunnel and self.Position <= 0:
            self.BuyMarket()
            entry = float(candle.ClosePrice)
            self._stop_price = entry - float(self.stop_mult) * self._std_val
            self._take_price = entry + (entry - self._stop_price) * float(self.risk_reward_ratio)
            self._cooldown = 80
        elif slow_cross_below_tunnel and float(candle.ClosePrice) < tunnel and self.Position >= 0:
            self.SellMarket()
            entry = float(candle.ClosePrice)
            self._stop_price = entry + float(self.stop_mult) * self._std_val
            self._take_price = entry - (self._stop_price - entry) * float(self.risk_reward_ratio)
            self._cooldown = 80
        self._prev_slow = slow
        self._prev_tunnel = tunnel

    def CreateClone(self):
        return vegas_tunnel_strategy()
