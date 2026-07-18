# Estratégia MT5 de modelo multimoeda
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **Estratégia MT5 do modelo MultiCurrency** replica o comportamento do consultor especialista MetaTrader com o mesmo nome. Ele negocia um padrão simples de duas velas no período diário, permitindo ao usuário operar uma cesta de instrumentos simultaneamente. A estratégia abre uma posição inicial somente quando a vela diária anterior é de alta ou de baixa o suficiente para acionar o padrão e, em seguida, gerencia a negociação em um período de controle mais rápido. Um bloco de média martingale adiciona tickets adicionais quando o preço se move contra a posição em um número configurável de MetaTrader pontos, enquanto a lógica de saída combina lucro fixo, média de ponto de equilíbrio e um trailing stop opcional.

A porta StockSharp mantém o gerenciamento de vários símbolos, permitindo ao usuário definir uma lista de títulos separados por vírgula. Cada símbolo é tratado de forma independente com seu próprio contexto de rastreamento, cesta de posições e valores de gerenciamento de dinheiro. Quando o parâmetro `TradeMultipair` está desabilitado a estratégia negocia o `Security` principal anexado à instância da estratégia.

## Geração de sinal

* A estratégia assina o `SignalCandleType` (diariamente por padrão) e armazena duas velas concluídas consecutivas.
* Uma configuração **longa** é detectada quando o último fechamento está abaixo da abertura anterior e a vela anterior fecha acima de sua abertura.
* Uma configuração **curta** é detectada quando o último fechamento está acima da abertura anterior e a vela anterior fechou abaixo de sua abertura.
* Apenas uma direção pode estar ativa por vez. Novas negociações são ignoradas até que a cesta atual seja totalmente fechada.

## Execução de ordem

* As inscrições são enviadas a mercado com o volume definido por `Lots`.
* Quando `NewBarTrade` está habilitado, a estratégia espera por uma vela concluída em `TradeCandleType` antes de armar uma nova entrada. A bandeira é consumida na primeira decisão de negociação para replicar o comportamento MetaTrader "negociar apenas em uma nova barra".
* As metas de stop-loss e take-profit são inicializadas usando MetaTrader pips (multiplicados pelo tamanho do pip detectado) para que a distância corresponda ao especialista original.
* Se `EnableMartingale` for verdadeiro, a estratégia adiciona tickets médios sempre que o preço se afasta `StepPoints` da melhor entrada da cesta atual. Os volumes são dimensionados em `NextLotMultiplier` aumentados para o número de tickets já abertos nesse lado.

## Gestão comercial

* O comportamento de obtenção de lucro depende de `EnableTakeProfitAverage`:
  * Quando desativado, o take-profit permanece na distância inicial definida por `TakeProfitPips` do melhor preço da cesta.
  * Quando ativado e a cesta contém pelo menos dois ingressos, a meta é alterada para o preço de equilíbrio mais `TakeProfitOffsetPoints`.
* Os níveis de stop-loss são recalculados após cada preenchimento para que reflitam o pior preço da cesta.
* Um trailing stop atua quando apenas um ticket está aberto. Ele reproduz a lógica MetaTrader saltando primeiro para o ponto de equilíbrio mais `TrailingStopPoints` quando o movimento excede `TrailingStopPoints + TrailingStepPoints` e, em seguida, seguindo o preço com a mesma distância quando a negociação continua avançando.
* As saídas de risco acionam uma ordem de mercado que fecha a cesta completa em uma transação de cada lado.

## Parâmetros

| Parâmetro | Descrição |
| --- | --- |
| `Lots` | Volume base de negociação para o primeiro bilhete de cada cesta. |
| `StopLossPips` | Distância inicial do stop-loss expressa em MetaTrader pips. |
| `TakeProfitPips` | Distância inicial de lucro em MetaTrader pips. |
| `TrailingStopPoints` | Distância final (MetaTrader pontos) quando apenas um ticket está ativo. |
| `TrailingStepPoints` | Buffer extra (pontos) necessário antes que o trailing stop seja movido novamente. |
| `SlippagePoints` | Reservado para análises para imitar a entrada de derrapagem MetaTrader (não usada para execução). |
| `NewBarTrade` | Ativa o filtro de negociação em nova barra com base nas velas `TradeCandleType`. |
| `TradeCandleType` | Período de pulsação que impulsiona a detecção de novas barras e o gerenciamento de dinheiro. |
| `TradeMultipair` | Quando verdadeiro, ativa o modo multisímbolo. |
| `PairsToTrade` | Lista separada por vírgulas de identificadores de segurança adicionais resolvidos por meio de `GetSecurity`. |
| `Commentary` | Comentário do pedido preservado para referência. |
| `EnableMartingale` | Ativa o bloco de média que adiciona tickets em movimentos adversos. |
| `NextLotMultiplier` | Multiplicador aplicado ao volume de tickets anterior quando um novo pedido de média é feito. |
| `StepPoints` | Distância em MetaTrader pontos que aciona a próxima ordem média. |
| `EnableTakeProfitAverage` | Ativa a meta de ponto de equilíbrio + compensação para cestas com vários tickets. |
| `TakeProfitOffsetPoints` | MetaTrader pontos adicionados acima (longo) ou abaixo (curto) do preço de equilíbrio quando a média está ativa. |
| `SignalCandleType` | Prazo usado para construir o padrão de duas velas (diariamente por padrão). |

## Notas

* A estratégia depende de ordens de mercado tanto para entradas como para saídas; As ordens de proteção do corretor de MetaTrader são emuladas internamente.
* `PairsToTrade` deve conter identificadores que o conector conectado possa resolver. Símbolos desconhecidos são ignorados silenciosamente.
* Os blocos martingale e trailing operam por contexto de símbolo, portanto cada título mantém uma cesta independente.
* `SlippagePoints` é preservado para ser completo, mas não afeta a execução em StockSharp.
