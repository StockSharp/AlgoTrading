# Estrategia de Pares
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia de trading de pares compra cuando el activo de referencia cierra por encima de su apertura mientras el símbolo actual forma una vela bajista. La posición se cierra cuando el precio rompe por encima del máximo de la vela anterior.

## Detalles

- **Criterios de entrada**: Activo de referencia al alza y vela bajista en el símbolo actual.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: Cierre por encima del máximo anterior.
- **Stops**: No.
- **Valores predeterminados**:
  - `CandleType` = 1 minuto
- **Filtros**:
  - Categoría: Negociación de pares
  - Dirección: Solo largos
  - Indicadores: Price action
  - Stops: No
  - Complejidad: Principiante
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
