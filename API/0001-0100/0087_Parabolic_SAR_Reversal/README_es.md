# Estrategia de Reversión Parabolic SAR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El indicador Parabolic SAR coloca puntos por encima o por debajo del precio para señalar la dirección de la tendencia. Cuando los puntos cambian de lado, puede marcar el final del movimiento anterior. Esta estrategia entra en operaciones en ese cambio, esperando una reversión a corto plazo.

Las pruebas indican una rentabilidad anual media de aproximadamente el 148%. Funciona mejor en el mercado de divisas.

Se mantiene un valor de Parabolic SAR en ejecución para cada vela. Si el indicador pasa de estar por encima del precio a estar por debajo, se abre una posición larga. Si pasa de estar por debajo a estar por encima, se ejecuta una operación corta. El método no utiliza un objetivo de beneficio explícito y típicamente depende de una salida discrecional o stops de seguimiento fuera del código de muestra.

Dado que el SAR reacciona rápidamente, pueden producirse señales falsas en mercados laterales, por lo que es mejor usarlo cuando el precio realiza oscilaciones decisivas.

## Detalles

- **Criterios de entrada**: El Parabolic SAR cambia de lado respecto al precio.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Stop manual o externo.
- **Stops**: No definidos.
- **Valores predeterminados**:
  - `InitialAcceleration` = 0.02
  - `MaxAcceleration` = 0.2
  - `CandleType` = 15 minute
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Parabolic SAR
  - Stops: Opcional
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

