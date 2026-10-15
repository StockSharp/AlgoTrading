# Estrategia Zahorchak Measure
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Calcula una puntuación ponderada usando múltiples medias móviles. Compra cuando la puntuación se vuelve positiva y vende cuando se vuelve negativa.

## Detalles

- **Criterios de entrada**: La puntuación cruza por encima de cero
- **Largo/Corto**: Ambos
- **Criterios de salida**: Señal opuesta
- **Stops**: No
- **Valores predeterminados**:
  - `Points` = 1
  - `EmaLength` = 10
- **Filtros**:
  - Categoría: Amplitud de mercado
  - Dirección: Ambos
  - Indicadores: SMA, EMA
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
