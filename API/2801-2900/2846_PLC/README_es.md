# Estrategia PLC
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La estrategia PLC replica el comportamiento del asesor experto de MetaTrader `PLC (barabashkakvn's edition)` usando la API de alto nivel de StockSharp. El algoritmo opera en el marco temporal de alto nivel especificado por el parámetro `Entry Timeframe` y coloca órdenes stop de ruptura por encima y por debajo de la vela finalizada más reciente. Los fractales de marcos temporales más bajos (M5 y H1 por defecto) se usan para escalar dinámicamente el volumen de la orden. Una vez que la ganancia flotante de todas las posiciones abiertas supera el umbral configurado, la estrategia liquida toda la posición y espera la próxima configuración.

## Lógica de trading

1. **Procesamiento de nueva vela** – la estrategia reacciona solo cuando una vela está completamente cerrada en el marco temporal principal. Todos los cálculos se realizan con los datos de la barra cerrada para evitar el repintado.
2. **Mantenimiento de órdenes/posición** – antes de evaluar una nueva configuración el algoritmo cancela las órdenes stop pendientes programadas para eliminación y cierra posiciones cuando se alcanzó el objetivo de beneficio en una barra anterior.
3. **Desplazamientos de precio** – el máximo y mínimo de la última vela finalizada se desplazan por el número de pips configurados a través de `Shift OHLC`. El tamaño de pip se ajusta automáticamente para símbolos forex de 3 o 5 dígitos.
4. **Actualizaciones de fractales** – suscripciones dedicadas rastrean patrones de fractales en los marcos temporales M5 y H1. Los valores del fractal ascendente y descendente más recientes se almacenan cuando se completa un patrón clásico de cinco barras.
5. **Verificación de distancia** – se coloca una nueva compra stop solo si el máximo desplazado está al menos `Shift Position` pips por encima del precio de entrada más alto de las operaciones largas abiertas, o si no hay operaciones largas y ningún buy stop activo. La misma regla con comparaciones invertidas se aplica a las ventas stop.
6. **Dimensionamiento dinámico de lotes** – el volumen base (`Buy Volume` o `Sell Volume`) se multiplica por el multiplicador M5 o H1 cuando el nivel de stop rompe por encima del fractal correspondiente. Establecer un multiplicador en cero desactiva el escalado para ese marco temporal.
7. **Registro de órdenes** – las órdenes stop se envían via `BuyStop`/`SellStop`. Las referencias a las órdenes registradas se rastrean para simplificar la cancelación posterior.
8. **Supervisión de beneficios** – después de sumar la ganancia abierta de todos los lotes largos y cortos (usando el valor de paso del instrumento) la estrategia activa el modo de `cerrar posiciones` una vez que el beneficio supera el `Minimum Profit`. Se usan órdenes de mercado en la siguiente barra para aplanar la exposición.
9. **Retroalimentación de operaciones** – cuando se ejecuta una orden stop pendiente, todas las demás stops pendientes se cancelan para imitar la lógica MQL original.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `Shift OHLC` | Número de pips añadidos por encima del máximo de la última vela y por debajo del mínimo de la última vela para determinar los niveles de activación del stop. |
| `Minimum Profit` | Beneficio (en la moneda del instrumento) que desencadena el cierre de todas las posiciones abiertas. |
| `Shift Position` | Distancia mínima en pips entre el nuevo nivel de stop y el precio de apertura extremo de las posiciones existentes. Previene el apilamiento de órdenes demasiado cerca de entradas anteriores. |
| `Buy Volume` / `Sell Volume` | Tamaño base de la orden (lotes). Usado antes de aplicar multiplicadores de fractales. |
| `M5 Multiplier` / `H1 Multiplier` | Multiplicadores de volumen activados cuando el precio stop está por encima (para largos) o por debajo (para cortos) del fractal más reciente en el marco temporal respectivo. Use `0` para deshabilitar el escalado. |
| `Entry Timeframe` | Marco temporal principal usado para generar entradas. Cada vela finalizada en este marco temporal desencadena una nueva evaluación. |
| `M5 Fractal Timeframe` | Marco temporal que alimenta el detector de fractales inferior (por defecto 5 minutos). |
| `H1 Fractal Timeframe` | Marco temporal que alimenta el detector de fractales superior (por defecto 1 hora). |

## Gestión de posición

- **Cancelación** – La estrategia mantiene referencias a todas las órdenes stop pendientes. Cuando una orden stop se ejecuta, todas las órdenes pendientes restantes se cancelan en el siguiente ciclo de evaluación.
- **Aplanamiento** – Cuando se supera el `Minimum Profit`, la posición neta se aplana usando órdenes de mercado (`SellMarket` para largos, `BuyMarket` para cortos). La bandera se limpia una vez que el tamaño de la posición regresa a cero.
- **Seguimiento de inventario** – Las órdenes ejecutadas se registran como lotes individuales para replicar el comportamiento de MetaTrader que diferencia entre los precios de entrada de compra más altos y de venta más bajos.

## Notas

- Los parámetros predeterminados reflejan la configuración del asesor experto original. Puede cambiar los marcos temporales de fractales editando los parámetros `M5 Fractal Timeframe` y `H1 Fractal Timeframe` si el instrumento requiere diferentes ventanas de contexto.
- Los volúmenes se redondean hacia abajo al paso de volumen del exchange antes de enviar órdenes. Si el valor resultante es cero, la orden se omite.
- El cálculo de beneficios usa el valor de precio y paso del instrumento para mantenerse compatible con instrumentos que tienen valor de tick no unitario.
