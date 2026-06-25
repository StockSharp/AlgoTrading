# Estrategia Chaos Trader Lite
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Chaos Trader Lite replica las técnicas de entrada de los tres hombres sabios de Bill Williams utilizando el API de alto nivel de StockSharp. Analiza cada vela finalizada del marco temporal configurado (1 hora por defecto) y coloca órdenes stop cuando se cumple cualquiera de las siguientes condiciones:

1. **Primer Hombre Sabio – Barra divergente**: detecta velas divergentes alcistas o bajistas y requiere una distancia mínima entre el precio y la línea de labios del Alligator.
2. **Segundo Hombre Sabio – Aceleración del Awesome Oscillator**: espera cinco lecturas consecutivas del Awesome Oscillator que muestren momentum acelerado.
3. **Tercer Hombre Sabio – Ruptura de fractal**: confirma un fractal dos velas atrás y verifica que el precio esté operando suficientemente lejos de la línea de dientes del Alligator antes de encolar una orden de ruptura.

Siempre que aparece una configuración larga, la estrategia cancela los sell stops existentes, cierra posiciones cortas, coloca un nuevo buy stop justo por encima del máximo anterior y registra un stop protector debajo de la vela. Lo opuesto ocurre para configuraciones cortas. Los stops protectores se monitorean en cada barra; si el precio cruza el nivel almacenado, la posición se cierra a mercado.

## Indicadores y cálculos

- **Labios del Alligator**: media móvil suavizada de 5 períodos del precio mediano desplazada tres velas hacia adelante. La estrategia mantiene una cola para que el valor alineado con la barra actual coincida con la implementación de MetaTrader.
- **Dientes del Alligator**: media móvil suavizada de 8 períodos del precio mediano desplazada cinco velas hacia adelante. El valor desplazado impulsa el filtro del tercer hombre sabio.
- **Awesome Oscillator**: el indicador integrado de StockSharp (SMA de 5 vs 34 del precio mediano) proporciona la serie de momentum utilizada por el segundo hombre sabio.
- **Fractales**: el código inspecciona el máximo/mínimo de la vela que está dos barras detrás de la última barra. Un fractal válido requiere que esa vela sea más alta (o más baja) que las dos velas de cada lado.

## Lógica de trading

1. Suscribirse al tipo de vela solicitado y procesar solo las velas finalizadas.
2. Actualizar los indicadores Alligator y Awesome Oscillator y almacenar valores desplazados.
3. Evaluar las condiciones de los hombres sabios:
   - La barra divergente debe cerrar en la mitad superior (para alcistas) o inferior (para bajistas) de la vela y mostrar una distancia de los labios mayor que `MagnitudePips * PriceStep`.
   - La aceleración del AO requiere cinco valores: `AO[1] > AO[2] > AO[3] > AO[4]` y `AO[4] < AO[5]` para largos, reflejado para cortos.
   - La ruptura de fractal verifica que el precio cierre por encima (o debajo) del fractal confirmado y por encima (o debajo) de los dientes del Alligator más el umbral de magnitud.
4. Cuando una configuración está activa, colocar una orden `BuyStop` o `SellStop` con volumen `Volume` en el máximo de la vela más un paso de precio (o mínimo menos un paso). Cancelar el stop opuesto y aplanar posiciones contrarias.
5. Actualizar los niveles de stop-loss almacenados: los stops largos siguen hacia arriba, los stops cortos hacia abajo. Si una vela perfora el stop almacenado, la estrategia sale de la posición abierta a mercado.

## Parámetros

- `MagnitudePips` *(predeterminado 10)* – distancia mínima en pips entre la barra divergente y los labios del Alligator.
- `UseFirstWiseMan` *(predeterminado true)* – habilitar o deshabilitar la entrada por barra divergente.
- `UseSecondWiseMan` *(predeterminado true)* – habilitar o deshabilitar la entrada por aceleración del Awesome Oscillator.
- `UseThirdWiseMan` *(predeterminado true)* – habilitar o deshabilitar la entrada por ruptura de fractal.
- `Volume` *(predeterminado 0.01)* – tamaño de orden para entradas stop.
- `CandleType` *(predeterminado 1 hora)* – tipo de datos procesado por la estrategia.

## Notas

- Las verificaciones de bid/ask del código MQL4 original se aproximan con el precio de cierre de la vela en StockSharp.
- Las rutinas de validación de margen y volumen de MetaTrader se omiten porque StockSharp maneja la validación de órdenes internamente.
- Las órdenes stop se cancelan cuando aparece la configuración opuesta para evitar órdenes pendientes conflictivas, coincidiendo con el comportamiento de `CloseAll` del EA.
