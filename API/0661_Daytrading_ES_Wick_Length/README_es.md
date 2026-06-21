# Estrategia de Daytrading ES por Longitud de Mecha
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia entra en una posición larga cuando la longitud total de la mecha de una vela supera su media móvil más un desplazamiento, y sale después de mantener la posición durante un número fijo de barras.

## Detalles

- **Criterios de entrada**: Longitud total de mecha mayor que la media móvil con desplazamiento.
- **Criterios de salida**: Posición cerrada después de mantener `Hold periods` barras.
- **Largo/Corto**: Solo largos.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `MA length` = 20
  - `MA type` = VolumeWeighted
  - `MA offset` = 10
  - `Hold periods` = 18
  - `Candle type` = velas de 1 minuto
- **Filtros**:
  - Categoría: Volatilidad
  - Dirección: Largo
  - Indicadores: Moving Average, longitud de mecha
  - Stops: No
  - Complejidad: Simple
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
