# Estrategia Ma2Cci EMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de cruce de dos medias móviles exponenciales confirmada por una ruptura de la línea cero del Índice del Canal de Materias Primas (CCI). El tamaño de la posición y la colocación del stop se derivan de la volatilidad del Rango Verdadero Promedio (ATR) y un porcentaje de riesgo configurable.

## Detalles

- **Datos**: Velas basadas en tiempo (predeterminado 1 hora) suministradas por el parámetro `Candle Type` seleccionado.
- **Entrada**: Ir largo cuando la EMA rápida cruza por encima de la EMA lenta y el CCI cruza por encima de cero en la misma barra; ir corto en el cruce opuesto con el CCI rompiendo por debajo de cero.
- **Salida**: Cerrar posiciones largas cuando la EMA rápida vuelve a cruzar por debajo de la EMA lenta o el precio toca el stop fijo; cerrar cortos cuando la EMA rápida cruza por encima de la EMA lenta o el precio alcanza el stop corto.
- **Riesgo**: La distancia del stop equivale al mayor entre el ATR (longitud `AtrPeriod`) o `MinStopPoints` multiplicado por el paso de precio del instrumento. El tamaño de la operación es el valor de la cartera por `RiskPercent`, dividido por esa distancia de stop.
- **Instrumentos**: Símbolos de forex o índices con seguimiento de tendencia que admiten cobertura en la versión original de MetaTrader; también aplicable a otros activos con oscilaciones de momentum claras.
- **Entorno**: Diseñado para mercados de sesión continua donde las señales EMA/CCI se alinean con los controles de riesgo basados en ATR.

## Parámetros

- `CandleType` – Marco temporal y tipo de datos utilizado para cálculos y flujo de órdenes.
- `FastMaPeriod` – Período de la EMA rápida (predeterminado 10).
- `SlowMaPeriod` – Período de la EMA lenta (predeterminado 37).
- `CciPeriod` – Lookback del oscilador CCI que confirma el momentum (predeterminado 39).
- `AtrPeriod` – Longitud del ATR utilizado para estimar la volatilidad actual para la colocación de stops (predeterminado 3).
- `RiskPercent` – Fracción del capital de la cartera actual arriesgado por operación (predeterminado 2%).
- `MinStopPoints` – Distancia mínima del stop expresada en pasos de precio para emular el filtro de pips de MetaTrader (predeterminado 15).

## Notas

- Funciona mejor cuando se ejecuta en pares líquidos e índices donde los cruces EMA/CCI son fiables; los mercados delgados pueden activar salidas prematuras.
- Como los stops se recalculan solo en la entrada, la estrategia mantiene el perfil de riesgo estable y refleja la lógica de stop-loss fijo del experto MQL original.
- La valoración de la cartera debe ser proporcionada por la cuenta conectada para que el dimensionamiento de la posición funcione; de lo contrario, el motor recurre al `Volume` de la estrategia o al volumen mínimo del instrumento.
