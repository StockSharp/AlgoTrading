# Estrategia de Aroon WPR Crossover
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de seguimiento de tendencia que combina cruces de Aroon con filtros de momentum de Williams %R. Se abre una operación larga cuando la línea Aroon Up rápida cruza por encima de Aroon Down mientras Williams %R confirma un entorno de sobrevendido. Las operaciones cortas siguen la lógica inversa con Williams %R en territorio de sobrecomprado. Las posiciones abiertas pueden cerrarse por reversiones de Williams %R o por niveles opcionales de stop-loss y take-profit medidos en pasos de precio.

## Detalles

- **Criterios de entrada**:
  - Largo: Aroon Up cruza por encima de Aroon Down y Williams %R < `-(100 - OpenWprLevel)`
  - Corto: Aroon Down cruza por encima de Aroon Up y Williams %R > `-OpenWprLevel`
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - Williams %R sale de la zona de sobrevendido/sobrecomprado definida por `CloseWprLevel`
  - Umbrales opcionales de take-profit y stop-loss en pasos de precio
- **Stops**: Stop-loss y take-profit fijo opcional en pasos de precio
- **Valores predeterminados**:
  - `AroonPeriod` = 14
  - `WprPeriod` = 35
  - `OpenWprLevel` = 20
  - `CloseWprLevel` = 10
  - `TakeProfitSteps` = 0m (desactivado)
  - `StopLossSteps` = 0m (desactivado)
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Aroon, Williams %R
  - Stops: Opcional
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Moderado
