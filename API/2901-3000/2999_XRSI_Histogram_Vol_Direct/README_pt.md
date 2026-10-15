# Estratégia XRSI Histograma Vol Direto
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral
- **Fonte original**: `Exp_XRSI_Histogram_Vol_Direct.mq5`
- **Plataforma convertida**: API de estratégia de alto nível StockSharp C#
- **Ideia**: negociar reversões quando o histograma RSI suavizado ponderado por volume muda de inclinação
- **Dados**: instrumento único, período único (padrão H4)

A estratégia avalia um oscilador personalizado construído a partir de valores RSI multiplicados por volume. Quando a inclinação deste oscilador suavizado muda, a estratégia reverte uma posição ou abre um novo trade na direção oposta. A lógica replica a abordagem de buffer de cores do expert advisor original rastreando a direção da inclinação das últimas duas velas concluídas.

## Pilha de indicadores e cálculos
1. **RSI** (`RsiPeriod`) é calculado na série de velas selecionada e centralizado em torno de zero subtraindo 50.
2. **Seleção de volume** usa contagem de ticks ou volume negociado, controlado pelo parâmetro `Use Tick Volume`.
3. **Oscilador ponderado por volume** multiplica o RSI centralizado pelo volume escolhido, magnificando os movimentos que coincidem com maior atividade.
4. **Suavização** aplica a média móvel selecionada (`SMA`, `EMA`, `SMMA`, `WMA`) com período `SmoothLength` tanto ao oscilador quanto ao fluxo de volume bruto. O indicador é considerado pronto apenas depois que ambos os valores suavizados estiverem formados.
5. **Detecção de inclinação** compara o valor atual do oscilador suavizado com o anterior:
   - Valor mais alto → cor de inclinação `0` (subindo)
   - Valor mais baixo → cor de inclinação `1` (caindo)
   - Plano → manter a cor anterior

## Parâmetros
| Nome | Padrão | Descrição |
| --- | --- | --- |
| Candle Type | Período H4 | Assinatura de vela alvo. |
| RSI Period | 14 | Período de retrospecto para o cálculo RSI. |
| Smoothing Length | 12 | Período da média móvel aplicada tanto ao oscilador quanto ao volume. |
| Smoothing Method | SMA | Tipo de média móvel (`SMA`, `EMA`, `SMMA`, `WMA`). |
| Use Tick Volume | `true` | Usar contagem de ticks (`true`) ou volume negociado (`false`). |
| Allow Buy Open | `true` | Habilitar abertura de posições compradas. |
| Allow Sell Open | `true` | Habilitar abertura de posições vendidas. |
| Allow Buy Close | `true` | Permitir fechar posições compradas em sinal oposto. |
| Allow Sell Close | `true` | Permitir fechar posições vendidas em sinal oposto. |

> **Nota**: Ao contrário do indicador MQL original, suavizadores avançados como JJMA ou VIDYA não estão disponíveis no framework StockSharp. A estratégia, portanto, expõe as alternativas integradas mais próximas.

## Regras de negociação
1. Aguardar até que ambos os indicadores de suavização tenham dados suficientes.
2. Determinar a cor de inclinação das últimas duas velas concluídas.
3. **Se a cor mais antiga estiver subindo (`0`)**:
   - Fechar qualquer posição vendida aberta se permitido.
   - Se a cor mais recente estiver caindo (`1`) e entradas compradas forem permitidas, abrir uma posição comprada (reflete a lógica de reversão do EA).
4. **Se a cor mais antiga estiver caindo (`1`)**:
   - Fechar qualquer posição comprada aberta se permitido.
   - Se a cor mais recente estiver subindo (`0`) e entradas vendidas forem permitidas, abrir uma posição vendida.

A estratégia efetivamente negocia o "flip de cor" da inclinação do histograma, executando no fechamento da vela mais nova terminada.

## Dicas práticas
- A lógica é sensível ao período escolhido. Teste vários intervalos para corresponder ao comportamento do EA original.
- Como apenas a direção da inclinação é usada, adicionar um stop loss ou take profit via `StartProtection` pode melhorar o controle de risco no trading ao vivo.
- Use a visualização de gráficos no terminal para comparar a inclinação do oscilador StockSharp com o indicador MT5 original ao validar o port.

## Diferenças da versão MQL
- Os auxiliares de gerenciamento de dinheiro (`TradeAlgorithms.mqh`) não são portados; a implementação StockSharp depende do volume da estratégia base.
- Apenas os métodos de suavização suportados pelo StockSharp são expostos. Modos não suportados se comportam como SMA.
- Ordens são enviadas imediatamente na vela terminada, portanto o deslocamento de tempo explícito (`SignalBar` / `TimeShiftSec`) não é necessário.
- Stops protetores não estão codificados de forma rígida; os usuários podem adicioná-los via `StartProtection` se necessário.

## Limitações
- Requer uma fonte de velas que forneça contagens de ticks ou totais de volume para reproduzir a amplitude do oscilador corretamente.
- A estratégia não desenha o histograma personalizado em si; foca na lógica de negociação e sobreposições de gráfico opcionais para RSI.
