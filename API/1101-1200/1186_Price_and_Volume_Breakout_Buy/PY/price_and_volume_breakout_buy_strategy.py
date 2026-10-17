import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import Math, TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class price_and_volume_breakout_buy_strategy(Strategy):
    def __init__(self):
        super(price_and_volume_breakout_buy_strategy, self).__init__()
        self._price_breakout_period = self.Param("PriceBreakoutPeriod", 60) \
            .SetGreaterThanZero() \
            .SetDisplay("Price Breakout Period", "Number of preceding candles used for the price extreme.", "Breakout")
        self._volume_breakout_period = self.Param("VolumeBreakoutPeriod", 60) \
            .SetGreaterThanZero() \
            .SetDisplay("Volume Breakout Period", "Number of preceding candles used for maximum volume.", "Breakout")
        self._trendline_length = self.Param("TrendlineLength", 200) \
            .SetGreaterThanZero() \
            .SetDisplay("Trendline Length", "Trend SMA period.", "Trend")
        self._order_direction = self.Param("OrderDirection", "Long") \
            .SetDisplay("Order Direction", "Long, Short, or Both.", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromDays(1))) \
            .SetDisplay("Candle Type", "Candle type.", "General")
        self._previous_bars = []
        self._opposite_closes = 0
        self._entry_requested = False
        self._exit_requested = False

    def GetWorkingSecurities(self):
        return [(self.Security, self._candle_type.Value)]

    def OnReseted(self):
        super(price_and_volume_breakout_buy_strategy, self).OnReseted()
        self._previous_bars = []
        self._opposite_closes = 0
        self._entry_requested = False
        self._exit_requested = False

    def OnStarted2(self, time):
        super(price_and_volume_breakout_buy_strategy, self).OnStarted2(time)
        if self._order_direction.Value not in ("Long", "Short", "Both"):
            raise ValueError("OrderDirection must be Long, Short, or Both")

        self._trend = SimpleMovingAverage()
        self._trend.Length = int(self._trendline_length.Value)
        subscription = self.SubscribeCandles(self._candle_type.Value)
        subscription.Bind(self._trend, self.on_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._trend)
            self.DrawOwnTrades(area)

    def on_candle(self, candle, average):
        if candle.State != CandleStates.Finished:
            return

        price_period = int(self._price_breakout_period.Value)
        volume_period = int(self._volume_breakout_period.Value)
        window = max(price_period, volume_period)

        # Evaluate using previous bricks/bars only, before adding the current one.
        if self._trend.IsFormed and len(self._previous_bars) >= window:
            close = float(candle.ClosePrice)
            sma = float(average)
            position = self.Position

            if position != 0:
                self._entry_requested = False
                if not self._exit_requested:
                    opposite = close < sma if position > 0 else close > sma
                    self._opposite_closes = self._opposite_closes + 1 if opposite else 0
                    if self._opposite_closes == 5:
                        self._exit_requested = True
                        self._opposite_closes = 0
                        if position > 0:
                            self.SellMarket(position)
                        else:
                            self.BuyMarket(Math.Abs(position))
            else:
                self._exit_requested = False
                self._opposite_closes = 0
                if not self._entry_requested:
                    previous_price_bars = self._previous_bars[-price_period:]
                    previous_volume_bars = self._previous_bars[-volume_period:]
                    max_volume = max(bar[2] for bar in previous_volume_bars)
                    volume = float(candle.TotalVolume)
                    long_signal = (close > max(bar[0] for bar in previous_price_bars)
                                   and volume > max_volume and close > sma)
                    short_signal = (close < min(bar[1] for bar in previous_price_bars)
                                    and volume > max_volume and close < sma)
                    direction = self._order_direction.Value
                    if long_signal and direction in ("Long", "Both"):
                        self._entry_requested = True
                        self.BuyMarket(self.Volume)
                    elif short_signal and direction in ("Short", "Both"):
                        self._entry_requested = True
                        self.SellMarket(self.Volume)

        self._previous_bars.append((float(candle.HighPrice),
                                    float(candle.LowPrice),
                                    float(candle.TotalVolume)))
        if len(self._previous_bars) > window:
            self._previous_bars.pop(0)

    def CreateClone(self):
        return price_and_volume_breakout_buy_strategy()
