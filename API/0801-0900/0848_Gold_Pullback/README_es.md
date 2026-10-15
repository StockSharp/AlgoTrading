# Estrategia de Pullback del Oro
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La Estrategia de Pullback del Oro combina la dirección de tendencia con EMA con filtros MACD y TDI. Las operaciones largas se activan cuando el precio toca la EMA de 21 períodos durante una tendencia alcista y tanto MACD como TDI son alcistas. Las operaciones cortas ocurren en pullbacks hacia la EMA21 en tendencias bajistas con MACD y TDI bajistas. Cada posición usa un take profit y stop loss 1:1 basado en la vela de señal más un desplazamiento.

## Detalles
- **Datos**: Velas de precio.
- **Criterios de entrada**:
  - **Largo**: EMA14 por encima de EMA60, la vela toca EMA21, línea MACD por encima de la línea de señal, TDI MA por encima de la señal TDI y RSI por encima de 50.
  - **Corto**: EMA14 por debajo de EMA60, la vela toca EMA21, línea MACD por debajo de la línea de señal, TDI MA por debajo de la señal TDI y RSI por debajo de 50.
- **Criterios de salida**: Stop loss o take profit alcanzado a igual distancia desde la entrada con un desplazamiento añadido.
- **Stops**: `Offset` = 0.1 aplicado al mínimo/máximo de la vela.
- **Valores predeterminados**:
  - `EmaFastLength` = 14
  - `EmaSlowLength` = 60
  - `EmaPullbackLength` = 21
  - `SlOffset` = 0.1
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Largo y Corto
  - Indicadores: EMA, MACD, RSI, SMA
  - Complejidad: Medio
  - Nivel de riesgo: Medio
