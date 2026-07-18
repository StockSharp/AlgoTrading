# Estrategia de Cruce RSI EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia RSI EA replica el asesor experto "RSI EA" de MetaTrader 5. Observa el Índice de Fuerza Relativa (RSI) en la serie de velas seleccionada y reacciona cuando el impulso cruza niveles configurables de sobrecompra o sobreventa. La conversión mantiene las ideas de stop-loss, take-profit, trailing-stop y gestión automática del dinero del sistema original mientras las adapta a la API de estrategia de alto nivel de StockSharp.

## Lógica de la Estrategia

### Indicadores
- **RSI** con un período configurable aplicado al tipo de vela elegido.

### Criterios de Entrada
- **Largo**: el RSI cruza **hacia arriba** `RsiBuyLevel` (valor previo por debajo del umbral, valor actual por encima del umbral) y el trading largo está habilitado.
- **Corto**: el RSI cruza **hacia abajo** `RsiSellLevel` (valor previo por encima del umbral, valor actual por debajo del umbral) y el trading corto está habilitado.

Solo se mantiene una posición neta. Si la estrategia ya está en el mercado, no se abren posiciones de cobertura adicionales.

### Criterios de Salida
- **Salida por señal**: cuando `CloseBySignal` está habilitado, el cruce RSI opuesto cierra inmediatamente la posición activa.
- **Stop protector**: cuando `StopLoss` es mayor que cero, la estrategia monitorea la distancia del precio desde el precio promedio de entrada y sale una vez que la pérdida alcanza el monto especificado.
- **Take-profit**: cuando `TakeProfit` es mayor que cero, la posición se cierra tan pronto como se alcanza la distancia objetivo.
- **Trailing stop**: cuando `TrailingStop` es mayor que cero, el nivel del stop sigue al precio. Para posiciones largas, el stop se eleva a `Close - TrailingStop` una vez que el precio avanza al menos `TrailingStop` desde el stop actual; los cortos se comportan simétricamente.

### Dimensionamiento de Posición
- Cuando `UseAutoVolume` es `true`, el volumen se calcula a partir del patrimonio de la cuenta y el riesgo: `Volume = Equity * RiskPercent / (100 * stopDistance)`, donde `stopDistance` usa `StopLoss` si está disponible y de lo contrario `TrailingStop`. Si no se establece ninguna distancia de protección, la estrategia recurre al volumen manual.
- Cuando `UseAutoVolume` es `false`, el parámetro fijo `ManualVolume` se usa para cada orden.

## Parámetros
- `CandleType`: serie de velas usada para el cálculo del indicador (predeterminado: marco temporal de 1 minuto).
- `RsiPeriod`: número de barras en la ventana de cálculo del RSI (predeterminado: 14).
- `RsiBuyLevel`: límite de sobreventa que desencadena entradas largas y salidas cortas (predeterminado: 30).
- `RsiSellLevel`: límite de sobrecompra que desencadena entradas cortas y salidas largas (predeterminado: 70).
- `EnableLong`: habilitar o deshabilitar operaciones largas (predeterminado: true).
- `EnableShort`: habilitar o deshabilitar operaciones cortas (predeterminado: true).
- `CloseBySignal`: cerrar posiciones cuando el RSI cruza el umbral opuesto (predeterminado: true).
- `StopLoss`: distancia del stop-loss en unidades de precio (predeterminado: 0, deshabilitado).
- `TakeProfit`: distancia del take-profit en unidades de precio (predeterminado: 0, deshabilitado).
- `TrailingStop`: distancia del trailing stop en unidades de precio (predeterminado: 0, deshabilitado).
- `UseAutoVolume`: activar el dimensionamiento de posición basado en riesgo (predeterminado: true).
- `RiskPercent`: porcentaje del patrimonio a arriesgar cuando el dimensionamiento automático está activo (predeterminado: 10).
- `ManualVolume`: tamaño de orden fijo cuando el dimensionamiento automático está deshabilitado (predeterminado: 0.1).

## Notas de Implementación
- La implementación de StockSharp usa el flujo de trabajo de alto nivel `SubscribeCandles(...).Bind(...)`, lo que permite al indicador RSI entregar valores directamente a la estrategia sin gestión manual de búferes.
- La estrategia restablece todos los niveles de protección cuando la posición vuelve a cero para evitar valores obsoletos de stop o take-profit.
- La lógica de trailing refleja el código MQL: el stop solo se ajusta después de que el precio viaja más del doble de la distancia de trailing más allá del nivel de stop actual, evitando el ajuste prematuro.
- Debido a que las estrategias de StockSharp operan en un entorno de netting, no es posible mantener posiciones largas y cortas simultáneas como en el EA de cobertura original. En cambio, la estrategia espera a que la posición actual se cierre antes de abrir en la dirección opuesta.
- El dimensionamiento automático requiere que `StopLoss` o `TrailingStop` estén definidos; de lo contrario, se usa el volumen manual porque la distancia de riesgo es desconocida.

## Configuración Predeterminada
- Marco temporal: velas de 1 minuto.
- RSI: período 14, niveles 30/70.
- Gestión del dinero: volumen automático habilitado, riesgo de patrimonio del 10%, volumen de respaldo manual 0.1.
- Controles de riesgo: sin stop-loss, take-profit, ni trailing stop por defecto (deben configurarse para trading en vivo).

## Consejos de Uso
- Configure `CandleType` para que coincida con el instrumento y el horizonte temporal que pretende operar; la estrategia funciona en cualquier intervalo compatible con las velas de StockSharp.
- Proporcione distancias de stop-loss o trailing-stop realistas antes de habilitar el dimensionamiento automático para que el cálculo de riesgo use valores significativos.
- Combine la estrategia con `StartProtection()` (ya llamado en el código) para permitir que el framework gestione desconexiones inesperadas o posiciones huérfanas.
- Monitoree las ejecuciones y ajuste los niveles de RSI al aplicar la estrategia a diferentes mercados, ya que los umbrales óptimos pueden variar.
