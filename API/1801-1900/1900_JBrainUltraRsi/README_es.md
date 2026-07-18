# Estrategia JBrainUltraRSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia de ejemplo combina el Índice de Fuerza Relativa (RSI) y el oscilador Estocástico para generar señales de trading.
La idea se deriva del Asesor Experto original de MetaTrader que utilizaba los indicadores *JBrainTrendSig1* y *UltraRSI*. En esta adaptación, el oscilador Estocástico actúa como filtro de tendencia mientras que el RSI proporciona señales de entrada.

## Cómo funciona

1. **Indicadores**
   - **RSI**: Mide el momentum comparando ganancias y pérdidas recientes. Un cruce por encima del nivel 50 indica momentum alcista, mientras que un cruce por debajo de 50 indica momentum bajista.
   - **Oscilador Estocástico**: Evalúa la posición del cierre relativa al rango reciente. Los cruces de las líneas %K y %D confirman la dirección de la tendencia.
2. **Modos**
   - **JBrainSig1Filter** – El RSI genera señales y el oscilador Estocástico confirma la dirección.
   - **UltraRsiFilter** – El oscilador Estocástico proporciona señales filtradas por el RSI.
   - **Composition** – Las señales se toman solo cuando ambos indicadores coinciden en la dirección.
3. **Reglas de trading**
   - Una posición larga se abre cuando aparece una señal de compra y la posición corta está ausente o cerrada.
   - Una posición corta se abre cuando aparece una señal de venta y la posición larga está ausente o cerrada.
   - Las señales inversas cierran posiciones existentes si está permitido.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `RsiPeriod` | Período de cálculo del RSI. |
| `StochLength` | Período %K para el oscilador Estocástico. |
| `SignalLength` | Período %D para el oscilador Estocástico. |
| `Mode` | Modo de combinación de indicadores. |
| `AllowLongEntry` / `AllowShortEntry` | Permisos para abrir posiciones largas o cortas. |
| `AllowLongExit` / `AllowShortExit` | Permisos para cerrar posiciones largas o cortas. |
| `CandleType` | Marco temporal de velas utilizado por la estrategia. |

## Notas

- La estrategia utiliza la API de alto nivel de StockSharp con `Bind` / `BindEx` para el procesamiento de indicadores.
- Los stops y objetivos pueden configurarse con el mecanismo de protección integrado `StartProtection()`.
- La visualización de ejemplo dibuja velas, indicadores y operaciones propias si hay un área de gráfico disponible.
