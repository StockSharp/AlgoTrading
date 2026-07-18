# Estrategia Pinball Machine
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia Pinball Machine** es una conversión lúdica del asesor experto MetaTrader 5 "Pinball machine (barabashkakvn's edition)". En lugar de analizar la estructura del mercado, la estrategia emula una máquina de lotería: cada vela terminada activa varias extracciones aleatorias que pueden resultar en una operación si dos números coinciden. El port de StockSharp mantiene el espíritu del experto original mientras adapta la gestión de dinero y la ejecución a la API de alto nivel.

## Lógica de trading
1. **Disparador** – la estrategia trabaja en el marco temporal definido por `Candle Type`. Cuando se completa una vela, el proceso aleatorio se ejecuta una vez.
2. **Extracciones aleatorias** – se generan cuatro enteros en el rango 0–100. Una configuración larga aparece si el primer par coincide y una configuración corta aparece si el segundo par coincide. Dado que las extracciones son independientes, es posible (aunque raro) generar ambas señales en la misma vela.
3. **Elegibilidad de orden** – la estrategia solo coloca una nueva orden cuando no hay ninguna posición actualmente abierta. Esto mantiene la exposición neta unilateral, a diferencia del comportamiento de cobertura del original MQL.
4. **Distancias de stop/objetivo** – para cada orden se producen dos números aleatorios adicionales en el rango definido por `Min Offset Points` y `Max Offset Points`. Determinan la distancia (en pasos de precio) para los niveles de stop-loss y take-profit alrededor del precio de entrada.
5. **Dimensionamiento de posición** – el capital en riesgo está limitado por el parámetro `Risk Percent`. La estrategia estima el valor del portafolio (prefiriendo `CurrentValue`, luego `CurrentBalance`, luego `BeginValue`) y divide el riesgo permitido por la distancia al stop. Cuando el cálculo no es posible o resultaría en tamaño cero, el fallback es el `Volume` de la estrategia (predeterminado 1 lote).
6. **Ejecución de órdenes** – las órdenes de mercado se emiten vía `BuyMarket` / `SellMarket`. El precio de cierre de la vela se usa como proxy para la cotización de entrada porque los datos de Bid/Ask a nivel de tick no están disponibles en el flujo de trabajo impulsado por velas.
7. **Gestión de operaciones** – los niveles de stop-loss y take-profit se verifican en cada vela terminada. Si el precio penetra un nivel la posición se cierra mediante una orden de mercado, reflejando el comportamiento de las órdenes protectoras en la versión de MetaTrader.

## Parámetros
- **Risk Percent** – porcentaje del valor del portafolio que puede perderse si se alcanza el stop-loss. Valores superiores a cero habilitan el dimensionamiento de posición basado en riesgo.
- **Min Offset Points / Max Offset Points** – límites inclusivos (expresados en pasos de precio) para seleccionar aleatoriamente las distancias de stop y objetivo. Ambos parámetros deben mantenerse positivos; la implementación los intercambia automáticamente si el mínimo supera al máximo.
- **Candle Type** – la serie de datos que impulsa el motor aleatorio. Cualquier `DataType` compatible con `SubscribeCandles` puede usarse (velas de un minuto por defecto).

## Diferencias con la versión MetaTrader
- **Fuente de eventos** – el experto MT5 trabaja en cada tick. La estrategia StockSharp evalúa la lotería aleatoria en velas terminadas para seguir el enfoque de API de alto nivel recomendado.
- **Cobertura** – MetaTrader puede acumular múltiples posiciones en ambos lados. El port se limita a una única posición neta (larga, corta o plana) porque las estrategias StockSharp típicamente son neteadas.
- **Gestión de dinero** – el original dependía de `CMoneyFixedMargin`. La versión C# reproduce la idea usando métricas de portafolio y dimensionamiento de riesgo porcentual.
- **Colocación de órdenes** – los bucles de slippage explícito y reintento son innecesarios en StockSharp y fueron eliminados. Las órdenes de mercado se envían una vez que el entorno reporta preparación (`IsFormedAndOnlineAndAllowTrading`).

## Notas de uso
- Asegurarse de que el instrumento seleccionado exponga un `PriceStep` válido. Si no está disponible, la estrategia recurre a un paso de 1 para mantener la simulación en marcha.
- Dado que el sistema es intencionalmente aleatorio, el rendimiento variará fuertemente entre backtests. Usar la estrategia principalmente para experimentar con infraestructura, manejo de riesgo o aleatoriedad estilo Monte Carlo.
- Ajustar el marco temporal de la vela para controlar la frecuencia con que pueden aparecer operaciones. Velas más cortas aumentan el número de loterías por sesión.
- La estrategia dibuja tanto velas como operaciones ejecutadas en un área de gráfico cuando está disponible el charteo, lo que ayuda a diagnosticar con qué frecuencia se cumplen las condiciones aleatorias.

## Notas de conversión
- Archivo original: `MQL/17744/Pinball machine.mq5`.
- Mantenidos todos los controles de entrada (porcentaje de riesgo, rangos de stop y objetivo) en forma de parámetro adecuados para optimización dentro de StockSharp.
- La semilla aleatoria usa el predeterminado de la plataforma (`Random()`), que es equivalente a la llamada `MathSrand(GetTickCount())` del experto MetaTrader.
