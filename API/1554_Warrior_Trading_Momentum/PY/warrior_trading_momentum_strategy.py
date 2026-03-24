import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex, StandardDeviation
from StockSharp.Algo.Strategies import Strategy


class warrior_trading_momentum_strategy(Strategy):
    def __init__(self):
        super(warrior_trading_momentum_strategy, self).__init__()
        self._min_red_candles = self.Param("MinRedCandles", 3) \
            .SetDisplay("Min Red", "Red candles before reversal", "Momentum")
        self._risk_reward = self.Param("RiskReward", 2.0) \
            .SetDisplay("Risk Reward", "TP to SL ratio", "Risk")
        self._max_daily_trades = self.Param("MaxDailyTrades", 1) \
            .SetDisplay("Max Trades", "Daily trade limit", "Risk")
        self._vol_avg_length = self.Param("VolAvgLength", 20) \
            .SetDisplay("Vol Avg Length", "Volume average period", "Parameters")
        self._vol_mult = self.Param("VolMult", 3.0) \
            .SetDisplay("Vol Mult", "Volume spike multiplier", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._volumes = []
        self._stop_price = 0.0
        self._take_profit_price = 0.0
        self._red_count = 0
        self._current_day = None
        self._daily_trades = 0

    @property
    def min_red_candles(self):
        return self._min_red_candles.Value

    @property
    def risk_reward(self):
        return self._risk_reward.Value

    @property
    def max_daily_trades(self):
        return self._max_daily_trades.Value

    @property
    def vol_avg_length(self):
        return self._vol_avg_length.Value

    @property
    def vol_mult(self):
        return self._vol_mult.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(warrior_trading_momentum_strategy, self).OnReseted()
        self._volumes = []
        self._stop_price = 0.0
        self._take_profit_price = 0.0
        self._red_count = 0
        self._current_day = None
        self._daily_trades = 0

    def OnStarted(self, time):
        super(warrior_trading_momentum_strategy, self).OnStarted(time)
        ema = ExponentialMovingAverage()
        ema.Length = 20
        rsi = RelativeStrengthIndex()
        rsi.Length = 14
        std_dev = StandardDeviation()
        std_dev.Length = 14
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, rsi, std_dev, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def on_process(self, candle, ema_val, rsi_val, std_val):
        if candle.State != CandleStates.Finished:
            return
        ema_val = float(ema_val)
        rsi_val = float(rsi_val)
        std_val = float(std_val)
        close = float(candle.ClosePrice)
        day = candle.OpenTime.Date
        if day != self._current_day:
            self._current_day = day
            self._daily_trades = 0
        self._volumes.append(float(candle.TotalVolume))
        while len(self._volumes) > int(self.vol_avg_length) + 1:
            self._volumes.pop(0)
        if self.Position > 0:
            if float(candle.LowPrice) <= self._stop_price or float(candle.HighPrice) >= self._take_profit_price:
                self.SellMarket()
                self._stop_price = 0.0
                self._take_profit_price = 0.0
                return
        elif self.Position < 0:
            if float(candle.HighPrice) >= self._stop_price or float(candle.LowPrice) <= self._take_profit_price:
                self.BuyMarket()
                self._stop_price = 0.0
                self._take_profit_price = 0.0
                return
        if candle.ClosePrice < candle.OpenPrice:
            self._red_count += 1
        else:
            self._red_count = 0
        val = int(self.vol_avg_length)
        if std_val <= 0 or len(self._volumes) < val or self._daily_trades >= int(self.max_daily_trades):
            return
        if self.Position != 0:
            return
        vol_avg = sum(self._volumes[:val]) / val
        volume_spike = float(candle.TotalVolume) > vol_avg * float(self.vol_mult)
        bullish = candle.ClosePrice > candle.OpenPrice
        if self._red_count >= int(self.min_red_candles) and bullish and volume_spike and close > ema_val and rsi_val > 55:
            self.BuyMarket()
            stop_dist = std_val * 2.0
            self._stop_price = close - stop_dist
            self._take_profit_price = close + stop_dist * float(self.risk_reward)
            self._daily_trades += 1
        elif close < ema_val and rsi_val < 45 and volume_spike:
            self.SellMarket()
            stop_dist = std_val * 2.0
            self._stop_price = close + stop_dist
            self._take_profit_price = close - stop_dist * float(self.risk_reward)
            self._daily_trades += 1

    def CreateClone(self):
        return warrior_trading_momentum_strategy()
