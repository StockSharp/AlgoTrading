# Estratégia BullsBearsEyes EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral
Esta estratégia é um port StockSharp do **BullsBearsEyes EA** para MetaTrader 5. Ela reconstrói o indicador personalizado combinando os osciladores clássicos Bulls Power e Bears Power com o mesmo suavização IIR de quatro estágios usado no código original. O rácio resultante oscila entre 0 e 1 e reflete o domínio de vendedores ou compradores. Quando o rácio cai para **0**, o mercado é considerado esgotado pelos ursos e a estratégia prepara uma entrada longa. Quando o rácio sobe para **1**, a pressão altista é considerada esgotada e a estratégia procura uma entrada curta. Todos os cálculos são realizados apenas em velas totalmente fechadas, replicando a implementação MQL que avaliava `custom[1]` ao nascer de cada nova barra.

## Lógica de Negociação
1. Assinar a série de velas configurada e vincular os indicadores Bulls Power e Bears Power.
2. Em cada vela terminada, os valores do indicador são processados pela mesma cascata de suavização IIR (`L0` – `L3`) que o EA original.
3. O rácio `CU / (CU + CD)` é calculado. Uma sequência puramente baixista faz `CU` igual a zero, enquanto uma sequência puramente altista força `CD` a zero.
4. A estratégia armazena o rácio da vela anterior e o usa como sinal acionável:
   - Rácio anterior igual a **0** ⇒ fechar posições curtas e abrir uma posição longa.
   - Rácio anterior igual a **1** ⇒ fechar posições longas e abrir uma posição curta.
   - Rácios intermediários são ignorados para manter fidelidade ao código-fonte.
5. As ordens são enviadas com o valor `Volume` atual e automaticamente netam a posição oposta antes de abrir uma nova.

## Gestão de Risco
- **Stop Loss / Take Profit** – expressos em pips, traduzidos para preços absolutos com detecção de tamanho de pip idêntica à implementação MT5 (instrumentos de 5 e 3 dígitos são tratados pelo multiplicador de passo).
- **Trailing Stop / Trailing Step** – lógica idêntica: uma vez que o preço avança `TrailingStop + TrailingStep`, o stop é movido para manter uma distância `TrailingStop` constante do fechamento atual. Posições longas e curtas são gerenciadas simetricamente.
- Os níveis protetores são recalculados sempre que a posição líquida muda, garantindo que o preço médio de posição seja usado para cálculos adicionais.
- A estratégia fecha a posição inteira quando um nível protetor é violado dentro do intervalo da vela atual.

## Filtro de Sessão
O filtro de tempo opcional corresponde às entradas do consultor especialista:
- `Use Time Control` – habilita/desabilita o filtro.
- `Start Hour` – hora de início inclusiva (0–23).
- `End Hour` – hora de término exclusiva (0–23). Se a hora de término for menor que a de início, a sessão se estende pela meia-noite.
Durante as horas restritas, a estratégia evita abrir novas posições, mas ainda gerencia stops e trailing para operações existentes.

## Parâmetros
| Parâmetro | Descrição | Padrão |
|-----------|-------------|---------|
| `Period` | Comprimento de médio para Bulls/Bears Power. | 13 |
| `Gamma` | Fator de suavização usado pelo filtro de quatro estágios (0–1). | 0.6 |
| `StopLossPips` | Distância de stop-loss medida em pips. | 150 |
| `TakeProfitPips` | Distância de take-profit medida em pips. | 150 |
| `TrailingStopPips` | Distância de trailing stop em pips (0 desativa o trailing). | 25 |
| `TrailingStepPips` | Avanço mínimo antes que o trailing stop possa se mover. | 5 |
| `UseTimeControl` | Habilita o filtro de sessão de negociação. | true |
| `StartHour` | Primeira hora de negociação (inclusiva). | 10 |
| `EndHour` | Última hora de negociação (exclusiva). | 16 |
| `CandleType` | Tipo de vela/período usado para análise. | Velas de 1 hora |

## Notas Adicionais
- A API de alto nível `SubscribeCandles().Bind(...)` é usada para replicar os cálculos originais sem coletar velas manualmente.
- Os valores do indicador são processados apenas após o fechamento da vela (`CandleStates.Finished`).
- A detecção do tamanho do pip recorre a `1` se o passo do instrumento não estiver disponível, permitindo que a estratégia execute em ambientes de teste sintéticos.
- Comentários inline no arquivo C# explicam cada seção lógica para manutenção mais fácil e comparação com a fonte MQL.
