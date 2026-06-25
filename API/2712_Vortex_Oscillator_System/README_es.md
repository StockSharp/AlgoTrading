# Estrategia del Sistema Oscilador Vortex
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
El Sistema Oscilador Vortex es un puerto directo del asesor experto de MetaTrader 5 que se basa en el Oscilador Vortex para capturar cambios bruscos entre el movimiento direccional positivo y negativo. El oscilador se construye como la diferencia entre la línea positiva del Vortex (VI+) y la línea negativa del Vortex (VI-) calculada en la serie de velas seleccionada. Las lecturas profundamente negativas indican que VI- domina a VI+, mientras que los valores fuertemente positivos muestran el liderazgo de VI+. La estrategia interpreta esos extremos como zonas de inflexión potencial y reacciona con entradas de estilo reversión a la media respaldadas por salidas impulsadas por el oscilador.

## Cómo funciona la estrategia
1. Las velas se construyen usando el marco temporal configurado y se alimentan al `VortexIndicator` integrado.
2. Una vez que el indicador se forma, el valor del oscilador se deriva como `VI+ - VI-` en cada vela terminada.
3. El oscilador se compara con umbrales definidos por el usuario:
   - Cuando cae por debajo del umbral de compra, se detecta una configuración larga.
   - Cuando sube por encima del umbral de venta, se detecta una configuración corta.
4. Los filtros opcionales pueden restringir las señales largas a la zona entre el umbral de compra y un nivel de stop-loss dedicado (y viceversa para las señales cortas).
5. Cuando aparece una nueva configuración, la estrategia cierra cualquier posición opuesta y abre una operación en la dirección de la señal con el volumen configurado.
6. Las posiciones abiertas se monitorean continuamente. Si el oscilador alcanza los límites de stop-loss o take-profit configurados, la posición se cierra inmediatamente.

Esta secuencia reproduce la lógica original de MetaTrader: las operaciones se evalúan solo en barras completadas, ambas direcciones son mutuamente excluyentes, y las reglas protectoras basadas en el oscilador gobiernan las salidas.

## Criterios de entrada
- **Entrada larga**
  - Se activa cuando el oscilador es menor o igual al umbral de compra.
  - Si la opción de stop-loss largo está habilitada, el oscilador también debe permanecer por encima del nivel de stop-loss largo.
  - Cualquier posición corta activa se cierra antes de abrir la operación larga.
- **Entrada corta**
  - Se activa cuando el oscilador es mayor o igual al umbral de venta.
  - Si la opción de stop-loss corto está habilitada, el oscilador también debe permanecer por debajo del nivel de stop-loss corto.
  - Cualquier posición larga activa se cierra antes de abrir la operación corta.
- Si el valor del oscilador está entre los umbrales de compra y venta, todas las configuraciones se cancelan y no se produce ningún cambio de posición.

## Criterios de salida
- **Posiciones largas**
  - Cierre inmediato cuando el oscilador cruce por debajo o iguale el nivel de stop-loss largo (si está habilitado).
  - Cierre inmediato cuando el oscilador suba hasta o por encima del nivel de take-profit largo (si está habilitado).
- **Posiciones cortas**
  - Cierre inmediato cuando el oscilador cruce por encima o iguale el nivel de stop-loss corto (si está habilitado).
  - Cierre inmediato cuando el oscilador caiga hasta o por debajo del nivel de take-profit corto (si está habilitado).

Las verificaciones de salida se realizan después de cada cierre de vela, garantizando una recreación fiel del bucle de monitoreo de MT5.

## Parámetros
- **Vortex Length** – período de retroceso para el indicador Vortex (predeterminado 14).
- **Candle Type** – marco temporal usado para construir las velas suministradas al indicador.
- **Use Buy Stop Loss** – habilita el filtro de stop-loss basado en el oscilador y la salida para operaciones largas.
- **Use Buy Take Profit** – habilita la salida de take-profit basada en el oscilador para operaciones largas.
- **Use Sell Stop Loss** – habilita el filtro de stop-loss basado en el oscilador y la salida para operaciones cortas.
- **Use Sell Take Profit** – habilita la salida de take-profit basada en el oscilador para operaciones cortas.
- **Buy Threshold** – valor del oscilador que califica una entrada larga (predeterminado -0.75).
- **Buy Stop Loss Level** – valor del oscilador que cierra posiciones largas cuando la opción de stop-loss largo está activa (predeterminado -1.00).
- **Buy Take Profit Level** – valor del oscilador que cierra posiciones largas cuando la opción de take-profit largo está activa (predeterminado 0.00).
- **Sell Threshold** – valor del oscilador que califica una entrada corta (predeterminado 0.75).
- **Sell Stop Loss Level** – valor del oscilador que cierra posiciones cortas cuando la opción de stop-loss corto está activa (predeterminado 1.00).
- **Sell Take Profit Level** – valor del oscilador que cierra posiciones cortas cuando la opción de take-profit corto está activa (predeterminado 0.00).
- **Volume** – tamaño de la operación usado para nuevas posiciones (predeterminado 0.1, coincidiendo con el asesor experto original).

## Notas de implementación
- El procesamiento ocurre estrictamente en velas completadas para evitar duplicar señales dentro de la misma barra.
- Los umbrales del oscilador pueden optimizarse gracias a los rangos proporcionados en los metadatos de parámetros.
- La estrategia voltea automáticamente las posiciones enviando una orden de mercado lo suficientemente grande para cerrar el lado contrario y establecer la nueva exposición.
- Las características de stop-loss y take-profit funcionan de forma independiente; habilitar una no requiere la otra.
