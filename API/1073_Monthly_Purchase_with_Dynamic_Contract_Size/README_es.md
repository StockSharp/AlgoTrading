# Estrategia de Compra Mensual con Tamaño de Contrato Dinámico
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Compra un número dinámico de contratos en un día elegido de cada mes utilizando un porcentaje fijo del capital de la cuenta. El drawdown se registra con fines informativos.

## Detalles

- **Criterios de entrada**: tiempo >= StartDate Y día del mes = BuyDay
- **Largo/Corto**: Solo largos
- **Criterios de salida**: ninguno
- **Stops**: ninguno
- **Valores predeterminados**:
  - `CandleType` = 1 día
  - `StartDate` = 2010-01-01
  - `PercentOfEquity` = 0.03
  - `BuyDay` = 1
- **Filtros**:
  - Categoría: Promedio de coste en dólares
  - Dirección: Largo
  - Indicadores: No
  - Stops: No
  - Complejidad: Principiante
  - Marco temporal: Largo plazo
  - Estacionalidad: Mensual
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Bajo
