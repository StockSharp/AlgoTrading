# Estrategia CH2010 Structure de Rompimiento Multi-Marco Temporal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia replica el comportamiento del experto original **ch2010structure.mq5** rastreando múltiples pares forex en dos marcos temporales. Cada instrumento monitorea la vela diaria para determinar un sesgo direccional y luego observa velas de 30 minutos en busca de rompimientos más allá del rango diario anterior. Las posiciones de mercado se abren cuando el rompimiento se alinea con la tendencia diaria y se cierran usando niveles protectores de stop-loss y take-profit.

## Lógica principal

1. **Detección de sesgo diario**  
   * La estrategia se suscribe a velas diarias para USDCHF, GBPUSD, AUDUSD, USDJPY y EURGBP.  
   * Cuando una vela diaria termina, la relación cierre/apertura define el sesgo: alcista, bajista o neutral.  
   * El máximo, mínimo y cierre diario se almacenan junto con la fecha de sesión para que la lógica intradía pueda confirmar que está operando en la misma sesión.

2. **Ejecución de rompimientos intradía**  
   * Las velas de 30 minutos se evalúan una vez que cierran.  
   * Si el cierre está por encima del máximo diario anterior más un búfer configurable y el sesgo no es bajista, se activa una operación larga.  
   * Si el cierre está por debajo del mínimo diario anterior menos el búfer y el sesgo no es alcista, se activa una operación corta.  
   * Solo se puede activar un rompimiento largo y uno corto por instrumento cada día para evitar operar en exceso.

3. **Gestión de riesgo inspirada en las funciones helper originales**  
   * Los volúmenes se limitan entre `MinTradeVolume` y `MaxTradeVolume` y la posición agregada en todos los instrumentos está restringida por `MaxAggregateVolume`.  
   * Cada posición completada calcula inmediatamente los niveles absolutos de stop-loss y take-profit usando offsets porcentuales desde el precio de entrada.  
   * Las posiciones se cierran mediante órdenes de mercado tan pronto como se alcanza el stop o el objetivo; las órdenes de salida repetidas se evitan con el flag `ExitInProgress`.

4. **Seguimiento de estado**  
   * Para cada instrumento, la estrategia rastrea sus propios niveles diarios, última posición conocida, lado de entrada, órdenes de salida y flags de rompimiento en un `InstrumentContext`.  
   * Esto permite el flujo de trabajo multi-símbolo sin tener que mantener colecciones personalizadas fuera de la clase de contexto.

## Parámetros de la estrategia

| Parámetro | Descripción |
| --- | --- |
| `TradeVolume` | Volumen base usado para nuevas entradas, sujeto a los límites de volumen. |
| `MinTradeVolume` y `MaxTradeVolume` | Límites que reflejan el filtro de riesgo original de MQL. |
| `MaxAggregateVolume` | Suma máxima de posiciones absolutas en todos los pares operados. |
| `StopLossPercent` | Offset del stop de protección en porcentaje desde el precio de entrada detectado. |
| `TakeProfitPercent` | Offset del take-profit en porcentaje desde el precio de entrada detectado. |
| `BreakoutBufferPercent` | Porcentaje del rango diario anterior añadido a los disparadores de rompimiento. |
| `DailyCandleType` | DataType usado para solicitar las velas de marco temporal superior. |
| `IntradayCandleType` | DataType usado para solicitar las velas de marco temporal de ejecución. |
| `UsdChfSecurity` .. `EurGbpSecurity` | Objetos de instrumento para los cinco símbolos forex monitoreados por defecto. |

## Datos requeridos

* Velas diarias para cada símbolo configurado (por defecto: marco temporal de 1 día).  
* Velas intradía (por defecto: 30 minutos) para los mismos símbolos.  
* Enrutamiento de órdenes en tiempo real para enviar órdenes de mercado para cada instrumento.

## Notas de uso

1. Configurar los cinco parámetros de instrumento antes de iniciar la estrategia. Pueden reemplazarse con otros instrumentos si se desea.  
2. Establecer el portafolio y conector como en otras estrategias de StockSharp.  
3. Opcionalmente ajustar el búfer de rompimiento o los parámetros de riesgo para reflejar las especificaciones de contrato del broker objetivo.  
4. Iniciar la estrategia. Se suscribirá automáticamente a ambos flujos de velas para cada instrumento, registrará la estructura diaria y esperará rompimientos intradía válidos.  
5. Monitorear el log para entradas como `Daily candle captured` y `Enter Buy` para verificar el flujo de decisiones.

## Diferencias vs. el Experto MQL original

* Las órdenes pendientes se reemplazan con órdenes de mercado inmediatas una vez que se observa la condición de rompimiento. Esto mantiene la lógica compatible con la API de alto nivel de StockSharp mientras preserva la idea de limitar la exposición y reaccionar solo una vez por dirección cada día.  
* Las restricciones de volumen del helper `DebugOrderSend` se adaptaron en parámetros que limitan los tamaños de operaciones individuales y la exposición total.  
* Se agrega registro extenso para mostrar niveles diarios, razones de entrada y disparadores de salida en comentarios en inglés para facilitar la depuración en StockSharp.

## Descargo de responsabilidad

Este ejemplo está destinado a propósitos educativos. Los parámetros e instrumentos deben revisarse y ajustarse antes de usar la estrategia en trading de producción.
