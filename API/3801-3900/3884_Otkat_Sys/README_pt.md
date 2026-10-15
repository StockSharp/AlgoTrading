# Estratégia Otkat Sys
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia reproduz o consultor especialista MetaTrader **1_Otkat_Sys**. Ele monitora a abertura, fechamento, máxima e
e baixo para decidir se deseja entrar em uma posição durante os primeiros três minutos após a meia-noite (horário da corretora) de terça a
Quinta-feira.

## Lógica de negociação

1. **Estatísticas diárias** – a última vela diária concluída é armazenada em cache para calcular:
   - `Open - Close` e `Close - Open` para detectar se a sessão anterior foi de baixa ou alta.
   - `Close - Low` e `High - Close` para medir o quão profundamente o preço recuou dos extremos.
2. **Janela de entrada** – novas negociações são avaliadas quando a vela de entrada abre entre 00h00 e 00h03. Segunda e sexta são
ignorado, correspondendo aos filtros `DayOfWeek` do robô original.
3. **Filtros direcionais** – quatro condições mutuamente exclusivas espelham as regras MQL:
   - Dia anterior de baixa (`Open - Close` acima do limite do corredor) combinado com uma retração superficial (`Close - Low`
abaixo de `Pullback - Tolerance`) abre um longo.
   - A alta do dia anterior com uma retração ascendente estendida (`High - Close` acima de `Pullback + Tolerance`) também abre uma posição longa.
   - A alta do dia anterior com uma retração ascendente fraca (`High - Close` abaixo de `Pullback - Tolerance`) abre uma venda.
   - O dia anterior de baixa com uma retração negativa estendida (`Close - Low` acima de `Pullback + Tolerance`) abre uma venda.
4. **Pedidos** – as entradas são ordens de mercado colocadas com o tamanho de lote configurado. As negociações de compra usam uma distância de lucro igual a
`TakeProfit + 3` pontos (como no EA original); shorts usam exatamente `TakeProfit` pontos. Ambos os lados aplicam o mesmo stop-loss
distância.
5. **Saída baseada em tempo** – qualquer posição aberta é estabilizada após 22h45, replicando a limpeza noturna implementada em MetaTrader
roteiro.

Todos os parâmetros de limite são expressos em pontos e traduzidos em distâncias de preço com o `PriceStep` do instrumento.

## Parâmetros

| Nome | Descrição |
| --- | --- |
| `EntryCandleType` | Período utilizado para a janela de negociação (padrão: 1 minuto). |
| `DailyCandleType` | Prazo que fornece as estatísticas diárias (padrão: 1 dia). |
| `TakeProfit` | Meta de lucro em pontos. As negociações longas adicionam um buffer de 3 pontos. |
| `StopLoss` | Distância de parada protetora em pontos. |
| `PullbackThreshold` | Limite de retrocesso básico ("Otkat") em pontos. |
| `CorridorThreshold` | Limite do corredor direcional (`KoridorOC`). |
| `ToleranceThreshold` | Tolerância de retrocesso (`KoridorOt`). |
| `TradeVolume` | Tamanho do lote para cada entrada. |

A estratégia redefine automaticamente seus valores armazenados em cache em `Reset`, inscreve-se em fluxos de velas de entrada e diários e sorteia
velas mais marcadores comerciais quando uma área do gráfico estiver disponível.
