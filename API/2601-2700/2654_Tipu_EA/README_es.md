# Estrategia Tipu EA Multi-Temporalidad
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia recrea la lógica central del Asesor Experto Tipu en StockSharp. Reemplaza los indicadores propietarios Tipu Trend y Tipu Stops con una combinación de medias móviles exponenciales (EMA), filtrado por Average Directional Index (ADX) y controles de riesgo con Average True Range (ATR). El sistema busca la alineación de tendencia entre un marco temporal superior (por defecto 1 hora) y un marco temporal de señal (por defecto 15 minutos), luego gestiona la posición con un módulo de piramidaje de punto de equilibrio, lógica de trailing stop y take profit fijo opcional.

La implementación se centra en instrumentos líquidos y tendenciales donde las señales de momentum en múltiples temporalidades son confiables. El marco temporal superior define el contexto y filtra las fases de rango, mientras que el marco temporal de señal proporciona los puntos de entrada reales.

## Suscripciones de datos
- Velas del marco temporal superior (por defecto 1 hora) para la tendencia EMA y la detección de rango ADX.
- Velas del marco temporal de señal (por defecto 15 minutos) para señales de entrada, colocación de stop ATR y actualizaciones de gestión de trades.

## Lógica de trading
1. **Contexto del marco temporal superior**
   - Calcular EMAs rápida y lenta y detectar cruces. Un cruce alcista produce una señal de tendencia alcista; un cruce bajista produce una señal de tendencia bajista.
   - Medir la fortaleza de la tendencia con ADX. Si el ADX está por debajo del umbral configurado, el mercado se marca como en rango y no se permiten nuevos trades.
   - Almacenar el timestamp de la última señal del marco temporal superior. La validez de la señal expira tras un número configurable de minutos.
2. **Entradas en el marco temporal de señal**
   - Esperar un cruce EMA en el marco temporal de señal **y** una señal fresca del marco temporal superior en la misma dirección mientras el marco temporal superior no está en rango.
   - Las entradas largas requieren que la EMA rápida cruce por encima de la EMA lenta; las entradas cortas requieren lo contrario.
   - Antes de enviar una nueva orden, la estrategia opcionalmente cierra la posición opuesta (comportamiento de reversión en señal) y respeta el flag de cobertura.
   - La distancia inicial del stop se establece en `ATR * AtrMultiplier` y está limitada por el parámetro `MaxRiskPips`. Las órdenes se omiten si el riesgo requerido supera este umbral.
3. **Gestión de riesgos**
   - **Take profit**: objetivo fijo opcional basado en `TakeProfitPips`.
   - **Trailing stop**: una vez que el precio se mueve `TrailingStartPips` a favor, el stop sigue al mercado con un offset de `TrailingCushionPips`.
   - **Modo sin riesgo**: cuando está habilitado, la estrategia mueve el stop a punto de equilibrio tras `RiskFreeStepPips` de ganancia y añade volumen adicional en pasos de `PyramidIncrementVolume` hasta alcanzar `PyramidMaxVolume`. Cada paso de piramidaje también ajusta el stop protector.
   - Las posiciones se cierran inmediatamente en la señal opuesta si `CloseOnReverseSignal` es verdadero.

## Parámetros
- `AllowHedging` – Permitir añadir posiciones sin cerrar primero el lado opuesto.
- `CloseOnReverseSignal` – Aplanar la posición actual cuando llega una señal opuesta.
- `EnableTakeProfit`, `TakeProfitPips` – Habilitar y configurar la distancia de take profit fijo en pips.
- `MaxRiskPips` – Distancia máxima de stop permitida en pips. Previene entradas con riesgo inicial excesivo.
- `TradeVolume` – Tamaño de orden base para la primera posición.
- `EnableRiskFreePyramiding`, `RiskFreeStepPips`, `PyramidIncrementVolume`, `PyramidMaxVolume` – Controlar la lógica de piramidaje sin riesgo.
- `EnableTrailingStop`, `TrailingStartPips`, `TrailingCushionPips` – Configurar el comportamiento del trailing stop.
- `HigherFastLength`, `HigherSlowLength`, `LowerFastLength`, `LowerSlowLength` – Longitudes de EMA para detección de tendencia en ambos marcos temporales.
- `AdxLength`, `AdxThreshold` – Parámetros ADX usados para filtrar mercados en rango en el marco temporal superior.
- `AtrLength`, `AtrMultiplier` – Parámetros ATR para el cálculo del stop inicial.
- `HigherSignalWindowMinutes` – Período de validez de la señal del marco temporal superior.
- `HigherCandleType`, `LowerCandleType` – Tipos/marcos temporales de velas para el procesamiento de contexto y señal.

## Notas de comportamiento
- El precio de entrada promedio se recalcula cada vez que se añade nuevo volumen, asegurando que los trailing stops y el módulo sin riesgo referencien la base de costo real de la posición.
- Todas las decisiones de trading se toman solo en velas completadas; las velas sin terminar se ignoran para evitar señales prematuras.
- La estrategia emite órdenes de mercado (`BuyMarket`/`SellMarket`) y realiza la gestión de posiciones internamente sin depender de órdenes stop pendientes.
- Dado que los indicadores Tipu originales son propietarios, se usan combinaciones EMA/ADX/ATR como una aproximación fiel manteniendo las características originales de gestión de trades (reversión en señal, piramidaje de punto de equilibrio y trailing stop).

## Consejos de uso
- Optimizar longitudes de EMA, multiplicador ATR y umbral ADX para el instrumento objetivo; los valores por defecto funcionan como punto de partida genérico para divisas principales.
- Establecer `HigherSignalWindowMinutes` cerca de la duración del marco temporal superior para requerir alineación casi sincrónica, o aumentarlo para permitir más desfase entre señales del marco temporal superior e inferior.
- Cuando el piramidaje está deshabilitado, la estrategia aún mueve el stop a punto de equilibrio una vez alcanzada la distancia `RiskFreeStepPips`, proporcionando protección básica de riesgo.
- Deshabilitar `CloseOnReverseSignal` si prefieres gestionar las salidas manualmente o permitir que el trailing stop gestione todo el trade.
