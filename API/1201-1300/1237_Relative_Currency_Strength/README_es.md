# Fuerza Relativa de Divisas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La Fuerza Relativa de Divisas compara un par de divisas con una cesta de las principales monedas.
Compra cuando el par negociado supera el promedio de los demás principales y vende cuando queda por debajo.
La comparación se basa en el cambio porcentual desde el inicio de la sesión.

## Detalles

- **Criterios de entrada**: la fuerza del par principal supera el promedio por encima del umbral.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: la fuerza cae por debajo del promedio en el umbral.
- **Stops**: No.
- **Valores predeterminados**:
  - `Threshold` = 0.01m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: Cambio de precio
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
