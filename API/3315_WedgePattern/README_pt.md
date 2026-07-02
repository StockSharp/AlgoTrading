# Estratégia Wedge Pattern
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **Estratégia Wedge Pattern** é uma conversão do expert advisor *Wedge pattern.mq4* do MetaTrader para a API de alto nível do StockSharp. A estratégia procura consolidações de cunha simétricas derivadas dos fractais de Bill Williams e negocia os rompimentos quando filtros de tendência e momentum se alinham.

A implementação de alto nível substitui a gestão manual de ordens original por recursos StockSharp enquanto preserva a lógica de decisão:

- **Filtro de tendência:** compara uma média móvel linearmente ponderada (LWMA) rápida e uma lenta calculadas em preços típicos.
- **Filtro de momentum:** avalia a distância absoluta do indicador de momentum de 14 períodos em relação ao nível neutro (100). As três últimas leituras de momentum devem exceder um limiar configurável.
- **Confirmação MACD:** exige que a linha principal MACD esteja acima da linha de sinal para compras (ou abaixo para vendas).
- **Detecção de cunha fractal:** coleta pontos fractais superiores e inferiores para construir linhas de tendência convergentes. Sinais de negociação são produzidos quando o preço fecha além dessas linhas mais um buffer de confirmação configurável.
- **Gestão de risco:** imita a implementação MQL com distâncias fixas de stop-loss e take-profit, movimento automático para break-even e ajustes de trailing stop.

## Como funciona

1. Assinar um único timeframe definido pelo parâmetro `CandleType`.
2. Atualizar valores dos indicadores a cada candle concluído e manter buffers rolantes de máximas e mínimas para detectar novos fractais.
3. Construir linhas de tendência da cunha a partir dos dois fractais de máxima e mínima mais recentes. Apenas cunhas convergentes (máximas descendentes e mínimas ascendentes) são consideradas setups válidos.
4. Uma compra é aberta quando:
   - LWMA rápida > LWMA lenta.
   - Linha MACD > linha de sinal.
   - Qualquer uma das três últimas leituras de momentum excede o limiar configurado.
   - O candle atual fecha acima da linha de tendência superior projetada por pelo menos o buffer de rompimento.
5. Uma venda espelha as condições com linhas e limiares invertidos.
6. Após a entrada, a estratégia coloca imediatamente ordens de stop-loss e take-profit. Depois pode mover o stop para break-even e trailar conforme a posição se torna lucrativa.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| `CandleType` | Timeframe usado para análise e ordens. |
| `FastMaPeriod` | Comprimento do filtro LWMA rápido. |
| `SlowMaPeriod` | Comprimento do filtro LWMA lento. |
| `MomentumPeriod` | Período de retrospectiva do indicador de momentum (padrão 14). |
| `MomentumThreshold` | Distância mínima de 100 exigida do indicador de momentum para considerar o mercado impulsivo. |
| `MacdFastPeriod` / `MacdSlowPeriod` / `MacdSignalPeriod` | Configuração MACD padrão. |
| `FractalDepth` | Número de barras de cada lado necessário para confirmar uma máxima ou mínima fractal. |
| `StopLossPips` | Distância inicial do stop protetor em pips. |
| `TakeProfitPips` | Distância inicial da meta de lucro em pips. |
| `UseBreakeven`, `BreakevenTriggerPips`, `BreakevenOffsetPips` | Habilita e configura a automação de break-even. |
| `UseTrailing`, `TrailingActivationPips`, `TrailingDistancePips`, `TrailingStepPips` | Habilita e configura o comportamento do trailing-stop. |
| `BreakoutBufferPips` | Buffer extra aplicado à confirmação do rompimento da cunha. |

Todas as configurações baseadas em pips são convertidas em distâncias de preço usando o tamanho de tick do ativo. O cálculo padrão de pip considera precificação fracionária (3 ou 5 casas decimais) exatamente como no Expert Advisor original.

## Diretrizes de uso

1. Anexe a estratégia ao instrumento desejado e selecione o timeframe de candles correspondente ao setup original (por exemplo, candles de 15 minutos).
2. Configure o tamanho da posição pela propriedade base `Strategy.Volume`.
3. Opcionalmente ajuste filtros e parâmetros de risco para corresponder à volatilidade do mercado-alvo.
4. Inicie a estratégia; ela assinará candles, desenhará dados no gráfico e negociará automaticamente quando rompimentos de cunha ocorrerem.

## Diferenças em relação à versão MQL

- A versão StockSharp usa APIs de alto nível `SubscribeCandles` e binding de indicadores, evitando processamento manual de ticks.
- Gestão de trailing stop e break-even depende de `SetStopLoss`/`SetTakeProfit`, integrando-se ao comportamento protetor incorporado.
- Apenas uma posição é mantida por vez; o script MetaTrader suportava piramidação até um número máximo de operações.
- Funções de alerta, e-mail e notificação são omitidas; o tratamento de eventos deve ser implementado externamente se necessário.

Apesar dessas adaptações, a lógica central de entrada e as regras protetoras seguem de perto o expert MetaTrader original usando padrões idiomáticos do StockSharp.
