# Estratégia de Canal de Cruzamento Percentual
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A Estratégia de Canal de Cruzamento Percentual origina-se do consultor especialista de MetaTrader 5 *Percentage_Crossover_Channel_EA*. Ela depende de um canal personalizado construído em torno de uma média móvel rápida e reage a toques de banda ou cruzamentos da linha média. Esta implementação do StockSharp segue a mesma lógica enquanto usa a API de alto nível para processar velas concluídas.

## Construção do canal
O indicador subjacente constrói um canal dinâmico em torno do preço selecionado (fechamento por padrão):

1. Calcular o preço base usando o modo **Applied Price** configurado.
2. Aplicar uma média móvel simples de 1 período para obter o preço de referência de curto prazo.
3. Calcular dois limites usando o parâmetro **Percent** (p. ex., 50 → ±0,5%).
4. Limitar a linha média anterior dentro dos novos limites para obter o valor médio atual.
5. As bandas superior e inferior são o valor médio limitado multiplicado pelos fatores ±percentagem.

Esta recursão permite que o canal atrase durante tendências fortes enquanto mantém um envelope apertado quando o preço consolida.

## Lógica de trading
Dois modos de sinal diferentes estão disponíveis:

- **Modo de toque de banda (padrão):**
  - Entrada comprada quando o mínimo da vela anterior estava acima da banda inferior e a última vela concluída a toca ou perfura.
  - Entrada vendida quando o máximo da vela anterior estava abaixo da banda superior e a última vela concluída a toca ou perfura.
- **Modo de cruzamento da linha média (TradeOnMiddleCross = true):**
  - Entrada comprada quando o preço cruza a linha média de cima para baixo.
  - Entrada vendida quando o preço cruza a linha média de baixo para cima.

O indicador **ReverseSignals** troca as regras compradas e vendidas. A estratégia sempre fecha e reverte posições existentes enviando uma única ordem a mercado cujo volume equivale ao **OrderVolume** configurado mais o valor absoluto da posição atual.

## Gestão de risco
Os níveis protetores opcionais emulam as configurações originais de stop-loss e take-profit do MT5:

- **StopLossPoints** – distância em passos de preço subtraída (comprado) ou adicionada (vendido) do preço de entrada estimado.
- **TakeProfitPoints** – distância em passos de preço adicionada (comprado) ou subtraída (vendido) do preço de entrada.

Se qualquer parâmetro for zero, a proteção correspondente é desativada. Os stops são avaliados em cada vela finalizada comparando máximos e mínimos da vela com os níveis armazenados. Nenhuma lógica de trailing é aplicada.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-------------|
| `CandleType` | Tipo de dados de vela para assinar (período de 15 minutos por padrão). |
| `Percent` | Largura do canal em porcentagem do preço (convertido para fatores ±porcentagem/100). |
| `PriceMode` | Preço aplicado para o canal. Opções: Close, Open, High, Low, Median (H+L)/2, Typical (H+L+C)/3, Weighted (H+L+2C)/4, Average (O+H+L+C)/4. |
| `TradeOnMiddleCross` | Alternar entre lógica de toque de banda e lógica de cruzamento de linha média. |
| `ReverseSignals` | Inverter as condições compradas e vendidas. |
| `StopLossPoints` | Distância do stop protetor expressa em passos de preço do instrumento. |
| `TakeProfitPoints` | Distância do alvo de lucro expressa em passos de preço do instrumento. |
| `OrderVolume` | Volume base para entradas a mercado. A estratégia adiciona a posição aberta absoluta para reverter em uma transação. |

## Notas de implementação
- As ordens são emitidas apenas após as velas terminarem, o que espelha o consultor especialista do MT5 que agia no início da próxima barra usando os dados da barra anterior.
- O indicador de canal é recriado dentro da estratégia sem armazenar coleções históricas, contando com variáveis de estado escalares.
- Stops e alvos de proteção são verificados manualmente para replicar o tratamento de ordens específico da plataforma do MT5.
- Garantir que o instrumento selecionado exponha um `PriceStep` válido; caso contrário, as distâncias de stop-loss e take-profit serão ignoradas.
