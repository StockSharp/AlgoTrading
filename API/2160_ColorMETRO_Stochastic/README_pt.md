# Estratégia ColorMETRO Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma adaptação em C# do especialista MQL5 **exp_colormetro_stochastic.mq5**. Substitui o indicador original ColorMETRO Stochastic pelo `StochasticOscillator` integrado do StockSharp e opera em eventos de cruzamento.

## Lógica
- Subscreve velas de 8 horas por padrão (configurável).
- Calcula o oscilador Stochastic com os parâmetros:
  - Período %K (`KPeriod`)
  - Período %D (`DPeriod`)
  - Suavização adicional (`Slowing`)
- Armazena os valores anteriores de %K e %D para detectar cruzamentos.
- **Compra** quando %K cruza acima de %D.
- **Venda** quando %K cruza abaixo de %D.
- Aplica um stop-loss e take-profit simples de 2% via `StartProtection`.

## Parâmetros
| Nome | Descrição |
|------|-----------|
| `KPeriod` | Período de retrocesso para a linha %K (padrão 5). |
| `DPeriod` | Período de suavização para a linha %D (padrão 3). |
| `Slowing` | Valor de suavização adicional (padrão 3). |
| `CandleType` | Período das velas, padrão 8 horas. |

## Notas
A versão MQL original usava um indicador ColorMETRO Stochastic personalizado com linhas de passo rápido e lento. Esta adaptação aproxima seus sinais usando o oscilador Stochastic padrão.
