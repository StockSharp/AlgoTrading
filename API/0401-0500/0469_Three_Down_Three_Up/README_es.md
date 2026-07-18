# Estrategia Three Down Three Up
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia compra después de un número especificado de cierres consecutivos a la baja y cierra la posición después de una racha de cierres al alza. Un filtro EMA opcional permite entradas solo cuando el precio está por encima de la media móvil.

## Detalles

- **Criterios de entrada**: El precio cierra por debajo de la barra anterior durante N barras. Condición opcional: precio por encima de la EMA.
- **Criterios de salida**: El precio cierra por encima de la barra anterior durante M barras.
- **Largo/Corto**: Solo largos.
- **Stops**: Ninguno.
- **Valores predeterminados**: Disparo de compra = 3, disparo de venta = 3, período EMA = 200.
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Long
  - Indicadores: EMA (opcional)
  - Stops: No
  - Complejidad: Bajo
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
