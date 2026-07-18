# Estrategia de Beneficio Exponencial de Bitcoin
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia entra largo cuando la EMA rápida cruza por encima de la EMA lenta. El tamaño de la posición se calcula a partir de un porcentaje de riesgo del patrimonio de la cuenta. Las salidas se producen en un cruce de EMA hacia abajo, stop-loss, take-profit o trailing stop.

## Detalles

- **Criterios de entrada**:
  - EMA rápida cruza por encima de la EMA lenta → largo.
- **Largo/Corto**: Solo largos
- **Criterios de salida**:
  - EMA rápida cruza por debajo de la EMA lenta.
  - Stop-loss al porcentaje de riesgo.
  - Take-profit a riesgo × multiplicador de recompensa.
  - Trailing stop desde el precio más alto.
- **Stops**: SL, TP, trailing stop
- **Valores predeterminados**:
  - Longitud EMA rápida = 9
  - Longitud EMA lenta = 21
  - Porcentaje de riesgo = 1
  - Multiplicador de recompensa = 2
  - Porcentaje de trailing stop = 0.5
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Largo
  - Indicadores: EMA
  - Stops: SL & TP & Trailing
  - Complejidad: Bajo
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
