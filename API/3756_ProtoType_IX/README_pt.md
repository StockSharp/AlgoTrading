# Estratégia do Protótipo IX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
ProtoType IX é uma estratégia de seguimento de tendências com vários filtros convertida do consultor especialista MetaTrader 4 original. O algoritmo observa oscilações de Williams %R para detectar novos movimentos impulsivos e os valida com a expansão Average True Range (ATR). As negociações são abertas apenas quando a relação recompensa/risco projetada é suficientemente atraente e o rompimento é confirmado.

## Indicadores e Sinais
- **Williams %R (período configurável)** – monitora rotações de sobrecompra/sobrevenda. A estratégia registra os dois máximos e mínimos de oscilação mais recentes que aparecem quando o indicador sai de suas zonas extremas.
- **Average True Range (ATR)** – mede a volatilidade atual. Os rompimentos são considerados válidos quando a distância entre o balanço mais recente e o anterior excede `ATR × multiplier`.

## Regras de entrada
1. Aguarde até que os máximos e mínimos recentes sejam registrados.
2. Determine a direção Williams %R. Se o indicador estiver acima do limite superior, o viés de alta será armazenado; se estiver abaixo do limite inferior, a tendência de baixa será armazenada.
3. Confirme a estrutura swing com ATR:
   - Tendência de alta – a última oscilação máxima deve exceder a oscilação máxima anterior em pelo menos `ATR × multiplier` e a última oscilação mínima deve ser maior que a oscilação mínima anterior.
   - Tendência de baixa – a última oscilação mínima deve cair abaixo da oscilação mínima anterior em pelo menos `ATR × multiplier` e a última oscilação máxima deve ser inferior à oscilação máxima anterior.
4. Avalie a relação recompensa/risco usando o preço de fechamento atual:
   - **Longo**: alvo = max(último balanço máximo, balanço anterior máximo); stop = max (última oscilação mínima, oscilação anterior mínima).
   - **Curto**: alvo = min(última oscilação mínima, oscilação anterior mínima); stop = min (última oscilação máxima, oscilação anterior máxima).
5. Abra uma posição apenas quando `take profit distance / stop loss distance ≥ TP/SL criteria` e a distância alvo for maior que o requisito mínimo de spread.

## Regras de saída
- As ordens de proteção iniciais são feitas imediatamente após a entrada. Os níveis de stop-loss e take-profit são convertidos em etapas de preço para usar StockSharp ordens de proteção.
- Depois que o atraso `Zero Bar` configurado expirar, o stop-loss é reforçado usando um modelo de rastreamento baseado em ATR:
  - As posições longas seguem o stop para `max(previous stop, close − 2 × ATR)`.
  - As posições curtas seguem o stop para `min(previous stop, close + 2 × ATR)`.

## Dimensionamento de posições
O tamanho do lote é estimado a partir do valor do portfólio e do parâmetro `Risk %`. A distância stop-loss nas etapas de preço é usada para traduzir o risco monetário permitido em volume. Os volumes são normalizados para a etapa de volume do instrumento e limitados por `Max Order Size`.

## Parâmetros
| Nome | Descrição |
| --- | --- |
| Williams %R Período | Comprimento do indicador Williams %R. |
| Critérios WPR | Limite absoluto que define zonas de sobrecompra/sobrevenda. |
| ATR Período | Comprimento do indicador Average True Range. |
| ATR Multiplicador | Multiplicador aplicado a ATR para validação de breakout. |
| Barra Zero | Número de barras antes de ativar o rastreamento ATR. |
| Spread alvo mínimo | Distância mínima aceitável do alvo expressa em múltiplos de dispersão. |
| Critérios TP/SL | Relação mínima de take-profit/stop-loss necessária para entrar em uma negociação. |
| Máximo de pedidos | Máximo de pedidos abertos simultaneamente. |
| Tamanho máximo do pedido | Limite superior para o volume do pedido após o dimensionamento. |
| % de risco | Porcentagem de risco utilizada para dimensionamento de posição. |
| Tipo de vela | Tipo de dados Candle para cálculos. |

## Notas
- A estratégia se concentra em uma única segurança, mas mantém a lógica multifiltro do EA original.
- As ordens de proteção dependem da variação do preço do instrumento; certifique-se de que os metadados do instrumento estejam configurados antes de executar a estratégia.
- Valores zero para etapa de volume ou preço de etapa são substituídos por padrões razoáveis para manter estável a rotina de dimensionamento.
