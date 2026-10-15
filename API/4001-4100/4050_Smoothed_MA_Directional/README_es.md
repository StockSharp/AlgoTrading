# Estrategia direccional MA suavizada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es un puerto API de alto nivel de StockSharp del MetaTrader 4 expertos `oc08_vy_m0moqesu15` de la carpeta `MQL/8615`. El experto original alinea su posición con una media móvil suavizada única (SMMA) y adjunta niveles fijos de stop-loss y take-profit a cada orden. La versión C# mantiene el mismo comportamiento direccional al tiempo que adopta componentes idiomáticos StockSharp.

## idea comercial

- **Sesgo direccional:** El precio que cierra por encima del promedio móvil suavizado indica una tendencia alcista; cerrar por debajo indica una tendencia bajista.
- **Alineación de posición:** La estrategia siempre intenta mantener una única posición en la dirección de la tendencia detectada. Si el mercado cambia de bando, inmediatamente invierte la posición.
- **Control de riesgos:** Cada entrada está protegida por compensaciones de stop-loss y take-profit expresadas en incrementos de precios. El ayudante StockSharp `StartProtection` reemplaza la asignación manual de SL/TP en el código MQ4 original.
- **Estilo de ejecución:** Las órdenes se envían como órdenes de mercado al cierre de la vela, replicando la lógica `OrdersTotal()==0` del experto MetaTrader.

## como funciona

1. Al iniciarse, la estrategia se suscribe a velas del período de tiempo configurado y vincula un indicador `SmoothedMovingAverage` con el período seleccionado.
2. Cuando termina una vela, el valor del indicador se compara con el cierre de la vela.
3. Si el cierre es más alto que el SMMA y la estrategia es plana o corta, envía una compra de mercado del tamaño de cubrir la exposición corta (si la hay) y abre una posición larga.
4. Si el cierre es más bajo que el SMMA y la estrategia es plana o larga, envía una venta de mercado del tamaño de cubrir la exposición larga (si la hay) y abre una posición corta.
5. Las distancias protectoras de stop-loss y take-profit se configuran una vez al inicio utilizando la seguridad actual `PriceStep`. Si ambas compensaciones se establecen en cero, la protección se desactiva.
6. La salida del gráfico (velas, indicadores, operaciones) se dibuja automáticamente cuando la estrategia se ejecuta dentro de entornos que exponen un área del gráfico.

## Parámetros

| Nombre | Predeterminado | Descripción |
| --- | --- | --- |
| `StopLossPoints` | 100 | Distancia de stop-loss en pasos de precio. Establezca en `0` para desactivar la parada.
| `TakeProfitPoints` | 100 | Distancia de obtención de beneficios en pasos de precio. Establezca en `0` para desactivar el objetivo.
| `MaPeriod` | 12 | Período de la media móvil suavizada utilizada para medir la tendencia.
| `TradeVolume` | 1 | Volumen de órdenes de mercado. La estrategia también escribe este valor en `Strategy.Volume` al inicio.
| `CandleType` | plazo de 15 minutos | Tipo de vela (marco de tiempo) que impulsa el indicador y las señales.

Todos los parámetros se pueden configurar a través de StockSharp Designer/Runner e incluyen rangos de optimización para pruebas automatizadas.

## Diferencias con la versión MetaTrader

- El tamaño de lote basado en márgenes (`Lots`/`Prots`) se reemplaza por un parámetro fijo `TradeVolume`. Esto mantiene el comportamiento determinista y compatible con la abstracción de cartera de StockSharp.
- El stop-loss y la toma de ganancias se manejan mediante `StartProtection` en lugar de modificaciones manuales de las órdenes, coincidiendo con las compensaciones originales pero usando StockSharp primitivas.
- La estrategia ignora las velas inacabadas para evitar operaciones prematuras, reflejando la bandera `New_Bar` en MQ4.

## Notas practicas

- Asegúrese de que la seguridad conectada proporcione un `PriceStep` válido. De lo contrario, la estrategia vuelve a un paso unitario de `1` al calcular las distancias SL/TP.
- La longitud del indicador se sincroniza con el valor del parámetro actual en cada vela, lo que permite ajustes de parámetros en vivo.
- Para reproducir el comportamiento original, configure el mismo período de tiempo que el gráfico que alojó el experto MQ4 y mantenga el volumen comercial consistente con el tamaño de contrato deseado.
