# Estrategia de Densidad Léxica en Presentaciones Regulatorias
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia de factores examina el lenguaje utilizado en las presentaciones regulatorias para evaluar el rendimiento futuro de las acciones. La densidad léxica se mide como la fracción de términos únicos en el informe más reciente. Las presentaciones densas sugieren divulgaciones ricas y cargadas de información que suelen preceder a rendimientos más sólidos, mientras que una redacción escasa puede enmascarar debilidades.

Cada trimestre, el universo se ordena por densidad léxica. El quintil más alto se mantiene largo y el quintil más bajo se vende en corto, con posiciones de igual ponderación. El rebalanceo ocurre durante los primeros tres días hábiles de febrero, mayo, agosto y noviembre, y las posiciones permanecen abiertas entre revisiones sin stops.

Las pruebas retrospectivas en renta variable estadounidense amplia muestran que el factor proporciona una prima estable con una rotación moderada, lo que lo convierte en un componente útil en carteras multifactor.

## Detalles

- **Criterios de entrada**: Ordenación trimestral por densidad léxica; largo quintil superior,
  corto quintil inferior
- **Largo/Corto**: Ambos
- **Criterios de salida**: Próximo rebalanceo
- **Stops**: No
- **Valores predeterminados**:
  - `Quintile` = 5
  - `MinTradeUsd` = 200
  - `CandleType` = TimeSpan.FromDays(1)
- **Filtros**:
  - Categoría: Fundamental
  - Dirección: Ambos
  - Indicadores: Análisis de texto
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Multimensual
  - Estacionalidad: Sí
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
