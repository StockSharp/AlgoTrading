# Estrategia de Patrones de Velas Bj
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia busca los patrones de velas Dragonfly Doji y Gravestone Doji. Un Dragonfly Doji con una larga mecha inferior puede señalar reversión alcista, mientras que un Gravestone Doji con una larga mecha superior puede indicar reversión bajista. La estrategia compra después de un Dragonfly Doji y vende después de un Gravestone Doji.

## Detalles

- **Criterios de entrada**: Dragonfly Doji → largo; Gravestone Doji → corto.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Señal opuesta o discrecional.
- **Stops**: No.
- **Valores predeterminados**:
  - `CandleType` = 15 minutos
  - `DojiThreshold` = 0.1
- **Filtros**:
  - Categoría: Patrón
  - Dirección: Ambos
  - Indicadores: Candlestick
  - Stops: No
  - Complejidad: Principiante
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
