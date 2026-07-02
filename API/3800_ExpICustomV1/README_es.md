# Estrategia ExpICustomV1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Resumen

La **Estrategia ExpICustomV1** es un puerto StockSharp del MetaTrader experto `exp_iCustom_v1`. La estrategia lee señales comerciales de una instancia de indicador configurable y reacciona a valores distintos de cero en los buffers seleccionados. Cuando el colchón de compra es distinto de cero, la estrategia abre una posición larga, mientras que el colchón de venta activa una entrada corta. La lógica protectora de stop-loss, take-profit, trailing y breakeven reproduce las opciones de gestión del dinero del experto original.

> **Nota:** Solo se proporciona la implementación de C#. Aún no hay una versión de Python disponible.

## Lógica de trading

1. Suscríbase al período de tiempo principal especificado por **Tipo de vela** y procese únicamente velas cerradas.
2. Cree una instancia del indicador definido por **Nombre del indicador** y aplique los **Parámetros del indicador** separados por barras (admite tanto pares `Name=Value` como valores numéricos ordenados).
3. Almacene las salidas finales del indicador en un búfer de historial para que se pueda acceder a cualquier cambio en velas posteriores.
4. Cuando el valor del buffer de compra en **Indicator Shift** no es cero, la estrategia abre/mantiene una posición larga. Cuando el buffer de venta es distinto de cero, la estrategia abre/mantiene una posición corta.
5. Si ambos buffers devuelven valores distintos de cero simultáneamente, las señales se cancelan entre sí para evitar entradas ambiguas.
6. Opcional **Cerrar en reversa** sale de la posición actual antes de reaccionar a la señal opuesta.
7. La lógica de suspensión impone un número mínimo de barras entre entradas consecutivas en la misma dirección. El temporizador se puede cancelar cuando se activa la señal opuesta si **Cancelar Dormir** está habilitado.
8. Las posiciones están protegidas por stop-loss, take-profit, trailing stop opcional y bloqueo de equilibrio. Todas las distancias se expresan en puntos de precio.

## Configuración del indicador

* **Nombre del indicador**: nombre completo o nombre corto del indicador StockSharp (por ejemplo, `SMA`, `MACD`, `BollingerBands`).
* **Parámetros del indicador**: lista separada por barras aplicada al indicador. Se admiten tanto `Length=14/Width=2` como valores ordenados como `14/2/0.7`.
* **Anular bloques**: hasta cinco reemplazos le permiten ajustar los valores de los parámetros por índice durante la optimización, similar a las entradas `Opt_X` en el experto original. Los índices tienen base cero.

## Gestión de riesgos y dinero

* **Volumen Base** – Monto enviado con cada orden de mercado.
* **Stop Loss / Take Profit** – Distancias absolutas en puntos desde el precio de entrada.
* **Trailing Stop**: se activa después de la ganancia especificada y mantiene la distancia configurada desde el precio extremo.
* **Break Even**: mueve el stop hacia el precio de entrada después de la ganancia especificada y, opcionalmente, bloquea puntos adicionales.

## Referencia de parámetros

| Parámetro | Descripción |
|-----------|-------------|
| Tipo de vela | Plazo utilizado para la evaluación de indicadores y señales. |
| Nombre del indicador | Escriba el nombre de la instancia del indicador. |
| Parámetros del indicador | Lista de parámetros de indicador separados por barras. |
| Comprar búfer / Vender búfer | Índices de búfer que contienen los marcadores de compra/venta. |
| Cambio de indicador | Cambio histórico aplicado al leer los buffers del indicador. |
| Anular bloques | Reemplace posiciones de parámetros específicos durante el tiempo de ejecución. |
| Barras para dormir | Barras mínimas entre entradas en la misma dirección. |
| Cancelar Dormir | Reinicie el temporizador de apagado después de una señal opuesta. |
| Cerrar al revés | Cierre la posición existente cuando aparezca la señal opuesta. |
| Órdenes máximas/Compra máxima/Venta máxima | Tapas blandas que limitan el número de posiciones simultáneas. |
| Detener pérdidas / Tomar ganancias | Distancia en puntos por órdenes de protección. |
| Configuración de parada móvil | Parámetros que controlan la activación y la distancia del trailing stop. |
| Configuración de punto de equilibrio | Parámetros que controlan la activación del punto de equilibrio y la distancia de bloqueo. |
| Volumen básico | Volumen de cada entrada al mercado. |

## Uso

1. Agregue la estrategia a su contenedor de estrategias y configure **Seguridad** y **Cartera**.
2. Configure **Nombre del indicador** y **Parámetros del indicador** para que coincidan con el indicador personalizado de destino.
3. Ajuste la configuración de riesgo (stop, take, trailing, breakeven) y el volumen de la orden base.
4. Ejecute la estrategia. Se suscribirá al plazo elegido, evaluará los colchones del indicador y enviará órdenes de mercado cuando se cumplan las condiciones.

## Limitaciones

* El indicador debe estar disponible como tipo de indicador StockSharp. Los indicadores binarios MetaTrader no se pueden cargar directamente.
* Los modos de administración de dinero que dependen del margen libre del corredor se simplifican a un volumen base fijo.
