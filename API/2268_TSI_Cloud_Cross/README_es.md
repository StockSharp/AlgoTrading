# Estrategia de Cruce de Nube TSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La Estrategia de Cruce de Nube TSI compara el True Strength Index (TSI) con una copia retardada de sí mismo para formar una nube. Se abre una posición larga cuando el TSI cruza por encima de la línea desplazada, indicando impulso alcista. Se abre una posición corta cuando el TSI cruza por debajo de la línea desplazada. Las señales pueden invertirse y las posiciones opuestas pueden cerrarse opcionalmente.

## Detalles

- **Criterios de entrada**:
  - TSI cruza por encima de su valor desplazado (largo).
  - TSI cruza por debajo de su valor desplazado (corto).
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - Cierre opcional con señal opuesta.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `LongLength` = 25
  - `ShortLength` = 13
  - `TriggerShift` = 1
  - `Invert` = false
- **Filtros**:
  - Categoría: Oscilador de momentum
  - Dirección: Largo/Corto
  - Indicadores: True Strength Index
  - Stops: No
  - Complejidad: Bajo
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Bajo
