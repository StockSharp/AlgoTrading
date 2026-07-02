# Estrategia de código de la suerte
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Lucky Code es un revendedor de ruptura a corto plazo convertido del asesor experto MetaTrader "Lucky_code" original. La estrategia observa los extremos de los diferenciales y reacciona cuando la mejor oferta salta por encima o la mejor oferta cae por debajo de la cotización anterior en una distancia configurable. Todas las operaciones se cierran agresivamente: las ganancias se obtienen inmediatamente una vez que el precio oscila favorablemente, mientras que las pérdidas se reducen cuando una excursión adversa supera un límite de protección.

## Datos y ejecución

- **Datos de mercado**: requiere un flujo constante de cotizaciones de Nivel 1 para leer los mejores valores de oferta y demanda más recientes.
- **Tipos de órdenes**: utiliza órdenes de mercado para cada entrada y salida para reflejar la ejecución basada en ticks de la versión MQL.
- **Modo de posición**: admite cuentas de compensación y de cobertura. Múltiples rellenos se acumulan en una única posición neta que se gestiona como un bloque.

## Parámetros

- **Puntos de cambio**: número mínimo de puntos (pips) entre cotizaciones consecutivas que desbloquea una nueva entrada. Los valores más altos reducen la frecuencia comercial y la sensibilidad al ruido.
- **Puntos límite**: distancia adversa máxima permitida antes de que se cierren a la fuerza las posiciones. El valor se convierte en unidades de precio con el tamaño del tick del instrumento.

## Lógica comercial

1. **Inicialización**
   - Convierte parámetros basados en puntos en compensaciones de precios reales utilizando el tamaño del tick de seguridad.
   - Se suscribe a los datos de Nivel 1 y restablece los buffers internos para la última oferta y demanda vista.
2. **Reglas de entrada**
   - Cuando la mejor demanda avanza al menos el desplazamiento configurado por encima de la demanda anterior, la estrategia abre una posición corta (que coincide con el comportamiento original de EA que vende después de picos alcistas).
   - Cuando la mejor oferta cae al menos en el mismo desplazamiento que la oferta anterior, la estrategia abre una posición larga para capturar el rebote.
3. **Tamaño del volumen**
   - Comienza desde la propiedad de la estrategia `Volume`.
   - Si el valor de la cartera está disponible, el tamaño aumenta a `round(Equity / 10,000, 1)` lotes, emulando el tamaño basado en margen MetaTrader.
4. **Reglas de salida**
   - La exposición larga se cierra inmediatamente una vez que la oferta excede el precio de entrada promedio o la demanda baja por el límite de pérdida configurado.
   - La exposición corta se cierra una vez que la demanda cae por debajo del precio de entrada o la oferta lo supera en el límite de pérdidas.

## Notas de implementación

- La estrategia reacciona ante cada actualización de cotización, así que considere limitar las transmisiones ruidosas o aumentar el parámetro de cambio en entornos de producción.
- Debido a que las órdenes de mercado se utilizan tanto para abrir como para cerrar operaciones, asegúrese de tener suficiente liquidez para evitar picos de deslizamiento durante los saltos rápidos de las cotizaciones.
- Se recomiendan controles de riesgo adicionales a nivel de cartera (parada diaria, reducción máxima, etc.) al ejecutar la estrategia en vivo.
