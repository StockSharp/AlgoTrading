# Estratégia de triângulo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia porta o expert advisor do MetaTrader **Triangle v1** para a API de alto nível do StockSharp. O EA original combinava filtros de média móvel ponderada em um período mais alto, uma verificação de divergência de momentum e uma confirmação MACD de prazo muito longo antes de colocar ordens no estilo rompimento. A versão StockSharp mantém a lógica multitemporal, substituindo a gestão de dinheiro tick a tick por ordens de proteção baseadas em candles.

## Como funciona

1. **Filtros multitemporais.** O período de trabalho (`CandleType`, padrão 15 minutos) é usado para executar operações. Filtros de tendência e momentum são calculados em um período mais alto (`TrendCandleType`, padrão 1 hora) para espelhar as chamadas MQL que referenciavam `T`.
2. **Portão de tendência LWMA.** Médias móveis ponderadas rápida e lenta (equivalente LWMA) devem estar alinhadas. Configurações compradas exigem que a LWMA rápida permaneça acima da LWMA lenta; vendidas exigem a relação oposta.
3. **Desvio de momentum.** Uma série de momentum de 14 períodos no período mais alto deve se desviar do nível neutro (100) em pelo menos `MomentumThreshold` em qualquer um dos três últimos candles concluídos, reproduzindo as verificações `MomLevelB/MomLevelS`.
4. **Confirmação MACD.** Um período muito alto (`MacdCandleType`, padrão candles de 30 dias ≈ mensal) deve mostrar a linha principal MACD no lado correto da linha de sinal antes que operações sejam permitidas, copiando a condição `MacdMAIN0` versus `MacdSIGNAL0`.
5. **Saídas de proteção.** Distâncias de stop loss e take profit são configuradas em passos de preço. Quando qualquer nível é atingido em uma barra concluída, a estratégia fecha a posição com uma ordem a mercado.

## Parâmetros

| Parâmetro | Descrição |
| --- | --- |
| `FastMaPeriod`, `SlowMaPeriod` | Comprimentos das médias móveis ponderadas do período mais alto. |
| `MomentumPeriod` | Período do filtro de momentum no período mais alto. |
| `MomentumThreshold` | Desvio absoluto mínimo a partir de 100 exigido em qualquer uma das três últimas leituras de momentum. Defina como 0 para desabilitar o filtro. |
| `MacdFastLength`, `MacdSlowLength`, `MacdSignalLength` | Parâmetros MACD aplicados a `MacdCandleType`. |
| `StopLossSteps`, `TakeProfitSteps` | Stop de proteção e distâncias de alvo medidos em passos de preço do instrumento (ticks). Use 0 para desabilitar. |
| `CandleType` | Período de negociação usado para execução de ordens. |
| `TrendCandleType` | Período mais alto que alimenta LWMAs e momentum. |
| `MacdCandleType` | Período usado para o filtro de confirmação MACD. |

## Uso

1. Selecione um ativo e configure `CandleType`, `TrendCandleType` e `MacdCandleType` para corresponder aos períodos que deseja analisar.
2. Ajuste comprimentos de MA, momentum e MACD se quiser adaptar o sistema a outro mercado ou regime de volatilidade.
3. Defina `StopLossSteps` e `TakeProfitSteps` de acordo com o tamanho do tick do instrumento. A estratégia converte automaticamente as contagens de passos em distâncias reais de preço.
4. Inicie a estratégia. Ela assina todos os fluxos de candles necessários, atualiza indicadores com a API de alto nível `Bind` e gerencia a posição quando stops ou alvos são atingidos.

## Diferenças em relação ao EA original

- Saídas baseadas em dinheiro (`Use_TP_In_Money`, `Use_TP_In_percent`) e o bloco de proteção de saldo não são recriados porque o StockSharp trabalha em unidades do instrumento. Comportamento equivalente pode ser obtido ajustando `StopLossSteps`/`TakeProfitSteps`.
- Lógicas de trailing-stop, break-even e equity-stop do EA dependiam de processamento de ticks e chamadas de modificação de ordens específicas do MetaTrader. A versão mantém a abordagem mais simples de stop fixo por clareza; usuários podem estender `UpdatePositionState` com regras de trailing se desejarem.
- Linhas de tendência manuais (`TREND`/`TRENDLOW`) e arrays de fractais eram usados como filtros discricionários no EA. Eles são omitidos intencionalmente para que a estratégia StockSharp permaneça totalmente sistemática.
- A estratégia sempre mantém no máximo uma posição líquida, o que corresponde ao uso típico, embora o EA expusesse um parâmetro `Max_Trades`.

Ajuste os limiares e parâmetros de período ao instrumento negociado. Valores mais amplos geralmente são necessários para mercados voláteis, a fim de evitar filtragem por pequenas flutuações de momentum.
