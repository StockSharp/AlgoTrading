# MAMACD Estratégia Sem Volatilidade
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
MAMACD No Volatility é uma porta direta do MetaTrader 4 consultor especialista `MAMACD_novlt.mq4`. A estratégia combina três médias móveis calculadas nos mínimos das velas e fecha com um filtro de impulso MACD. Ele espera até que o EMA rápida caia abaixo (para posições longas) ou suba acima (para posições curtas) de dois filtros LWMA de base baixa, arma uma configuração pendente e aciona uma entrada somente após a linha principal MACD confirmar a mudança de impulso.

## Indicadores
- **EMA rápida** (`FastEmaPeriod`) calculado em preços de fechamento.
- **Primeiro LWMA** (`FirstLowWmaPeriod`) calculado com base em preços baixos.
- **Segundo LWMA** (`SecondLowWmaPeriod`) calculado com base em preços baixos.
- **MACD linha principal** com período rápido `FastSignalEmaPeriod` e período lento `SlowEmaPeriod`.

Todos os indicadores operam no período definido por `CandleType` (padrão: velas de 5 minutos).

## Parâmetros
| Parâmetro | Descrição | Padrão |
| --- | --- | --- |
| `FirstLowWmaPeriod` | Período do primeiro LWMA construído a partir dos mínimos das velas. | 85 |
| `SecondLowWmaPeriod` | Período do segundo LWMA construído a partir dos mínimos das velas. | 75 |
| `FastEmaPeriod` | Período do EMA rápida construído a partir do fechamento da vela. | 5 |
| `SlowEmaPeriod` | Período EMA lento para o cálculo MACD. | 26 |
| `FastSignalEmaPeriod` | Período EMA rápido para o cálculo de MACD. | 15 |
| `StopLossPoints` | Distância de stop-loss em etapas de preço (0 desativa o stop-loss). | 15 |
| `TakeProfitPoints` | Distância de take-profit em etapas de preço (0 desativa o take-profit). | 15 |
| `TradeVolume` | Volume de pedidos usado para entradas no mercado. | 0,1 |
| `CandleType` | Série de velas usada para todos os indicadores. | Período de 5 minutos |

## Regras de negociação
1. **Configuração de braço longo**: EMA rápida está abaixo de ambos os filtros LWMA.
2. **Configuração de braço curto**: EMA rápida está acima de ambos os filtros LWMA.
3. **Insira longo**:
   - O EMA rápida cruza acima de ambos os LWMAs,
   - Uma configuração longa foi armada anteriormente,
   - A linha principal MACD é positiva ou aumentou em comparação com o valor anterior,
   - A posição líquida atual não é longa.
4. **Insira curto**:
   - O EMA rápida cruza abaixo de ambos os LWMAs,
   - Uma configuração curta foi armada anteriormente,
   - A linha principal MACD é negativa ou diminuiu em comparação com o valor anterior,
   - A posição líquida atual não é curta.
5. **Gerenciamento de riscos**: Take-profit e stop-loss opcionais são aplicados automaticamente por meio do serviço de proteção integrado.

A estratégia não implementa um sinal de saída dedicado; as posições são gerenciadas pelos níveis de stop-loss/take-profit configurados ou intervenção manual.

## Notas
- A confirmação MACD replica a lógica MQL: a linha principal deve estar acima de zero ou subindo (para posições compradas) ou abaixo de zero ou caindo (para posições vendidas).
- Os cálculos do LWMA usam preços baixos de velas para refletir a configuração original do indicador.
- O dimensionamento de volume espelha o EA original usando o parâmetro `TradeVolume` para cada pedido.
