# Estrategia Knux de Múltiples Indicadores
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia combina indicadores de fuerza de tendencia y osciladores de momentum para operar rupturas. Espera un cruce alcista o bajista de dos medias móviles mientras el Average Directional Index (ADX) señala una tendencia fuerte. El Relative Vigor Index (RVI), el Commodity Channel Index (CCI) y Williams %R actúan como filtros para asegurar que el momentum confirme el movimiento y que el mercado no esté sobreextendido.

El sistema puede abrir posiciones largas y cortas. Mantiene la posición hasta que aparece una señal opuesta y no utiliza un stop-loss dedicado. Todos los parámetros, como los períodos y umbrales de los indicadores, son configurables.

## Detalles

- **Criterios de entrada**:
  - **Largo**: La SMA rápida cruza por encima de la SMA lenta, `ADX > AdxLevel`, `RVI` en ascenso, `CCI < -CciLevel`, y `WPR <= -100 + WprBuyRange`.
  - **Corto**: La SMA rápida cruza por debajo de la SMA lenta, `ADX > AdxLevel`, `RVI` en descenso, `CCI > CciLevel`, y `WPR >= -WprSellRange`.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Señal opuesta (cruce en la dirección contraria).
- **Stops**: Sin stop-loss explícito.
- **Valores predeterminados**:
  - `FastMaLength` = 5
  - `SlowMaLength` = 20
  - `AdxPeriod` = 14
  - `AdxLevel` = 15
  - `RviPeriod` = 20
  - `CciPeriod` = 40
  - `CciLevel` = 150
  - `WprPeriod` = 60
  - `WprBuyRange` = 15
  - `WprSellRange` = 15
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Múltiples
  - Stops: Ninguno
  - Complejidad: Medio
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
