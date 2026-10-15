# Estrategia Color Trend CF
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es una conversión del experto MQL **Exp_ColorTrend_CF**. Utiliza dos medias móviles exponenciales para detectar cambios de tendencia. La EMA rápida reacciona rápidamente a los movimientos de precio, mientras que la EMA lenta actúa como filtro de tendencia. Se abre una posición larga cuando la EMA rápida cruza por encima de la EMA lenta. Se abre una posición corta cuando la EMA rápida cruza por debajo de la EMA lenta.

## Parámetros

- `Period` – período base para la EMA rápida; la EMA lenta usa el doble de este valor.
- `StopLoss` – distancia de stop-loss en unidades de precio.
- `TakeProfit` – distancia de take-profit en unidades de precio.
- `AllowBuyOpen` – permiso para abrir posiciones largas.
- `AllowSellOpen` – permiso para abrir posiciones cortas.
- `AllowBuyClose` – permiso para cerrar posiciones largas en señal de venta.
- `AllowSellClose` – permiso para cerrar posiciones cortas en señal de compra.
- `CandleType` – marco temporal para el cálculo de indicadores.

## Lógica de trading

1. Suscribirse a las velas del marco temporal seleccionado.
2. Calcular las EMA rápida y lenta.
3. Cuando la EMA rápida cruza por encima de la EMA lenta:
   - Cerrar posiciones cortas si está permitido.
   - Abrir posición larga si está permitido.
4. Cuando la EMA rápida cruza por debajo de la EMA lenta:
   - Cerrar posiciones largas si está permitido.
   - Abrir posición corta si está permitido.
5. Para posiciones abiertas, aplicar niveles de stop-loss y take-profit.

Esta implementación utiliza la API de alto nivel de StockSharp con enlace de indicadores.
