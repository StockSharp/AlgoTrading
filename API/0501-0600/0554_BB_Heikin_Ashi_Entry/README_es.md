# Estrategia de Entrada Bollinger Heikin Ashi
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que utiliza Bollinger Bands en velas Heikin Ashi. Compra después de dos velas Heikin Ashi bajistas consecutivas tocando la banda inferior, seguidas de una vela alcista por encima de ella. Vende en reversa.

Tras entrar, se toma un primer objetivo igual al riesgo y el stop se ajusta de forma trailing usando los extremos de la vela anterior.

## Detalles

- **Criterios de entrada**:
  - Largo: dos velas HA bajistas tocando la banda inferior, luego alcista por encima
  - Corto: dos velas HA alcistas tocando la banda superior, luego bajista por debajo
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - Largo: primer objetivo a 1R, luego stop trailing en mínimos previos
  - Corto: primer objetivo a 1R, luego stop trailing en máximos previos
- **Stops**: Mínimo/máximo de la vela anterior
- **Valores predeterminados**:
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Reversión
  - Dirección: Ambos
  - Indicadores: Bollinger Bands, Heikin Ashi
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
