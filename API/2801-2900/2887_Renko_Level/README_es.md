# Estrategia de Nivel Renko
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La Estrategia de Nivel Renko es una conversión fiel del asesor experto de MetaTrader 5 "Renko Level EA". Reconstruye la lógica impulsada por indicadores dentro de StockSharp y opera cuando el nivel Renko redondeado salta a un nuevo bloque. La estrategia interpreta cada cambio de nivel como un rompimiento del ladrillo Renko sintético y entra en la dirección del rompimiento o en la dirección opuesta cuando el modo inverso está habilitado.

El sistema usa velas regulares basadas en tiempo (1 minuto por defecto) solo como fuente de datos. Los cierres de velas se redondean a un tamaño de bloque configurable que emula los ladrillos Renko sin necesidad de suscripciones de datos Renko. Cada vez que el bloque redondeado cambia, la estrategia cierra cualquier exposición opuesta y abre una nueva posición alineada con el movimiento detectado.

## Lógica de trading
1. **Inicialización**
   - Detectar el tamaño del pip del instrumento (`PriceStep`).
   - Convertir el parámetro `Block Size` de pips a unidades de precio (los instrumentos de 3 y 5 dígitos multiplican automáticamente el valor del pip por 10).
   - Redondear el cierre de la primera vela finalizada al bloque más cercano para crear los niveles Renko superior e inferior iniciales.
2. **Mantenimiento de niveles**
   - En cada vela finalizada el precio de cierre se redondea al tamaño de bloque más cercano.
   - Cuando el cierre permanece dentro del bloque actual, los niveles almacenados permanecen sin cambios.
   - Cuando el cierre rompe por debajo del límite inferior, el algoritmo redondea el precio hacia abajo y desplaza el bloque hacia abajo (`lower = round`, `upper = round + size`).
   - Cuando el cierre rompe por encima del límite superior, el bloque se desplaza hacia arriba (`upper = round`, `lower = round - size`).
3. **Generación de señales**
   - Un nivel superior creciente indica un rompimiento alcista del bloque Renko. Un nivel superior decreciente indica un rompimiento bajista.
   - Si `Reverse` está deshabilitado, la estrategia compra en cambios alcistas y vende en cambios bajistas. Cuando `Reverse` está habilitado, las acciones se intercambian.
   - Cuando se desencadena una señal, la exposición existente en la dirección opuesta se elimina automáticamente (la orden de compra cierra cortos, la orden de venta cierra largos). Si `Allow Increase` está deshabilitado, la estrategia rechaza añadir tamaño encima de una posición ya abierta en la misma dirección.
4. **Ejecución de órdenes**
   - Las órdenes se envían con la configuración de `Volume` de la estrategia. Al revertir una posición existente, el tamaño de la orden es igual a la posición absoluta más el volumen configurado para que el cambio ocurra inmediatamente.
   - `StartProtection()` se llama durante el inicio para que las protecciones de riesgo configuradas en Designer o mediante composición estén activas.

## Parámetros
| Parámetro | Descripción | Predeterminado |
|-----------|-------------|---------|
| `Block Size` | Tamaño del bloque Renko en pips. La estrategia lo multiplica por el valor del pip del instrumento para obtener el incremento de precio real. Los valores más grandes reducen la frecuencia de trading. | 30 |
| `Reverse` | Cuando `true`, invierte todas las señales de trading (comprar en cambio bajista, vender en cambio alcista). | `false` |
| `Allow Increase` | Cuando `true`, permite la piramidación añadiendo órdenes adicionales en la misma dirección en cada señal. Cuando `false`, solo se envía una nueva orden si la posición neta es plana después de cerrar el lado opuesto. | `false` |
| `Candle Type` | Datos de velas fuente. Cualquier `DataType` soportado puede usarse; por defecto la estrategia se suscribe a velas de 1 minuto. | `TimeFrame(1m)` |
| `Volume` *(heredado)* | Tamaño de la orden usado al enviar órdenes de mercado. Establezca esta propiedad en la instancia de estrategia antes de iniciarla. | Depende del portafolio |

## Notas de uso
- Elija el tamaño del bloque según la volatilidad del instrumento. Para los principales pares de divisas, 30–50 pips emulan el comportamiento del EA original. En índices o activos cripto use tamaños de bloque más grandes.
- La estrategia funciona con cualquier fuente de velas (tick, marco temporal, rango) siempre que el cierre de vela refleje el muestreo de precio deseado. Para una fuente Renko pura puede cambiar el tipo de vela a una serie de datos Renko.
- Habilite `Reverse` para transformar el sistema de rompimiento en un sistema de reversión a la media que desvanece cada cambio de nivel Renko.
- `Allow Increase` se puede activar para imitar el parámetro "Increase" del EA original que añade contratos en cada nuevo nivel en la misma dirección.
- El riesgo y la gestión monetaria (stop-loss, take-profit, control de drawdown) se pueden configurar a través de protecciones de StockSharp o estrategias envolventes. La muestra mantiene la lógica idéntica al experto MT5 y no impone salidas fijas más allá de los cambios de nivel.

## Requisitos de datos
- Datos de velas históricos y en tiempo real para el `Candle Type` configurado.
- Los metadatos del instrumento deben proporcionar `PriceStep` y `Decimals` para que la conversión de pip funcione correctamente. Cuando estos valores no están disponibles, la estrategia recurre a un paso predeterminado de 0.0001.

## Flujo de trabajo sugerido
1. Añada la estrategia a Designer o créela programáticamente a través de la API de StockSharp.
2. Establezca `Security`, `Portfolio`, `Volume` y, opcionalmente, ajuste los parámetros listados anteriormente.
3. Inicie la estrategia. Esperará a la primera vela finalizada para establecer el bloque Renko inicial.
4. Monitoree el gráfico de trades integrado o suscríbase a los logs para verificar que las órdenes se desencadenan solo cuando cambia el nivel redondeado.

Esta documentación refleja el comportamiento del EA Renko Level original mientras explica cómo está implementado dentro de StockSharp para que pueda personalizarlo o extenderlo.
