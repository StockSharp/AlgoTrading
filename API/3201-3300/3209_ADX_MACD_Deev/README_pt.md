# Estratégia de ADX MACD Deev
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia de ADX MACD Deev** é um port StockSharp do consultor especialista MetaTrader com o mesmo nome. Combina o sinal de força de tendência do Índice Direcional Médio (ADX) com a confirmação de momentum da Convergência/Divergência de Médias Móveis (MACD). A estratégia opera apenas quando ambos os indicadores concordam na direção do mercado e pode opcionalmente assegurar lucros através de trailing stops e saídas parciais de posição.

## Como funciona
1. **Preparação de indicadores**
   - ADX é calculado com um período de médio configurável. A estratégia rastreia os valores mais recentes de ADX e requer que se movam consistentemente em uma direção antes de permitir uma negociação.
   - MACD usa EMAs rápidas, lentas e de sinal exponenciais configuráveis. O histograma e a linha de sinal devem mostrar conjuntamente um crescimento sustentado para comprados ou um declínio sustentado para vendidos.
2. **Lógica de entrada**
   - **Entradas compradas**: acionadas quando o histograma MACD excede o limiar `MACD Minimum (pips)`, tanto o histograma MACD quanto a linha de sinal aumentam pelo número selecionado de barras, e ADX permanece acima da força necessária enquanto também sobe.
   - **Entradas vendidas**: acionadas quando o histograma MACD está abaixo do limiar negativo, tanto o histograma MACD quanto a linha de sinal declinam sobre o intervalo selecionado, e ADX permanece acima do mínimo enquanto decresce.
   - Apenas uma posição pode estar aberta por vez.
3. **Gestão de risco**
   - Os níveis iniciais de stop-loss e take-profit são colocados em unidades de preço derivadas do instrumento `PriceStep` e das distâncias de pip escolhidas.
   - Um trailing stop pode seguir posições rentáveis assim que o preço avança `Trailing Stop + Trailing Step` pips.
   - Quando `Take Half Profit` está habilitado, a estratégia fecha metade da posição atual no nível de take-profit e deixa o restante correr com o trailing stop.

## Parâmetros
| Grupo | Nome | Descrição |
| --- | --- | --- |
| Trading | Order Volume | Volume de cada nova ordem de mercado. |
| Risco | Stop Loss (pips) | Deslocamento inicial do stop-loss desde a entrada. |
| Risco | Take Profit (pips) | Deslocamento inicial do take-profit desde a entrada. |
| Risco | Trailing Stop (pips) | Distância do trailing stop. Definir como zero para desabilitar o trailing. |
| Risco | Trailing Step (pips) | Movimento de preço adicional antes do trailing stop se mover novamente. |
| Risco | Take Half Profit | Habilita saída parcial quando o nível de take-profit é atingido. |
| Indicadores | ADX Period | Período de médio do ADX. |
| Indicadores | ADX Bars Interval | Número de barras ADX recentes que devem seguir uma tendência em uma direção. |
| Indicadores | ADX Minimum | Valor mínimo de ADX necessário para entradas. |
| Indicadores | MACD Fast EMA | Comprimento da EMA rápida usada pelo MACD. |
| Indicadores | MACD Slow EMA | Comprimento da EMA lenta usada pelo MACD. |
| Indicadores | MACD Signal EMA | Comprimento da EMA de sinal usada pelo MACD. |
| Indicadores | MACD Bars Interval | Número de barras MACD que devem se alinhar na mesma direção. |
| Indicadores | MACD Minimum (pips) | Magnitude mínima do MACD convertida para pips. |
| Geral | Candle Type | Tipo de vela ou período usado para cálculos. |

## Notas de uso
- A estratégia requer instrumentos com um `PriceStep` válido. Se `PriceStep` for zero, os limiares baseados em pips revertem para valores brutos de MACD.
- O arredondamento de volume para saídas parciais segue o `VolumeStep` do instrumento.
- Ajustes do trailing stop são avaliados apenas em velas fechadas.
- A estratégia usa bindings de API de alto nível (`SubscribeCandles().BindEx(...)`) e não depende de polling manual de valores de indicadores.
