# Estratégia de Harami
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
HaramiStrategy converte o consultor especialista "Harami" do MetaTrader na API de alto nível do StockSharp. A estratégia combina um padrão Harami altista/baixista detectado em um período de tempo superior com expansão de momentum e um filtro MACD de longo prazo. Apenas velas concluídas são processadas e toda a gestão de operações é realizada através do motor de proteção integrado do StockSharp.

## Dados e indicadores
- **Período base:** configurável (velas de 15 minutos por padrão) para detecção de tendência por médias móveis.
- **Período superior:** configurável (padrão de uma hora) para reconhecimento de padrões e confirmação de momentum.
- **Período MACD:** configurável (padrão de velas de 30 dias) para emular o filtro MACD mensal original.
- **Indicadores:**
  - Média Móvel Ponderada Linear (`FastMaLength`) no período base.
  - Média Móvel Exponencial (`SlowMaLength`) no período base.
  - Momentum (`MomentumPeriod`) no período superior. A estratégia usa a distância absoluta do valor neutro (100) para as últimas três barras do período superior.
  - Convergência/Divergência de Médias Móveis (12/26/9) no período MACD.

## Configuração comprada
1. A EMA lenta está acima da LWMA rápida no período base, sinalizando uma tendência de alta.
2. O período superior forma uma sequência Harami altista: duas velas atrás foi baixista, a vela anterior foi altista e seu corpo é menor que o corpo baixista anterior.
3. Qualquer um dos três últimos desvios de momentum do período superior excede `MomentumBuyThreshold`.
4. A linha principal MACD está acima da linha de sinal no período MACD.
5. Nenhuma posição comprada está aberta (`Position <= 0`).
6. A estratégia envia uma ordem de compra a mercado dimensionada para reverter qualquer exposição vendida e adicionar `Volume` lotes.

## Configuração vendida
1. A EMA lenta está abaixo da LWMA rápida no período base.
2. O período superior forma um Harami baixista: duas velas atrás foi altista, a vela anterior foi baixista e o último corpo é menor.
3. Qualquer um dos três últimos desvios de momentum do período superior excede `MomentumSellThreshold`.
4. A linha principal MACD está abaixo da linha de sinal.
5. Nenhuma exposição vendida está aberta (`Position >= 0`).
6. A estratégia envia uma ordem de venda a mercado suficientemente grande para fechar posições compradas e abrir uma nova posição vendida de tamanho `Volume`.

## Gestão de risco
`StartProtection` instala níveis de stop-loss e take-profit (expressos em pontos). Recursos adicionais de trailing, break-even e gestão monetária do EA original são intencionalmente omitidos para manter a versão StockSharp concisa. As mudanças de direção de operação achatam automaticamente a exposição oposta.

## Parâmetros
| Nome | Descrição | Padrão |
| ---- | ----------- | ------- |
| `CandleType` | Período primário para médias móveis e execução de sinais. | Velas de 15 minutos |
| `HigherCandleType` | Período usado para confirmação de Harami e momentum. | Velas de 1 hora |
| `MacdCandleType` | Período para o filtro de tendência MACD. | Velas de 30 dias |
| `FastMaLength` | Comprimento da MA ponderada linear rápida. | 6 |
| `SlowMaLength` | Comprimento da MA exponencial lenta. | 85 |
| `MomentumPeriod` | Período de retrocesso do Momentum no período superior. | 14 |
| `MomentumBuyThreshold` | Desvio mínimo de momentum para confirmação comprada. | 0.3 |
| `MomentumSellThreshold` | Desvio mínimo de momentum para confirmação vendida. | 0.3 |
| `StopLossPoints` | Distância de stop-loss em pontos. | 40 |
| `TakeProfitPoints` | Distância de take-profit em pontos. | 100 |

## Dicas de uso
- Alinhar `CandleType`, `HigherCandleType` e `MacdCandleType` com dados históricos disponíveis; garantir que o período superior seja mais longo que o período base.
- Ajustar os limiares de momentum para corresponder à volatilidade do instrumento negociado.
- Usar o otimizador do StockSharp através dos intervalos de parâmetros fornecidos para ajustar comprimentos de MA e limiares de momentum.
- Sempre realizar backtesting com configurações realistas de comissão/latência antes de implantar ao vivo.
