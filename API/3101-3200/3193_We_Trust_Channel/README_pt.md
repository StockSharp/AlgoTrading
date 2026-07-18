# Estratégia WE TRUST Channel
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia WE TRUST Channel** é um port de alto nível do StockSharp do expert advisor do MetaTrader 5 "WE TRUST". O sistema negocia pullbacks em direção a uma média móvel ponderada linear que é cercada por bandas de desvio padrão. Quando o preço fecha fora das bandas, a estratégia antecipa reversão à média e abre uma posição de mercado de volta ao centro do canal. Reversão de sinais, fechamento opcional de trades opostos e parâmetros de gestão monetária baseados em pips espelham o expert original.

## Lógica de negociação
1. Assinar o tipo de vela configurado (velas horárias por padrão) e calcular dois indicadores na fonte de preço selecionada:
   - Uma média móvel ponderada linear (**LWMA**) com período e deslocamento configuráveis.
   - Um envelope de desvio padrão com seu próprio período e deslocamento.
2. Converter offsets baseados em pips em distâncias de preço absolutas usando o `PriceStep` do instrumento. Cotações de cinco e três dígitos multiplicam o passo por 10 para emular a definição de pip do MetaTrader.
3. Calcular os limites superior e inferior do canal: `LWMA ± StdDev ± ChannelIndentPips` (convertidos em unidades de preço).
4. Avaliar apenas velas concluídas. Quando o preço da vela escolhida fecha abaixo do canal inferior, a estratégia gera um sinal de **compra**. Quando fecha acima do canal superior, gera um sinal de **venda**.
5. Opcionalmente inverter os sinais quando **ReverseSignals** está habilitado. Opcionalmente zerar uma posição oposta antes de agir em um novo sinal quando **CloseOpposite** está habilitado.
6. Enviar ordens de mercado com o volume configurado quando a posição atual está flat ou alinhada com a direção do sinal.

## Gestão de risco
- **StopLossPips** e **TakeProfitPips** traduzem distâncias em pips para ordens protetoras absolutas via `StartProtection`. Definir como `0` para desabilitar o nível respectivo.
- **TrailingStopPips** e **TrailingStepPips** controlam um trailing stop baseado em pips que segue trades lucrativos. Ambos os parâmetros são convertidos em distâncias de preço usando a mesma lógica de tamanho de pip.
- Todas as saídas são realizadas com ordens de mercado para permanecer próximo à implementação MQL5.

## Parâmetros
| Parâmetro | Descrição | Padrão |
|-----------|-------------|---------|
| `OrderVolume` | Volume de trade enviado com cada ordem de mercado. | `0.1` |
| `StopLossPips` | Distância de stop-loss expressa em pips (0 desabilita o stop). | `40` |
| `TakeProfitPips` | Distância de take-profit expressa em pips (0 desabilita o alvo). | `60` |
| `TrailingStopPips` | Distância de trailing stop em pips. | `10` |
| `TrailingStepPips` | Passo de trailing em pips entre ajustes de stop. | `10` |
| `MaPeriod` | Período da média móvel ponderada linear. | `60` |
| `MaShift` | Número de barras que a média móvel é deslocada para frente. | `0` |
| `StdDevPeriod` | Período do cálculo de desvio padrão. | `50` |
| `StdDevShift` | Número de barras que o valor de desvio é deslocado. | `0` |
| `SignalBarOffset` | Número de barras concluídas atrás ao avaliar sinais. | `1` |
| `ChannelIndentPips` | Buffer adicional adicionado fora das bandas de desvio. | `1` |
| `ReverseSignals` | Inverter a lógica de compra/venda do rompimento do canal. | `false` |
| `CloseOpposite` | Fechar uma posição oposta antes de entrar em um novo trade. | `false` |
| `AppliedPrice` | Componente de preço da vela alimentado em ambos os indicadores. | `Weighted` |
| `CandleType` | Tipo de dados de vela solicitado ao conector. | `período de 1 hora` |

## Notas
- A estratégia depende de metadados válidos de `PriceStep`. Se a bolsa não os fornecer, o código recorre a `Security.Step` e finalmente a `1`.
- Apenas a implementação em C# está incluída neste diretório. O port Python é intencionalmente omitido conforme as instruções.
- A lógica processa apenas velas concluídas e não tenta acumular dados de barra parciais.
