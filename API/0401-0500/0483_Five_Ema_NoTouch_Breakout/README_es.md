# Estrategia de Rompimiento Sin Toque de 5 EMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia de Rompimiento Sin Toque de 5 EMA espera una vela que permanezca completamente a un lado de la EMA de 5 períodos. Cuando el precio posteriormente rompe el extremo de esa vela de configuración, la estrategia entra en la dirección del rompimiento. El stop-loss se coloca en el extremo opuesto y el take-profit se establece en un múltiplo del riesgo.

## Detalles

- **Criterios de entrada**:
  - Máximo de la vela por debajo de la EMA → preparar largo; entrar cuando el precio rompe por encima del máximo de esa vela.
  - Mínimo de la vela por encima de la EMA → preparar corto; entrar cuando el precio rompe por debajo del mínimo de esa vela.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - Stop en el extremo de la vela de configuración.
  - Objetivo en `RewardRisk` × riesgo.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `EmaPeriod` = 5
  - `RewardRisk` = 3.0
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Largo/Corto
  - Indicadores: EMA
  - Stops: Sí
  - Complejidad: Bajo
  - Marco temporal: 5 minutos
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
