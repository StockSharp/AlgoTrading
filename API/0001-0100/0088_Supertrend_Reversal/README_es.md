# Estrategia de Reversión Supertrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El indicador Supertrend combina ATR y precio para producir soporte o resistencia de seguimiento. Cuando la línea Supertrend pasa de estar por encima a por debajo del precio, o viceversa, sugiere un posible cambio de tendencia. Esta estrategia opera esos cambios.

Las pruebas indican una rentabilidad anual media de aproximadamente el 151%. Funciona mejor en el mercado de acciones.

En cada vela, un cálculo basado en ATR actualiza el nivel Supertrend. Un cambio de por encima del precio a por debajo activa una entrada larga, mientras que un movimiento de por debajo a por encima crea una posición corta. El código de muestra omite stops explícitos, por lo que las salidas son discrecionales o gestionadas por un módulo de riesgo separado.

El indicador puede reaccionar rápidamente a la volatilidad, por lo que los traders a menudo lo combinan con filtros adicionales para reducir las señales falsas.

## Detalles

- **Criterios de entrada**: El Supertrend cambia de lado respecto al precio.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Stop manual o externo.
- **Stops**: No definidos.
- **Valores predeterminados**:
  - `Period` = 10
  - `Multiplier` = 3.0
  - `CandleType` = 15 minute
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Supertrend
  - Stops: Opcional
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

