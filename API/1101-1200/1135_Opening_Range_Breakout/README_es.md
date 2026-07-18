# Estrategia de Ruptura del Rango de Apertura
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia define un rango de apertura y opera las rupturas por encima o por debajo de él. Tras el cierre de la ventana del rango de apertura, si la amplitud supera un porcentaje del precio de cierre, se preparan órdenes stop en los límites del rango. Las posiciones utilizan un stop loss y un objetivo de beneficio basados en el tamaño del rango. Opcionalmente solo se realiza una operación por día, y las operaciones perdedoras pueden revertirse. Todas las posiciones se cierran al final de la sesión.

## Detalles

- **Criterios de entrada**:
  - **Largo**: el precio rompe por encima del máximo del rango de apertura.
  - **Corto**: el precio rompe por debajo del mínimo del rango de apertura.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - Stop loss o take profit basado en el rango.
  - Cierre al final del día.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `Rango de apertura` = 09:30–10:15.
  - `Fin del día` = 15:45.
  - `MinRangePercent` = 0.35.
  - `RewardRisk` = 1.1.
  - `Retrace` = 0.5.
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Precio
  - Stops: Sí
  - Complejidad: Moderado
  - Marco temporal: Intradía
