# Estratégia MTF suavizada de Heiken Ashi
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia Heiken Ashi Smoothed MTF é uma versão do consultor especialista "HASNEWJ" MetaTrader. Ele reconstrói o indicador Heiken Ashi suavizado personalizado em seis intervalos de tempo (M1, M5, M15, M30, H1, H4) e aguarda o alinhamento da tendência nos quadros mais altos. Uma negociação é aberta quando o fluxo inferior do M5 mostra uma nova retração, enquanto as velas suavizadas de longo prazo permanecem fortemente de alta ou de baixa. A lógica manual de stop-loss e take-profit replica o comportamento do EA original, incluindo a capacidade de ampliar ligeiramente o stop após uma negociação perdedora.

## Indicadores e Dados
- **Velas Heiken Ashi suavizadas** em M1, M5, M15, M30, H1 e H4.
  - A primeira passagem de suavização aplica um método/comprimento de média móvel configurável aos valores OHLC brutos.
  - A segunda passagem suaviza a abertura/fechamento provisório do Heiken Ashi com outra média móvel configurável.
- **Contadores direcionais** que rastreiam quantas atualizações de um minuto cada período permaneceu de alta ou de baixa.
- **Preço bruto de fechamento** da série M1 para verificações de gerenciamento de risco.

## Lógica de entrada
1. Atualize a direção suavizada de Heiken Ashi para cada período de tempo sempre que uma vela termina.
2. Em cada vela M1 finalizada, aumente ou redefina os contadores de alta/baixa, dependendo da direção mais recente de cada período de tempo.
3. **Condições de compra:**
   - M5 suavizado Heiken Ashi é otimista e o contador de alta está abaixo de `MaxM5TrendLength` (padrão 10 atualizações).
   - M15 suavizado Heiken Ashi é otimista e seu contador de alta está acima de `MinM15TrendLength` (padrão 200 atualizações).
   - As velas Heiken Ashi suavizadas M30, H1 e H4 também são otimistas.
   - Nenhuma posição longa está aberta no momento (a exposição curta é permitida e será invertida).
4. **Condições de venda:**
   - M5 suavizado Heiken Ashi está em baixa e o contador de baixa está abaixo de `MaxM5TrendLength`.
   - M15 suavizado Heiken Ashi está em baixa e seu contador de baixa está acima de `MinM15TrendLength`.
   - As velas suavizadas M30, H1 e H4 são de baixa.
   - Nenhuma posição curta está aberta no momento (a exposição longa foi fechada ou revertida).
5. O volume da ordem de mercado é igual a `TradeVolume` mais o valor absoluto da exposição oposta para garantir que os flips fechem a negociação anterior.

## Gestão de risco
- Um stop-loss e um take-profit manuais são avaliados em cada vela M1 finalizada usando `Security.PriceStep`.
- O take-profit fecha a posição assim que o preço se move `TakeProfitPoints` passos a favor da negociação.
- O stop-loss fecha a posição assim que o preço se move `StopLossPoints` passos contra a negociação.
- Após uma negociação perdedora, a próxima entrada amplia o stop-loss em `ExtraStopLossPoints` passos, imitando o sinalizador de "falha" do EA.
- O volume de negociação é fixado em `TradeVolume`; nenhuma lógica de pirâmide ou escala é aplicada além de reverter a exposição existente.

## Parâmetros
| Nome | Descrição | Padrão |
| ---- | ----------- | ------- |
| `TradeVolume` | Volume base do pedido usado para entradas | `0.1` |
| `TakeProfitPoints` | Distância de lucro em etapas de preço | `20` |
| `StopLossPoints` | Distância stop-loss em etapas de preço | `500` |
| `ExtraStopLossPoints` | Etapas de parada adicionais aplicadas após uma negociação perdida | `5` |
| `FirstMaPeriod` | Comprimento da primeira média móvel de suavização | `6` |
| `FirstMaMethod` | Método do primeiro MA de suavização (`Simple`, `Exponential`, `Smoothed`, `LinearWeighted`) | `Smoothed` |
| `SecondMaPeriod` | Comprimento da segunda média móvel de suavização | `2` |
| `SecondMaMethod` | Método da segunda suavização MA | `LinearWeighted` |
| `MaxM5TrendLength` | Número máximo de atualizações M5 permitidas antes de cancelar uma entrada de pullback | `10` |
| `MinM15TrendLength` | Número mínimo de atualizações M15 necessárias para confirmar a tendência de alta | `200` |
| `M1CandleType` | Tipo de dados para o fluxo base de velas de um minuto | `TimeFrame(00:01:00)` |
| `M5CandleType` | Tipo de dados para o fluxo de confirmação de cinco minutos | `TimeFrame(00:05:00)` |
| `M15CandleType` | Tipo de dados para o fluxo de confirmação de quinze minutos | `TimeFrame(00:15:00)` |
| `M30CandleType` | Tipo de dados para o fluxo de confirmação de trinta minutos | `TimeFrame(00:30:00)` |
| `H1CandleType` | Tipo de dados para o fluxo de confirmação por hora | `TimeFrame(01:00:00)` |
| `H4CandleType` | Tipo de dados para o fluxo de confirmação de quatro horas | `TimeFrame(04:00:00)` |

## Notas de uso
- Os contadores direcionais são atualizados uma vez por vela M1 concluída, o que se aproxima dos contadores baseados em ticks de MetaTrader enquanto mantém a implementação orientada por velas.
- Certifique-se de que `Security.PriceStep` esteja configurado; caso contrário, a estratégia volta para um passo de 0,0001 ao calcular os níveis de parada e meta.
- Ambas as passagens de suavização dependem de médias móveis; experimentar diferentes combinações de métodos e períodos pode adaptar o sistema a instrumentos com diferentes perfis de volatilidade.
