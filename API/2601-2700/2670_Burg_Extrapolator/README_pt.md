# Estratégia Burg Extrapolator
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral

A Estratégia Burg Extrapolator replica o assessor especialista MetaTrader "Burg Extrapolator" usando a API de alto nível do StockSharp. O sistema aplica um modelo autorregressivo (AR) resolvido com o método Burg para prever futuros preços de abertura. As decisões de trading são impulsionadas pela amplitude da trajetória da previsão: quando a distância prevista entre futuros máximos e mínimos excede os limites configurados, a estratégia abre ou fecha posições.

## Lógica Principal

1. **Preparação de dados**
   - Coleta `PastBars` preços de abertura em cada vela finalizada.
   - Opcionalmente transforma a série em valores de momentum logarítmico ou de taxa de variação.
   - Normaliza os preços subtraindo a média móvel quando preços brutos são usados.
2. **Modelagem autorregressiva**
   - Estima coeficientes AR pelo método Burg com uma ordem determinada por `ModelOrderFraction`.
   - Extrapola vários passos à frente (horizonte de previsão = `PastBars - order - 1`) e reconstrói as previsões de preço.
3. **Geração de sinais**
   - Rastreia os preços máximos e mínimos previstos.
   - Se o swing da previsão excede `MinProfitPips`, gera um sinal de entrada na direção respectiva.
   - Se o swing da previsão excede `MaxLossPips`, emite um sinal de saída para posições existentes.
4. **Execução de ordens**
   - Posições são abertas com ordens de mercado usando o volume calculado baseado em risco.
   - Quando um stop ou sinal oposto ocorre, a estratégia fecha posições com ordens de mercado.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-----------|
| `RiskPercent` | Percentagem do capital arriscado por operação. Usado para dimensionar ordens quando uma distância de stop-loss está disponível. |
| `MaxPositions` | Volume cumulativo máximo expresso como múltiplos do tamanho de ordem permitido por direção. |
| `MinProfitPips` | Swing de lucro previsto mínimo (em pips) necessário para abrir novas posições. |
| `MaxLossPips` | Drawdown previsto máximo permitido (em pips) que acionará saídas de posição. |
| `TakeProfitPips` | Distância de take-profit estático (em pips). Definir como zero para desativar. |
| `StopLossPips` | Distância de stop-loss estático (em pips). Necessário para dimensionamento de risco. |
| `TrailingStopPips` | Distância de trailing stop (em pips). Funciona apenas quando o stop-loss está habilitado. |
| `PastBars` | Número de barras históricas usadas como entrada no modelo Burg. |
| `ModelOrderFraction` | Fração de `PastBars` que define a ordem AR (truncamento inteiro). |
| `UseMomentum` | Habilita o pré-processamento de momentum logarítmico (`log(p[i]/p[i-1])`). |
| `UseRateOfChange` | Habilita o pré-processamento de taxa de variação (`p[i]/p[i-1]-1`) quando o momentum está desativado. |
| `OrderVolume` | Tamanho de ordem de reserva quando o dimensionamento baseado em risco não pode ser calculado. |
| `CandleType` | Tipo de dados (período) das velas usadas para cálculos. |

## Regras de Trading

- **Entrada**: Quando a trajetória prevista indica um swing maior que `MinProfitPips`, abrir uma posição comprada se o preço projetado mais alto aparece primeiro, ou abrir uma posição vendida se a projeção mais baixa aparece primeiro.
- **Saída**: Fechar posições quando o swing da previsão excede `MaxLossPips` ou quando o sinal de entrada oposto é detectado.
- **Proteção**: Usa `StartProtection` para configurar stop-loss, take-profit e trailing stop opcionais em unidades de preço absolutas derivadas de pips.
- **Dimensionamento de posição**: Se tanto `StopLossPips` quanto `RiskPercent` forem positivos, o volume da operação é calculado como `risk_amount / (stop_distance)`. Caso contrário, `OrderVolume` é usado.

## Notas de Implementação

- Trabalha exclusivamente com velas finalizadas para evitar viés de antecipação.
- Evita chamadas `GetValue` de indicadores processando valores diretamente dentro do callback `Bind`.
- Respeita as convenções da API de alto nível do StockSharp, usando `SubscribeCandles` e `StartProtection` para gestão de risco.
- A lógica de trailing reflete o EA original ao habilitar trailing stops gerenciados pela plataforma.

## Dicas de Uso

- Escolha `PastBars` e `ModelOrderFraction` cuidadosamente; ordens altas podem levar a sobreajuste ou previsões instáveis.
- O horizonte de previsão é igual a `PastBars - order - 1`; certifique-se de que o horizonte seja de pelo menos algumas barras mantendo `ModelOrderFraction` abaixo de 1.
- Os modos Momentum e ROC requerem preços positivos. Instrumentos que podem cruzar zero devem usar o modo de preço bruto.
- Para mercados com pips fracionados, a estratégia escala automaticamente o tamanho do pip usando as casas decimais do instrumento (×10 para 3 ou 5 decimais).

## Limitações

- O modelo AR assume estacionaridade; tendências fortes ou mudanças de regime podem reduzir a precisão.
- Os sinais baseados em previsão são sensíveis ao ruído — considere combinar com filtros adicionais se usar em trading ao vivo.
- O dimensionamento de risco preciso requer avaliação de portfólio e uma distância de stop-loss válida; caso contrário, volumes padrão são usados.
