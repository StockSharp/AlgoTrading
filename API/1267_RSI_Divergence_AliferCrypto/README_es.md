# Estrategia de Divergencia RSI - AliferCrypto
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en divergencias de RSI con filtros opcionales de zona y tendencia. El stop loss y el take profit pueden calcularse a partir de swings o ATR con actualizaciones dinámicas o estáticas.

## Lógica
- **Entrada**
  - Divergencia alcista: el precio forma un mínimo más bajo mientras el RSI forma un mínimo más alto.
  - Divergencia bajista: el precio forma un máximo más alto mientras el RSI forma un máximo más bajo.
  - El filtro opcional de zona RSI requiere un estado previo de sobreventa/sobrecompra.
  - El filtro opcional de tendencia usa la dirección de la media móvil.
- **Salida**
  - SL/TP a partir del swing reciente o ATR.
  - Los niveles pueden fijarse en la entrada o recalcularse en cada barra.

## Indicadores
- Relative Strength Index
- Moving Average
- Average True Range
- Highest/Lowest
