# Estrategia de Ruptura de la Primera Vela de 30m del HSI1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Opera rupturas del rango de los primeros 30 minutos en un gráfico de 15 minutos, permitiendo solo una operación por día.

## Detalles

- **Criterios de entrada**: El precio rompe por encima/debajo del máximo/mínimo de los primeros 30 minutos durante la sesión.
- **Largo/Corto**: Ambos, seleccionable.
- **Criterios de salida**: Take profit o stop loss basado en el rango.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `RiskReward` = 1
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Precio
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
