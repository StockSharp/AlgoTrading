# Estrategia de Reversión del Cuerpo de Vela Go
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en el indicador Go que promedia el tamaño del cuerpo de la vela. Abre una posición larga cuando el cuerpo suavizado de la vela cruza por debajo de cero después de ser positivo y abre una posición corta en el cruce opuesto. Las posiciones existentes se cierran con señales opuestas.

## Detalles

- **Criterios de entrada**: cambio de signo del SMA del cuerpo (positivo a negativo para largo, negativo a positivo para corto)
- **Largo/Corto**: Ambos
- **Criterios de salida**: cambio de signo opuesto del SMA del cuerpo
- **Stops**: No
- **Valores predeterminados**:
  - `Period` = 174
  - `CandleType` = 1 hora
- **Filtros**:
  - Categoría: Reversión
  - Dirección: Largo y Corto
  - Indicadores: SMA
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
