# Estratégia de couro cabeludo VirtPO TestBed
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia transfere o consultor especialista **VirtPOTestBed_ScalpM1** MetaTrader 4 para o StockSharp API de alto nível. Ele mantém a ideia original de criar *ordens pendentes virtuais* que são armadas por cruzamentos de osciladores Stochastic e executadas assim que a dinâmica do preço confirmar o movimento. Todos os filtros, regras de gerenciamento de dinheiro e controles de agendamento da versão MQL foram replicados com indicadores e métodos de pedido StockSharp.

## Lógica principal

1. **Ordens pendentes virtuais** – Quando nenhuma posição está aberta, a estratégia verifica o bloco de filtro em cada vela concluída:
   * O spread deve permanecer abaixo de `SpreadMaxPips` (melhor bid/ask obtido do Nível 1).
   * O volume médio de ticks nas últimas três barras deve exceder `VolumeLimit`.
   * A volatilidade absoluta do preço (tamanho médio do corpo para `VolatilityPeriod` barras) deve estar acima de `VolatilityLimit`.
   * A largura de banda Bollinger (período `BollingerPeriod`, largura 2) deve ficar entre `BollingerLowerLimit` e `BollingerUpperLimit`.
   * O horário de negociação deve estar dentro da janela configurada (`EntryHour` + `OpenHours`) e fora dos dias da semana desabilitados (`Day1`, `Day2`, horário limite de sexta-feira).
   * Filtro de tendência SMA – a diferença entre o rápido (`SmaFastPeriod`) e o lento (`SmaSlowPeriod`) SMA em pips deve exceder `SmaDifferencePips` em qualquer direção.
   * O corpo da barra anterior deve ser menor que `LastBarLimitPips` para evitar perseguir velas longas.

Se os filtros forem bem-sucedidos, Stochastic cruzamentos serão avaliados:
   * Um cruzamento de alta através de `StochasticSetLevel` arma um **stop de compra virtual** acima do lance em `PoThresholdPips`.
   * Um cruzamento de baixa através de `100 - StochasticSetLevel` arma um **stop de venda virtual** abaixo do lance pelo mesmo limite.
Cada ordem pendente virtual lembra seu vencimento (`PoTimeLimitMinutes`) e as distâncias stop-loss/take-profit obtidas de `StopLossPips` e `TakeProfitPips`.

2. **Fase de execução** – Quando `TickLevel` está habilitado, a estratégia escuta as negociações recebidas para executar ordens virtuais assim que o último preço ultrapassar o limite. Se `TickLevel` estiver desativado, a verificação do gatilho será executada no fechamento de cada vela concluída. Assim que o preço ultrapassa o stop virtual, uma ordem de mercado é enviada e a ordem virtual é compensada.

3. **Gerenciamento de riscos** – Depois de preencher as trilhas de estratégia:
   * Níveis iniciais de stop-loss e take-profit medidos em pips a partir do preço de entrada.
   * Trailing stop opcional (`TrailingStopPips`) que segue o preço extremo desde a entrada.
   * Tempo máximo de retenção (`CloseTimeMinutes`). Dependendo de `ProfitType` ele pode fechar todas as posições (0), apenas as lucrativas (1) ou apenas as perdedoras (2) quando o cronômetro expirar.

Todas as distâncias de preços são convertidas de pips usando o título `PriceStep` e o multiplicador de dígitos, reproduzindo o tratamento do corretor de cinco dígitos na implementação MQL. O padrão `OrderVolume` é aplicado a todas as ordens de mercado. A estratégia redefine automaticamente seu estado interno quando as posições se estabilizam.

## Notas importantes

* Os dados de nível 1 são necessários para calcular spreads e acionar níveis com precisão. Sem atualizações de melhor oferta/venda, os filtros bloquearão a negociação.
* A execução em nível de tick reflete o sinalizador `TickLevel` do EA original; quando desabilitada, a execução aguarda o fechamento da vela, o que é mais conservador, mas mais fácil de testar.
* A estratégia mantém apenas uma única posição líquida, assim como a versão MQL que restringia o número de ordens de mercado ativas.

## Parâmetros

| Grupo | Nome | Descrição |
| --- | --- | --- |
| Geral | Tipo de vela | Período usado para assinatura de velas (padrão: 1 minuto). |
| Execução | Nível de escala | Use ticks comerciais para executar ordens virtuais imediatamente. |
| Execução | Limite PO (pips) | Distância em pips entre o preço de compra e o nível de stop virtual. |
| Execução | Vida útil do pedido (min) | Tempo de expiração para cada ordem pendente virtual. |
| Filtros | Spread máximo (pips) | Spread máximo permitido antes de armar ordens. |
| Filtros | Limite de volume | Volume médio mínimo de ticks nas últimas três barras. |
| Filtros | Período de Volatilidade | Número de barras usadas para calcular a média dos corpos absolutos das velas. |
| Filtros | Limite de volatilidade | Tamanho médio mínimo do corpo da vela (em pips). |
| Filtros | Bollinger Período | Bollinger período de cálculo da banda. |
| Filtros | Bollinger Inferior/Superior | Faixa de largura de banda permitida em pips. |
| Filtros | Limite da última barra | Tamanho máximo do corpo da vela anterior em pips. |
| Tendência | Rápido SMA / Lento SMA | Períodos para o filtro de tendência de média móvel. |
| Tendência | SMA Diferença | Distância mínima de SMA em pips para confirmar uma tendência. |
| Stochastic | %K /%D /Suave | Períodos padrão do oscilador Stochastic. |
| Stochastic | Stochastic Definir | Nível utilizado para armar ordens pendentes virtuais. |
| Stochastic | Stochastic Vá | Limite usado para executar a ordem armada. |
| Negociação | Volume do pedido | Volume base de ordens de mercado. |
| Risco | Take Profit / Stop Loss / Trailing Stop | Distâncias de saída em pips. |
| Cronograma | Desativar dias, primeiro/segundo dia sem negociação | Filtros de dias da semana (use 99 para desativar). |
| Cronograma | Horário de entrada / Horário de funcionamento | Início e duração da janela de negociação. |
| Cronograma | Corte de sexta-feira | Hora após a qual a negociação de sexta-feira para. |
| Risco | Vida útil máxima | Saída baseada em tempo em minutos (defina ≥5000 para desabilitar). |
| Risco | Filtro de Lucro | 0 – fecha independentemente, 1 – fecha apenas os vencedores, 2 – fecha apenas os perdedores quando o cronômetro dispara. |

## Diferenças do original EA

* A classe auxiliar MQL `CPO` é substituída por variáveis de estado internas que chamam `BuyMarket` / `SellMarket` diretamente quando o preço cruza o nível virtual.
* A execução de stop-loss e take-profit usa máximos/mínimos de velas (para back-tests) ou atualizações de ticks, quando disponíveis. Preenchimentos parciais ou posições cobertas do ambiente MT4 original não são suportados.
* O gerenciamento de dinheiro baseado em conta (`GLots`) não é portado; a estratégia StockSharp usa o parâmetro `OrderVolume` fixo.

Essas adaptações preservam a ideia comercial ao mesmo tempo que se ajustam ao modelo de programação de alto nível e posição única de StockSharp.
