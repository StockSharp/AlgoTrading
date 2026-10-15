# Estrategia de Pure Price Action Fractal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia de Pure Price Action** es un port de StockSharp del asesor experto de MetaTrader "Pure Price Action" (MQL id 24291).
Combina la confirmación de ruptura de los fractales de Bill Williams con un filtro de momentum calculado en un marco temporal superior y un filtro de tendencia MACD a largo plazo.
El algoritmo intenta capturar operaciones de continuación de tendencia inmediatamente después de que el mercado retoca el nivel fractal más reciente.

## Lógica de trading
1. **Velas de señal** – Las decisiones de trading se toman en el marco temporal seleccionado por el usuario (15 minutos por defecto).
2. **Confirmación de toque fractal** – Una operación solo está permitida si la vela terminada más reciente cierra dentro de un paso de precio del nivel fractal confirmado más reciente (fractal superior para cortos, fractal inferior para largos).
3. **Patrón de cuerpo direccional** – El tamaño absoluto del cuerpo de la vela más reciente debe ser menor que el cuerpo de la vela anterior, mientras que el cuerpo anterior debe ser mayor que la vela anterior a él. Esto imita el filtro de retroceso de momentum del EA original.
4. **Medias móviles** – Dos medias móviles lineales ponderadas (LWMA 6 y LWMA 85 por defecto) proporcionan la tendencia base. Las operaciones largas requieren que el LWMA rápido esté por encima del LWMA lento; las cortas requieren lo opuesto.
5. **Filtro de momentum** – Un indicador de momentum de 14 períodos evaluado en un marco temporal superior (H1 por defecto) debe desviarse del nivel de equilibrio (100) al menos por el umbral configurado durante cualquiera de las últimas tres lecturas de momentum.
6. **Filtro MACD** – Un MACD(12, 26, 9) calculado en un marco temporal superior (mensual por defecto) debe mostrar la línea principal por encima de la línea de señal para largos y por debajo para cortos.
7. **Dimensionamiento de posición** – La estrategia usa la propiedad `Volume` de la clase base `Strategy`. Si `Volume` no está establecido, toma como predeterminado un contrato/lote. El parámetro `MaxPosition` limita el tamaño absoluto de la posición.

## Gestión de posición
- **Protección inicial** – Las distancias opcionales fijas de stop-loss y take-profit se especifican en pasos de precio y se aplican simétricamente a ambos lados.
- **Trailing stop** – Cuando está habilitado, la estrategia rastrea el precio más alto/más bajo alcanzado después de la entrada a la distancia configurada.
- **Bloqueo de break-even** – Después de que el precio recorre la distancia de activación, el nivel protector se mueve a la entrada ± offset para asegurar ganancias.
- **Salidas manuales** – La lógica evalúa niveles de stop-loss, take-profit, trailing y break-even en cada vela terminada y cierra toda la posición cuando se cumple cualquier condición.

## Parámetros
- `CandleType` – Marco temporal de señal principal (predeterminado: marco temporal de 15 minutos).
- `MomentumCandleType` – Marco temporal para el indicador de momentum (predeterminado: marco temporal de 1 hora).
- `MacdCandleType` – Marco temporal para el filtro MACD (predeterminado: marco temporal de 30 días, emulando velas mensuales).
- `FastPeriod` / `SlowPeriod` – Períodos del LWMA rápido y lento.
- `MomentumPeriod` – Longitud del indicador de momentum.
- `MomentumThreshold` – Desviación absoluta mínima del Momentum respecto a 100.
- `MacdFastPeriod`, `MacdSlowPeriod`, `MacdSignalPeriod` – Configuración MACD.
- `StopLossPoints`, `TakeProfitPoints` – Distancias de protección de riesgo en pasos de precio.
- `TrailingStopPoints` – Distancia de trailing en pasos de precio.
- `BreakEvenTriggerPoints`, `BreakEvenOffsetPoints` – Distancias de activación y beneficio asegurado del break-even.
- `MaxPosition` – Tamaño máximo absoluto de posición manejado por la estrategia.
- `EnableTrailing`, `EnableBreakEven`, `UseStopLoss`, `UseTakeProfit` – Controles para los bloques de gestión de riesgo.

## Notas
- Todos los comentarios dentro del código están escritos en inglés, como requieren las directrices del proyecto.
- La estrategia se basa únicamente en velas terminadas; las señales dentro de la barra no se procesan.
- Las suscripciones multi-marco temporal se usan para emular el comportamiento del asesor experto original (velas de señal M15, momentum H1, MACD mensual por defecto).
- No se proporcionan pruebas automáticas en esta carpeta. La suite de pruebas del repositorio global debe permanecer intacta, como se solicitó.
