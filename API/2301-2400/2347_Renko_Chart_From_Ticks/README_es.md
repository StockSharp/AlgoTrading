# Estrategia de Gráfico Renko desde Ticks
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Genera ladrillos Renko directamente desde ticks y opera cuando cambia la dirección del ladrillo. Demuestra la construcción de velas no basadas en tiempo usando la API de alto nivel de StockSharp.

## Detalles

- **Criterios de entrada**:
  - Cuando un nuevo ladrillo completado invierte la dirección, entrar en la dirección del nuevo ladrillo.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Invertir la posición cuando la dirección del ladrillo sea opuesta.
- **Stops**: No.
- **Valores predeterminados**:
  - `BrickSize` = 10
  - `Volume` = 1
- **Filtros**:
  - Categoría: Renko
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: No
  - Complejidad: Principiante
  - Marco temporal: Basado en ticks
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
