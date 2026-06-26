# Estratégia Exp BrainTrend2 AbsolutelyNoLagLwma X2MACandle MMRec
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia recria o Consultor Especialista multi-módulo do MetaTrader combinando três filtros na API de alto nível do StockSharp:

1. **Inspiração BrainTrend2** – um canal Average True Range (ATR) detecta fases de contração e expansão de volatilidade.
2. **Aproximação AbsolutelyNoLagLwma** – uma média móvil ponderada linearmente (LWMA) rastreia a direção dominante com mínimo lag.
3. **Réplica X2MACandle** – um par de média móvil exponencial (EMA) rápida e lenta avalia a cor das velas para validar o momentum.

Uma posição é aberta apenas quando todos os filtros apontam na mesma direção. Os alvos de stop-loss e take-profit impulsionados por ATR gerenciam o processo de saída e emulam o conceito original de gestão monetária MMRec.

## Lógica de trading
- **Configuração altista**: a vela fecha acima da LWMA enquanto a EMA rápida está mais alta que a EMA lenta. Uma nova entrada comprada só é permitida após o viés altista anterior desaparecer, evitando ordens múltiplas em sinais idênticos.
- **Configuração baixista**: a vela fecha abaixo da LWMA enquanto a EMA rápida está mais baixa que a EMA lenta. Posições vendidas obedecem as mesmas regras de confirmação e resfriamento que o lado comprado.
- **Gestão de risco**: o ATR define níveis de saída dinâmicos. Tanto o stop-loss quanto o take-profit escalam com a volatilidade atual e são reavaliados em cada vela. Se o mercado tocar qualquer um dos níveis, a estratégia fecha toda a posição com uma ordem de mercado.

## Parâmetros
| Nome | Descrição |
| --- | --- |
| `CandleType` | Período da série de velas de trabalho. Padrão são velas de 6 horas para refletir os padrões originais do EA. |
| `AtrPeriod` | Período de lookback usado pelo filtro de volatilidade ATR. |
| `LwmaLength` | Período da média móvil ponderada linearmente para o filtro de tendência. |
| `FastMaLength` | Período da EMA rápida usada para coloração de velas. |
| `SlowMaLength` | Período da EMA lenta usada para coloração de velas. |
| `StopLossAtrMultiplier` | Multiplicador aplicado ao ATR para calcular a distância do stop de proteção. |
| `TakeProfitAtrMultiplier` | Multiplicador aplicado ao ATR para determinar a distância do take-profit. |

Todos os parâmetros são expostos através de `StrategyParam<T>` para que possam ser otimizados dentro do StockSharp.

## Notas
- O Consultor Especialista original depende de buffers de indicadores proprietários. Este port usa indicadores padrão do StockSharp que reproduzem as mesmas dicas direcionais sem depender de scripts externos.
- O gerenciamento monetário é simplificado para saídas de posição completa porque as estratégias do StockSharp tipicamente operam com ordens do tamanho do portfólio. As distâncias impulsionadas por ATR fornecem o comportamento adaptativo esperado do módulo MMRec.
- Os comentários no código estão em inglês conforme exigido pelas diretrizes de conversão.
