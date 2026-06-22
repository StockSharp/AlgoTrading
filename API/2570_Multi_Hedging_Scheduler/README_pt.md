# Estratégia de Agendador de Hedge Múltiplo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia de Agendador de Hedge Múltiplo** é uma conversão direta do StockSharp do consultor especialista original MetaTrader 5 `MultiHedg_1.mq5`. A estratégia foi projetada para contas que permitem hedge e pode gerenciar até dez instrumentos diferentes simultaneamente. Abre posições na mesma direção durante uma janela de negociação configurável e fornece lógica de saída no nível do portfólio baseada em tempo ou limiares de porcentagem de equity.

Em vez de depender de indicadores, a estratégia usa um fluxo de velas de um minuto (configurável) puramente como fonte de temporização. Cada vela terminada aciona verificações para abrir operações, fechar tudo quando a janela de negociação expira e impor regras de risco baseadas em equity. A estratégia é, portanto, adequada para portfólios onde a execução é impulsionada por agenda e não por padrões de preço.

## Lógica de negociação
1. **Seleção de instrumentos** – Até dez símbolos podem ser habilitados. Para cada entrada habilitada, a estratégia resolve o ticker através do `SecurityProvider`, subscreve velas do tipo configurado e usa a mesma lógica em todos os instrumentos.
2. **Janela de negociação** – quando o timestamp da vela entra na janela `TradeStartTime` (com duração `TradeDuration`), a estratégia abre uma posição de mercado na direção configurada (`TradeDirection`) para cada símbolo habilitado que ainda não tem uma posição aberta nessa direção. Se uma posição oposta existir, o volume é aumentado para inverter para o lado desejado.
3. **Proteção de equity** – se `CloseByEquityPercent` estiver habilitado e o equity do portfólio desviar do saldo inicial em `PercentProfit` ou `PercentLoss`, cada posição aberta gerenciada pela estratégia é fechada.
4. **Saída baseada em tempo** – se `UseTimeClose` estiver habilitado, a estratégia fecha todas as posições rastreadas quando o relógio alcança a janela `CloseTime` (com duração `TradeDuration`).
5. **Registro** – ações como entradas, saídas baseadas em equity e saídas baseadas em tempo são registradas por chamadas `LogInfo` para rastreabilidade.

## Parâmetros
| Parâmetro | Descrição | Padrão |
|-----------|-------------|---------|
| `TradeDirection` | Direção de todas as ordens (`Buy` ou `Sell`). | Buy |
| `TradeStartTime` | Hora local quando a janela de entrada abre. | 19:51 |
| `TradeDuration` | Duração das janelas de entrada e fechamento. | 00:05:00 |
| `UseTimeClose` | Habilita a janela de fechamento baseada em tempo. | true |
| `CloseTime` | Hora local quando a janela de fechamento abre. | 20:50 |
| `CloseByEquityPercent` | Habilita o fechamento de todas as posições nos limiares de equity. | true |
| `PercentProfit` | Porcentagem de ganho em equity que aciona um fechamento global. | 1.0 |
| `PercentLoss` | Porcentagem de drawdown em equity que aciona um fechamento global. | 55.0 |
| `CandleType` | Tipo de vela usado como condutor de agendamento. | Período de 1 minuto |
| `UseSymbol0..9` | Alterna a negociação para o símbolo correspondente. | true para os símbolos 0–5, false para 6–9 |
| `Symbol0..9` | Ticker para cada slot, resolvido via `SecurityProvider.LookupById`. | Ver padrões abaixo |
| `Volume0..9` | Volume de ordem para cada slot (lotes no EA original). | 0.1–1.0 |

**Configuração de símbolos padrão**

| Slot | Habilitado | Símbolo | Volume |
|------|---------|--------|--------|
| 0 | ✔ | EURUSD | 0.1 |
| 1 | ✔ | GBPUSD | 0.2 |
| 2 | ✔ | GBPJPY | 0.3 |
| 3 | ✔ | EURCAD | 0.4 |
| 4 | ✔ | USDCHF | 0.5 |
| 5 | ✔ | USDJPY | 0.6 |
| 6 | ✖ | USDCHF | 0.7 |
| 7 | ✖ | GBPUSD | 0.8 |
| 8 | ✖ | EURUSD | 0.9 |
| 9 | ✖ | USDJPY | 1.0 |

## Notas de uso
- Certificar que a conta suporta hedge se planeja replicar o comportamento original do MetaTrader. Em contas de compensação, a estratégia compensará automaticamente posições opostas ao mudar de direção.
- Fornecer identificadores de instrumentos nos parâmetros `SymbolX` exatamente como são conhecidos pelo `SecurityProvider` do StockSharp (por exemplo `EURUSD@FXCM`).
- O fluxo de velas é usado apenas para impulsionar a lógica de agendamento. Ajustar `CandleType` se a fonte de dados fornecer um intervalo de agregação diferente.
- A proteção de equity compara o equity ao vivo contra o saldo capturado em `OnStarted`. Reiniciar a estratégia redefine o saldo de referência.
- A estratégia não inclui ordens de stop protetor ou take profit. As saídas globais são controladas exclusivamente pelas porcentagens de equity e a janela de fechamento.

## Notas de conversão
- O especialista MT5 original usava `OnTick`. Na versão StockSharp, as velas terminadas substituem os eventos de tick para avaliar janelas de tempo de forma orientada por eventos de alto nível.
- A filtragem por número mágico é desnecessária porque a estratégia opera dentro do contêiner de estratégias do StockSharp; portanto `CloseAllManagedPositions` itera apenas pelos símbolos configurados.
- Alertas sonoros e comentários no gráfico foram omitidos, mas a estratégia registra todas as ações críticas via `LogInfo` para auditoria mais fácil.
