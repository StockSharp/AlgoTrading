# Esquema de Estrategia Basada en Tiempo de Trabajo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este archivo de esquema demuestra la aplicación del bloque "Tiempo de Trabajo" junto con otros bloques relevantes en la plataforma Designer para implementar estrategias de trading basadas en tiempo.

## Descripción General

El esquema explora diversas configuraciones utilizando el bloque "Tiempo de Trabajo", que permite a los traders ejecutar estrategias basadas en condiciones temporales específicas.

## Componentes Clave

- **Bloque Tiempo de Trabajo**: Utilizado para definir las horas de trading activo o momentos específicos para ejecutar operaciones.
- **Bloque Variable**: Denominado "Estrategia", este bloque se usa para almacenar y manipular variables específicas de la estrategia.
- **Bloque Convertidor**: Utilizado para convertir y recuperar datos relacionados con el tiempo que respaldan las decisiones temporales.

## Detalles de la Estrategia

### Estrategia con Condición de Tiempo de Trabajo
- **Compra Pre-Cierre**: La estrategia inicia una orden de compra un minuto antes del cierre del horario de trabajo definido, con el objetivo de aprovechar posibles movimientos de precio al final de la sesión de trading.

### Disparador de Tiempo Específico
- **Compra a Hora Fija**: Implementa una compra exactamente a las 18:00, alineando la ejecución del trade con eventos de mercado significativos o momentos típicos de cierre.

### Cierre Avanzado Basado en Tiempo de la Lección 7
- **Cierre de Posiciones**: Cierra cualquier posición abierta cinco minutos antes del fin del horario de trabajo, una estrategia diseñada para evitar mantener posiciones nocturnas o reaccionar ante fluctuaciones de precio de fin de día.

## Nota sobre los Cambios en la Versión 5

En la quinta versión del software Designer, se han mejorado los cálculos de tiempo y el funcionamiento conjunto del bloque "Tiempo de Trabajo". Después de importar estrategias que utilizan estas funciones, se recomienda recrearlas dentro de la plataforma para garantizar la funcionalidad correcta y aprovechar las fórmulas de cálculo de tiempo actualizadas.

Este esquema proporciona un marco integral para desarrollar y probar estrategias que dependen en gran medida de la precisión temporal para la ejecución de operaciones, convirtiéndolo en una herramienta esencial para traders enfocados en estrategias intradía o que necesitan adherirse a horarios de mercado específicos.
