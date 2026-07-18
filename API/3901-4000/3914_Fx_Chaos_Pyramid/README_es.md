# Estrategia de pirámide del caos FX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

FX Chaos Pyramid es una estrategia de ruptura de varias etapas convertida del asesor experto MetaTrader 4 "FX-CHAOS" ubicado en `MQL/8055`. El puerto mantiene la lógica original de múltiples períodos de tiempo: la ejecución principal ocurre en el período de 4 horas, mientras que el período de tiempo diario proporciona filtros de ruptura de nivel superior. Las entradas se confirman con el filtro de impulso Awesome Oscillator antes de que se abra la primera etapa. Las etapas adicionales se acumulan en la posición existente siempre que la tendencia continúa en el período de tiempo principal.

La implementación StockSharp utiliza el API de alto nivel con suscripciones de velas, vinculación de indicadores y asistentes de órdenes nativos, por lo que la estrategia se puede utilizar tanto para pruebas retrospectivas como para operaciones reales sin código de infraestructura adicional.

## Lógica de trading

### Filtro de marco de tiempo más alto

* Suscríbase a velas diarias y calcule la última oscilación en ZigZag confirmada utilizando un detector fractal de 5 velas.
* Guarde los máximos y mínimos del día anterior. Se agrega un búfer configurable en pasos de precios a ambos niveles antes de realizar las verificaciones de ruptura.

### Ejecución del plazo principal

* Suscríbase a velas de 4 horas y vincule Awesome Oscillator (configuración predeterminada 5/34).
* Realice un seguimiento de la última oscilación fractal en el período de 4 horas como un análogo del indicador personalizado `zzf` original.
* Registre la primera vela abierta de 4 horas para cada nuevo día de negociación. Este valor juega el mismo papel que `iOpen(NULL, 1440, 0)` en MQL.

### Reglas de entrada

* **Etapa larga inicial**: el día actual se abre por debajo del máximo diario anterior amortiguado, el cierre de 4 horas supera ese nivel amortiguado, el precio aún se mantiene por debajo del último fractal alcista diario y el Oscilador Impresionante es negativo. Las posiciones cortas existentes se cierran antes de abrir las largas.
* **Etapa corta inicial**: lógica especular con el mínimo diario y el Awesome Oscillator por encima de cero.

### Etapas de la pirámide

Una vez completada la etapa inicial, la estrategia evalúa cada vela completa de 4 horas:

* Se coloca una adición larga cuando la vela se abre por debajo y cierra por encima del máximo anterior de 4 horas amortiguado, mientras que el cierre permanece por debajo del último fractal ascendente del marco temporal primario.
* Una breve adición utiliza el mínimo amortiguado de 4 horas y el último fractal descendente.
* Filtro de capital opcional: solo se permiten etapas adicionales cuando el capital de la cartera es mayor que el saldo, replicando el requisito `AccountEquity() > AccountBalance()` del experto MQL.

El número de etapas adicionales es configurable (hasta cinco para coincidir con la matriz de lote original). Las etapas se reinician cada vez que se cierra la posición o cuando una señal de inversión cierra el lado opuesto.

## Gestión monetaria

El experto original ajusta la matriz de lotes según el saldo de la cuenta. Este puerto mantiene las mismas definiciones por partes y expone el equilibrio base, el paso de equilibrio y el multiplicador de volumen global como parámetros. El capital de la cartera actual se asigna a un segmento `MAX_Lots` (que varía de 3,0 a 15,0 lotes) y se selecciona el vector de lote apropiado:

| rango `MAX_Lots` | Etapa 1 | Etapa 2 | Etapa 3 | Etapa 4 | Etapa 5 |
|------------------|---------|---------|---------|---------|---------|
| < 2             | 0,10    | 0,50    | 0,40    | 0,30    | 0,20    |
| [2, 4)           | 0,20    | 1.00    | 0,80    | 0,60    | 0,40    |
| [4, 5)           | 0,30    | 1,50    | 1.20    | 0,90    | 0,60    |
| [5, 7)           | 0,40    | 2.00    | 1,60    | 1.20    | 0,80    |
| [7, 8)           | 0,50    | 2.50    | 2.00    | 1,50    | 1.00    |
| [8, 10)          | 0,60    | 3.00    | 2.40    | 1,80    | 1.20    |
| [10, 11)         | 0,70    | 3.50    | 2,80    | 2.10    | 1,40    |
| [11, 13)         | 0,80    | 4.00    | 3.20    | 2.40    | 1,60    |
| [13, 14)         | 0,90    | 4.50    | 3.60    | 2.70    | 1,80    |
| ≥ 14             | 1.00    | 5.00    | 4.00    | 3.00    | 2.00    |

Multiplicar por el parámetro `VolumeScale` permite aplicar la misma distribución relativa a diferentes corredores o clases de activos.

## Parámetros

| Nombre | Descripción |
|------|-------------|
| **Vela primaria** | Plazo de negociación utilizado para las entradas (predeterminado 4 horas). |
| **Vela diaria** | Velas de marco temporal más alto que proporcionan niveles máximos/mínimos anteriores (predeterminado 1 día). |
| **AO Rápido / AO Lento** | Períodos cortos y largos del Awesome Oscillator. |
| **Búfer de ruptura** | Amortiguador en las subidas de precios añadido a máximos/mínimos anteriores. |
| **Etapas máximas** | Número máximo de entradas piramidales (1-5). |
| **Requiere beneficio** | Solo permita etapas adicionales cuando el capital exceda el saldo. |
| **Escala de volumen** | Multiplicador global aplicado al vector de lote seleccionado. |
| **Saldo base** | Saldo asignado al vector de lote más pequeño. |
| **Paso de equilibrio** | Incremento de equilibrio que se mueve al siguiente vector. |

## Diferencias con el experto MQL4

* La versión StockSharp utiliza suscripciones de velas integradas en lugar de llamadas directas `iClose`/`iHigh` y almacena los niveles de precios requeridos internamente.
* El indicador personalizado original `zzf` se emula a través de un detector fractal liviano que confirma oscilaciones de cinco velas.
* No se incluye la gestión de stop-loss y take-profit; El experto original modificó las paradas dinámicamente, pero el algoritmo dependía en gran medida de funciones específicas del corredor. Los comerciantes pueden agregar su propio módulo de riesgo si es necesario.
* Las notificaciones sonoras y las variables globales del terminal se omiten intencionalmente.

## Consejos de uso

1. Adjunte la estrategia a una cartera que informe tanto el saldo como el capital para que la matriz de lotes se comporte exactamente como en MetaTrader.
2. Utilice datos históricos realistas de 4 horas y diarios al realizar pruebas retrospectivas. Las resoluciones mixtas degradarán la lógica piramidal.
3. Experimente con el parámetro `BreakoutBuffer` cuando cambie a mercados que utilicen diferentes tamaños de ticks o diferenciales.
4. Habilite el gráfico durante la depuración: la estrategia traza automáticamente las velas, el histograma Awesome Oscillator y las operaciones ejecutadas.
