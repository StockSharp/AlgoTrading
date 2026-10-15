# Estratégia de Operador Técnico
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Technical Trader reimplementa o expert advisor do MetaTrader de `MQL/22304/Technical_trader.mq5` combinando duas médias móveis simples com um detector adaptativo de clusters de liquidez. A estratégia busca níveis de preço negociados repetidamente próximos ao bid/ask atual e só abre operações quando esses clusters se alinham com a direção do cruzamento das SMAs rápida/lenta. O risco é controlado por offsets de stop-loss e take-profit baseados em passos de preço que espelham a configuração original de MQL.

## Visão geral
- **Plataforma:** API de estratégia de alto nível do StockSharp.
- **Dados de mercado:** Candles definidos por período mais instantâneos do livro de ordens para obter os preços bid/ask atuais.
- **Estilo:** Seguimento de rompimento direcional seguindo clusters de liquidez próximos.
- **Mapeamento da fonte:** O cruzamento de SMA, a amostragem histórica de fechamentos, a tolerância de clustering e o dimensionamento de ordens foram portados do expert MQL.

## Lógica de trading
1. Assinar candles do período configurado e calcular duas SMAs (`FastMaPeriod` e `SlowMaPeriod`).
2. Manter uma janela deslizante (`HistoryDepth`) dos preços de fechamento mais recentes e arredondá-los para três decimais, emulando o comportamento original do `NormalizeDouble`.
3. Construir um histograma de ocorrências de preços e classificar níveis cuja frequência supera `ResistanceThreshold`.
4. Rastrear o bid e ask mais recentes usando o livro de ordens; recorrer ao fechamento do candle se não houver cotações disponíveis.
5. Condições de entrada comprada:
   - A SMA rápida está acima da SMA lenta.
   - Um cluster de preços qualificado está logo abaixo do ask atual (`LevelTolerance` define a distância permitida).
   - Se a estratégia estiver plana ou vendida, compra volume suficiente para cobrir a posição vendida e estabelecer a posição comprada de volume base.
6. As condições de entrada vendida espelham a lógica comprada, mas usam clusters logo acima do bid e requerem que a SMA rápida esteja abaixo da SMA lenta.
7. Ao entrar em uma posição, calcula os níveis de stop-loss e take-profit usando o `PriceStep` do instrumento multiplicado por `StopLossPoints` e `TakeProfitPoints`, respectivamente. Esses offsets recriam os multiplicadores `_Point` na versão MQL.
8. Em cada candle terminado, sai das posições quando o bid/ask rastreado atinge o nível de stop-loss ou take-profit.

## Parâmetros
| Parâmetro | Descrição | Padrão |
|-----------|-----------|--------|
| `FastMaPeriod` | Comprimento da SMA rápida que impulsiona o sinal de cruzamento. | 25 |
| `SlowMaPeriod` | Comprimento da SMA lenta que atua como filtro de tendência. | 30 |
| `StopLossPoints` | Distância do stop expressa em passos de preço (`PriceStep * StopLossPoints`). | 30 |
| `TakeProfitPoints` | Alvo de lucro expresso em passos de preço (`PriceStep * TakeProfitPoints`). | 100 |
| `ResistanceThreshold` | Número mínimo de ocorrências necessárias para que um nível de preço seja tratado como um cluster de liquidez. | 15 |
| `HistoryDepth` | Número de candles recentes armazenados para detecção de clusters (definir como 100 para pares de ouro como no EA original). | 500 |
| `LevelTolerance` | Distância máxima permitida entre o bid/ask atual e um nível de cluster. | 0.0005 |
| `CandleType` | Série de candles processada pela estratégia (período ou tipo personalizado). | Período de 1 minuto |

## Notas de implementação
- A assinatura do livro de ordens é usada para capturar os melhores preços bid/ask atualizados, correspondendo à execução baseada em ticks no expert MQL.
- O cálculo de clusters evita LINQ e armazena resultados em buffers reutilizáveis para respeitar as diretrizes de conversão do StockSharp.
- Os alvos de stop e take-profit são gerenciados internamente porque as estratégias StockSharp executam ordens sintéticas em vez de ordens pendentes do lado do broker.
- Os helpers de gráficos desenham candles, ambas as SMAs e operações executadas para verificação visual durante os testes.

## Dicas de uso
- Aumentar `HistoryDepth` quando se trabalha em períodos mais altos para manter um tamanho de amostra significativo para o clustering de níveis.
- Ajustar `LevelTolerance` em instrumentos com tamanhos de tick pequenos para evitar clusters não relacionados.
- Reduzir `ResistanceThreshold` em mercados ilíquidos onde menos repetições são esperadas.
- O parâmetro de volume padrão da classe base `Strategy` controla o tamanho da ordem; ajustá-lo no ambiente de hospedagem ou sobrescrever antes de iniciar a estratégia.
