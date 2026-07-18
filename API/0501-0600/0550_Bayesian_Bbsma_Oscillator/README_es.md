# Estrategia de Oscilador Bayesian BBSMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia estima la probabilidad de que la siguiente vela rompa hacia arriba o hacia abajo usando un modelo Bayesian construido a partir de Bollinger Bands y una media móvil simple. La confirmación opcional de los indicadores Accelerator y Alligator de Bill Williams puede filtrar las señales. Cuando la probabilidad de una ruptura alcista supera el umbral, se abre una operación larga. Una alta probabilidad de ruptura bajista activa un corto.

## Detalles

- **Criterios de entrada**:
  - Largo cuando la probabilidad principal o alcista cruza por encima de `LowerThreshold` (por defecto 15%) y, si está habilitado, la confirmación de Bill Williams es alcista.
  - Corto cuando la probabilidad principal o bajista cruza por encima del umbral y, si está habilitado, la confirmación de Bill Williams es bajista.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - Señal inversa.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `BbSmaPeriod` = 20
  - `BbStdDevMult` = 2.5
  - `AoFast` = 5
  - `AoSlow` = 34
  - `AcFast` = 5
  - `SmaPeriod` = 20
  - `BayesPeriod` = 20
  - `LowerThreshold` = 15
  - `UseBwConfirmation` = false
  - `JawLength` = 13
- **Filtros**:
  - Categoría: Seguimiento de tendencia probabilístico
  - Dirección: Ambos
  - Indicadores: Bollinger Bands, SMA, Awesome Oscillator, Accelerator Oscillator, Alligator
  - Stops: No
  - Complejidad: Alto
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
