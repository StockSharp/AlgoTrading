# Estrategia Lucky
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Lucky es un scalper de rompimiento que monitorea cambios rápidos entre los mejores precios de bid y ask. Compra cuando el precio ask salta hacia arriba un número configurable de pips y vende cuando el bid cae la misma cantidad. Las posiciones se cierran inmediatamente una vez que se vuelven rentables o si el precio se mueve adversamente más allá de un umbral protector.

## Datos y ejecución

- **Datos de mercado**: requiere cotizaciones de Nivel 1 para acceder al flujo de mejor bid y ask.
- **Tipos de órdenes**: usa órdenes de mercado para todas las entradas y salidas para reaccionar rápidamente a los shocks de cotización.
- **Modo de posición**: diseñado para cuentas estilo cobertura pero funciona con cuentas de netting acumulando exposición neta.

## Parámetros

- **Shift points** – distancia mínima en pips entre cotizaciones consecutivas que dispara una nueva operación. Un valor mayor filtra el ruido, mientras que uno menor reacciona incluso a saltos diminutos.
- **Limit points** – movimiento adverso máximo (en pips) tolerado antes de cerrar forzosamente una posición abierta. También escala con el tamaño del tick del instrumento.
- **Reverse mode** – invierte la dirección de trading. Cuando está habilitado, los shocks alcistas del ask abren cortos y los shocks bajistas del bid abren largos.

## Lógica de trading

1. **Inicialización**
   - Convierte los parámetros basados en puntos en distancias de precio reales usando el tamaño de tick del instrumento.
   - Se suscribe a datos de Nivel 1 y reinicia los buffers internos para los precios previos de bid y ask.
2. **Entrada**
   - Cuando el ask aumenta al menos el shift configurado respecto al ask anterior, la estrategia abre un largo (o corto en modo reverso).
   - Cuando el bid disminuye al menos el shift respecto al bid anterior, la estrategia abre un corto (o largo en modo reverso).
3. **Dimensionamiento de volumen**
   - La cantidad de orden predeterminada proviene de la propiedad `Volume` de la estrategia.
   - Si el patrimonio del portafolio está disponible, emula la lógica de MetaTrader asignando aproximadamente `FreeMargin / 10,000`, redondeado a un lote decimal, asegurando que cuentas más grandes operen con tamaños mayores.
4. **Salida**
   - Las posiciones largas se cierran tan pronto como el bid supera el precio de entrada promedio o el ask cae por debajo de la entrada por el límite configurado.
   - Las posiciones cortas se cierran una vez que el ask cae por debajo de la entrada o el bid sube por encima de la entrada por el límite.

## Notas y consejos de uso

- Funciona mejor en pares de FX altamente líquidos o CFDs de índices con saltos de cotización notables.
- Combina con gestión de riesgo adicional como stop-outs a nivel de portafolio al probar en vivo.
- Activa **Reverse mode** para transformar el rompimiento en una estrategia fade sin modificar ningún otro parámetro.
- Dado que la estrategia reacciona a cada actualización de cotización que califica, considera reducir los datos entrantes o aumentar el umbral de shift en feeds ruidosos.
