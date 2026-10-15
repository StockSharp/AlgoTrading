# Estrategia de Promediado CCI de Ivan
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Port del asesor experto MetaTrader "Ivan" que opera en extremos de CCI con entradas de promediado y un stop de media móvil suavizada. La estrategia monitorea un CCI(100) a largo plazo para establecer regímenes globales de compra o venta, opcionalmente añade posiciones adicionales cuando el CCI(13) retrocede, y gestiona el riesgo con lógica de break-even y trailing alrededor de una media móvil suavizada. El dimensionamiento de posición refleja el modelo de riesgo porcentual original y un coeficiente de protección de ganancias cierra el libro cuando el equity se multiplica.

## Detalles

- **Criterios de entrada**:
  - **Señal global largo**: El CCI(100) sube por encima de `GlobalSignalLevel` mientras no hay ningún régimen de compra activo. Se envía una orden larga de mercado con el stop inicial en el valor de la MA suavizada, siempre que el stop esté al menos `MinStopDistance` por debajo del precio.
  - **Promediado largo**: Si `UseAveraging` está habilitado y la bandera global de compra está activa, cualquier caída del CCI(13) por debajo de `-GlobalSignalLevel` añade otro largo usando la misma plantilla de stop.
  - **Señal global corto**: El CCI(100) cae por debajo de `-GlobalSignalLevel` mientras no hay ningún régimen de venta activo, activando una entrada corta cuando el stop de la MA está al menos `MinStopDistance` por encima del precio.
  - **Promediado corto**: Con `UseAveraging` habilitado, una subida del CCI(13) por encima de `GlobalSignalLevel` dentro de un régimen de venta añade a la exposición corta.
- **Largo/Corto**: Opera en ambas direcciones y puede piramidizar posiciones dentro del sesgo activo.
- **Criterios de salida**:
  - Volver a cruzar dentro de `±ReverseLevel` en el CCI(100) cancela ambos regímenes y fuerza la exposición a plana.
  - El equity del portafolio que supera `ProfitProtectionFactor` veces el saldo inicial fuerza la liquidación de todas las posiciones.
  - Alcanzar el precio de stop rastreado (break-even o trailing de MA) cierra la parte de la posición.
- **Stops**:
  - El stop inicial proviene de una media móvil suavizada (SMMA) del período `StopLossMaPeriod`.
  - El break-even mueve el stop al precio de entrada una vez que el precio avanza `BreakEvenDistance` (configurar en cero para deshabilitar).
  - El trailing ajusta el stop solo si la MA progresa al menos `TrailingStep` más allá del stop actual.
- **Filtros**:
  - `UseZeroBar` replica la opción MT5 de leer la barra recién abierta o la última barra cerrada para comparaciones de señales.
  - `MinStopDistance` previene operaciones cuando el stop de la MA está demasiado cerca del precio.
- **Dimensionamiento de posición**:
  - Cada nueva orden arriesga `RiskPercent` del valor actual del portafolio dividido entre la distancia entre el precio y el stop, con `MinimumVolume` como piso de seguridad.

## Parámetros

- **Use Averaging** *(bool, predeterminado: true)* — Habilitar órdenes de promediado adicionales durante un régimen global activo.
- **Stop MA Period** *(int, predeterminado: 36)* — Período de la MA suavizada usada para derivar niveles de stop.
- **Risk %** *(decimal, predeterminado: 10)* — Porcentaje del equity de la cuenta a arriesgar en cada nueva operación.
- **Use Zero Bar** *(bool, predeterminado: true)* — Si es verdadero, usa los valores del último vela; de lo contrario las señales se basan en la barra cerrada anterior.
- **Reverse Level** *(decimal, predeterminado: 100)* — Umbral absoluto de CCI que cancela ambos regímenes y cierra todas las posiciones.
- **Global Level** *(decimal, predeterminado: 100)* — Umbral absoluto de CCI que activa una nueva señal global de compra o venta.
- **Min Stop Distance** *(decimal, predeterminado: 0.005)* — Separación mínima de precio entre la entrada y el stop de MA (0.005 ≈ 50 pips en pares FX de 5 dígitos).
- **Trailing Step** *(decimal, predeterminado: 0.001)* — Mejora mínima requerida antes de que el stop trailing de MA avance.
- **BreakEven Distance** *(decimal, predeterminado: 0.0005)* — Movimiento de precio necesario para desplazar el stop al precio de entrada; configurar en 0 para deshabilitar el break-even.
- **Profit Protection** *(decimal, predeterminado: 1.5)* — Múltiplo de equity que activa la liquidación total para asegurar ganancias.
- **Minimum Volume** *(decimal, predeterminado: 1)* — Tamaño de operación de reserva cuando el dimensionamiento basado en riesgo arroja un volumen pequeño o cero.
- **Candle Type** *(DataType)* — Serie de velas usada para indicadores (marco temporal de 15 minutos por defecto).

## Notas

- Las distancias como `MinStopDistance`, `TrailingStep` y `BreakEvenDistance` se expresan en unidades de precio y deben ajustarse al tamaño del tick del instrumento.
- La estrategia asume fills síncronos de las órdenes `BuyMarket`/`SellMarket`; ajustar la configuración de ejecución si se esperan slippages o fills parciales.
- El dimensionamiento basado en portafolio requiere un adaptador de portafolio conectado; de lo contrario se usa `MinimumVolume` para todas las órdenes.
