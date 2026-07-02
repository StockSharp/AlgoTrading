# Estratégia TRAYLERv
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia TRAYLERv** é uma conversão direta do consultor especialista MetaTrader 4 *TRAYLERv*. O código original agia como um gerenciador comercial automatizado, em vez de um gerador de sinais; ajustou continuamente as ordens de proteção para posições existentes usando fractais de Bill Williams e permitiu que os traders limpassem as ordens pendentes pendentes. Esta porta StockSharp preserva o mesmo comportamento enquanto aproveita o API de alto nível para gerenciamento de pedidos e assinaturas de velas.

A estratégia **não** abre posições por conta própria. Ele espera que as negociações sejam criadas manualmente ou por outra estratégia e então assume a tarefa de manter stops e take-profits de acordo com a lógica abaixo. Todos os comentários e nomes de configuração seguem o legado EA para que usuários experientes possam mapear o comportamento rapidamente.

## Lógica de negociação
1. Assine a série de velas configurada (velas de um minuto por padrão) e registre cada barra finalizada. Os máximos e mínimos fractais são detectados quando cinco velas estão disponíveis, reproduzindo a definição fractal MT4 padrão.
2. Cada vez que uma nova vela fecha durante um minuto par, a estratégia verifica a posição líquida atual:
   - **Posições longas**: pesquise o fractal descendente mais recente dentro de `StopFractalDepth` barras (padrão 7). Se encontrado, coloque ou mova um stop de venda abaixo do mínimo fractal menos o spread atual e um buffer de dois pontos. Se não existir nenhum fractal válido, use o mínimo da vela três barras atrás menos dois pontos. Quando uma posição longa é lucrativa e os lucros estão ativados, procure o fractal ascendente mais recente dentro das barras `TakeProfitFractalDepth` (padrão 21) e coloque um limite de venda ligeiramente abaixo desse nível para corresponder à implementação MetaTrader.
   - **Posições curtas**: espelhe a lógica usando fractais ascendentes para o trailing buy stop e fractais descendentes para a meta de lucro. Buffers são adicionados acima dos máximos fractais para evitar paradas prematuras.
3. Quando `DeleteAllPendingOrders` está ativado, a estratégia cancela todas as ordens pendentes ativas que pode ver. Alternativamente, `DeleteOwnPendingOrders` remove apenas as ordens pendentes que pertencem ao símbolo atual. Ambas as opções replicam as opções de limpeza manual do EA original.
4. Se nenhuma posição estiver aberta, todas as ordens de proteção registradas pela estratégia serão canceladas para manter a carteira de ofertas organizada.

## Gestão de risco
- As ordens de proteção são criadas com contrapartes de ordens de mercado (`SellStop`, `BuyStop`, `SellLimit`, `BuyLimit`). O volume da ordem de proteção sempre corresponde ao tamanho absoluto da posição líquida.
- Trailing stops e take-profit são opcionais. Desativar o parâmetro take-profit remove qualquer ordem de limite existente, mas deixa a lógica final intacta.
- As informações de spread são obtidas do melhor par de compra/venda, quando disponível. Se nenhum spread puder ser medido, o código volta ao incremento mínimo de preço do instrumento para evitar colocar ordens diretamente no preço atual.
- Todos os níveis de preços são normalizados para o tamanho do tick do instrumento para que as ordens resultantes cumpram os requisitos cambiais.

## Parâmetros
| Parâmetro | Descrição | Padrão |
|-----------|-------------|---------|
| `OrderVolume` | Volume padrão sugerido para entradas manuais. Ele é mantido para compatibilidade com o EA original e não é usado internamente. | `0.1` |
| `DeleteAllPendingOrders` | Quando `true`, cancele todas as ordens pendentes ativas na conexão após cada vela. | `false` |
| `DeleteOwnPendingOrders` | Quando `true`, cancele apenas as ordens pendentes do símbolo atual. | `false` |
| `UseTakeProfit` | Permite o cálculo de lucro baseado em fractal. Quando desativado, qualquer ordem de lucro existente será removida. | `true` |
| `EnableSound` | Bandeira herdada preservada do MT4; fornecido para fins de integridade, mas não usado em StockSharp. | `true` |
| `ShowCommentary` | Switch legado equivalente ao comentário no gráfico MT4. Está disponível para telas de configuração, mas não tem efeito na porta. | `true` |
| `StopFractalDepth` | Número de barras inspecionadas para encontrar um fractal para o trailing stop. | `7` |
| `TakeProfitFractalDepth` | Número de barras inspecionadas para encontrar um fractal para o lucro. | `21` |
| `CandleType` | Tipo de dados usado para a série de velas primárias. O padrão é um período de 1 minuto. | `1 minute` período de tempo |

## Notas de implementação
- A estratégia usa o fluxo de trabalho de alto nível `SubscribeCandles().Bind(...)` e processa apenas velas concluídas, espelhando o loop baseado em ticks MT4, evitando atualizações prematuras.
- A detecção fractal é implementada manualmente usando uma lista contínua de instantâneos de velas. Isso reproduz o comportamento do indicador MT4 `iFractals` sem depender de indicadores StockSharp extras.
- Os preços dos pedidos são arredondados para o tick válido mais próximo e os volumes respeitam as restrições `VolumeStep`, `MinVolume` e `MaxVolume` para garantir a compatibilidade de troca.
- Nenhuma tradução Python está incluída. O diretório `PY` está ausente intencionalmente, atendendo aos requisitos das diretrizes de conversão.
