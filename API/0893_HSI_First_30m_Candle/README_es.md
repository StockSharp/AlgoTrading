# Estrategia de la Primera Vela de 30m del HSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia captura el máximo y mínimo de los primeros 30 minutos tras la apertura de la sesión de Hong Kong y opera rupturas en un gráfico de 5 minutos. Solo se permite una operación por día.

## Detalles

- **Criterios de entrada**:
  - **Largo**: el precio rompe por encima del máximo de los primeros 30 minutos durante la sesión.
  - **Corto**: el precio cae por debajo del mínimo de los primeros 30 minutos durante la sesión.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - Stop loss en el lado opuesto del rango.
  - Take profit a una distancia del tamaño del rango multiplicado por `RiskReward` desde la entrada.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `RiskReward` = 1.
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame().
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Price action
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
