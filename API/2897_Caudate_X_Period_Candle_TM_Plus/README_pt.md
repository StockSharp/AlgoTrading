# Estratégia Caudate X Período Vela TM Plus
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral
A estratégia replica a lógica do especialista Caudate X Period Candle TM Plus. Ela suaviza os preços de abertura, máximo, mínimo e fechamento da vela com uma média móvel configurável, constrói um intervalo estilo Donchian e classifica cada vela finalizada em um de seis códigos de cor dependendo da posição do corpo dentro do intervalo. Entradas compradas são acionadas pelas cores de cauda inferior de alta (0 ou 1), enquanto entradas vendidas são acionadas pelas cores de cauda superior de baixa (5 ou 6). Grupos de cores opostos são usados para sair de posições existentes.

## Regras de Trading
1. Assinar a série de velas selecionada e suavizar cada componente com a média móvel escolhida.
2. Calcular o máximo mais alto e o mínimo mais baixo dos máximos e mínimos suavizados durante o `Donchian Period` especificado, depois expandir o intervalo para que sempre contenha a abertura e o fechamento suavizados.
3. Determinar a cor da vela:
   * Cores **0/1** – corpo próximo ao topo do intervalo (cauda inferior).
   * Cores **2/4** – corpo centrado dentro do intervalo.
   * Cores **5/6** – corpo próximo ao fundo do intervalo (cauda superior).
4. Avaliar a cor da barra deslocada por `Signal Bar` (o padrão `1` usa a vela completada anterior).
5. Abrir posições quando a cor pertence ao grupo de entrada e a posição oposta não está ativa.
6. Fechar posições quando a cor pertence ao grupo de saída ou quando o tempo máximo de retenção expira.
7. Offsets opcionais de stop-loss e take-profit são definidos através do módulo de proteção incorporado.

## Parâmetros
| Parâmetro | Descrição |
| --- | --- |
| `Candle Type` | Período usado para os cálculos de sinal. |
| `Donchian Period` | Número de velas para o intervalo suavizado de máximo/mínimo. |
| `Signal Bar` | Número de barras para atrasar a avaliação de sinal (0 = barra atual). |
| `Smoothing Method` | Média móvel aplicada aos preços OHLC (SMA, EMA, SMMA, LWMA, aproximação Jurik JJMA, Kaufman AMA). |
| `MA Length` | Comprimento do filtro de suavização. |
| `MA Phase` | Reservado para compatibilidade JJMA (não usado pelas médias do StockSharp). |
| `Enable Long/Short Entries` | Alternar a abertura de novas posições compradas ou vendidas. |
| `Enable Long/Short Exits` | Alternar o fechamento de posições compradas ou vendidas existentes em sinais. |
| `Enable Time Exit` | Habilitar o filtro de tempo máximo de retenção. |
| `Time Exit (minutes)` | Duração de retenção antes de um fechamento forçado. |
| `Stop Loss (points)` | Distância de stop-loss em passos de preço (multiplicado por `Security.PriceStep`). |
| `Take Profit (points)` | Distância de take-profit em passos de preço. |

## Notas
- `Signal Bar = 1` corresponde ao comportamento do especialista MQL5 agindo na última vela completamente fechada.
- Quando as distâncias de stop ou alvo são maiores que zero, a estratégia chama `StartProtection` com offsets absolutos baseados no passo de preço do instrumento.
- `MA Phase` é mantido por compatibilidade mas não é consumido pelas implementações de média móvel do StockSharp.
- Definir o tamanho base da ordem através da propriedade `Strategy.Volume` herdada; a implementação sempre fecha posições opostas antes de abrir uma nova.
