# CCI Hook Reversal Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia CCI Hook Reversal utiliza el Commodity Channel Index como disparador cuando engancha alejándose de una lectura extrema. Después de que el indicador supera +100 o cae por debajo de -100, frecuentemente se retrae rápidamente a medida que el impulso se detiene.

Las pruebas indican un rendimiento anual promedio de aproximadamente 169%. Funciona mejor en el mercado de criptomonedas.

Las operaciones largas ocurren cuando el CCI gira al alza desde la sobreventa mientras el precio aún imprime un nuevo mínimo marginal. Los cortos se inician cuando el CCI se vuelve desde la sobrecompra con el precio alcanzando nuevos máximos.

Cada operación lleva un pequeño stop fijo y se cierra cuando el CCI engancha de vuelta en la dirección opuesta o se alcanza el stop.

## Detalles

- **Criterios de entrada**: señal del indicador
- **Largo/Corto**: Ambos
- **Criterios de salida**: stop-loss o señal opuesta
- **Stops**: Sí, basado en porcentaje
- **Valores predeterminados**:
  - `CandleType` = 15 minutos
  - `StopLoss` = 2%
- **Filtros**:
  - Categoría: Reversión
  - Dirección: Ambos
  - Indicadores: CCI
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
