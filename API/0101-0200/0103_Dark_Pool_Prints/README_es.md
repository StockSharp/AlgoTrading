# Estrategia de Impresiones de Dark Pool
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este ejemplo aproxima la actividad de gran tamaño mediante el **volumen de las velas**, no informes reales de dark pools. No distingue ejecuciones en bolsa de operaciones fuera de bolsa. Opera una señal sustitutiva de precio, SMA y picos de volumen, no una fuente real de impresiones de dark pool. Los retornos históricos citados no están demostrados por este código ni por BTC/TON.

### Reglas ejecutadas (C# y Python)

Por defecto se usan velas de 1 minuto y SMA20. Tras 20 observaciones de volumen, el volumen actual debe superar **estrictamente** 1,5 veces la media de las 19 anteriores. Solo sin posición: compra si `Close > Open` y `Close > SMA20`; venta si `Close < Open` y `Close < SMA20`. Entre entradas hay 500 velas terminadas de espera. Se cierra el largo si `Close < SMA20` y el corto si `Close > SMA20`; **ni el volumen ni la espera bloquean las salidas**. Las posiciones ejecutadas tienen stop nativo del 2% y no tienen objetivo de beneficios. La rentabilidad citada arriba no está verificada por el replay BTC/TON.


## Detalles

- **Criterios de entrada**: señal de indicador
- **Largo/Corto**: Ambos
- **Criterios de salida**: stop-loss o señal opuesta
- **Stops**: Sí, basados en porcentaje
- **Valores predeterminados**:
  - `CandleType` = 1 minute
  - `StopLossPercent` = 2%
- **Filtros**:
  - Categoría: Volumen
  - Dirección: Ambos
  - Indicadores: Volume
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

