# Estratégia do Plano Diretor de Saída
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

`MasterExitPlanStrategy` reproduz a lógica de gerenciamento de risco do consultor especialista MetaTrader "Plano Mestre de Saída" usando o API de alto nível de StockSharp. A estratégia não abre novas negociações. Em vez disso, supervisiona a exposição existente, aplica uma combinação de regras de stop ocultas e visíveis, rastreia as ordens pendentes e fecha tudo quando o capital atinge uma meta de lucro configurada.

A implementação assina velas de um minuto para emular as chamadas `iOpen(symbol, PERIOD_M1, 1)` do script original. Todos os cronômetros são acionados pelo agendador de estratégia e avaliados a cada segundo, correspondendo ao comportamento do loop MetaTrader `EventSetTimer(1)`.

## Recursos

- **Saída da meta de capital** – fecha todas as posições quando os ganhos de capital do portfólio atingem o percentual configurado.
- **Níveis de stop estáticos e dinâmicos** – monitora as distâncias de stop a partir do preço de entrada e as âncoras dinâmicas baseadas em minutos.
- **Tratamento de parada oculta** – executa saídas de proteção internamente em vez de depender de ordens de câmbio.
- **Módulo trailing stop** – é ativado após um ganho mínimo de dinheiro e segue o stop com compensação de spread.
- **Acompanhamento de ordens pendentes** – registra automaticamente novamente as ordens buy-stop e sell-stop para que elas sigam o mercado.

## Parâmetros

| Nome | Descrição | Padrão |
| --- | --- | --- |
| `EnableTargetEquity` | Permitir a liquidação da meta de capital. | `false` |
| `TargetEquityPercent` | Percentual do saldo atual utilizado como meta. | `1` |
| `EnableStopLoss` | Ative o stop loss estilo corretor estático. | `false` |
| `StopLossPoints` | Distância de parada estática (MetaTrader pontos). | `2000` |
| `EnableDynamicStopLoss` | Amarre a parada brusca ao minuto anterior aberto. | `false` |
| `DynamicStopLossPoints` | Distância de parada dinâmica (pontos). | `2000` |
| `EnableHiddenStopLoss` | Ative o stop loss estático oculto. | `false` |
| `HiddenStopLossPoints` | Distância de parada estática oculta (pontos). | `800` |
| `EnableHiddenDynamicStopLoss` | Habilite a parada dinâmica oculta com base no minuto aberto. | `false` |
| `HiddenDynamicStopLossPoints` | Distância de parada dinâmica oculta (pontos). | `800` |
| `EnableTrailingStop` | Habilite o módulo de parada móvel. | `false` |
| `TrailingStopPoints` | Distância final mantida atrás do preço (pontos). | `5` |
| `TrailingTargetPercent` | Lucro mínimo em% do saldo antes da ativação do trailing. | `0.2` |
| `SureProfitPoints` | Pontos extras que devem ser protegidos antes de armar o trailing stop. | `30` |
| `EnableTrailPendingOrders` | Habilite o rastreamento de ordens de parada ativas (entradas). | `false` |
| `TrailPendingOrderPoints` | Compensação em pontos para ordens de stop pendentes. | `10` |

## Notas de uso

1. Anexe a estratégia a um título que já é gerenciado por outro módulo de entrada ou ordens manuais. Defina `Volume` de acordo com os contratos que você precisa fechar durante o nivelamento.
2. Forneça um portfólio que relate `Portfolio.CurrentValue`. A estratégia usa esse valor para aproximar `AccountBalance` e `AccountEquity` de MetaTrader. Se o valor estiver faltando, a lógica de meta de capital permanecerá inativa.
3. A estratégia avalia as melhores cotações de compra/venda ao verificar as condições de stop. Certifique-se de que os dados de nível 1 estejam disponíveis para que os cálculos com reconhecimento de propagação sejam significativos.
4. Paradas ocultas e saídas móveis são implementadas como ordens de mercado gerenciadas por software. As ordens stop do lado da corretora **não** são criadas; o comportamento reflete a natureza "oculta" do EA original.

## Diferenças da versão MQL

- Os níveis de stop são aplicados através da emissão de ordens de mercado quando os limites são violados. O EA original modificou o campo `OrderStopLoss`; StockSharp usa monitoramento ativo.
- Os cálculos de parada dinâmica baseiam-se na última vela de um minuto concluída, entregue por meio de `SubscribeCandles`. Se esta assinatura estiver faltando, as regras dinâmicas permanecerão desativadas.
- O rastreamento de ordens pendentes ignora ordens de stop de proteção criadas por outras estratégias porque o próprio `MasterExitPlanStrategy` não as registra.
- As verificações de patrimônio usam `Portfolio.CurrentValue` (substituição para `Portfolio.BeginValue`) em vez de `AccountBalance`/`AccountEquity`.

## Teste

A estratégia não contém testes automatizados. Use o testador do StockSharp com dados históricos para verificar o comportamento dos seus instrumentos antes de implantar na produção.
