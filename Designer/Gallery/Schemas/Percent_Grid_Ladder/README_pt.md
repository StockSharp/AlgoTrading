# Diagrama de escada percentual em grade
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O diagrama monta uma grade percentual simétrica ao redor do primeiro fechamento concluído de cinco minutos. Três compras limitadas ficam abaixo da âncora e três vendas acima; após a primeira execução, as demais ordens são canceladas e a posição recebe proteção percentual.

![schema](schema.svg)

## Visão geral da estratégia

- O fechamento do primeiro candle concluído de cinco minutos é fixado como âncora; Level1 e o ponto médio bid/ask não são usados.
- O espaçamento de 1,5% cria até três níveis de compra abaixo e três de venda acima da âncora.
- Grid Levels per Side habilita os degraus um a três, enquanto as chaves long e short controlam os lados separadamente.
- A primeira entrada executada cancela todas as ordens restantes e inicia proteção de lucro em 2% e perda em 3%.
- Uma saída de proteção cancela resíduos, fixa o último fechamento como nova âncora e registra outra escada.

## Regras de entrada e saída

- **Entrada comprada**: Com long habilitado, compras de uma unidade são registradas em anchor × (1 − spacing × degrau), com um a três níveis inferiores ativos.
- **Entrada vendida**: Com short habilitado, vendas de uma unidade são registradas em anchor × (1 + spacing × degrau), com um a três níveis superiores ativos.
- **Saída**: A primeira ordem executada inicia o único ciclo de posição. A proteção fecha em +2% ou −3% e reancora seis novas ordens no último fechamento concluído.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Grid Spacing, % | 1.5 | Distância percentual entre degraus vizinhos; 1,5 significa 1,5%. |
| Grid Levels per Side | 3 | Degraus habilitados por lado, de um ao máximo visual de três. |
| Enable Long | true | Habilita compras limitadas abaixo da âncora. |
| Enable Short | true | Habilita vendas limitadas acima da âncora. |
| Take Profit, % | 2 | Distância de lucro desde a entrada usada pela proteção. |
| Stop Loss, % | 3 | Distância de perda desde a entrada usada pela proteção. |

## Detalhes do diagrama

- A estratégia C# mantém níveis virtuais e envia ordens a mercado quando o fechamento os alcança. O diagrama materializa limites pendentes para mostrar os blocos de ordem e o ciclo de cancelamento.
- A fonte pode acionar vários níveis ao longo do tempo. Aqui há uma simplificação deliberada: uma execução cancela os demais degraus, negocia volume fixo um e espera a proteção antes de reconstruir.
- Grid Levels per Side aceita de um a três. O máximo visual é três, portanto valores maiores não criam blocos adicionais.
- Reancorar significa cancelar e registrar novas ordens, sem substituição; o ajuste ao passo de preço fica desligado no replay sem passo declarado.
- O fechamento exato do candle é a âncora inicial e posterior, igual ao preço de reinício da fonte e sem dependência de Level1.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
