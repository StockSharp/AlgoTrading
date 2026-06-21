# Biblioteca de Screeners Limpia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de screener simple que evalúa el RSI en múltiples símbolos e imprime valoraciones de compra o venta. Sirve como base para construir screeners personalizados de múltiples activos.

## Detalles

- **Criterios de entrada**: Los valores de RSI se comprueban frente a umbrales para cada símbolo.
- **Largo/Corto**: Ninguno (solo señales)
- **Criterios de salida**: Ninguno
- **Stops**: Ninguno
- **Valores predeterminados**:
  - `RsiLength` = 14
  - `StrongThreshold` = 70m
  - `WeakThreshold` = 60m
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filtros**:
  - Categoría: Screener
  - Dirección: N/A
  - Indicadores: RSI
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: N/A
