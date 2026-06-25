# Estrategia Exp CandlesticksBW Tm
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia reproduce el experto de MetaTrader **Exp_CandlesticksBW_Tm** sobre la API de alto nivel de StockSharp. Se basa en el indicador Candlesticks BW de Bill Williams, que pinta los colores de las velas combinando el Awesome Oscillator (AO) y el Accelerator Oscillator (AC). La aceleración y desaceleración del momentum se detectan mediante transiciones de color de velas, mientras que un filtro opcional de sesión de trading restringe las entradas a horas intradiarias específicas.

## Cómo funciona

1. Se suscribe al marco temporal configurado (predeterminado **H4**) y alimenta cada vela finalizada a un Awesome Oscillator (5/34). La serie AO se suaviza con una media móvil simple de 5 períodos para producir el componente Accelerator.
2. Cada vela se clasifica en uno de seis estados de color: dos colores de momentum alcista (AO y AC subiendo), dos colores de momentum bajista (AO y AC cayendo) y dos colores neutros (dirección mixta AO/AC). La dirección del cuerpo de la vela decide entre el tono más oscuro o más claro en cada par.
3. Un buffer circular almacena los índices de color recientes junto con sus tiempos de apertura. El parámetro **SignalBar** selecciona qué barra histórica evaluar (predeterminado = vela anterior, es decir, offset 1). Una barra más atrás se usa como contexto.
4. Las entradas largas se habilitan cuando la barra más antigua pertenecía a una zona de momentum alcista y la barra de señal sale de esa zona. Las entradas cortas reflejan la lógica con zonas bajistas. Las señales de salida usan los mismos filtros de momentum para cerrar la dirección opuesta.
5. El filtro de sesión opcional (**UseTimeFilter**) mantiene un registro de trading entre **StartHour:StartMinute** y **EndHour:EndMinute**. Salir de la ventana liquida inmediatamente las posiciones abiertas, previniendo exposición nocturna.
6. Las protecciones de stop-loss y take-profit se registran a través de `StartProtection`, traduciendo distancias en puntos en pasos de precio del instrumento.

## Reglas de trading

- **Abrir largo**: índice de color de la barra anterior `< 2` (AO y AC acelerando hacia arriba) y el índice de color de la barra de señal `> 1`. Las entradas largas se omiten si ya se está largo o si los largos están deshabilitados.
- **Abrir corto**: índice de color de la barra anterior `> 3` (AO y AC acelerando hacia abajo) y el índice de color de la barra de señal `< 4`.
- **Cerrar largo**: se activa cuando el índice de color de la barra más antigua `> 3` (aceleración bajista) y las salidas largas están habilitadas.
- **Cerrar corto**: se activa cuando el índice de color de la barra más antigua `< 2` (aceleración alcista) y las salidas cortas están habilitadas.
- Cuando el filtro de tiempo está activo, las posiciones se cierran forzosamente fuera de la sesión permitida incluso sin señales de salida basadas en color.

## Parámetros

| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `CandleType` | Marco temporal usado para los cálculos AO/AC. | `TimeSpan.FromHours(4).TimeFrame()` |
| `Volume` | Tamaño de la orden para nuevas entradas. | `1m` |
| `SignalBar` | Número de velas finalizadas a omitir antes de evaluar señales (1 = vela anterior). | `1` |
| `StopLossPoints` | Distancia del stop protector en puntos del instrumento. Establezca `0` para deshabilitar. | `1000m` |
| `TakeProfitPoints` | Distancia del objetivo de beneficio en puntos del instrumento. Establezca `0` para deshabilitar. | `2000m` |
| `EnableLongEntries`, `EnableShortEntries` | Permitir abrir operaciones en la dirección respectiva. | `true` |
| `EnableLongExits`, `EnableShortExits` | Permitir cerrar operaciones en la dirección respectiva. | `true` |
| `UseTimeFilter` | Habilitar restricciones de sesión de trading. | `true` |
| `StartHour`, `StartMinute`, `EndHour`, `EndMinute` | Límites de sesión (inicio inclusivo, fin exclusivo para horas idénticas). | `0/0/23/59` |

## Notas

- La estrategia sincroniza automáticamente las distancias de stop-loss y take-profit con el paso de precio del instrumento.
- Las señales tienen marca de tiempo usando el tiempo de cierre de la barra evaluada para suprimir operaciones repetidas dentro de la misma barra.
- No se proporciona versión de Python, coincidiendo con la estructura del paquete MQL fuente.
