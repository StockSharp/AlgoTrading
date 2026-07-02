# Estrategia VQ EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
- Conversión del experto MetaTrader "VQ_EA" que opera utilizando el indicador Volatility Quality (VQ).
- La versión StockSharp se aproxima a la línea VQ con un precio medio suavizado para mantener la lógica dentro del nivel alto API.
- Las posiciones se abren en caso de cambios de dirección de la línea suavizada y se gestionan con órdenes de protección opcionales.

## Comportamiento original de MQL
1. Solicita señales de compra o venta desde el indicador personalizado VQ (buffers 3 y 4).
2. Abre una nueva posición de mercado cuando aparece una nueva señal y no hay ninguna operación activa en esa dirección.
3. Cierra la posición opuesta inmediatamente ante una señal opuesta.
4. Funciones opcionales de administración de dinero: lotes fijos, lotes fraccionados, punto de equilibrio, stop dinámico, salida de registro manual y notificaciones de alerta/correo electrónico.

## StockSharp implementación
- En lugar del indicador propietario VQ, la estrategia aplica una media móvil simple al precio medio y, opcionalmente, lo suaviza una vez más.
- La pendiente de la serie suavizada desempeña el papel del cambio de color original de la línea VQ.
- Un filtro configurable expresado en puntos previene señales causadas por fluctuaciones menores.
- Las órdenes de mercado se utilizan para entradas y salidas, reflejando el comportamiento original de EA.

### Generación de señal
1. Suscríbase al tipo de vela seleccionado y calcule el precio medio de cada vela completada.
2. Aplique la media móvil base (`Length`) y, si se solicita, un suavizado adicional (`Smoothing`).
3. Compare el valor suavizado actual con el anterior. Si el cambio absoluto excede `FilterPoints` (convertido en unidades de precio), marque la dirección como ascendente o descendente.
4. Cuando la dirección cambia de abajo a arriba, se emite una entrada larga. Un giro de arriba a abajo produce una entrada corta. Las posiciones existentes se revierten sumando el volumen absoluto de la posición al tamaño de la orden.

### Gestión de riesgos
- `StopLossPoints`, `TakeProfitPoints` y `TrailingStopPoints` se convierten a precios absolutos multiplicándolos por el paso del precio del instrumento.
- Si al menos una de estas protecciones está habilitada, se llama a `StartProtection` con ajustes de orden de mercado para que las paradas sigan la posición como en el experto MQL.
- El trailing stop opcional se activa solo cuando `UseTrailing` es `true` y la distancia de seguimiento es mayor que cero.

## Parámetros
- `Length` – período base de suavizado del precio medio. Predeterminado: 5.
- `Smoothing` – período de suavizado secundario. Valor predeterminado: 1 (deshabilitado).
- `FilterPoints`: movimiento mínimo en los puntos necesarios para confirmar que la pendiente cambió. Predeterminado: 5.
- `StopLossPoints` – stop-loss protector en puntos. Predeterminado: 60 (0 lo deshabilita).
- `TakeProfitPoints` – toma de ganancias protectora en puntos. Valor predeterminado: 0 (deshabilitado).
- `UseTrailing`: habilita o deshabilita las paradas finales. Valor predeterminado: falso.
- `TrailingStopPoints` – distancia de seguimiento en puntos. Valor predeterminado: 0 (ignorado cuando `UseTrailing` es falso).
- `CandleType` – timeframe used for calculations. Predeterminado: velas de 1 hora.
- `Volume`: heredado de `Strategy.Volume`, por defecto es 1 contrato y se utiliza para cada entrada nueva.

## Diferencias con el experto original.
- Los valores exactos del colchón VQ se aproximan mediante precios medianos suavizados; el indicador no se transfiere uno a uno.
- No se reproducen funciones avanzadas como turnos de equilibrio, programación de alertas sonoras, salida de registros manual y administración de dinero de lotes fraccionarios.
- El manejo de pasos finales se simplifica en el administrador de paradas finales integrado de StockSharp.

## Notas de uso
- Las señales se generan solo en velas terminadas, coincidiendo con el modo "negociar al cierre de la barra" del EA original.
- Asegúrese de que el instrumento tenga un `PriceStep` adecuado; de lo contrario, la estrategia vuelve a un paso de 1,0 al convertir parámetros basados ​​en puntos.
- La estrategia está destinada a ser demostrativa y puede ampliarse con reglas adicionales de administración del dinero si es necesario.
