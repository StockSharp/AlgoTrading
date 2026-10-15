# Estratégia Ichimoku Momentum MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Resumo
- **Tipo**: Seguidor de tendência com confirmação de Momentum.
- **Período**: Configurável (padrão de velas de 15 minutos).
- **Indicadores**: Ichimoku (Tenkan/Kijun), Médias Móveis Ponderadas Lineares, Momentum, MACD.
- **Stops**: Take-profit e stop-loss fixos opcionais em pontos de preço via `StartProtection`.

## Descrição da estratégia
Esta estratégia recria o fluxo de decisão do expert do MetaTrader "Ichimoku" (pasta `MQL/23469`). Ela avalia a vela fechada anterior e abre novos trades no início da próxima barra quando todas as quatro confirmações concordam:

1. **Alinhamento Ichimoku** – Tenkan (linha de conversão) deve estar acima de Kijun (linha base) para trades longos e abaixo para curtos.
2. **Filtro de tendência LWMA** – Uma média móvel ponderada linear rápida deve permanecer acima da LWMA lenta para longos e abaixo para curtos. Ambas as médias são calculadas no mesmo período que as velas assinadas.
3. **Força do Momentum** – A distância absoluta do oscilador de momentum do nível neutro 100 deve ser maior que um limite configurável em pelo menos uma das últimas três velas fechadas.
4. **Confirmação MACD** – O histograma MACD deve concordar com a direção (linha MACD posicionada além da linha de sinal com o mesmo sinal).

Quando todas as quatro condições se alinham de forma altista e a estratégia não está atualmente longa, ela compra o volume configurado mais as unidades necessárias para zerar uma posição curta existente. Quando as condições mudam para baixistas, o processo é espelhado no lado da venda. Sinais opostos sempre fecham posições abertas, fornecendo uma saída determinística mesmo sem ordens protetoras.

O gerenciamento de risco é tratado através do `StartProtection` do StockSharp, permitindo distâncias fixas de take-profit e stop-loss expressas em pontos do instrumento. Definir qualquer parâmetro como zero desabilita a perna de proteção correspondente.

## Visão geral dos parâmetros
| Parâmetro | Descrição |
|-----------|-------------|
| `FastMaPeriod` | Comprimento da média móvel ponderada linear rápida usada para o filtro de tendência. |
| `SlowMaPeriod` | Comprimento da média móvel ponderada linear lenta. |
| `MomentumPeriod` | Período de lookback do oscilador de momentum. |
| `MomentumThreshold` | Distância mínima de 100 que o momentum deve atingir em pelo menos uma das últimas três velas. |
| `MacdFastPeriod` | Comprimento EMA rápido do filtro MACD. |
| `MacdSlowPeriod` | Comprimento EMA lento do filtro MACD. |
| `MacdSignalPeriod` | Comprimento EMA de sinal do filtro MACD. |
| `TenkanPeriod` | Comprimento do Ichimoku Tenkan-sen. |
| `KijunPeriod` | Comprimento do Ichimoku Kijun-sen. |
| `SenkouSpanBPeriod` | Comprimento do Ichimoku Senkou Span B. |
| `TakeProfitPoints` | Distância de take-profit opcional em pontos de preço (0 desabilita). |
| `StopLossPoints` | Distância de stop-loss opcional em pontos de preço (0 desabilita). |
| `CandleType` | Período usado para todos os cálculos de indicadores. |

## Notas de uso
- A estratégia lê apenas velas concluídas e armazena os valores de indicadores da barra anterior, correspondendo à lógica `shift=1` do EA do MetaTrader.
- Ajustar `MomentumThreshold` ao mudar para mercados com diferente escalonamento de momentum (por exemplo, cripto vs. pares forex).
- As ordens protetoras são gerenciadas internamente; ordens de bracket no nível da bolsa não são enviadas.
- Os gráficos, se disponíveis, exibirão velas de preço, ambas as LWMAs, a nuvem Ichimoku e trades executados.
