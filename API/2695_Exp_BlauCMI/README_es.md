# Exp BlauCMI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción General
La estrategia recrea el asesor experto de MetaTrader 5 **Exp_BlauCMI** usando la API de alto nivel de StockSharp. Calcula el Blau Candle Momentum Index (CMI), una ratio de momentum con triple suavizado, en una serie de velas configurable y reacciona a los giros del oscilador. Las operaciones largas se abren cuando el indicador gira hacia arriba después de un movimiento descendente, las cortas cuando el indicador gira hacia abajo. El módulo mantiene la implementación totalmente dirigida por eventos: las órdenes se envían solo después de que se cierran las velas.

## Lógica del indicador
1. Dos fuentes de precio se seleccionan mediante `Momentum Price` y `Reference Price`. El momentum bruto es la diferencia entre el valor actual del primer precio y el valor retrasado del segundo precio. El retraso se controla mediante `Momentum Depth`.
2. Tanto el momentum como su valor absoluto pasan por tres medias móviles consecutivas (`First/Second/Third Smoothing`). Se usa el mismo método de promediado para cada etapa y se puede elegir entre medias móviles simples, exponenciales, suavizadas (RMA) y ponderadas linealmente.
3. El Blau CMI se calcula como `100 * smoothedMomentum / smoothedAbsMomentum`. El indicador empieza a producir señales de trading una vez que la tercera etapa de suavizado ha acumulado suficientes barras.
4. El parámetro `Signal Shift` determina cuántas velas cerradas hacia atrás inspecciona la estrategia antes de evaluar las reversiones (un valor de 1 reproduce el EA original y usa la última barra cerrada).

## Reglas de trading
- **Entrada larga** – permitida cuando `Allow Long Entry` está habilitado y se observa la secuencia de indicador `Value[Signal Shift - 1] < Value[Signal Shift - 2]` seguida de `Value[Signal Shift] > Value[Signal Shift - 1]`, lo que significa que el oscilador acaba de girar hacia arriba. Las posiciones cortas existentes se cierran primero si `Allow Short Exit` está habilitado.
- **Entrada corta** – permitida cuando `Allow Short Entry` está habilitado y el indicador gira hacia abajo (`Value[Signal Shift - 1] > Value[Signal Shift - 2]` y `Value[Signal Shift] < Value[Signal Shift - 1]`). Las posiciones largas existentes se cierran de antemano si `Allow Long Exit` está habilitado.
- **Salida larga** – cuando hay una posición larga y se activa la condición de entrada corta, la posición se cierra si `Allow Long Exit` es verdadero.
- **Salida corta** – cuando hay una posición corta y se activa la condición de entrada larga, la posición se cierra si `Allow Short Exit` es verdadero.
- Todas las operaciones se ejecutan con órdenes de mercado usando el volumen especificado en `Order Volume`. Los brackets de stop-loss y take-profit protectores se adjuntan automáticamente mediante `StartProtection` y permanecen activos mientras la posición esté abierta.

## Parámetros
- `Candle Type` – tipo de dato (marco temporal u otra descripción de velas) utilizado para el cálculo del indicador y las decisiones de trading. El valor predeterminado son velas de 4 horas.
- `Smoothing Method` – algoritmo de promediado compartido por las tres etapas de suavizado (Simple, Exponencial, Suavizado, Ponderado Lineal).
- `Momentum Depth` – número de barras entre los dos puntos de precio que forman el momentum bruto.
- `First/Second/Third Smoothing` – longitudes de las tres etapas de promediado aplicadas tanto al momentum como a su valor absoluto.
- `Signal Shift` – número de velas ya cerradas a mirar hacia atrás al evaluar patrones de reversión (valor mínimo es 1).
- `Momentum Price` – precio aplicado usado para el lado no retrasado del cálculo del momentum.
- `Reference Price` – precio aplicado usado para el lado de comparación retrasado.
- `Allow Long Entry`, `Allow Short Entry` – interruptores para permitir abrir operaciones en cada dirección.
- `Allow Long Exit`, `Allow Short Exit` – interruptores que controlan si las señales opuestas cierran las respectivas posiciones.
- `Stop-Loss Points`, `Take-Profit Points` – límites de riesgo medidos en pasos de precio (`Security.PriceStep`). Cuando se establecen en cero, el bracket correspondiente está deshabilitado.
- `Order Volume` – cantidad absoluta utilizada al enviar órdenes de mercado. La estrategia también asigna este valor a la propiedad base `Strategy.Volume`.

## Notas adicionales
- Los métodos de suavizado soportados corresponden a indicadores de StockSharp: Media Móvil Simple, Media Móvil Exponencial, Media Móvil Suavizada (RMA) y Media Móvil Ponderada.
- La constante de precio Demark replica la implementación de MT5 promediando los extremos de precio y el cierre de la vela antes de ajustar las distancias al máximo/mínimo.
- Dado que los cálculos usan solo velas terminadas, la estrategia reacciona una vez por barra, coincidiendo con el comportamiento original del EA que verificaba barras nuevas mediante `IsNewBar`.
- `Stop-Loss Points` y `Take-Profit Points` se interpretan como múltiplos del paso de precio del instrumento para mantener consistencia con las entradas basadas en puntos de la estrategia MQL5 original.
