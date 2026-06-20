# Estrategia Rotacional de Momentum por Clase de Activo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este modelo rotacional asigna capital a las clases de activos que muestran el mayor momentum reciente. Cada período el sistema clasifica los ETF de activos y mantiene los líderes mientras evita los rezagados.

El rebalanceo ocurre mensualmente usando efectivo como activo defensivo cuando ningún momentum es positivo.

## Detalles

- **Datos**: Retornos totales mensuales de ETF de clases de activos.
- **Entrada**: Mantener los N mejores activos con momentum positivo.
- **Salida**: Reemplazar activos cuando caen fuera del ranking superior.
- **Instrumentos**: ETF amplios de clases de activos.
- **Riesgo**: Usa proxy de efectivo y límites de posición.

