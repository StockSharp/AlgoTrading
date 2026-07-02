# Estratégia de daytrade mais fácil de todos os tempos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
- Conversão do MetaTrader 4 consultor especialista **"O robô daytrade mais fácil de todos os tempos"** para o StockSharp API de alto nível.
- Projetado para day trading simples: cada sessão abre no máximo uma posição de mercado que segue a direção da vela diária anterior.
- Utiliza apenas dados de velas, sem indicadores técnicos ou osciladores. Toda a gestão de ordens é realizada através de ordens de mercado.

## Lógica de negociação
1. Colete velas diárias (`DailyCandleType`, padrão `TimeSpan.FromDays(1)`) e armazene os preços de abertura e fechamento do último dia concluído.
2. Assine velas intradiárias (`IntradayCandleType`, padrão `TimeSpan.FromMinutes(1)`) para impulsionar a execução.
3. Durante as primeiras horas da sessão (embora o horário de abertura da vela seja estritamente inferior a `EntryHourLimit`, padrão `1`):
   - Se o fechamento diário anterior estiver acima da abertura diária anterior, insira uma posição longa usando `BuyMarket(TradeVolume)`.
   - Se o fechamento diário anterior estiver abaixo da abertura diária anterior, insira uma posição curta usando `SellMarket(TradeVolume)`.
   - Se a vela diária fechar plana (abertura é igual a fechamento), nenhuma negociação será aberta.
4. Mantenha a posição durante o dia. Quando a hora da vela intradiária for maior ou igual a `MarketCloseHour` (padrão `20`), feche qualquer exposição aberta com uma ordem de mercado (`SellMarket` para posições longas, `BuyMarket` para posições curtas).
5. A estratégia só abre uma nova posição quando não existe posição ativa, garantindo no máximo uma negociação por dia.

## Parâmetros
| Parâmetro | Descrição | Padrão |
|-----------|-------------|---------|
| `TradeVolume` | Volume de pedidos para entradas longas e curtas. Deve ser positivo. | `1` |
| `EntryHourLimit` | Última hora (exclusiva) em que uma nova negociação pode ser iniciada. Valores fora de `[0, 23]` são fixados por meio de validação. | `1` |
| `MarketCloseHour` | Hora em que a estratégia fecha à força qualquer posição aberta. Aplica-se diariamente. | `20` |
| `IntradayCandleType` | Prazo usado para lógica de execução de negociação e gerenciamento de posição. | `TimeSpan.FromMinutes(1).TimeFrame()` |
| `DailyCandleType` | Período usado para ler os preços de abertura e fechamento do dia anterior. | `TimeSpan.FromMinutes(5).TimeFrame()` |

Todos os parâmetros são registrados por meio de `Param()` e podem ser otimizados no otimizador StockSharp.

## Gestão de risco
- A estratégia não utiliza níveis de stop-loss ou take-profit; o risco é controlado pela saída diária em `MarketCloseHour`.
- `StartProtection()` é ativado no início para proteger contra posições não planas inesperadas durante a negociação.
- Como apenas uma posição pode estar ativa por dia, a exposição máxima é definida por `TradeVolume`.

## Notas de uso
- Execute a estratégia em instrumentos que fornecem históricos de velas intradiários e diários. A configuração padrão requer velas diárias e de minuto.
- Alinhe `EntryHourLimit` e `MarketCloseHour` com a sessão de negociação do instrumento selecionado.
- O algoritmo espera o horário local da troca nos carimbos de data e hora da vela; ajuste as fontes de dados adequadamente.
- A lógica reflete o consultor especialista MQL original, permitindo que o comportamento seja replicado dentro do ambiente StockSharp sem componentes Python.
