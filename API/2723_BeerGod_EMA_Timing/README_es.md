# Estrategia de Temporización EMA BeerGod
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia replica el asesor experto BeerGodEA de MetaTrader dentro de StockSharp. Negocia configuraciones de reversión a la
media en un único símbolo monitoreando una media móvil exponencial (EMA) de 60 períodos y comparando la acción del precio actual
con la barra anterior. Las señales se evalúan solo una vez por barra en un desplazamiento configurable de minutos después de que
la vela se abre, imitando el EA original que espera unos minutos antes de actuar.

Cuando el precio se aleja temporalmente de la EMA mientras la media se mueve en la dirección opuesta, la estrategia abre una
posición de mercado esperando que el movimiento revierta. Las posiciones existentes en la dirección opuesta se invierten
inmediatamente ajustando el tamaño de la orden para que los cortos se cubran antes de establecer una nueva posición larga
(y viceversa).

## Cómo Funciona

1. Suscribirse a velas de marco temporal (por defecto 5 minutos) y construir una EMA de 60 períodos sobre los precios de cierre.
2. Rastrear la vela actual en tiempo real. En el primer tick de cada nueva barra, almacenar el valor EMA anterior y el cierre de
   la barra previa para que la estrategia pueda compararlos más tarde.
3. Una vez transcurrido el número configurado de minutos desde la apertura (por defecto 3 minutos), evaluar las siguientes
   condiciones usando el precio actual y la pendiente de la EMA:
   - **Configuración de compra**: precio actual < EMA actual, EMA está por debajo de su valor anterior (cayendo), y precio actual
     < cierre de la barra anterior.
   - **Configuración de venta**: precio actual > EMA actual, EMA está por encima de su valor anterior (subiendo), y precio actual
     > cierre de la barra anterior.
4. Si ocurre una configuración de compra mientras no se está ya largo, enviar una orden de compra a mercado dimensionada para
   cerrar cualquier corto abierto y establecer el volumen largo deseado. La misma lógica se aplica simétricamente para
   configuraciones de venta.
5. Después de que se activa una operación, la señal para esa vela se considera procesada para evitar entradas duplicadas.

## Parámetros

- **Volume** – tamaño de orden en lotes (por defecto 1). La estrategia agrega automáticamente el valor absoluto de la posición
  actual cuando necesita invertir direcciones para que la nueva orden cierre la exposición antigua y abra la nueva operación en
  una sola transacción.
- **EMA Length** – período de observación para la media móvil exponencial (por defecto 60).
- **Trigger Minutes** – número de minutos después de que se abre la barra cuando se verifican las condiciones de entrada (por
  defecto 3). Si se pierde la ventana, la estrategia espera la siguiente vela.
- **Candle Type** – tipo de datos de vela utilizado para los cálculos (por defecto marco temporal de 5 minutos).

## Notas de Trading

- La lógica funciona en cualquier símbolo siempre que estén disponibles datos de velas y precios de nivel 1. Ajuste la duración
  de la vela si el instrumento opera en diferentes sesiones que la configuración original de MetaTrader.
- Solo se mantiene una posición (larga o corta) en cualquier momento. Invertir direcciones se realiza dimensionando la nueva
  orden de mercado para cubrir la posición pendiente y abrir la nueva operación en un solo paso.
- No se definen niveles explícitos de stop-loss o take-profit en el EA original. La gestión de riesgo debe añadirse externamente
  si es necesario.
- La protección de inicio está habilitada para que StockSharp gestione automáticamente las salidas de posición de emergencia
  cuando ocurren intervenciones manuales o problemas de conexión.
