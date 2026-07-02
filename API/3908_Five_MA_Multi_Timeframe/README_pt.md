# Estratégia Multi-Prazoal Cinco MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia Multi-Timeframe de Cinco MAs** replica o consultor especialista MT4 "5matf" original usando o API de alto nível de StockSharp. A estratégia analisa cinco médias móveis simples em três períodos de tempo (primário, mais alto e mais lento) e combina a inclinação de cada média com o oscilador do acelerador para produzir sinais de entrada graduados. Quando há evidências suficientes de alta ou baixa em todos os prazos, a estratégia abre ou fecha posições de acordo.

## Indicadores e Dados
- **Médias Móveis Simples (SMA)**: Períodos 5, 8, 13, 21 e 34 em todos os três períodos de tempo.
- **Oscilador Acelerador (AC)**: Aplicado nos prazos primários e terciários para avaliar a aceleração do momento.
- **Prazos**: Padrão definido como 15 minutos (sinal), 60 minutos (confirmação) e 240 minutos (filtro de tendência). Todos os prazos podem ser ajustados através de parâmetros.

## Lógica de Sinais
1. Cada SMA compara seu valor atual com a vela anterior para determinar uma inclinação ascendente ou descendente.
2. O Accelerator Oscillator verifica sequências de alta ou baixa usando os últimos quatro valores.
3. As contagens de inclinação e as contribuições do oscilador são agregadas em pontuações percentuais para cada período de tempo.
4. Quando todos os três períodos de tempo apresentam pontuações de alta acima de 50%, um sinal de **COMPRA** é gerado. Pontuações acima de 75% fortalecem o sinal.
5. Os mesmos limites aplicados na direção oposta geram sinais de **VENDA**.
6. As posições são fechadas quando um sinal oposto excede o nível de fechamento configurado. Novas negociações só são abertas quando nenhuma posição está ativa, refletindo o comportamento original do consultor especialista.

## Parâmetros
| Nome | Padrão | Descrição |
| --- | --- | --- |
| `CandleType` | Velas de 15 minutos | Período primário usado para sinais de negociação. |
| `HigherTimeframe1` | Velas de 60 minutos | Primeiro prazo maior para confirmação. |
| `HigherTimeframe2` | Velas de 240 minutos | Segundo período de tempo mais alto para filtro de tendência lenta. |
| `FirstPeriod` – `FifthPeriod` | 5, 8, 13, 21, 34 | SMA comprimentos aplicados a cada período de tempo. |
| `OpenLevel` | 0 | Grau mínimo de sinal necessário para abrir uma nova posição. |
| `CloseLevel` | 1 | Grau de sinal oposto necessário para fechar uma posição existente. |

Todos os parâmetros podem ser otimizados ou ajustados na interface de estratégia do StockSharp.

## Notas de uso
- A estratégia utiliza ordens de mercado e não emite reversões simultâneas; sempre espera por uma posição plana antes de abrir na direção oposta.
- Ative feeds de dados históricos para todos os períodos selecionados para garantir cálculos sincronizados.
- Considere ajustar os comprimentos de SMA ou o uso do oscilador ao aplicar a estratégia a diferentes mercados ou regimes de volatilidade.

## Notas de conversão
Esta implementação mantém o comportamento central do consultor especialista MT4 "5matf" enquanto aproveita o sistema de assinatura e vinculação de indicadores de StockSharp. A lógica do acelerador requer quatro velas completadas antes que os sinais se tornem ativos, assim como o script original.
