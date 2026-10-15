# Estratégia Simples de Retração de Fibonacci
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Utiliza níveis de retração de Fibonacci derivados da máxima mais alta e da mínima mais baixa durante uma janela de retrospectiva. Quando o preço cruza um nível de Fibonacci selecionado, a estratégia entra em uma posição e coloca ordens fixas de take profit e stop loss baseadas em pips.

## Detalhes

- **Entrada**: Cruzamento acima ou abaixo do nível de Fibonacci escolhido.
- **Saída**: Take profit ou stop loss fixo.
- **Indicadores**: Highest, Lowest.
- **Stops**: Sim.
- **Valores padrão**:
  - `LookbackPeriod` = 100
  - `TakeProfitPips` = 50
  - `StopLossPips` = 20
- **Direção**: Ambos.
