# Neuro Nirvaman EA 2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Neuro Nirvaman EA 2 é uma estratégia de perceptron de múltiplas camadas que foi originalmente escrita para MetaTrader 5. A lógica combina quatro fluxos +DI suavizados por Laguerre com dois detectores de rompimento SilverTrend. Cada barra a estratégia avalia três perceptrons cujos pesos são controlados pelos parâmetros X. Um módulo supervisor escolhe qual saída do perceptron deve ser negociada com base no modo de passagem selecionado. A negociação é permitida apenas dentro da janela de sessão configurada e todas as posições são encerradas quando a janela fecha.

## Indicadores e sinais
- **Filtros Laguerre +DI** – Cada bloco Laguerre suaviza o valor +DI de um indicador ADX (gamma = 0.764). O valor resultante oscila entre 0 e 1 e é comparado com uma linha central de 0.5 com limiares de distância definidos pelo usuário.
- **Rompimento SilverTrend** – Dois detectores SilverTrend calculam envoltórias dinâmicas de suporte/resistência usando as últimas nove barras. O ajuste de risco modifica a largura da envoltória (`K = 33 - risk`). Uma transição de baixista para altista (ou vice-versa) produz sinais ±1 que alimentam os perceptrons.

## Lógica de negociação
1. **Perceptron #1** usa Laguerre #1 para o componente de tensão e SilverTrend #1 para o componente de rompimento. Os pesos `X11` e `X12` deslocam as contribuições relativas a 100.
2. **Perceptron #2** espelha o primeiro perceptron, mas depende de Laguerre #2 e SilverTrend #2 com pesos `X21` e `X22`.
3. **Perceptron #3** combina as saídas de tensão de Laguerre #3 e Laguerre #4 ponderadas por `X31` e `X32`.
4. **Modos supervisor (`Pass`)**
   - `1` – Negociar o perceptron #1 (`< 0` abre vendido, caso contrário comprado).
   - `2` – Negociar o perceptron #2 (`> 0` abre comprado, caso contrário vendido).
   - `3` – Abrir uma posição comprada quando tanto o perceptron #3 quanto o #2 são positivos. Abrir uma vendida quando o perceptron #3 é não-positivo e o perceptron #1 é negativo.
   - `4` – Desabilitar negociação (corresponde ao comportamento padrão do EA original).

Cada entrada coloca uma ordem de mercado de volume fixo e registra níveis de stop-loss / take-profit expressos em passos de preço. As posições são monitoradas em cada vela finalizada: se o máximo/mínimo perfura os alvos registrados, a estratégia sai imediatamente. Sair da janela de negociação também força uma saída.

## Parâmetros
| Nome | Descrição |
| --- | --- |
| `Risk1`, `Risk2` | Configurações de risco SilverTrend. Valores maiores reduzem a envoltória e geram sinais mais frequentes. |
| `LaguerreXPeriod` | Comprimento ADX que alimenta o suavizador Laguerre (para cada um dos quatro fluxos). |
| `LaguerreXDistance` | Distância percentual em torno da linha central 0.5 que define tensão altista/baixista. |
| `X11`, `X12`, `X21`, `X22`, `X31`, `X32` | Pesos do perceptron (valores são deslocados em 100 dentro da fórmula, exatamente como na versão MQL). |
| `TakeProfit1`, `StopLoss1`, `TakeProfit2`, `StopLoss2` | Distâncias de alvo de lucro e stop de proteção em passos de preço para os respectivos sinais do perceptron. |
| `Pass` | Seletor de modo supervisor (1–4). |
| `TradeVolume` | Tamanho base de ordem usado para entradas de mercado. |
| `StartHour`, `StartMinute`, `EndHour`, `EndMinute` | Limites da sessão de negociação. Quando a hora atual está fora desta janela, todas as posições são fechadas e não são permitidas novas operações. |
| `CandleType` | Inscrição de velas para impulsionar a estratégia de alto nível. |

## Gestão de risco
A estratégia depende das distâncias fixas de stop-loss e take-profit definidas pelo perceptron que acionou a entrada. Não é realizada piramidagem ou média. Como a lógica opera apenas quando nenhuma posição está aberta, a exposição é limitada a uma única posição ativa e todas as operações são fechadas à força quando a janela de sessão termina.

## Notas
- O gamma para o suavizador Laguerre é fixado em 0.764 para corresponder à implementação MQL.
- O valor Pass `4` mantém a estratégia inativa, o que reflete o padrão de segurança do EA original.
- Os cálculos SilverTrend usam primitivos de indicador (highest, lowest, simple moving average) em vez de buffers personalizados para cumprir as diretrizes do StockSharp.
