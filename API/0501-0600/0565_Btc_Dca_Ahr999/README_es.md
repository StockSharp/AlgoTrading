# Estrategia BTC DCA AHR999
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia compra Bitcoin cada lunes entre las fechas de inicio y fin configuradas. El monto invertido depende del índice AHR999, que combina una media geométrica del precio con un modelo de crecimiento logarítmico de Bitcoin.

## Detalles

- **Criterios de entrada**:
  - Los lunes dentro del rango de fechas si AHR999 < 0.45, comprar el monto `UsdInvest2`.
  - Los lunes dentro del rango de fechas si AHR999 < 1.2, comprar el monto `UsdInvest1`.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**:
  - Las posiciones se mantienen; no se incluye lógica de salida automática.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - UsdInvest1 = 100.
  - UsdInvest2 = 1000.
  - Length = 200.
  - Fecha de inicio = 2024-02-01, fecha de fin = 2025-12-31.
- **Filtros**:
  - Categoría: Acumulación.
  - Dirección: Largo.
  - Indicadores: AHR999.
  - Stops: No.
  - Complejidad: Moderado.
  - Marco temporal: Diario.
  - Estacionalidad: No.
  - Redes neuronales: No.
  - Divergencia: No.
  - Nivel de riesgo: Medio.
