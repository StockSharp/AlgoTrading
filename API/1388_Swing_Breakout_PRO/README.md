# Swing Breakout Strategy PRO
[Русский](README_ru.md) | [中文](README_cn.md)

Breakout strategy that trades when price closes beyond the last confirmed swing high or low. The distance between the latest swing points defines stop-loss and target levels.

## Details

- **Long**: previous close above last swing high and current high above previous high.
- **Short**: previous close below last swing low and current low below previous low.
- **Stops**: opposite swing level.
- **Targets**: range between last swing high and low.
- **Indicators**: internal pivot calculation.

