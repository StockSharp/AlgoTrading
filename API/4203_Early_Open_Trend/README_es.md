# Estrategia de tendencia abierta temprana
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
- Puerto del MetaTrader 4 asesor experto `earlyOpenTrend.mq4` ubicado en `MQL/9826`.
- Se negocia una vez por dirección por día comparando el precio actual con la apertura diaria después de una confirmación basada en mecha.
- Imita la lógica de ventana de tiempo original, incluida la compensación del horario de verano que cambia la sesión del corredor en una o dos horas.
- Utiliza StockSharp API de alto nivel con suscripciones de velas, protección de posición automatizada y manejo de sesiones integrado.

## Lógica del mercado
1. Cree una serie de velas intradía (por defecto, 15 minutos) y reconstruya los valores de apertura, máximo y mínimo del día actual.
2. Determine el desplazamiento del horario de verano activo: entre `SummerTimeStartDay` y `WinterTimeStartDay` la estrategia resta dos horas de los tiempos de sesión configurados; en caso contrario se resta una hora. Esto reproduce la variable `ZD` original.
3. Solo evalúe las señales cuando la hora de inicio de la vela esté dentro de `[StartHour, EndHour)` después de la corrección del horario de verano y la estrategia sea plana.
4. Configuración larga:
   - La última vela cerró por encima del precio de apertura diario.
   - La distancia entre la apertura diaria y el mínimo del día actual supera `RangeFilterPips` (convertido a precio absoluto utilizando el tamaño del pip del instrumento).
   - No se ha abierto ninguna operación larga antes durante el mismo día de negociación.
5. Configuración breve:
   - La última vela cerró por debajo del precio de apertura diario.
   - La distancia entre el máximo del día actual y la apertura diaria supera `RangeFilterPips`.
   - No se ha abierto ninguna operación corta antes durante el mismo día de negociación.
6. Cuando se activa una señal, la estrategia emite una orden de mercado con un volumen `OrderVolume`. La marca de tiempo comercial se almacena para respaldar las salidas del tiempo de espera.

## Reglas de sesión y salida
- `EndHour` evita nuevas entradas después del tiempo especificado (ajustado por el desplazamiento de DST).
- `ClosingHour` fuerza el cierre de la posición una vez que la hora del servidor corregida alcanza el valor configurado.
- `HoldingHours` impone una duración máxima adicional de tenencia; una vez superada la posición se cierra independientemente del tiempo de sesión.
- Cada dirección comercial se puede ejecutar como máximo una vez por día calendario. Las banderas diarias se restablecen cuando la estrategia detecta el inicio de una nueva sesión.

## Gestión del riesgo
- `StopLossPips` y `TakeProfitPips` se transforman en compensaciones de precio absoluto utilizando el tamaño del pip derivado de `Security.PriceStep` (los símbolos de 5 dígitos multiplican automáticamente el paso por 10).
- Si cualquiera de los parámetros es mayor que cero, la estrategia habilita `StartProtection` con la ejecución del mercado, replicando la lógica original posterior a la entrada `OrderModify`.
- Fuera de las salidas forzadas descritas anteriormente, no se aplica ninguna lógica de seguimiento adicional.

## Parámetros
| Nombre | Predeterminado | Descripción |
|------|---------|-------------|
| `OrderVolume` | 0.1 | Tamaño de cada orden de mercado. |
| `OrderType` | 0 | Filtro de dirección: `0` = ambos, `1` = solo largo, `2` = solo corto. |
| `RangeFilterPips` | 1 | Distancia mínima de mecha entre la apertura diaria y el extremo opuesto antes de entrar. |
| `TakeProfitPips` | 100 | Distancia de toma de ganancias en pips (0 inhabilitaciones). |
| `StopLossPips` | 1000 | Distancia de stop-loss en pips (0 desactivaciones). |
| `StartHour` | 7 | Hora de inicio de sesión antes de la resta del horario de verano. |
| `EndHour` | 18 | Hora de finalización de la sesión antes de la resta del horario de verano. |
| `ClosingHour` | 20 | Hora utilizada para aplanar las operaciones abiertas. |
| `HoldingHours` | 0 | Tiempo máximo de espera en horas (0 inhabilitaciones). |
| `SummerTimeStartDay` | 87 | Primer día del año que activa el cambio de horario de verano de dos horas. |
| `WinterTimeStartDay` | 297 | Día del año en el que el desplazamiento vuelve a ser de una hora. |
| `CandleType` | plazo de 15 minutos | Serie de velas utilizadas para los cálculos. |

## Notas de uso
1. Adjunte la estrategia a un valor y asegúrese de que el tipo de vela coincida con la granularidad de la fuente de datos que desea negociar.
2. Ajuste el horario de la sesión para que coincida con el reloj del servidor del corredor. Los parámetros de horario de verano se pueden ajustar si el horario de verano local difiere del horario europeo predeterminado.
3. Configure paradas y objetivos basados en pips de acuerdo con el tamaño del tick del instrumento; la estrategia convierte automáticamente pips utilizando el valor de pip detectado.
4. Inicia la estrategia. Actualizará el perfil del día en cada vela terminada, evaluará los criterios de entrada dentro de la ventana de la sesión y aplicará la restricción de operación única por dirección.

## Diferencias frente al experto original MQL
- Utiliza velas terminadas en lugar de comprobaciones de nivel de tick `Bid`/`Ask`, lo que retrasa ligeramente las entradas pero mantiene la lógica determinista dentro de StockSharp.
- Las órdenes de protección se implementan mediante llamadas `StartProtection` en lugar de llamadas manuales `OrderModify`.
- Se omiten los objetos gráficos y los comentarios de estado del gráfico MetaTrader (rectángulos, etiquetas, visualización de pliegos).
- Las salidas forzadas al cerrar la sesión cierran la posición inmediatamente en lugar de cambiar a un objetivo de equilibrio cuando está bajo el agua.

## Recomendaciones de prueba
- Realice una prueba retrospectiva con datos intradía que cubran la sesión de negociación completa para que los máximos y mínimos diarios coincidan con el entorno real.
- Valide la configuración de DST simulando fechas en los períodos de verano e invierno.
- Experimente con diferentes umbrales de mecha y horas de sesión para alinear el comportamiento con el perfil de volatilidad de su corredor.
