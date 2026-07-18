# Estrategia Disturbed
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia de cobertura abre simultáneamente órdenes de mercado largas y cortas y las gestiona en función del spread actual. Una vez que el precio se mueve un spread en contra de cualquiera de los lados, esa posición se cierra. La posición restante apunta entonces a un beneficio o pérdida igual a un múltiplo configurable del spread.

## Detalles

- **Criterios de entrada**:
  - Al inicio, se colocan órdenes de mercado de compra y venta simultáneamente.
- **Largo/Corto**: Ambos simultáneamente.
- **Criterios de salida**:
  - Cerrar el lado que pierde un spread.
  - Cerrar el lado restante con un beneficio o pérdida de `gainMultiplier * spread`.
- **Stops**: Implícitos mediante niveles basados en el spread.
- **Filtros**: Ninguno.
