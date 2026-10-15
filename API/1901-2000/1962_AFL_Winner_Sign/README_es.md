# Estrategia AFL Winner Sign
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en el indicador AFL WinnerSign. Aplica un oscilador estocástico de doble suavizado a una serie de precios ponderada por volumen. Se abre una posición larga cuando la línea estocástica rápida cruza por encima de la línea lenta, y se abre una posición corta cuando la línea rápida cruza por debajo de la línea lenta.

## Detalles

- **Criterios de entrada**:
  - Largo: el %K rápido cruza por encima del %D lento
  - Corto: el %K rápido cruza por debajo del %D lento
- **Largo/Corto**: Ambos
- **Criterios de salida**: La señal opuesta cierra o revierte la posición
- **Stops**: Basados en porcentaje usando `StartProtection`
- **Valores predeterminados**:
  - `Period` = 10
  - `KPeriod` = 5
  - `DPeriod` = 5
  - `CandleType` = `TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Oscilador Estocástico
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
