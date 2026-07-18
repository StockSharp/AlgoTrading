# Ganancia Máxima
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Max Gain compara la distancia porcentual desde el mínimo más bajo hasta el máximo actual y desde el máximo más alto hasta el mínimo actual durante un período de retroceso. Va largo cuando la ganancia potencial supera la pérdida ajustada; de lo contrario, va corto.

## Detalles
- **Datos**: Velas de precio.
- **Criterios de entrada**:
  - **Largo**: Max gain > adjusted max loss.
  - **Corto**: Adjusted max loss > max gain.
- **Criterios de salida**: Señal inversa.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `PeriodLength` = 30
- **Filtros**:
  - Categoría: Momentum
  - Dirección: Largo y corto
  - Indicadores: Highest, Lowest
  - Complejidad: Bajo
  - Nivel de riesgo: Medio
