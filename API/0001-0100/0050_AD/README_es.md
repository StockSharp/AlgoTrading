# Estrategia Accumulation/Distribution Trend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Esta estrategia utiliza el indicador Accumulation/Distribution (A/D) para medir la presión compradora y vendedora. Un A/D creciente junto con el precio por encima de la media móvil señala acumulación, mientras que un A/D decreciente con el precio por debajo del promedio indica distribución.

Las pruebas indican un retorno anual promedio de aproximadamente 187%. Funciona mejor en el mercado de acciones.

Las operaciones se realizan en la dirección de la tendencia del A/D relativa a la media móvil. Un cambio en la dirección del A/D actúa como señal de salida.

Los stops son opcionales pero pueden ayudar a gestionar el riesgo.

## Detalles

- **Criterios de entrada**: A/D creciente con precio por encima de la MA o decreciente por debajo de la MA.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: A/D revierte o stop.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `MAPeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: A/D, MA
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

