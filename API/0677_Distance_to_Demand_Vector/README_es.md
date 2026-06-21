# Estrategia de Distancia al Vector de Demanda
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en el indicador de Distancia al Vector de Demanda. Compara las distancias a los vectores de demanda largo y corto y opera en sus cruces.

## Detalles

- **Criterios de entrada**:
  - Largo: distancia al vector largo > distancia al vector corto
  - Corto: distancia al vector largo < distancia al vector corto
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - Señal opuesta
- **Stops**: Ninguno
- **Valores predeterminados**:
  - `Length` = 100
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: Highest, Lowest
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
