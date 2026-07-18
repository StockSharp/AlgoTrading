# Estrategia Precipice Martin (C#)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción General

La estrategia Precipice Martin es un enfoque de cuadrícula mecánica que abre una orden de mercado al cierre de cada vela procesada. El asesor experto original de MetaTrader 5 creaba una posición de compra y venta simétrica en cada nueva barra y gestionaba las salidas usando offsets estáticos de stop-loss y take-profit expresados en pips. Las operaciones perdedoras aumentaban el tamaño de la próxima orden por un multiplicador martingala, mientras que las operaciones rentables restablecían el tamaño de la posición al lote mínimo.

Este port en C# sigue la misma lógica de alto nivel usando la API de alto nivel de StockSharp. Para cada vela terminada la estrategia:

1. Actualiza las posiciones largas y cortas existentes y las cierra si el rango de la vela penetró el nivel de stop-loss o take-profit configurado.
2. Cuando está plana, alterna entre abrir una posición de mercado larga o corta (cuando ambas direcciones están habilitadas) para emular el comportamiento de doble entrada del robot fuente mientras permanece compatible con la contabilidad de posición neta de StockSharp.
3. Aplica dimensionamiento martingala opcional para que las operaciones perdedoras consecutivas aumenten el volumen por el multiplicador configurado.
4. Calcula los objetivos de stop-loss y take-profit desde distancias de pips definidas por el usuario que se traducen a offsets de precio absolutos basados en el tamaño del tick del instrumento.

## Notas de Conversión

* El EA original abría una posición larga y corta en cada nueva barra cuando ambos toggles estaban habilitados. Dado que StockSharp usa posiciones netas por defecto, la versión en C# alterna entre direcciones en oportunidades consecutivas para evitar aplanar instantáneamente la posición neta. Esto asegura que ambos lados del mercado se negocien con el tiempo.
* La gestión de stop-loss y take-profit se realiza internamente verificando si el máximo/mínimo de una vela habría activado el nivel correspondiente. Cuando se alcanza un nivel, la estrategia cierra la posición usando una orden de mercado y registra el beneficio o pérdida realizados para la lógica martingala.
* La validación de lotes replica la rutina `LotCheck` de MQL5 redondeando el volumen calculado al `VolumeStep` del exchange, aplicando los límites mínimo y máximo, y cancelando la orden si el valor redondeado se vuelve cero.
* La rutina martingala refleja `CalculateLot`: cualquier salida no rentable multiplica el tamaño de la próxima orden por `MartingaleCoefficient`, mientras que una salida rentable restablece el multiplicador a uno.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| **Use Buy** | Habilita la apertura de posiciones largas. |
| **Buy SL/TP (pips)** | Distancia (en pips) usada tanto para el stop-loss como para el take-profit de las operaciones largas. Un valor de 0 deshabilita las salidas para ese lado. |
| **Use Sell** | Habilita la apertura de posiciones cortas. |
| **Sell SL/TP (pips)** | Distancia (en pips) usada tanto para el stop-loss como para el take-profit de las operaciones cortas. |
| **Use Martingale** | Activa el dimensionamiento de posición martingala. Cuando está deshabilitado cada orden usa el tamaño de lote mínimo. |
| **Martingale Coefficient** | Multiplicador aplicado al lote mínimo después de cada operación no rentable. |
| **Candle Type** | Marco temporal de las velas procesadas por la estrategia. Por defecto la estrategia trabaja en barras de un minuto, pero se puede seleccionar cualquier marco temporal disponible. |

## Lógica de Trading

1. **Cálculo del Tamaño de Pip** – la estrategia deriva el valor del pip del tamaño del tick del instrumento. Para instrumentos cotizados con pips fraccionarios (símbolos FX de 5 dígitos) el pip se considera 10 ticks, coincidiendo con la implementación MT5.
2. **Selección de Entrada** – si tanto `Use Buy` como `Use Sell` están habilitados, la estrategia alterna entre entradas largas y cortas cada vez que está plana. Si solo una dirección está habilitada, todas las operaciones siguen esa dirección. Las entradas se activan inmediatamente después de que se completa una vela y la estrategia está en línea.
3. **Niveles de Stop/Take** – cuando se abre una operación, el stop-loss y take-profit se almacenan como precios absolutos relativos a la entrada usando la distancia de pips seleccionada. Un valor de cero deshabilita ambos niveles para esa dirección.
4. **Manejo de Salidas** – en cada vela terminada se verifican los valores de máximo/mínimo. Si el mínimo supera el stop largo o el máximo supera el objetivo largo, la posición larga se cierra. Para los cortos la lógica se refleja. Las salidas se ejecutan con órdenes de mercado usando el último volumen registrado para esa posición.
5. **Dimensionamiento Martingala** – el volumen de la próxima orden es igual al lote mínimo del instrumento multiplicado por el multiplicador martingala actual. Las operaciones perdedoras (incluidos los resultados de empate) multiplican el multiplicador por `MartingaleCoefficient`; las operaciones rentables lo restablecen a uno. El redondeo de volumen al paso del exchange se aplica antes de enviar la orden.
6. **Verificaciones de Seguridad** – si el volumen redondeado está por debajo del lote mínimo del exchange, se omite la orden, evitando errores de "fondos insuficientes" que el EA original manejaba via `CheckVolume`.

## Directrices de Uso

1. Configure el marco temporal deseado en **Candle Type** para que coincida con el período del gráfico usado en MT5.
2. Ajuste las distancias en pips para que coincidan con el comportamiento deseado de stop-loss y take-profit. Recuerde que los offsets son precios absolutos, por lo que el stop real en moneda depende del símbolo.
3. Habilite o deshabilite el dimensionamiento martingala según su tolerancia al riesgo. Dado que el volumen crece exponencialmente después de pérdidas consecutivas, aplique multiplicadores conservadores.
4. Despliege la estrategia en un instrumento que proporcione velas en tiempo real. La estrategia requiere barras completadas para operar y no negociará en velas incompletas.
5. Monitoree el uso del margen cuando el martingala está activo. La versión de StockSharp alterna intencionalmente direcciones cuando ambos lados están habilitados, por lo que solo hay una posición neta abierta en cualquier momento.

## Diferencias con la Implementación MT5

* **Posiciones Netas** – la lógica de alternancia reemplaza las entradas de cobertura simultáneas del algoritmo original. Si se requiere una cuenta de verdadera cobertura, puede ejecutar dos instancias de la estrategia (una con `Use Buy`, otra con `Use Sell`).
* **Colocación de Órdenes** – las órdenes protectoras no se colocan en el libro del exchange. En cambio, las salidas se ejecutan via órdenes de mercado cuando la estrategia detecta que se cruzó el nivel de stop o take.
* **Escaneo de Historial** – el script MT5 recalculaba el coeficiente martingala escaneando todo el historial de operaciones en cada tick. La versión en C# mantiene el multiplicador de forma incremental para reducir la sobrecarga mientras preserva el comportamiento.

## Aviso de Riesgo

Las estrategias basadas en martingala pueden generar posiciones muy grandes durante rachas de pérdidas, que pueden exceder los límites de riesgo de la cuenta. Siempre pruebe la estrategia en datos simulados antes del despliegue en vivo y asegúrese de que el multiplicador seleccionado y las distancias en pips sean adecuados para la volatilidad del instrumento negociado.
