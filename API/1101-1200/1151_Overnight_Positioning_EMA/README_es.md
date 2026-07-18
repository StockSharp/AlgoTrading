# Estrategia de Posicionamiento Nocturno con EMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Abre una posición larga poco antes del cierre del mercado seleccionado y sale después de la apertura del mercado. Un filtro EMA opcional confirma las entradas. La estrategia soporta sesiones de EE. UU., Asia y Europa, y cierra cualquier posición abierta antes del fin de semana.

## Detalles

- **Entrada**: Minutos antes del cierre del mercado cuando el precio está por encima de la EMA (si está habilitado).
- **Salida**: Después de la apertura del mercado durante los minutos especificados o cinco minutos antes del cierre del viernes.
- **Mercado**: EE. UU., Asia o Europa.
- **Indicador**: EMA.
- **Dirección**: Solo largos.
- **Stops**: Ninguno.
