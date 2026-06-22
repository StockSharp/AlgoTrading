# Estratégia Scalpel EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma conversão simplificada do *Scalpel EA* original escrito para MetaTrader.
Combina um filtro de Índice de Canal de Commodities (CCI) com análise de rompimento em múltiplos períodos. O objetivo é negociar na direção do momentum de curto prazo quando vários períodos superiores confirmam o movimento.

## Lógica

1. **Indicador** – CCI calculado no período principal. As negociações são permitidas apenas quando o valor CCI permanece dentro de uma faixa configurável em torno de zero.
2. **Confirmação de tendência** – Para velas de 30 minutos, 1 hora e 4 horas, os máximos e mínimos mais recentes são comparados com os anteriores.
   - Negociações compradas requerem mínimos ascendentes em todos os três períodos.
   - Negociações vendidas requerem máximos descendentes em todos os três períodos.
3. **Rompimento** – A entrada é acionada quando o preço de fechamento da vela principal rompe a máxima (para comprados) ou a mínima (para vendidos) da vela anterior.
4. **Controle de risco** – `StartProtection` coloca um take-profit e stop-loss fixos medidos em unidades de preço.

## Parâmetros

| Nome | Descrição |
| ---- | ----------- |
| `CciPeriod` | Período do Índice de Canal de Commodities. |
| `CciLimit` | Limite absoluto do CCI. Entradas são permitidas apenas dentro de ±limite. |
| `TakeProfit` | Valor de take profit em unidades de preço. |
| `StopLoss` | Valor de stop loss em unidades de preço. |
| `CandleType` | Período principal para negociação (padrão 1 minuto). |

## Notas

- A estratégia assina velas adicionais de 30 minutos, 1 hora e 4 horas para avaliar tendências de períodos superiores.
- O volume é retirado da propriedade `Strategy.Volume` da classe base.
- Apenas uma posição está aberta de cada vez. Sinais opostos fecham a posição existente e abrem uma nova.
