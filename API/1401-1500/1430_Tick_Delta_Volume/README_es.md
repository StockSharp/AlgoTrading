# Estrategia de Volumen Delta de Tick
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Analiza los cambios de precio y volumen por tick. El delta se compara con su media móvil y desviación estándar para generar entradas simples basadas en momentum.

## Detalles

- **Criterios de entrada**: delta > media + desv. estándar para largo, delta < -(media + desv. estándar) para corto
- **Largo/Corto**: Ambos
- **Criterios de salida**: Señal opuesta
- **Stops**: No
- **Valores predeterminados**:
  - `Mode` = Volume
  - `Length` = 10
- **Filtros**:
  - Categoría: Volumen
  - Dirección: Ambos
  - Indicadores: EMA, StandardDeviation
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Tick
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
