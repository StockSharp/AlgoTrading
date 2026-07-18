# Estratégia de reversão RPoint 250
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A **Estratégia de reversão RPoint 250** é uma versão StockSharp do MetaTrader 4 consultor especialista `e_RPoint_250`. O robô original
depende de um indicador personalizado chamado *RPoint* que destaca a oscilação máxima e mínima mais recente. Porque esse indicador é
não disponível em StockSharp, a conversão reproduz o mesmo comportamento com os indicadores `Highest` e `Lowest` integrados.
Sempre que um novo extremo substitui o anteriormente detectado, a estratégia imediatamente inverte a posição e restaura a mesma.
lógica de stop-loss, take-profit e trailing definidas na versão MQL.

## Fluxo de trabalho de negociação

1. Assine a série de velas especificada por `CandleType` (padrão: velas de 5 minutos).
2. Acompanhe o máximo e o mínimo contínuos nas últimas `ReversePoint` barras. Esses valores representam os níveis RPoint emulados.
3. Se o preço imprimir uma nova máxima máxima, feche qualquer posição longa e abra uma posição curta com volume `OrderVolume`.
4. Se o preço imprimir um novo mínimo, feche qualquer posição curta e abra uma posição longa com volume `OrderVolume`.
5. Aplique ordens de proteção usando `StartProtection`. As distâncias de stop-loss e take-profit são expressas em pontos de preço por meio de
os parâmetros `StopLossPoints` e `TakeProfitPoints`.
6. Opcionalmente, acompanhe os lucros por `TrailingStopPoints`. O mecanismo de trilha mede até que ponto o preço se moveu em favor do
posição e fecha-a quando o preço retrocede pelo número configurado de pontos.
7. Lembre-se do tempo da vela da última entrada bem-sucedida para evitar a abertura de múltiplas negociações dentro da mesma barra, correspondendo ao
`TimeN` proteção do script MQL.

A estratégia sempre mantém no máximo uma posição aberta. Fecha negociações existentes antes de entrar na direção oposta e
nunca aumenta.

## Parâmetros

| Parâmetro | Tipo | Padrão | Descrição |
|-----------|------|---------|-------------|
| `OrderVolume` | `decimal` | `0.1` | Volume enviado com cada ordem de mercado. Espelha a entrada `Lots` na versão MetaTrader. |
| `TakeProfitPoints` | `decimal` | `15` | Distância até a ordem de lucro medida em faixas de preço. Defina como `0` para desativar as metas de lucro. |
| `StopLossPoints` | `decimal` | `999` | Distância até o stop de proteção expressa em faixas de preço. Defina como `0` para negociar sem um stop fixo. |
| `TrailingStopPoints` | `decimal` | `0` | Distância final opcional em faixas de preço. Quando zero, a lógica final é desabilitada. |
| `ReversePoint` | `int` | `250` | Número de velas consideradas ao pesquisar as últimas oscilações máximas e mínimas. Valores maiores suavizam o ruído. |
| `CandleType` | `DataType` | `TimeSpan.FromMinutes(5).TimeFrame()` | Agregação de velas analisada pela estratégia. Altere-o para corresponder ao período do gráfico usado em MetaTrader. |

## Notas de implementação

- `Highest` e `Lowest` estão vinculados à assinatura de vela por meio do `Bind` API de alto nível, portanto, nenhuma fila de indicador manual é
necessário.
- `StartProtection` reproduz as distâncias originais de stop-loss e take-profit em unidades de preço absoluto. StockSharp lida com o
colocação de pedido assim que uma nova posição aparecer.
- Os trailing stops são implementados monitorando cada vela concluída. Quando o preço recua pelo número configurado de pontos de
ao melhor preço alcançado após a entrada, a posição é fechada com uma ordem de mercado.
- A classe armazena os níveis de reversão executados mais recentemente (`_executedHighLevel` e `_executedLowLevel`) para evitar duplicação
entradas. Isso é equivalente às variáveis ​​`Reverse_High` / `Reverse_Low` no código MQL.
- O campo `_lastSignalTime` espelha a variável `TimeN` e bloqueia vários pedidos dentro da mesma vela, evitando
submissões duplas acidentais em mercados ilíquidos.

## Diretrizes de uso

1. Anexe a estratégia a um portfólio que suporte o instrumento selecionado e o tipo de vela.
2. Ajuste `OrderVolume` para cumprir o tamanho do contrato e as regras de gerenciamento de risco do seu corretor.
3. Ajuste `ReversePoint` para corresponder à volatilidade do ativo negociado. Valores mais altos geram menos reversões, mas mais significativas.
4. Verifique se `StopLossPoints`, `TakeProfitPoints` e `TrailingStopPoints` são compatíveis com o `PriceStep` da segurança.
5. Execute um backtest no StockSharp Designer ou Backtester para confirmar o comportamento antes de negociar capital real.
6. Monitore a saída do log: mensagens informativas destacarão as mudanças de posição e podem ajudar a validar a conversão.

Como o indicador RPoint é aproximado com componentes integrados, pequenas diferenças em relação à execução MetaTrader são
possível em dados históricos com lacunas ou regras de arredondamento diferentes. Sempre valide os resultados com seus próprios feeds de dados de mercado
antes de confiar na estratégia na produção.
