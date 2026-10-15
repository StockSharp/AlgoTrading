# Estrategia MA Crossover con Zonas de Demanda/Oferta y SLTP
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia combina el cruce de medias móviles simples corta/larga con la detección de zonas de demanda y oferta. El sistema busca cruces que ocurran cerca de zonas de demanda o oferta recientemente confirmadas, luego entra en la dirección del cruce y gestiona la posición con stop-loss y take-profit de porcentaje fijo.

## Detalles

- **Criterios de entrada**:
  - Largo: SMA corta cruza por encima de la SMA larga cerca de una zona de demanda.
  - Corto: SMA corta cruza por debajo de la SMA larga cerca de una zona de oferta.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - El precio alcanza los niveles de take-profit o stop-loss.
- **Stops**: Stop-loss y take-profit basados en porcentaje.
- **Valores predeterminados**:
  - `ShortMaLength` = 9
  - `LongMaLength` = 21
  - `ZoneLookback` = 50
  - `ZoneStrength` = 2
  - `StopLossPercent` = 1
  - `TakeProfitPercent` = 2
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: SMA, Highest, Lowest
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
