# Implied Volatility Spike
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Esta estrategia vigila la volatilidad implícita en busca de saltos repentinos respecto al valor anterior. Un fuerte spike combinado con un precio que opera contra la media móvil puede señalar una reversión a corto plazo.

Las pruebas indican un retorno anual promedio de aproximadamente 163%. Funciona mejor en el mercado de acciones.

Cuando la volatilidad implícita aumenta por encima del umbral configurado, el sistema entra en la dirección opuesta al movimiento del precio, esperando que la volatilidad revierta.

Las posiciones se cierran una vez que la volatilidad comienza a caer o se produce un stop-loss.

## Detalles

- **Criterios de entrada**: IV spike por encima de `IVSpikeThreshold` y precio relativo a la MA.
- **Largo/Corto**: Ambos direcciones.
- **Criterios de salida**: IV disminuye o stop.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `MAPeriod` = 20
  - `IVPeriod` = 20
  - `IVSpikeThreshold` = 1.5m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Volatilidad
  - Dirección: Ambos
  - Indicadores: IV, MA
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

