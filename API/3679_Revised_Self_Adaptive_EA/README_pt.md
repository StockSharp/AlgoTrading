# Autoadaptável revisado EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Porta do MetaTrader 5 consultor especialista `revised_self_adaptive_ea.mq5` para a estrutura de estratégia de alto nível StockSharp.

## Visão geral da estratégia

O algoritmo verifica uma série de velas configuráveis e procura configurações de reversão envolventes confirmadas por filtros de impulso e tendência:

* **Detecção de padrão** – avalia a última vela fechada em relação à anterior. Uma configuração de alta requer um corpo verde que abre abaixo do fechamento anterior, enquanto a vela anterior é de baixa. A lógica de espelho é aplicada para configurações de baixa. Os corpos das velas são comparados com uma média móvel para filtrar sinais fracos.
* **Filtro de impulso** – um RSI clássico garante que as negociações de alta sejam desencadeadas apenas em território de sobrevenda e as negociações de baixa em condições de sobrecompra.
* **Filtro de tendência** – uma média móvel simples curta deve concordar com a direção da negociação. Isso evita o desbotamento de tendências fortes sem confirmação.
* **Gerenciamento de risco** – os níveis de stop-loss e take-profit orientados por ATR são calculados para cada nova posição. Os trailing stops opcionais continuam seguindo movimentos lucrativos, sem nunca reduzir a proteção. As posições são fechadas à força quando o preço atinge os níveis de proteção.
* **Spread e proteção de risco** – as negociações são ignoradas sempre que o spread atual excede o limite configurado ou quando o stop baseado em ATR arriscaria mais do que a porcentagem permitida do preço.

## Parâmetros

| Nome | Descrição |
| --- | --- |
| `CandleType` | Agregação de velas usada para análise. O padrão é barras de uma hora. |
| `AverageBodyPeriod` | Número de velas usadas para calcular o filtro de tamanho médio do corpo. |
| `MovingAveragePeriod` | Comprimento da média móvel simples que atua como filtro direcional. |
| `RsiPeriod` | Comprimento RSI usado para confirmação de sobrevenda/sobrecompra. |
| `OversoldLevel` | RSI limite que deve ser atingido antes de aceitar uma reversão de alta. |
| `OverboughtLevel` | RSI limite que deve ser atingido antes de aceitar uma reversão de baixa. |
| `AtrPeriod` | Comprimento ATR usado para distâncias de proteção baseadas em volatilidade. |
| `StopLossAtrMultiplier` | Fator multiplicativo aplicado a ATR para a distância de stop-loss. |
| `TakeProfitAtrMultiplier` | Fator multiplicativo aplicado a ATR para a distância de take-profit. |
| `TrailingStopAtrMultiplier` | ATR distância mantida pela lógica do trailing stop. |
| `UseTrailingStop` | Habilita o supervisor de trailing stop. |
| `MaxSpreadPoints` | Spread máximo permitido (expresso em etapas/pips de preço). Os sinais são ignorados quando o mercado é mais amplo. |
| `MaxRiskPercent` | Risco percentual máximo aceitável com base no stop ATR relativo ao preço de entrada. |
| `TradeVolume` | Tamanho base do lote utilizado para ordens de mercado. |

## Notas de comportamento

* As posições são achatadas antes de reverter a direção para espelhar a implementação MetaTrader.
* Os níveis de parada/tomada de proteção são recalculados após cada abastecimento usando a leitura ATR mais recente.
* O trailing stop apenas se move na direção comercial e é desativado quando os dados de ATR ainda não estão disponíveis.
* Se a estratégia estiver sendo executada em um instrumento sem cotações de compra/venda confiáveis, o filtro de spread permanecerá inativo automaticamente.

## Diferenças vs. original MQL

O script original delineava apenas a rotina de detecção de sinal. Nesta porta os elementos faltantes foram reconstruídos usando os parâmetros fornecidos:

* Adicionada confirmação de média móvel para usar o identificador MA declarado na fonte MQL.
* Implementada lógica de stop-loss, take-profit e trailing stop baseada em ATR usando o identificador de volatilidade definido no especialista original.
* Adicionada uma proteção de porcentagem de risco para que paradas ATR superdimensionadas sejam ignoradas em vez de serem executadas cegamente.
* Os elementos de visualização (setas do gráfico) foram omitidos porque as estratégias StockSharp não desenham objetos nos gráficos por padrão.

## Uso

1. Anexe a estratégia a um portfólio e segurança dentro do Hydra ou de seu host StockSharp personalizado.
2. Certifique-se de que a assinatura da vela corresponda ao período pretendido (padrão: uma hora).
3. Ajuste os parâmetros de risco para refletir a volatilidade do instrumento.
4. Comece a estratégia. Ele assinará automaticamente velas, calculará indicadores e fará ordens de mercado quando as condições forem satisfeitas.
