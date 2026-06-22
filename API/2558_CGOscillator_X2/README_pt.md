# Estratégia CGOscillator X2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **Estratégia CGOscillator X2** é um sistema de seguidor de tendência multi-período que usa o oscilador Center of Gravity para operar recuos. A estratégia avalia a inclinação do oscilador em um período superior para determinar a tendência dominante e aguarda um gancho corretivo em um período inferior antes de entrar em uma operação na direção da tendência. Distâncias opcionais de stop-loss e take-profit expressas em unidades de preço absoluto podem ser usadas para gerenciar o risco após uma entrada ser aberta.

## Lógica de trading

1. **Detecção de tendência (período superior)**
   - O oscilador CG (Center of Gravity) é calculado no período de tendência usando o `TrendLength` configurado.
   - Se o valor atual do CG estiver acima de seu sinal (valor anterior), a estratégia considera o mercado altista; se estiver abaixo do sinal, o mercado é considerado baixista.
2. **Geração de sinais (período inferior)**
   - Uma segunda instância do oscilador CG com seu próprio comprimento funciona no período de sinal.
   - A estratégia monitora as duas velas finalizadas mais recentes. Um gancho altista (CG atual >= sinal enquanto CG anterior < sinal anterior) indica que um recuo terminou dentro de uma tendência de baixa. Um gancho baixista (CG atual <= sinal enquanto CG anterior > sinal anterior) destaca um recuo dentro de uma tendência de alta.
3. **Entradas e saídas**
   - Entradas compradas só são permitidas quando o período superior mostra uma tendência de alta e o último swing do período inferior indica um gancho baixista (recuo sobrevendido). Vendidos seguem a lógica espelhada para tendências de baixa.
   - As posições podem ser fechadas quando a tendência do período superior gira ou quando o último gancho vai contra a posição aberta, dependendo dos parâmetros booleanos.
4. **Controles de risco**
   - Distâncias opcionais absolutas de stop-loss e take-profit são aplicadas após cada entrada a mercado. Quando o preço cruza esses níveis dentro da vela atual, a posição é fechada imediatamente antes de novos sinais serem processados.

## Parâmetros

| Nome | Descrição |
| ---- | ----------- |
| `TrendCandleType` | Tipo de vela (período) usado para o oscilador CG de período superior. |
| `SignalCandleType` | Tipo de vela usado para o oscilador de sinal de período inferior. |
| `TrendLength` | Comprimento do oscilador CG no período de tendência. |
| `SignalLength` | Comprimento do oscilador CG no período de sinal. |
| `BuyOpen` | Habilita ou desabilita entradas compradas alinhadas com a tendência do período superior. |
| `SellOpen` | Habilita ou desabilita entradas vendidas alinhadas com a tendência do período superior. |
| `BuyClose` | Fecha posições compradas quando a tendência do período superior se torna baixista. |
| `SellClose` | Fecha posições vendidas quando a tendência do período superior se torna altista. |
| `BuyCloseSignal` | Fecha posições compradas quando o último gancho do período inferior é baixista. |
| `SellCloseSignal` | Fecha posições vendidas quando o último gancho do período inferior é altista. |
| `StopLoss` | Distância de preço absoluta para o stop protetor (0 desabilita o stop). |
| `TakeProfit` | Distância de preço absoluta para o alvo de lucro (0 desabilita o alvo). |

## Detalhes do indicador

O **CenterOfGravityOscillatorIndicator** personalizado replica o Oscilador CG do MT5:
- O preço mediano `(máximo + mínimo) / 2` é usado como entrada.
- Uma soma ponderada dos últimos `Length` medianos forma o valor CG.
- A linha de sinal é simplesmente o valor CG anterior, proporcionando um lag de uma barra para detecção de ganchos.

## Notas de uso

- Definir a propriedade `Volume` da estratégia para controlar o tamanho base da ordem. As reversões adicionam automaticamente o valor absoluto da posição atual para que a nova posição seja aberta na direção desejada.
- Como a estratégia trabalha apenas com velas finalizadas, é resistente ao ruído intrabar mas reage ao fechamento de cada vela.
- Os parâmetros de stop-loss e take-profit usam unidades de preço absoluto; ajustá-los ao tamanho do tick e ao perfil de volatilidade do instrumento.
- A estratégia pode ser anexada a qualquer instrumento suportado pelo StockSharp uma vez que os tipos de velas apropriados sejam configurados.
