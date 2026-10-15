# Indicador de Divergencia (Cualquier Oscilador)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Detecta divergencias regulares y ocultas entre el precio y el RSI. La estrategia compra en divergencias alcistas y vende en divergencias bajistas.

## Parámetros
- **Pivot Left** – barras a la izquierda del pivote
- **Pivot Right** – barras a la derecha del pivote
- **Min Range** – barras mínimas entre pivotes
- **Max Range** – barras máximas entre pivotes
- **RSI Length** – período del RSI
- **Candle Type** – tipo de serie de velas

## Indicador
- RSI

## Reglas
- **Entrada**:
  - Comprar cuando el precio hace un mínimo más bajo y el RSI hace un mínimo más alto (divergencia alcista)
  - Vender cuando el precio hace un máximo más alto y el RSI hace un máximo más bajo (divergencia bajista)
  - Las divergencias ocultas operan en dirección opuesta
