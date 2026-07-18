# Estrategia Lego 4 Beta
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es un sistema modular traducido del script de MetaTrader "exp_Lego_4_Beta". Combina varios indicadores técnicos comunes y permite habilitar o deshabilitar cada componente mediante parámetros.

## Algoritmo

1. **Cruce de Medias Móviles** – Se calculan una media móvil rápida y una lenta. Se abre una posición larga cuando la media rápida cruza al alza la media lenta. Se abre una posición corta en el cruce opuesto.
2. **Filtro del Oscilador Estocástico** – Cuando está habilitado, las entradas largas requieren que el valor %K del Estocástico esté por debajo del nivel de sobreventа, y las entradas cortas requieren que %K esté por encima del nivel de sobrecompra.
3. **Salida por RSI** – Cuando está habilitado, las posiciones largas existentes se cierran si el RSI sube por encima del umbral alto. Las posiciones cortas se cierran cuando el RSI cae por debajo del umbral bajo.

## Parámetros

- `UseMaOpen` – activar señales de cruce de medias móviles.
- `FastMaLength` / `SlowMaLength` – longitudes de las medias rápida y lenta.
- `MaType` – tipo de media móvil (SMA, EMA, WMA).
- `UseStochasticOpen` – habilitar el filtro estocástico para entradas.
- `StochLength` – período principal para el cálculo del Estocástico.
- `StochKPeriod` / `StochDPeriod` – períodos de suavizado para las líneas %K y %D.
- `StochBuyLevel` / `StochSellLevel` – umbrales de sobreventa y sobrecompra.
- `UseRsiClose` – habilitar salidas basadas en RSI.
- `RsiPeriod` – longitud del cálculo de RSI.
- `RsiHigh` / `RsiLow` – umbrales de RSI para cerrar posiciones.
- `CandleType` – tipo de vela para suscribirse.

## Notas

La estrategia utiliza `SubscribeCandles` de alto nivel con `BindEx` para procesar valores de indicadores y sigue el estilo recomendado de StockSharp. Solo se usan órdenes de mercado para entradas y salidas.
