# Estrategia Exp Digital MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Exp Digital MACD recrea el comportamiento del asesor experto original de MetaTrader 5 "Exp_Digital_MACD" dentro del framework StockSharp. El sistema escucha velas completadas de un marco temporal dedicado y reacciona a la posición relativa y pendiente de un oscilador estilo MACD. Cuatro modos de operación reproducen las reglas de decisión del código fuente:

1. **Breakdown** – opera transiciones de la línea cero del oscilador.
2. **MACD Twist** – observa una reversión en la pendiente de la línea MACD.
3. **Signal Twist** – usa el giro de la línea de señal misma como confirmación.
4. **MACD Disposition** – busca que el histograma MACD cruce por encima o por debajo de su línea de señal.

Dado que StockSharp no proporciona el filtro propietario "Digital MACD", la estrategia emplea el indicador estándar `MovingAverageConvergenceDivergenceSignal`. Los valores predeterminados (EMA rápida 12, EMA lenta 26, señal 5) aproximan la configuración original donde la longitud de suavizado de señal era igual a cinco. La estrategia procesa solo velas finalizadas y mantiene un historial deslizante corto en campos privados para reflejar el comportamiento `SignalBar = 1` de la implementación MQL.

## Parámetros
- **Mode** – selecciona uno de los cuatro algoritmos de trading descritos anteriormente. Por defecto: `MacdTwist`.
- **FastPeriod** – longitud de la EMA rápida utilizada por MACD. Por defecto: `12`.
- **SlowPeriod** – longitud de la EMA lenta utilizada por MACD. Por defecto: `26`.
- **SignalPeriod** – longitud de la EMA de suavizado de señal. Por defecto: `5` para coincidir con el asesor experto original.
- **CandleType** – marco temporal para la suscripción de velas. Por defecto: velas de `4h`.
- **OrderVolume** – número de contratos o lotes enviados en cada orden de mercado.
- **StopLossPoints / TakeProfitPoints** – compensaciones de protección expresadas en pasos de precio del valor. Se activan cuando el valor expone un valor `Step` válido; establecer en cero para deshabilitar.
- **EnableLongEntry / EnableShortEntry** – interruptores que permiten o prohíben la apertura de nuevas posiciones largas o cortas.
- **EnableLongExit / EnableShortExit** – interruptores que permiten a la estrategia cerrar posiciones existentes en la dirección correspondiente.

## Lógica de trading
El algoritmo trabaja sobre el valor de cierre de cada vela:

- **Breakdown**: Si el valor MACD de hace dos barras estaba por encima de cero, la estrategia opcionalmente cierra posiciones cortas y abre una operación larga cuando la barra siguiente cae de vuelta a cero o por debajo. A la inversa, cuando el MACD de hace dos barras estaba por debajo de cero, el sistema cierra largos y abre cortos si la siguiente barra sube a la línea cero o por encima de ella. Esto refleja la lógica contraria a la línea cero del asesor experto.
- **MACD Twist**: Sigue tres lecturas secuenciales de MACD. Una señal larga aparece cuando la línea forma un mínimo local (value[2] > value[1] y value[0] > value[1]). Un máximo local genera una señal corta. Las salidas siguen el giro opuesto.
- **Signal Twist**: Aplica la misma detección de punto de giro al buffer de la línea de señal.
- **MACD Disposition**: Trabaja con los buffers MACD y de señal. Si el MACD previamente estaba por encima de la línea de señal pero la siguiente observación cae de vuelta a ella o por debajo, la estrategia entra en largo y cierra cortos. La transición opuesta lleva a entradas cortas y salidas largas.

Cada entrada usa una orden de mercado con tamaño `OrderVolume + |posición actual|` para que una reversión cierre la exposición existente y establezca una nueva posición en una sola instrucción. Las señales de salida emiten órdenes de mercado que solo aplanan la posición abierta.

## Gestión de riesgos
`StartProtection` se habilita una vez que la estrategia inicia. Cuando `StopLossPoints` o `TakeProfitPoints` están establecidos por encima de cero y el paso del valor es conocido, las órdenes de protección correspondientes se configuran en términos absolutos de precio. Mantener los parámetros en cero deshabilita la protección automática.

## Notas de implementación
- La estrategia evalúa solo la vela completada más reciente, equivalente a `SignalBar = 1` en la versión MQL.
- La implementación de MACD de StockSharp difiere del Digital MACD propietario. Los usuarios pueden ajustar las longitudes de EMA para aproximar mejor el comportamiento original si se desea.
- Todos los comentarios dentro del archivo fuente C# se proporcionan en inglés según se solicitó.

## Uso
1. Adjuntar la estrategia a un portafolio y un valor que suministre el marco temporal de velas requerido.
2. Ajustar los parámetros para coincidir con el símbolo deseado y las características de volatilidad.
3. Iniciar la estrategia; se suscribirá automáticamente a las velas configuradas, procesará los valores MACD y colocará órdenes de mercado según el modo seleccionado.
4. Monitorear los registros o la salida gráfica opcional para seguir los valores del indicador y los cambios de posición.
