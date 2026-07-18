# Estrategia NRTR ATR Stop
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **Estrategia NRTR ATR Stop** es una conversión directa del asesor experto de MetaTrader `Exp_NRTR_ATR_STOP_Tm`. El sistema combina un stop de Reversión de Tendencia No Repintable (NRTR) con un filtro de Rango Verdadero Promedio (ATR) para determinar la tendencia dominante y arrastrar los niveles de protección. Las decisiones de trading se generan en el cierre del marco temporal seleccionado y pueden retrasarse por un número configurable de barras completamente formadas para imitar el desplazamiento de señal original.

La estrategia está implementada sobre la API de alto nivel de StockSharp. Toda la lógica de trading está impulsada por suscripciones de velas, vínculos de indicadores y asistentes de órdenes gestionadas, garantizando compatibilidad con los productos Designer, Shell, Runner y API.

## Lógica de trading

1. **Cálculo del indicador**
   - El ATR se calcula en el marco temporal seleccionado con el período proporcionado.
   - El valor del ATR se multiplica por un coeficiente para construir los niveles superior e inferior del NRTR.
   - La dirección de la tendencia cambia cuando la vela anterior rompe el nivel NRTR opuesto; estos eventos también crean señales de flecha que pueden activar entradas.
2. **Retraso de señal**
   - El parámetro `SignalBarDelay` reproduce la entrada `SignalBar` de MetaTrader. Retrasa la ejecución por el número elegido de velas completadas, permitiendo que la estrategia evalúe señales históricas exactamente como el experto fuente.
3. **Entradas**
   - Una posición **larga** se abre cuando ocurre una reversión alcista de NRTR y las entradas largas están habilitadas.
   - Una posición **corta** se abre cuando ocurre una reversión bajista de NRTR y las entradas cortas están habilitadas.
4. **Salidas**
   - Las reversiones direccionales cierran cualquier posición opuesta si el cierre está permitido para ese lado.
   - Un filtro de sesión opcional puede forzar el cierre de todas las posiciones fuera de la ventana de trading permitida.
   - La gestión de riesgo adicional se maneja a través de distancias de stop-loss y take-profit expresadas en pasos de precio. El nivel NRTR también arrastra una posición activa ajustando el stop de protección en la dirección de la tendencia.

## Gestión de riesgo

- **Volumen**: Las operaciones se abren con el parámetro configurable `OrderVolume`. El volumen puede optimizarse al igual que en la versión de MetaTrader.
- **Stop-loss / take-profit**: Las distancias se especifican en múltiplos del paso de precio del instrumento, coincidiendo con la configuración original basada en puntos. Cuando tanto un stop manual como un nivel NRTR están disponibles, el precio de protección se elige de forma conservadora (más cercano al mercado) para evitar ampliar el riesgo.
- **Control de sesión**: Cuando `UseTradingWindow` está habilitado, la estrategia solo abre posiciones dentro del intervalo `[StartHour:StartMinute, EndHour:EndMinute]` definido y cierra cualquier posición abierta tan pronto como el mercado abandona esa ventana.

## Parámetros

| Nombre | Predeterminado | Descripción |
| --- | --- | --- |
| `OrderVolume` | 1 | Volumen utilizado al enviar órdenes de mercado. |
| `StopLossPoints` | 1000 | Distancia de stop en pasos de precio. Poner en `0` para deshabilitar. |
| `TakeProfitPoints` | 2000 | Distancia de take-profit en pasos de precio. Poner en `0` para deshabilitar. |
| `BuyPosOpen` / `SellPosOpen` | `true` | Permitir la apertura de posiciones largas o cortas en reversiones NRTR. |
| `BuyPosClose` / `SellPosClose` | `true` | Permitir el cierre de posiciones largas o cortas cuando aparece una señal opuesta. |
| `UseTradingWindow` | `true` | Habilitar el filtro de tiempo que imita el asesor experto original. |
| `StartHour` / `StartMinute` | 0 / 0 | Inicio de la sesión de trading permitida. |
| `EndHour` / `EndMinute` | 23 / 59 | Fin de la sesión de trading permitida. Soporta rangos nocturnos. |
| `CandleType` | Marco temporal de 1 hora | Tipo de vela utilizado para los cálculos de ATR y NRTR. |
| `AtrPeriod` | 20 | Número de barras utilizadas para calcular el ATR. |
| `AtrMultiplier` | 2 | Coeficiente aplicado al ATR al construir los niveles NRTR. |
| `SignalBarDelay` | 1 | Número de barras completadas para retrasar la ejecución de la señal. |

## Notas

- La estrategia usa solo procesamiento a nivel de vela; la replicación tick a tick del EA original se evita intencionalmente para mantenerse consistente con la arquitectura de alto nivel de StockSharp.
- Los comentarios dentro del código están en inglés según los requisitos del proyecto.
- Se omite intencionalmente una versión en Python para cumplir con la solicitud del usuario.
