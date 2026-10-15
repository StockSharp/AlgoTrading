# La estrategia DeMarker más sencilla
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia DeMarker más simple reproduce la lógica del asesor experto MetaTrader original. Realiza un seguimiento del oscilador DeMarker para detectar cuándo el impulso del precio abandona zonas de sobrecompra o sobreventa. Cuando el oscilador vuelve a cruzar dentro del rango neutral, la estrategia abre una posición en la dirección de la reversión esperada mientras gestiona el riesgo a través de distancias configurables de stop-loss y take-profit.

## Lógica principal
1. Suscríbase a velas del período de tiempo seleccionado y calcule el indicador DeMarker con el período configurado.
2. Marque el estado del mercado como **sobrecomprado** siempre que el valor anterior de DeMarker esté por encima del umbral de sobrecompra y como **sobrevendido** cuando esté por debajo del umbral de sobreventa.
3. Genere señales cuando el valor actual de DeMarker vuelva a cruzar dentro del área neutral:
   - Vender cuando el oscilador caiga por debajo del nivel de sobrecompra después de haber estado previamente por encima de él.
   - Compre cuando el oscilador suba por encima del nivel de sobreventa después de haber estado previamente por debajo de él.
4. Coloque sólo una posición a la vez. Si `Trade On Bar Open` está habilitado, la orden se retrasa hasta que se abra la siguiente barra; de lo contrario, la posición se ingresa inmediatamente al cierre de la barra actual.
5. Aplique órdenes stop-loss y take-profit utilizando el servicio de protección integrado para imitar las distancias fijas de la versión MQL.

## Parámetros
- **Volumen** – tamaño del pedido en lotes/contratos.
- **Período DeMarker** – período del oscilador DeMarker.
- **Nivel de sobrecompra**: umbral superior de DeMarker que define las condiciones de sobrecompra.
- **Nivel de sobreventa**: umbral inferior de DeMarker que define las condiciones de sobreventa.
- **Negociar en barra abierta**: si está habilitado, las entradas se ejecutan en la siguiente barra abierta en lugar de hacerlo inmediatamente.
- **Puntos Stop Loss**: distancia protectora de stop-loss expresada en puntos de precio.
- **Obtener puntos de beneficio**: distancia objetivo de beneficio expresada en puntos de precio.
- **Tipo de vela** – tipo de vela (período de tiempo) utilizado para los cálculos del indicador.

## Gestión monetaria
- Las órdenes de stop-loss y take-profit se registran automáticamente a través de `StartProtection` y las distancias se convierten en puntos de precio.
- Sólo puede haber una posición activa a la vez. Las nuevas señales se ignoran mientras exista una posición.

## Elementos del gráfico
- Velas de precio para la suscripción seleccionada.
- La curva del indicador DeMarker.
- Marcadores de comercio propios para validación visual de entradas y salidas.

## Notas
- Utilice instrumentos de liquidez suficientemente altos para garantizar la calidad de la ejecución de stop-loss y take-profit.
- El indicador `Trade On Bar Open` se aproxima al comportamiento original del asesor experto que espera una nueva barra antes de enviar la orden.
