# Estrategia de Comprar y Mantener
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia entra en una única posición larga en la fecha de inicio especificada y la mantiene hasta la fecha de fin, implementando un simple enfoque de comprar y mantener.

## Detalles

- **Criterios de entrada**:
  - Cuando el tiempo de una vela es igual o posterior a la fecha de inicio, la estrategia compra una vez.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**:
  - Cuando el tiempo de una vela alcanza o supera la fecha de fin, la posición se cierra.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - Fecha de inicio = 2018-01-01.
  - Fecha de fin = 2069-12-31.
- **Filtros**:
  - Categoría: Buy and Hold.
  - Dirección: Largo.
  - Indicadores: Ninguno.
  - Stops: No.
  - Complejidad: Bajo.
  - Marco temporal: Cualquiera.
  - Estacionalidad: No.
  - Redes neuronales: No.
  - Divergencia: No.
  - Nivel de riesgo: Alto.
