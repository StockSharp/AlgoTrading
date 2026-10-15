# Estrategia JSatl Sistema Digit
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este ejemplo demuestra una adaptación simplificada del asesor experto MQL5 "JSatl Digit System" a StockSharp.

La estrategia usa la Media Móvil Jurik (JMA) para crear un estado de tendencia digital:

- Cuando el precio de cierre está por encima de la JMA, el estado se convierte en **alcista**.
- Cuando el precio de cierre está por debajo de la JMA, el estado se convierte en **bajista**.

Si el estado cambia a alcista, las posiciones cortas pueden cerrarse y/o se puede abrir una posición larga según los parámetros. Cuando el estado cambia a bajista, las posiciones largas pueden cerrarse y/o se puede abrir una posición corta.

**Parámetros**

- `JmaLength` – período de la JMA.
- `CandleType` – serie de velas usada para los cálculos.
- `StopLossPercent` – stop-loss protector en porcentaje.
- `TakeProfitPercent` – take-profit protector en porcentaje.
- `BuyPosOpen`, `SellPosOpen`, `BuyPosClose`, `SellPosClose` – habilitar o deshabilitar acciones para las señales correspondientes.
