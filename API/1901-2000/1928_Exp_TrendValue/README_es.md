# Estrategia Exp TrendValue
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Una estrategia basada en el indicador TrendValue. Construye bandas dinámicas de soporte y resistencia utilizando medias móviles ponderadas de los precios máximos y mínimos desplazados por ATR. Se detecta una nueva tendencia alcista o bajista cuando el precio cruza la banda opuesta.

## Entrada y Salida
- **Entrada largo**: Cuando comienza una nueva tendencia alcista.
- **Entrada corto**: Cuando comienza una nueva tendencia bajista.
- **Salida largo**: Con una señal bajista o línea de tendencia.
- **Salida corto**: Con una señal alcista o línea de tendencia.

## Parámetros
- `BuyPosOpen` / `SellPosOpen` – activar entradas largas/cortas.
- `BuyPosClose` / `SellPosClose` – permitir el cierre de posiciones largas/cortas.
- `StopLossPips` – stop loss en puntos de precio.
- `TakeProfitPips` – take profit en puntos de precio.
- `MaPeriod` – período de la media móvil ponderada.
- `ShiftPercent` – desplazamiento porcentual aplicado a las medias.
- `AtrPeriod` – período ATR.
- `AtrSensitivity` – multiplicador aplicado al ATR.
- `CandleType` – marco temporal de las velas.

## Notas
La estrategia se suscribe a datos de velas y actualiza los indicadores en cada vela completada. Las órdenes de mercado se colocan cuando se cumplen las condiciones y los niveles de stop y take profit se siguen internamente.
