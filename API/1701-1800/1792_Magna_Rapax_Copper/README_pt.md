# Estratégia Magna Rapax Copper
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia replica o sistema de médias móveis "arco-íris" do especialista MQL original.
Utiliza onze médias móveis exponenciais juntamente com filtros MACD e ADX.

## Como funciona

- Calcular EMA(2), EMA(3), EMA(5), EMA(8), EMA(13), EMA(21), EMA(34), EMA(55), EMA(89), EMA(144) e EMA(233) sobre preços de fechamento.
- Calcular MACD (Rápido, Lento, Sinal) e usar a linha de sinal.
- Calcular ADX para medir a força da tendência.
- **Comprar** quando:
  - A linha de sinal MACD está acima de zero.
  - Todas as EMAs estão estritamente ascendentes (cada EMA mais rápida acima da mais lenta).
  - O valor ADX está acima do limiar.
- **Vender** quando:
  - A linha de sinal MACD está abaixo de zero.
  - Todas as EMAs estão estritamente descendentes.
  - O valor ADX está acima do limiar.

As posições são invertidas quando um sinal oposto aparece.

## Parâmetros

| Nome | Descrição |
| --- | --- |
| `FastMacd` | Período de EMA rápida para MACD. |
| `SlowMacd` | Período de EMA lenta para MACD. |
| `SignalPeriod` | Período da linha de sinal para MACD. |
| `AdxPeriod` | Período para o indicador ADX. |
| `AdxThreshold` | Valor mínimo de ADX necessário para operar. |
| `CandleType` | Período de velas utilizado nos cálculos. |

## Observações

- A estratégia usa ordens de mercado via `BuyMarket` e `SellMarket`.
- Apenas uma posição é mantida por vez; um sinal oposto inverte a posição.
- Esta é uma conversão direta da estratégia MQL original sem a lógica opcional de martingale.
