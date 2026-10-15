# Estratégia de Sobreposição de Código de Cores
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Opera com mudanças de cor de velas usando um cálculo de código de cores personalizado com stops fixos em pips.

## Lógica
- Constrói velas de código de cores personalizadas a partir de valores OHLC.
- Detecta mudanças de cor quando o corpo ultrapassa 1% do intervalo da vela.
- Vai comprado na transição de vermelho para verde, vendido na transição de verde para vermelho conforme o tipo de operação.
- Opera apenas entre `StartTime` e `EndTime`.
- Aplica proteções `StopLossPips` e `TakeProfitPips`.
