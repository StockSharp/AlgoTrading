# DeMarker gana posición en la estrategia de volumen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es una versión StockSharp del asesor experto MetaTrader *"DeMarker ganando volumen de posición"*. Utiliza el oscilador DeMarker para detectar extremos de sobreventa y sobrecompra, acumulando exposición gradualmente cuando el mercado permanece en una condición tensa. La implementación opera con velas completas y garantiza que solo se procese una señal por barra.

La versión C# se centra en la lógica discrecional central del script original y al mismo tiempo adopta el StockSharp API de alto nivel. La gestión de órdenes, el crecimiento del volumen y el comportamiento de reversión opcional están disponibles a través de parámetros de estrategia, lo que permite que el algoritmo se adapte a diferentes mercados y marcos temporales.

## Parámetros
- **Período DeMarker**: número de velas utilizadas por el indicador DeMarker.
- **Nivel superior**: umbral del oscilador que prepara una exposición corta (predeterminado `0.7`).
- **Nivel inferior**: umbral del oscilador que prepara una exposición prolongada (predeterminado `0.3`).
- **Volumen comercial**: volumen de órdenes de mercado enviadas en cada señal.
- **Solo una posición**: cuando está habilitada, la estrategia se aplana antes de abrir una nueva operación para que la exposición neta nunca mezcle posiciones largas y cortas.
- **Señales inversas**: intercambia activadores de compra y venta, convirtiendo la estrategia en una versión contraria o de seguimiento de tendencias.
- **Tipo de vela**: período de tiempo de las velas utilizadas para la evaluación del indicador y la señal.

## Lógica de trading
1. Se abre una suscripción de vela para el período de tiempo seleccionado y se introduce en un indicador DeMarker.
2. Cuando se cierra la última vela terminada, el valor actual de DeMarker se compara con los niveles configurados.
3. Sin reversión:
   - Si DeMarker está por debajo del nivel inferior, la estrategia intenta construir o agregar una posición larga.
   - Si DeMarker está por encima del nivel superior, la estrategia intenta construir o agregar una posición corta.
4. Con la reversión habilitada, el significado de los niveles se invierte (los mínimos extremos activan cortos y los máximos extremos activan largos).
5. El algoritmo recuerda la hora de la barra de la última operación ejecutada para evitar múltiples entradas en la misma vela.

## Gestión de Puestos
- Antes de cambiar de dirección, la estrategia comprueba el beneficio no realizado de la posición existente. La exposición opuesta se cierra solo si el precio de la vela actual sale de la operación con un resultado positivo, reflejando el comportamiento protector del EA original.
- Los promedios de posición se rastrean internamente. Cuando se agregan órdenes adicionales en la misma dirección, el precio promedio se recalcula para evaluar la rentabilidad correctamente.
- El parámetro opcional *Solo una posición* fuerza un estado plano antes de ingresar a una nueva operación, lo cual es útil cuando se ejecuta en modo de posición neta.
- `StartProtection()` se invoca una vez que comienza la estrategia para garantizar que la liquidación de emergencia permanezca disponible si la posición deja de ser cero y el algoritmo se detiene.

## Notas
- La conversión está diseñada para StockSharp API de alto nivel y no depende de colecciones personalizadas ni de sondeos de valores de indicadores directos.
- Los modelos de dimensionamiento de riesgo de la versión MetaTrader (margen fijo, riesgo porcentual, etc.) se simplifican intencionalmente al parámetro constante `Trade Volume`. Ajuste el tamaño de la posición externamente si se requiere un control dinámico del riesgo.
- Debido a que las ejecuciones se simulan con órdenes de mercado a precios de cierre de velas, recuerde validar la configuración con respecto a los requisitos reales de ejecución y deslizamiento del corredor.
