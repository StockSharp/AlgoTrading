# Estrategia de Señal Virtual TradePad
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia recrea la lógica del panel de múltiples indicadores de la herramienta VirtualTradePad de MetaTrader. Rastrea doce señales —
basadas en tendencia, momentum y canales — y solo opera cuando un número configurable de indicadores están de acuerdo. El objetivo es imitar la
matriz de sentimiento visual del panel original y convertirla en una estrategia StockSharp completamente automatizada.

## Cómo funciona

- **Datos**: opera un único instrumento en el tipo de vela seleccionado (por defecto 15 minutos).
- **Indicadores**:
  - Medias móviles simples rápida/lenta para la dirección del cruce.
  - Cruce de la línea MACD y la señal.
  - Salidas de sobrecompra/sobreventa del Estocástico %K (niveles 20/80).
  - Reversiones en los umbrales 30/70 del RSI.
  - Reversiones en los niveles -100/+100 del CCI.
  - Reversiones en los niveles -80/-20 del Williams %R.
  - Ruptura de vuelta al interior del canal de las Bandas de Bollinger.
  - Ruptura de vuelta al interior del canal del Envelope de media móvil.
  - Alineación de mandíbula/dientes/labios del Alligator de Bill Williams.
  - Pendiente de la Media Móvil Adaptativa de Kaufman (ascendente/descendente).
  - Cruces de la línea cero del Awesome Oscillator.
  - Cruce de Tenkan-Kijun del Ichimoku.
- Cada indicador produce un voto de compra (+1), venta (-1) o neutral (0). Cuando el recuento de votos de compra (o venta) alcanza el
  parámetro **MinimumConfirmations** y supera el lado opuesto, la estrategia abre una posición en esa dirección.
- La opción **CloseOnOpposite** cierra la posición cuando el recuento de votos opuesto alcanza el umbral.
- **Gestión del riesgo**: take profit y stop loss opcionales definidos en pasos del precio del instrumento.

## Parámetros

- `FastMaLength`, `SlowMaLength` – longitudes de las medias móviles para el cruce.
- `MacdFastLength`, `MacdSlowLength`, `MacdSignalLength` – configuración del MACD.
- `StochasticLength`, `StochasticDLength`, `StochasticSlowing` – configuración del oscilador Estocástico.
- `RsiLength`, `CciLength`, `WilliamsLength` – lookbacks de los osciladores.
- `BollingerLength`, `BollingerDeviation` – Bandas de Bollinger.
- `EnvelopeLength`, `EnvelopeDeviation` – Envelopes porcentuales alrededor de la SMA.
- `AlligatorJawLength`, `AlligatorTeethLength`, `AlligatorLipsLength` – SMMAs del Alligator.
- `AmaLength`, `AmaFastPeriod`, `AmaSlowPeriod` – configuración del AMA de Kaufman.
- `IchimokuTenkanLength`, `IchimokuKijunLength`, `IchimokuSenkouLength` – líneas del Ichimoku.
- `AoShortPeriod`, `AoLongPeriod` – ventanas del Awesome Oscillator.
- `MinimumConfirmations` – número de señales alineadas requeridas para entrar.
- `AllowLong`, `AllowShort` – habilitar lados largo/corto.
- `CloseOnOpposite` – salir cuando el recuento de votos opuesto satisface el umbral.
- `TakeProfitPips`, `StopLossPips` – objetivos de riesgo opcionales en pasos de precio (0 desactiva).
- `CandleType` – marco temporal/tipo de dato para el análisis.

## Resumen de la lógica de trading

1. Actualizar todos los indicadores cuando cierra una vela.
2. Contar los votos alcistas y bajistas de los indicadores.
3. Entrar largo/corto cuando los votos alcanzan el umbral de confirmación y superan el lado opuesto.
4. Opcionalmente cerrar cuando el lado opuesto alcanza el umbral.
5. Aplicar take profit/stop loss opcionales medidos en pasos de precio.

La estrategia está diseñada para traders discrecionales que disfrutaban del tablero de sentimiento de VirtualTradePad pero desean una
implementación automatizada dentro del framework StockSharp.
