# Estrategia de Trading con Doji
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia busca velas Doji que aparezcan por encima de una media móvil exponencial. Cuando ocurre dicho patrón, entra en una posición larga. El stop-loss se fija en el mínimo más bajo de las barras recientes y un stop trailing protege el beneficio una vez que el precio se mueve suficientemente a favor.

## Detalles

- **Criterios de entrada**: Vela Doji con cierre por encima de la EMA.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: Stop en el mínimo más bajo y stop trailing.
- **Stops**: Sí, fijo y trailing.
- **Valores predeterminados**:
  - `CandleType` = 5 minutos
  - `EmaLength` = 60
  - `Tolerance` = 0.05
  - `StopBars` = 450
  - `TrailTriggerPercent` = 1
  - `TrailOffsetPercent` = 0.5
- **Filtros**:
  - Categoría: Patrón
  - Dirección: Largo
  - Indicadores: EMA, Vela japonesa
  - Stops: Sí
  - Complejidad: Principiante
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
