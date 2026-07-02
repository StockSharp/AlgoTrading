# Estrategia de tendencia plana
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia Flat Trend** reproduce las ideas centrales del asesor experto Flat Trend original al combinar filtros de tendencias de varias velocidades, confirmación ADX y un filtro de ruptura de "jugo" de desviación estándar. La estrategia se centra en detectar el momento en que el precio abandona una fase de rango y el impulso se expande, permitiéndole unir movimientos direccionales con protección de posición dinámica.

## Lógica de trading
1. **Filtros de tendencia**: tres promedios móviles exponenciales (EMA) con longitudes configurables representan el activador, el primer filtro y el segundo filtro. Su pendiente y la posición del precio relativa a cada EMA se traducen a estados:
   - Fuerte alcista (precio por encima de EMA y EMA subiendo).
   - Alcista moderado (precio por encima de EMA pero pendiente neutral).
   - Fuerte bajista (precio por debajo de EMA y EMA cayendo).
   - Bajista moderado (precio por debajo de EMA pero pendiente neutral).
2. **Reglas de entrada**
   - Las operaciones largas requieren estados alcistas en el disparador y filtro EMA. Opcionalmente, el segundo filtro se puede ignorar. El modo estricto obliga a utilizar únicamente estados alcistas fuertes.
   - Las operaciones cortas reflejan las condiciones de los estados bajistas.
   - La confirmación opcional ADX garantiza que el índice direccional promedio supere un umbral y, cuando está habilitado, los componentes +DI y –DI concuerdan con la dirección comercial.
   - El filtro "jugo" verifica que la desviación estándar de los precios esté por encima de un nivel de ruptura definido por el usuario, evitando operaciones durante las fases de volatilidad plana.
   - La negociación se puede restringir a una ventana intradiaria seleccionada.
3. **Reglas de salida**
   - Los estados de tendencia opuestos en el activador EMA inician una salida. En modo estricto, la estrategia espera la contraseñal más fuerte.
   - Las paradas dinámicas salen de posiciones cada vez que el precio toca el nivel de parada calculado.

## Gestión del riesgo
- **Parada inicial**: calculada a partir de una distancia de pip estática o del valor del rango verdadero promedio (ATR), emulando la lógica basada en ADR del EA original.
- **Trailing stop**: movimientos con el precio más alto (o más bajo) desde la entrada utilizando el ATR multiplicado por un divisor.
- **Equipo**: una vez que el precio avanza la distancia configurada, el stop se mueve más allá del precio de entrada en un pequeño valor de bloqueo.

## Parámetros
| Nombre | Descripción |
| --- | --- |
| `TriggerLength` | EMA longitud para el filtro de activación. |
| `FilterLength1` | EMA longitud para el primer filtro de confirmación. |
| `FilterLength2` | EMA longitud para el segundo filtro de confirmación. |
| `UseOnlyPrimaryIndicators` | Utilice únicamente el activador y el primer filtro para las entradas. |
| `IgnoreModerateForEntry` | Requerir estados de tendencia fuertes para nuevas operaciones. |
| `IgnoreModerateForExit` | Requiere fuertes contraseñales para cerrar operaciones. |
| `UseTradingHours` | Habilite la ventana de negociación intradía. |
| `TradingHourBegin` / `TradingHourEnd` | Hora de inicio y finalización de la ventana de negociación. |
| `UseJuiceFilter`, `JuicePeriod`, `JuiceThreshold` | Parámetros del filtro de ruptura de desviación estándar. |
| `UseAdxFilter`, `AdxPeriod`, `AdxThreshold`, `UseDirectionalFilter` | ADX fuerza y confirmación DI. |
| `UseAdrForStop`, `StopLossPips` | Configuración inicial de stop-loss. |
| `TrailingDivisor` | multiplicador ATR para el cálculo del trailing stop. |
| `BreakEvenPips`, `BreakEvenLockPips` | Activación del punto de equilibrio y distancia de bloqueo. |
| `AtrPeriod` | ATR retrospectiva utilizada para la estimación de la volatilidad. |
| `CandleType` | Periodo de tiempo de la vela principal. |

## Resumen de indicadores
- **Promedio móvil exponencial (EMA)**: tres instancias para evaluación de tendencias de múltiples velocidades.
- **Desviación estándar**: modela el filtro de ruptura de volatilidad "jugo".
- **Rango verdadero promedio (ATR)**: mide la volatilidad de las paradas y el seguimiento.
- **Índice direccional promedio (ADX)**: confirma la fuerza y dirección de la tendencia.

## Notas de uso
1. Asegúrese de que la seguridad de la estrategia tenga un `PriceStep` definido; de lo contrario, se utiliza el paso predeterminado de 0,0001 para distancias basadas en pips.
2. La estrategia utiliza órdenes de mercado (`BuyMarket`, `SellMarket`) y escala automáticamente el volumen al revertir posiciones.
3. Las paradas dinámicas se simulan internamente cerrando posiciones cuando se toca el nivel de parada virtual.
4. Combine la ventana de negociación y opciones de entrada estrictas para centrarse en sesiones de alta liquidez y evitar períodos agitados.
