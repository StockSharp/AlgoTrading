import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import BollingerBands, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class bollinger_rsi_countertrend_sol_strategy(Strategy):
    def __init__(self):
        super(bollinger_rsi_countertrend_sol_strategy, self).__init__()
        self._bollinger_period = self.Param("BollingerPeriod", 20) \
            .SetDisplay("Bollinger Period", "Bollinger period", "Parameters")
        self._bollinger_width = self.Param("BollingerWidth", 2.0) \
            .SetDisplay("Bollinger Width", "Bollinger width", "Parameters")
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetDisplay("RSI Length", "RSI period", "Parameters")
        self._long_rsi = self.Param("LongRsi", 25.0) \
            .SetDisplay("Long RSI", "RSI threshold for longs", "Parameters")
        self._short_rsi = self.Param("ShortRsi", 79.0) \
            .SetDisplay("Short RSI", "RSI threshold for shorts", "Parameters")
        self._short_profit_percent = self.Param("ShortProfitPercent", 3.5) \
            .SetDisplay("Short Profit %", "Short profit percent", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_close = 0.0
        self._prev_upper = 0.0
        self._prev_lower = 0.0
        self._prev_basis = 0.0
        self._prev_low = 0.0
        self._long_sl_level = None
        self._short_entry_price = None

    @property
    def bollinger_period(self):
        return self._bollinger_period.Value

    @property
    def bollinger_width(self):
        return self._bollinger_width.Value

    @property
    def rsi_length(self):
        return self._rsi_length.Value

    @property
    def long_rsi(self):
        return self._long_rsi.Value

    @property
    def short_rsi(self):
        return self._short_rsi.Value

    @property
    def short_profit_percent(self):
        return self._short_profit_percent.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(bollinger_rsi_countertrend_sol_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._prev_upper = 0.0
        self._prev_lower = 0.0
        self._prev_basis = 0.0
        self._prev_low = 0.0
        self._long_sl_level = None
        self._short_entry_price = None

    def OnStarted(self, time):
        super(bollinger_rsi_countertrend_sol_strategy, self).OnStarted(time)
        bollinger = BollingerBands()
        bollinger.Length = self.bollinger_period
        bollinger.Width = self.bollinger_width
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bollinger, rsi, self.on_process).Start()
        self.StartProtection(
            takeProfit=Unit(2, UnitTypes.Percent),
            stopLoss=Unit(1, UnitTypes.Percent))
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bollinger)
            self.DrawOwnTrades(area)

    def on_process(self, candle, bb_value, rsi_val):
        if candle.State != CandleStates.Finished:
            return
        upper = float(bb_value.UpBand)
        lower = float(bb_value.LowBand)
        middle = float(bb_value.MovingAverage)
        if upper == 0 or lower == 0:
            return
        rsi_value = float(rsi_val) if rsi_val.IsFormed else 50.0
        close = float(candle.ClosePrice)
        long_entry = self._prev_close != 0 and self._prev_close < self._prev_lower and close > lower and rsi_value < self.long_rsi
        short_entry = self._prev_close != 0 and self._prev_close > self._prev_upper and close < upper and rsi_value > self.short_rsi
        if long_entry and self.Position == 0:
            self.BuyMarket()
        elif short_entry and self.Position == 0:
            self.SellMarket()
        self._prev_close = close
        self._prev_upper = upper
        self._prev_lower = lower
        self._prev_basis = middle
        self._prev_low = float(candle.LowPrice)

    def CreateClone(self):
        return bollinger_rsi_countertrend_sol_strategy()
