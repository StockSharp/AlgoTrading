# Experto en Divergencia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que opera divergencias de precio con RSI. Detecta divergencia alcista cuando el precio forma un mínimo más bajo pero el RSI forma un mínimo más alto, y divergencia bajista cuando el precio forma un máximo más alto pero el RSI forma un máximo más bajo. Entra en posiciones largas o cortas en consecuencia y utiliza un stop loss porcentual.

## Detalles

- **Criterios de entrada:**
  - Largo: el precio forma un nuevo mínimo y el RSI forma un mínimo más alto (divergencia alcista)
  - Corto: el precio forma un nuevo máximo y el RSI forma un máximo más bajo (divergencia bajista)
- **Largo/Corto:** Ambos
- **Criterios de salida:**
  - Largo: el precio alcanza el stop loss o aparece divergencia bajista
  - Corto: el precio alcanza el stop loss o aparece divergencia alcista
- **Stops:** Porcentaje desde el precio de entrada
- **Valores predeterminados:**
  - `RsiPeriod` = 14
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros:**
  - Categoría: Divergencia
  - Dirección: Ambos
  - Indicadores: RSI
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: Sí
  - Nivel de riesgo: Medio
