# Estrategia CyberiaTrader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es un port simplificado de StockSharp del sistema original **CyberiaTrader.mq5**. Combina varios indicadores técnicos clásicos para evaluar la dirección del mercado y abrir operaciones cuando la mayoría de los filtros coinciden.

## Indicadores

- **MACD** – Detecta cambios de momentum usando EMAs rápidas/lentas y una línea de señal.
- **Media Móvil Simple** – Determina la tendencia predominante.
- **Commodity Channel Index** – Filtra condiciones de sobrecompra/sobreventa.
- **Average Directional Index** – Confirma la fuerza direccional mediante los componentes +DI y -DI.

## Parámetros

| Nombre | Descripción |
| --- | --- |
| `MacdFast` | Período de EMA rápida para MACD. |
| `MacdSlow` | Período de EMA lenta para MACD. |
| `MacdSignal` | Período de la línea de señal para MACD. |
| `MaPeriod` | Longitud del filtro de tendencia de media móvil. |
| `CciPeriod` | Período del Commodity Channel Index. |
| `AdxPeriod` | Período del Average Directional Index. |
| `EnableMacd` | Activar/desactivar el filtro MACD. |
| `EnableMa` | Activar/desactivar el filtro de media móvil. |
| `EnableCci` | Activar/desactivar el filtro CCI. |
| `EnableAdx` | Activar/desactivar el filtro ADX. |
| `CandleType` | Marco temporal de las velas de entrada. |

## Lógica de trading

1. Los valores de todos los indicadores activados se calculan en cada vela completada.
2. Los filtros pueden bloquear compras o ventas según sus respectivas reglas:
   - MACD por encima de su señal bloquea entradas cortas; por debajo bloquea largas.
   - Precio por encima de la media móvil bloquea cortos; por debajo bloquea largos.
   - CCI por encima de +100 bloquea largos; por debajo de -100 bloquea cortos.
   - +DI mayor que -DI bloquea cortos; -DI mayor que +DI bloquea largos.
3. Una operación se abre solo si un lado está permitido y el opuesto está bloqueado.
4. La protección básica de posición utiliza un take-profit del 2% y un stop-loss del 1%.

## Notas

Esta traducción se centra en los filtros direccionales principales del algoritmo original. El extenso análisis de probabilidad y los módulos auxiliares de la versión MQL5 se omiten intencionalmente para mayor claridad.
