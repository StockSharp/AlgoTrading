# Estratégia de Ciclo de Tendência Schaff com Cor
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera com base no indicador **Schaff Trend Cycle (STC)**. O STC aplica um duplo cálculo estocástico a uma série MACD e oscila entre -100 e 100. Valores acima do nível alto sugerem pressão de alta, enquanto valores abaixo do nível baixo sugerem pressão de baixa.

## Lógica de Negociação

- Assinar as velas do período selecionado.
- Calcular o MACD usando médias exponenciais rápida e lenta.
- Aplicar dois cálculos estocásticos consecutivos para derivar o STC.
- Quando o STC sobe acima do nível alto e continua subindo:
  - Fechar qualquer posição vendida.
  - Entrar em uma posição comprada.
- Quando o STC cai abaixo do nível baixo e continua caindo:
  - Fechar qualquer posição comprada.
  - Entrar em uma posição vendida.

A estratégia sempre age em velas completamente formadas.

## Parâmetros

| Nome | Descrição | Padrão |
|------|-----------|--------|
| `FastPeriod` | Período da EMA rápida usado no MACD | `23` |
| `SlowPeriod` | Período da EMA lenta usado no MACD | `50` |
| `Cycle` | Comprimento do ciclo estocástico | `10` |
| `HighLevel` | Limiar de sobrecompra para o STC | `60` |
| `LowLevel` | Limiar de sobrevenda para o STC | `-60` |
| `CandleType` | Período das velas processadas | `4h` |

## Observações

- Os valores do STC são redimensionados para um intervalo de -100…100 para facilitar a comparação com os níveis padrão.
- As ordens são enviadas com chamadas `BuyMarket()` e `SellMarket()`; as posições são revertidas automaticamente quando sinais opostos aparecem.
- Esta estratégia foca exclusivamente nos sinais do indicador e não utiliza ordens de stop-loss ou take-profit.
