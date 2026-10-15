# Estratégia Fibo Pivot MultiVal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **Estratégia Fibo Pivot MultiVal** é uma versão StockSharp do MetaTrader 4 consultor especialista `_Fibo_Pivot_multiVal.mq4`. O
estratégia combina pontos de pivô diários com retração de Fibonacci e taxas de extensão para implantar pedidos com limite dentro de cada preço
zona que circunda o pivô. Sessões de negociação, metas de posição e regras de parada seguem o consultor especialista original para que
o controle de risco e o comportamento de execução permanecem familiares aos traders que usaram a versão MetaTrader.

## Lógica principal

1. **Os níveis de referência diários** são calculados a partir da máxima, da mínima e do fechamento do dia anterior. Níveis de pivô clássicos (P, R1-R3, S1-S3)
são acompanhados por níveis internos baseados em Fibonacci que dividem a distância entre o pivô e o suporte vizinho ou
linhas de resistência. Extensões adicionais R3/S3 projetam alvos potenciais de ruptura.
2. **A ação do preço intradiário** é monitorada no período de vela configurado (15 minutos por padrão). Quando a corrente fechar
reside dentro de uma zona pivô específica (por exemplo, entre R2 e R3), a estratégia ativa as ordens de limite correspondentes.
3. **Pedidos com limite** são colocados nos subníveis Fibonacci. Cada zona mantém ordens longas e curtas, com a direção
filtrado pelo parâmetro `MidZoneOrderMode` quando o preço oscila entre R1-R2 e S1-S2.
4. **As metas** se adaptam à volatilidade do mercado. Quando `UseReversalTargets` está ativado, as saídas ficam no lado oposto do ativo
Banda Fibonacci para capturar saltos de reversão à média. Quando desabilitado, o algoritmo compara o intervalo do dia anterior com o
Limites de `LimitPointOut` e `LimitPointIn` para decidir se deseja buscar intervalos estendidos (em direção a extensões R3/S3) ou
reversões mais profundas (em direção ao pivô).
5. **Limites de risco** pausa novas negociações assim que os limites configuráveis de lucro/negociação diários ou por símbolo forem excedidos. Todos pendentes
as ordens são canceladas e a negociação é retomada na próxima sessão reiniciada (antes de `StartTime`).
6. **Gerenciamento de sessões** reflete o EA original: a negociação começa em `StartTime`, novas entradas param após `FinishTime` e todos
a exposição aberta é achatada após `CloseAllTime`.

## Parâmetros

| Nome | Padrão | Descrição |
| ---- | ------- | ----------- |
| `CandleType` | Velas de 15 minutos | Prazo usado para construir as velas de decisão. |
| `OrderVolume` | `0.1` | Volume para cada ordem limite registrada pela estratégia. |
| `StartTime` | `00:01` | Hora do dia da sessão que permite a negociação e zera os contadores. |
| `FinishTime` | `08:00` | Tempo de sessão que desativa novas entradas, mantendo as posições existentes. |
| `CloseAllTime` | `12:00` | Tempo de sessão que cancela ordens e fecha todas as posições. |
| `UseReversalTargets` | `true` | Quando verdadeiro, os alvos permanecem dentro da zona Fibonacci. Quando falso, as metas de breakout/pivot são usadas com base no intervalo diário. |
| `LimitPointIn` | `150` | Limite de intervalo diário (pontos) que impõe metas de reversão dinâmicas quando excedido. |
| `LimitPointOut` | `50` | Limite de intervalo diário (pontos) que incentiva metas de ruptura quando a ação do preço é comprimida. |
| `LevelPf1` | `33` | Porcentagem usada para dividir a distância Pivot–R1 e Pivot–S1. |
| `LevelF1F2` | `50` | Porcentagem usada para calcular o nível intermediário entre R1–R2 e S1–S2. |
| `LevelF2F3` | `33` | Porcentagem usada para calcular o nível intermediário entre R2–R3 e S2–S3. |
| `LevelF3Out` | `40` | Porcentagem usada para estender R3/S3 para metas de breakout. |
| `MidZoneOrderMode` | `"bs"` | Rotas permitidas dentro das zonas intermediárias (`"b"`= somente compra, `"s"`= somente venda, `"bs"`= ambas). |
| `DailyProfitTarget` | `50` | Limite de lucro diário em pontos. |
| `DailyTradeTarget` | `35` | Número máximo de negociações concluídas por dia. |
| `SymbolProfitTarget` | `150` | Meta de lucro por símbolo em pontos. |
| `SymbolTradeTarget` | `15` | Máximo de negociações concluídas por símbolo por dia. |

## Gerenciamento de ordens

* Cada zona ativa mantém suas próprias ordens de entrada, take-profit e stop opcionais. Quando uma entrada é preenchida, as ordens de saída são
recriado usando os níveis de destino/parada derivados da configuração Fibonacci.
* As saídas preenchidas atualizam as estatísticas diárias e por símbolo. Atingir qualquer limite pausa a negociação até a próxima redefinição.
* Os limites da sessão cancelam automaticamente os pedidos de entrada. O limite `CloseAllTime` fecha adicionalmente quaisquer posições abertas via
ordens de mercado.

## Dicas Práticas

* A estratégia espera instrumentos com etapas de preços bem definidas. Certifique-se de que a instância `Security` exponha `PriceStep` para que o
a conversão ponto-a-preço corresponde ao EA original.
* Para ativos com características de volatilidade diferentes, ajuste `LimitPointIn` e `LimitPointOut` para que o breakout vs.
comportamentos de reversão à média são desencadeados em intervalos apropriados.
* Se você preferir negociações direcionais em torno da zona intermediária (R1-R2 ou S1-S2), defina `MidZoneOrderMode` como `"b"` ou `"s"` para permitir apenas
configurações longas ou curtas.
* Use o suporte integrado de otimização de parâmetros para testar proporções alternativas de Fibonacci. Todos os parâmetros percentuais e
os limites expõem `SetCanOptimize` no código-fonte, permitindo verificações automatizadas dentro do StockSharp Designer.

## Diferenças em relação ao Expert Advisor original

* A versão StockSharp funciona em uma única segurança por instância de estratégia. Para negociar vários símbolos como em MetaTrader EA,
execute instâncias de estratégia separadas para cada instrumento.
* O dimensionamento da posição é expresso diretamente em unidades de volume, em vez de MetaTrader lotes. Configure `OrderVolume` para corresponder ao seu
requisitos do corretor.
* A execução do pedido depende do StockSharp API de alto nível (`BuyLimit`, `SellLimit`, etc.). Comportamento específico do corretor (como
compensações de pedidos pendentes) devem ser revisados antes da implantação na produção.
