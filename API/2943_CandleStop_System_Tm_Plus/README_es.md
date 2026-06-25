# CandleStop Sistema Tm Plus
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de ruptura construida alrededor del indicador de canal personalizado CandleStop. El sistema calcula continuamente bandas de máximo-máximo y mínimo-mínimo retrasadas, espera a que una vela completada cierre más allá de esas bandas y luego reacciona en la siguiente barra. Opcionalmente impone una vida máxima de posición y utiliza stops de protección basados en puntos.

## Detalles
- **Criterios de entrada**: La vela completada anterior cierra por encima del canal superior retrasado (para largos) o por debajo del canal inferior retrasado (para cortos), mientras la barra actual permanece de vuelta dentro del canal para evitar dobles disparadores.
- **Largo/Corto**: Lógica simétrica para operaciones largas y cortas con indicadores de habilitación independientes.
- **Criterios de salida**: Las rupturas CandleStop de color opuesto cierran posiciones existentes; la salida opcional basada en tiempo cierra operaciones que permanecen abiertas más allá del número configurado de minutos.
- **Stops**: Utiliza niveles de stop-loss y take-profit basados en el paso del mercado mediante `StartProtection`.
- **Valores predeterminados**:
  - `OrderVolume` = 1
  - `UpTrailPeriods` = 5, `UpTrailShift` = 5
  - `DownTrailPeriods` = 5, `DownTrailShift` = 5
  - `SignalBar` = 1
  - `StopLossPoints` = 1000, `TakeProfitPoints` = 2000
  - `MaxPositionMinutes` = 1920
  - `CandleType` = marco temporal de 8 horas
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Canales retrasados CandleStop
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Multi-hora
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

## Parámetros
- `OrderVolume`: Cantidad para cada entrada de mercado cuando se abre una nueva posición.
- `EnableLongEntry` / `EnableShortEntry`: Interruptores que permiten deshabilitar nuevos largos o cortos de forma independiente.
- `CloseLongOnBearishBreak` / `CloseShortOnBullishBreak`: Si cerrar posiciones existentes cuando aparece el color de ruptura CandleStop opuesto.
- `EnableTimeExit`: Activa el filtro de tiempo máximo de mantenimiento.
- `MaxPositionMinutes`: Número de minutos antes de que una operación abierta se cierre a la fuerza; establecer en cero para deshabilitar incluso cuando `EnableTimeExit` es verdadero.
- `UpTrailPeriods` y `UpTrailShift`: Longitud de lookback y desplazamiento hacia atrás para el canal CandleStop alcista. El desplazamiento retrasa la banda estilo Donchian en varias barras para emular el timing del indicador original.
- `DownTrailPeriods` y `DownTrailShift`: Parámetros equivalentes para el canal bajista.
- `SignalBar`: Índice de la barra inspeccionada para el color de ruptura (1 = vela completada anterior). La siguiente barra más antigua se usa como confirmación, igual que en la versión MQL.
- `StopLossPoints` / `TakeProfitPoints`: Distancias de stop de protección expresadas en pasos de precio. Se pasan a `StartProtection` para gestionar automáticamente las salidas.
- `CandleType`: Serie de velas primaria utilizada para la estrategia. Por defecto un marco temporal de 8 horas para coincidir con el script fuente.

## Notas de implementación
- Los valores del canal se calculan con los indicadores `Highest` y `Lowest` combinados con `Shift` para reproducir las bandas retrasadas del indicador CandleStop original.
- Los colores de señal se almacenan en un buffer circular para imitar las llamadas `CopyBuffer` de la estrategia MQL y evitar entradas duplicadas en velas consecutivas.
- Antes de colocar órdenes, la estrategia verifica las salidas basadas en tiempo, cierra posiciones opuestas si es necesario, y luego emite nuevas órdenes de mercado usando el volumen configurado.
