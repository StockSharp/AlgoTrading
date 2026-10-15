# Reversión de Momentum MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia usa el histograma MACD para detectar reversiones de momentum.
Vende en corto cuando la vela alcista crece pero el histograma MACD declina.
Compra cuando la vela bajista crece pero el histograma MACD sube.

## Detalles

- **Criterios de entrada**: Mayor cuerpo de vela con momentum MACD decreciente.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Señal opuesta.
- **Stops**: No.
- **Valores predeterminados**:
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Momentum
  - Dirección: Ambos
  - Indicadores: MACD
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía (1m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
