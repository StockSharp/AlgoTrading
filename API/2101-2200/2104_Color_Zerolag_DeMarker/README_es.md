# Estrategia Color Zerolag DeMarker
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia convierte el experto MQL5 `Exp_ColorZerolagDeMarker` al framework de StockSharp. Utiliza una combinación personalizada de varios indicadores **DeMarker** para construir líneas de tendencia rápidas y lentas. Las señales de trading se generan cuando estas líneas se cruzan.

## Indicadores

- Cinco indicadores DeMarker con diferentes períodos: 8, 21, 34, 55 y 89.
- Cada valor del indicador se multiplica por un factor de peso (0.05, 0.10, 0.16, 0.26 y 0.43).
- Los valores ponderados se suman para formar la línea **rápida**.
- La línea **lenta** es una versión suavizada exponencialmente de la línea rápida controlada por el parámetro `Smoothing`.

## Lógica de Operación

1. Suscribirse a velas con un marco temporal configurable.
2. En cada vela finalizada, calcular las líneas rápida y lenta.
3. Cuando la línea rápida anterior está por encima de la línea lenta anterior y la línea rápida actual cae por debajo de la línea lenta:
   - Cerrar posiciones cortas si está permitido.
   - Abrir una posición larga si está habilitado.
4. Cuando la línea rápida anterior está por debajo de la línea lenta anterior y la línea rápida actual sube por encima de la línea lenta:
   - Cerrar posiciones largas si está permitido.
   - Abrir una posición corta si está habilitado.
5. Se aplican porcentajes opcionales de stop-loss y take-profit para las posiciones recién abiertas.

## Parámetros

- `CandleTimeframe` – marco temporal para la suscripción de velas.
- `Smoothing` – factor de suavizado para la línea lenta.
- `Factor1`–`Factor5` – factores de peso para cada período de DeMarker.
- `DeMarkerPeriod1`–`DeMarkerPeriod5` – períodos para los indicadores DeMarker.
- `Volume` – volumen de la orden.
- `OpenBuy` / `OpenSell` – habilitar entradas largas/cortas.
- `CloseBuy` / `CloseSell` – habilitar salidas para posiciones largas/cortas.
- `StopLossPct` / `TakeProfitPct` – gestión de riesgo opcional basada en porcentaje.

## Notas

La estrategia opera solo en velas cerradas y utiliza la API de alto nivel de StockSharp (`SubscribeCandles` y `Bind`). Todos los comentarios en el código se proporcionan en inglés para mayor claridad.
