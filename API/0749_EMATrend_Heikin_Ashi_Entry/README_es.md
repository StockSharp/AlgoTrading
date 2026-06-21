# Estrategia de Tendencia EMA con Entrada Heikin Ashi
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que usa Bandas de Bollinger en velas Heikin Ashi con un filtro de tendencia EMA de marco temporal superior. Compra tras velas Heikin Ashi bajistas consecutivas tocando la banda inferior seguidas de una vela alcista por encima cuando la EMA rápida del marco temporal superior está sobre la EMA lenta. Vende a la inversa.

Tras entrar, se toma un primer objetivo igual al riesgo y el stop se ajusta usando los extremos de la vela anterior.

## Detalles

- **Criterios de entrada**:
  - Largo: al menos dos velas HA bajistas tocando la banda inferior, luego alcista por encima con la EMA rápida del marco temporal superior por encima de la EMA lenta
  - Corto: al menos dos velas HA alcistas tocando la banda superior, luego bajista por debajo con la EMA rápida del marco temporal superior por debajo de la EMA lenta
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - Largo: primer objetivo en 1R, luego trailing stop en mínimos anteriores
  - Corto: primer objetivo en 1R, luego trailing stop en máximos anteriores
- **Stops**: Mínimo/máximo de la vela anterior
- **Valores predeterminados**:
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2m
  - `FastEmaPeriod` = 9
  - `SlowEmaPeriod` = 21
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
  - `HigherTimeframe` = TimeSpan.FromMinutes(180).TimeFrame()
- **Filtros**:
  - Categoría: Patrón
  - Dirección: Ambos
  - Indicadores: Bollinger Bands, Heikin Ashi, EMA
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
