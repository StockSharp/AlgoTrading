# Buscador de Operaciones Largas a Corto Plazo por Divergencia Alcista
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia busca divergencias alcistas entre el precio y el RSI. Cuando el precio marca un mínimo más bajo pero el RSI forma un mínimo más alto dentro de un rango de pivote especificado y el RSI horario está por debajo de 40, la estrategia entra en una posición larga. La posición se cierra cuando el RSI sube por encima de un umbral, aparece una divergencia bajista o se activa el stop loss.

- **Condiciones de entrada**:
  - El mínimo actual está por debajo del precio del mínimo del pivote anterior.
  - RSI forma un mínimo más alto por debajo de `RsiBullConditionMin` y el pivote anterior ocurre dentro de 5–50 barras.
  - El RSI horario está por debajo de `RsiHourEntryThreshold`.
  - El precio de cierre está por debajo del precio del mínimo del pivote anterior.
- **Condiciones de salida**:
  - RSI cruza por encima de `SellWhenRsi`.
  - Divergencia bajista: el precio marca un máximo más alto mientras el RSI marca un máximo más bajo.
  - Stop loss activado mediante `StartProtection` en `StopLossPercent`.
- **Indicadores**: RSI.
