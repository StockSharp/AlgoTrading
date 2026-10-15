# Estrategia de Reversión VIX II de Larry Conners
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia opera basándose en el RSI del índice VIX. Se abre una posición larga cuando el RSI del VIX cruza por encima del nivel de sobrecompra. Se abre una posición corta cuando el RSI cruza por debajo del nivel de sobreventa. Las posiciones se cierran tras mantenerse durante un número mínimo de días.

## Detalles

- **Criterios de entrada**:
  - **Largo**: RSI(VIX) cruza por encima del `Overbought level`.
  - **Corto**: RSI(VIX) cruza por debajo del `Oversold level`.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**: Cerrar posición después de `Min holding days` a `Max holding days`.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `RSI period` = 25
  - `Overbought level` = 61
  - `Oversold level` = 42
  - `Min holding days` = 7
  - `Max holding days` = 12
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: RSI
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Diario
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
