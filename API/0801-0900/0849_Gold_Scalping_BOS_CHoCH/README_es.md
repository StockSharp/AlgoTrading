# Estrategia de Scalping del Oro BOS & CHoCH
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera patrones de ruptura de estructura (BOS) y cambio de carácter (CHoCH) en el oro. Deriva niveles de soporte y resistencia a corto plazo y entra cuando un BOS es seguido inmediatamente por un CHoCH, utilizando objetivos dinámicos de stop loss y take profit.

## Detalles

- **Criterios de entrada**:
  - **Largo**: `High > LastSwingHigh` y `Close` cruza por encima de `LastSwingLow`
  - **Corto**: `Low < LastSwingLow` y `Close` cruza por debajo de `LastSwingHigh`
- **Largo/Corto**: Ambos lados
- **Criterios de salida**: Stop loss o take profit
- **Stops**: Dinámicos
- **Valores predeterminados**:
  - `RecentLength` = 10
  - `SwingLength` = 5
  - `TakeProfitFactor` = 2
- **Filtros**:
  - Categoría: Scalping
  - Dirección: Ambos
  - Indicadores: Highest, Lowest
  - Stops: Sí
  - Complejidad: Medio
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
