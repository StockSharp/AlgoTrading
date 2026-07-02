# Estratégia de Gann Grid
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia porta o consultor especialista original **Gann Grid** de `MQL/25065/Gann Grid.mq4` para a API de alto nível do StockSharp. O script original misturava objetos de gráfico manuais com filtros de múltiplos períodos; a versão em C# mantém o fluxo de trabalho geral enquanto substitui os dados derivados de gráficos por lógica orientada por indicadores que pode ser executada de forma autônoma.

## Lógica de trading

1. **Grid de Gann sintético** – A máxima mais alta e a mínima mais baixa ao longo de `AnchorPeriod` velas aproximam os níveis de preço que eram desenhados manualmente no MetaTrader. Um rompimento acima da máxima aciona configurações compradas, um rompimento abaixo da mínima aciona vendidas.
2. **Confirmação de tendência** – Médias móvias ponderadas lineares rápida e lenta no período superior (`TrendCandleType`) devem concordar com a direção do rompimento.
3. **Filtro de Momentum** – A distância percentual entre o indicador de Momentum e o preço atual (também no período superior) precisa exceder `MomentumThreshold` para garantir aceleração suficiente.
4. **Confirmação MACD** – Um fluxo de velas separado (`MacdCandleType`) alimenta um MACD (12/26/9 por padrão). A linha MACD deve estar no mesmo lado de zero e da linha de sinal que a direção do trade.
5. **Gestão de risco** – Offsets simétricos de stop-loss e take-profit são aplicados a partir do preço de entrada. Módulos opcionais de break-even e trailing reproduzem os blocos de proteção de capital da implementação MQL.

Apenas velas concluídas são processadas para corresponder às verificações originais de "nova barra".

## Diferenças em relação à versão MQL

- O código MetaTrader esperava um objeto `GANNGRID` desenhado manualmente. O port o substitui por indicadores de máxima/mínima contínua, tornando a lógica determinística para testes automatizados.
- O Momentum no MetaTrader é centrado em torno de 100. O `Momentum` do StockSharp produz uma diferença de preço, portanto a estratégia o converte em porcentagem do fechamento atual antes de comparar com `MomentumThreshold`.
- Notificações (e-mail, push) e operações gráficas do script MQL são omitidas.
- A gestão de risco usa saídas de mercado em vez de modificar ordens existentes, pois as estratégias StockSharp gerenciam posições em vez de ordens no nível do terminal.

## Parâmetros

| Nome | Tipo | Padrão | Descrição |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | Período de 5 minutos | Velas primárias que definem os rompimentos. |
| `TrendCandleType` | `DataType` | Período de 15 minutos | Período superior usado para LWMA e filtros de Momentum. |
| `MacdCandleType` | `DataType` | Período de 1 dia | Fluxo de velas que alimenta o filtro de confirmação MACD. |
| `FastMaPeriod` | `int` | 6 | Comprimento LWMA rápida no período superior. |
| `SlowMaPeriod` | `int` | 85 | Comprimento LWMA lenta no período superior. |
| `MomentumPeriod` | `int` | 14 | Comprimento de retrocesso do Momentum. |
| `MomentumThreshold` | `decimal` | 0.3 | Desvio mínimo de Momentum em percentual necessário para operar. |
| `AnchorPeriod` | `int` | 100 | Número de velas primárias que formam o grid de Gann sintético. |
| `TakeProfitOffset` | `decimal` | 0.005 | Distância absoluta de take-profit a partir do preço de entrada. |
| `StopLossOffset` | `decimal` | 0.002 | Distância absoluta de stop-loss a partir do preço de entrada. |
| `EnableTrailing` | `bool` | `true` | Habilita o gerenciamento do trailing stop. |
| `TrailingActivation` | `decimal` | 0.003 | Lucro necessário antes do trailing stop começar a seguir o preço. |
| `TrailingStep` | `decimal` | 0.0015 | Distância entre a máxima local e o trailing stop. |
| `EnableBreakEven` | `bool` | `true` | Ativa a lógica de mover para break-even. |
| `BreakEvenTrigger` | `decimal` | 0.0025 | Lucro necessário antes do break-even ser ativado. |
| `BreakEvenOffset` | `decimal` | 0.0 | Offset aplicado ao preço de entrada ao fechar no break-even. |
| `MacdFastPeriod` | `int` | 12 | Comprimento EMA rápida dentro do MACD. |
| `MacdSlowPeriod` | `int` | 26 | Comprimento EMA lenta dentro do MACD. |
| `MacdSignalPeriod` | `int` | 9 | Comprimento EMA de sinal dentro do MACD. |

Todos os offsets são distâncias de preço absolutas. Ajuste-os para corresponder ao tamanho de tick do símbolo (por exemplo, 0.001 ≈ 10 pontos em uma cotação FX de 5 dígitos).

## Como usar

1. Anexe a estratégia a um ativo e configure os tipos de velas. É possível usar o mesmo tipo de vela para múltiplos filtros se um único período for desejado.
2. Ajuste `AnchorPeriod` e os offsets de preço para corresponder à volatilidade do instrumento.
3. Habilite ou desabilite break-even/trailing de acordo com sua política de risco.
4. Inicie a estratégia; ela assina automaticamente os fluxos de velas necessários e gerencia posições com ordens de mercado.

## Arquivos

- `CS/GannGridStrategy.cs` – implementação da estratégia.
- `README.md` – esta documentação.
- `README_ru.md` – descrição em russo.
- `README_zh.md` – descrição em chinês.
