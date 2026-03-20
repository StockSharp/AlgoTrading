import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class cross_line_trader_strategy(Strategy):
    DIR_FROM_LABEL = 0
    DIR_FORCE_BUY = 1
    DIR_FORCE_SELL = 2

    LINE_HORIZONTAL = 0
    LINE_TREND = 1
    LINE_VERTICAL = 2

    def __init__(self):
        super(cross_line_trader_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._trade_volume = self.Param("TradeVolume", 1.0)
        self._direction_mode = self.Param("DirectionMode", self.DIR_FROM_LABEL)
        self._buy_label = self.Param("BuyLabel", "Buy")
        self._sell_label = self.Param("SellLabel", "Sell")
        self._line_definitions = self.Param("LineDefinitions",
            "TrendLine|Trend|Buy|64000|50|20|false;HorizontalSell|Horizontal|Sell|68000|0|0|true;HorizontalBuy|Horizontal|Buy|62000|0|0|true")
        self._stop_loss_offset = self.Param("StopLossOffset", 0.0)
        self._take_profit_offset = self.Param("TakeProfitOffset", 0.0)

        self._lines = []
        self._previous_open = None
        self._entry_price = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def TradeVolume(self):
        return self._trade_volume.Value

    @property
    def DirectionMode(self):
        return self._direction_mode.Value

    @property
    def BuyLabel(self):
        return self._buy_label.Value

    @property
    def SellLabel(self):
        return self._sell_label.Value

    @property
    def LineDefinitions(self):
        return self._line_definitions.Value

    @property
    def StopLossOffset(self):
        return self._stop_loss_offset.Value

    @property
    def TakeProfitOffset(self):
        return self._take_profit_offset.Value

    def OnStarted(self, time):
        super(cross_line_trader_strategy, self).OnStarted(time)
        self._lines = self._parse_line_definitions(self.LineDefinitions)
        self._previous_open = None
        self._entry_price = 0.0

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

    def OnOwnTradeReceived(self, trade):
        super(cross_line_trader_strategy, self).OnOwnTradeReceived(trade)
        if trade is None or trade.Trade is None:
            return
        pos = float(self.Position)
        if pos == 0:
            self._entry_price = 0.0
            return
        self._entry_price = float(trade.Trade.Price)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if float(self.TradeVolume) <= 0:
            return

        current_open = float(candle.OpenPrice)

        for line in self._lines:
            if not line["is_active"]:
                continue

            previous_index = line["steps_processed"]
            current_index = previous_index + 1

            if line["type"] == self.LINE_VERTICAL:
                if current_index >= max(1, line["length"]):
                    direction = self._resolve_direction(line)
                    if direction is not None and self._try_open_position(direction, candle):
                        line["is_active"] = False
                line["steps_processed"] = current_index
                continue

            if not line["ray"] and line["length"] > 0 and previous_index >= line["length"]:
                line["is_active"] = False
                continue

            if self._previous_open is None:
                line["steps_processed"] = current_index
                continue

            prev_line_price = self._get_line_price(line, previous_index)
            curr_line_price = self._get_line_price(line, current_index)
            dir_for_line = self._resolve_direction(line)

            if dir_for_line is None:
                line["steps_processed"] = current_index
                continue

            if line["type"] == self.LINE_HORIZONTAL:
                cross_up = self._previous_open <= prev_line_price and current_open > prev_line_price
                cross_down = self._previous_open >= prev_line_price and current_open < prev_line_price
            else:
                cross_up = self._previous_open <= prev_line_price and current_open > curr_line_price
                cross_down = self._previous_open >= prev_line_price and current_open < curr_line_price

            pos = float(self.Position)
            if cross_up and dir_for_line == 1 and pos <= 0:
                if self._try_open_position(1, candle):
                    line["is_active"] = False
            elif cross_down and dir_for_line == -1 and pos >= 0:
                if self._try_open_position(-1, candle):
                    line["is_active"] = False

            line["steps_processed"] = current_index

            if not line["ray"] and line["length"] > 0 and current_index >= line["length"]:
                line["is_active"] = False

        self._manage_protective_exits(candle)
        self._previous_open = current_open

    def _resolve_direction(self, line):
        mode = self.DirectionMode
        if mode == self.DIR_FORCE_BUY:
            return 1
        elif mode == self.DIR_FORCE_SELL:
            return -1
        elif mode == self.DIR_FROM_LABEL:
            buy_label = self.BuyLabel
            sell_label = self.SellLabel
            label = line["label"]
            if buy_label and label.lower() == buy_label.lower():
                return 1
            if sell_label and label.lower() == sell_label.lower():
                return -1
        return None

    def _try_open_position(self, direction, candle):
        pos = float(self.Position)
        if direction == 1:
            if pos > 0:
                return False
            self.BuyMarket(float(self.TradeVolume))
            self._entry_price = float(candle.OpenPrice)
        else:
            if pos < 0:
                return False
            self.SellMarket(float(self.TradeVolume))
            self._entry_price = float(candle.OpenPrice)
        return True

    def _manage_protective_exits(self, candle):
        pos = float(self.Position)
        if pos > 0:
            volume = abs(pos)
            if float(self.StopLossOffset) > 0 and float(candle.LowPrice) <= self._entry_price - float(self.StopLossOffset):
                self.SellMarket(volume)
                return
            if float(self.TakeProfitOffset) > 0 and float(candle.HighPrice) >= self._entry_price + float(self.TakeProfitOffset):
                self.SellMarket(volume)
        elif pos < 0:
            volume = abs(pos)
            if float(self.StopLossOffset) > 0 and float(candle.HighPrice) >= self._entry_price + float(self.StopLossOffset):
                self.BuyMarket(volume)
                return
            if float(self.TakeProfitOffset) > 0 and float(candle.LowPrice) <= self._entry_price - float(self.TakeProfitOffset):
                self.BuyMarket(volume)

    def _get_line_price(self, line, index):
        if line["type"] == self.LINE_VERTICAL:
            return 0.0
        clamped = max(0, index)
        if not line["ray"] and line["length"] > 0:
            clamped = min(clamped, line["length"])
        return line["base_price"] + line["slope"] * clamped

    def _parse_line_definitions(self, raw):
        result = []
        if raw is None or raw.strip() == "":
            return result

        entries = raw.replace("\n", ";").replace("\r", ";").split(";")
        for entry in entries:
            entry = entry.strip()
            if not entry:
                continue
            parts = entry.split("|")
            if len(parts) < 7:
                continue

            name = parts[0].strip()
            type_text = parts[1].strip().lower()
            label = parts[2].strip()
            try:
                base_price = float(parts[3].strip())
            except:
                continue
            try:
                slope = float(parts[4].strip())
            except:
                slope = 0.0
            try:
                length = int(parts[5].strip())
            except:
                length = 0
            ray_text = parts[6].strip().lower()
            ray = ray_text == "true"

            if type_text == "horizontal":
                line_type = self.LINE_HORIZONTAL
            elif type_text == "trend":
                line_type = self.LINE_TREND
            elif type_text == "vertical":
                line_type = self.LINE_VERTICAL
            else:
                continue

            if line_type == self.LINE_VERTICAL and length <= 0:
                length = 1

            result.append({
                "name": name if name else type_text,
                "label": label,
                "type": line_type,
                "base_price": base_price,
                "slope": slope,
                "length": max(0, length),
                "ray": ray,
                "is_active": True,
                "steps_processed": 0
            })

        return result

    def OnReseted(self):
        super(cross_line_trader_strategy, self).OnReseted()
        self._lines = []
        self._previous_open = None
        self._entry_price = 0.0

    def CreateClone(self):
        return cross_line_trader_strategy()
