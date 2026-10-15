# Estrategia de Impresiones de Dark Pool
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Las Impresiones de Dark Pool rastrean grandes transacciones fuera de bolsa que a menudo preceden a movimientos bruscos una vez que la actividad se revela.
Un volumen inusual en la cinta puede señalar posicionamiento institucional que aún no ha impactado el mercado regular.

Las pruebas indican un rendimiento anual promedio de aproximadamente 46%. Funciona mejor en el mercado de acciones.

La estrategia entra en la misma dirección que las grandes compras o ventas del dark pool, esperando continuidad cuando el resto del mercado reaccione.

Un pequeño stop porcentual mantiene el riesgo controlado y las posiciones se cierran si el impulso anticipado no llega a materializarse.

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

